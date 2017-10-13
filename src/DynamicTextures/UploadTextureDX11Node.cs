using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;

using VVVV.DX11;
using SlimDXMatrix = SlimDX.Matrix;
using FeralTic.DX11.Resources;
using FeralTic.DX11;
using CraftLie;
using SharpDX;
using VVVV.Utils.VColor;
using VL.Lib.Collections;
using System.Diagnostics;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using System.Runtime.InteropServices;
using VVVV.Utils;
using System.IO;

namespace VVVV.DX11.Nodes
{

    [PluginInfo(Name = "UploadTexture", Category = "DX11.Texture", Help = "Copies the data from a VL DynamicTextureDescription to a texture", Author = "tonfilm")]
    public unsafe class UploadTextureDX11Node : IPluginEvaluate, IDX11ResourceHost, IDisposable, IPartImportsSatisfiedNotification
    {
        [Import()]
        protected ILogger logger;

        [Input("Data")]
        protected ISpread<DynamicTextureDescription> FDataIn;

        [Config("Suppress Warning", DefaultValue = 0)]
        protected ISpread<bool> FSuppressWarning;

        [Output("Texture Out")]
        protected Pin<DX11Resource<DX11Texture2D>> FTextureOutput;

        [Output("Is Valid")]
        protected ISpread<bool> FValid;

        public void Evaluate(int SpreadMax)
        {
            if (this.FDataIn.Any(d => d != null && d.Set))
            {
                this.FTextureOutput.Resize(FDataIn.SliceCount, () => new DX11Resource<DX11Texture2D>(), r => r?.Dispose());
                this.FValid.SliceCount = FDataIn.SliceCount;
            }        
        }

        public unsafe void Update(DX11RenderContext context)
        {           
            for (int i = 0; i < FTextureOutput.SliceCount; i++)
            {
                if (FDataIn[i] != null && FDataIn[i].Set)
                {
                    FValid[i] = false;
                    SetupTexture(i, context, FTextureOutput[i], FDataIn[i]); 
                }
            }        
        }

        private void SetupTexture(int slice, DX11RenderContext context, DX11Resource<DX11Texture2D> texture, DynamicTextureDescription description)
        {
            try
            {
                if (description.Format == TextureDescriptionFormat.FromImage)
                {
                    TextureFromImageData(context, texture, description);
                }
                else
                {
                    TextureFromPixelData(context, texture, description);
                }
            }
            catch (Exception)
            {
                FValid[slice] = false;
            }
        }

        private static void TextureFromImageData(DX11RenderContext context, DX11Resource<DX11Texture2D> texture, DynamicTextureDescription description)
        {
            Texture2DDescription desc;

            if (texture.Contains(context))
            {
                var resource = texture[context];
                desc = resource.Description;

                if ((resource is DX11DynamicTexture2D) || desc.Width != description.Width || desc.Height != description.Height)
                {
                    texture.Dispose(context);
                    texture[context] = GetTextureFromImage(context, description);
                }
            }
            else
            {
                texture[context] = GetTextureFromImage(context, description);
            }
        }

        private static DX11Texture2D GetTextureFromImage(DX11RenderContext context, DynamicTextureDescription description)
        {
            switch (description.DataType)
            {
                case TextureDescriptionDataType.IntPtr:
                    throw new NotImplementedException("Cannot create texture from image data in IntPtr");
                case TextureDescriptionDataType.Array:
                case TextureDescriptionDataType.Spread:
                    var bytes = (byte[])description.GetDataArray();
                    return DX11Texture2D.FromMemory(context, bytes);
                case TextureDescriptionDataType.Stream:
                    var stream = description.GetDataStream();
                    stream.Position = 0;
                    return DX11Texture2D.FromStream(context, stream, (int)stream.Length);
            }

            throw new NotImplementedException("Could not create texture from image data");
        }

        private static void TextureFromPixelData(DX11RenderContext context, DX11Resource<DX11Texture2D> texture, DynamicTextureDescription description)
        {
            var fmt = (SlimDX.DXGI.Format)description.Format;
            Texture2DDescription desc;

            if (texture.Contains(context))
            {
                desc = texture[context].Resource.Description;

                if (desc.Width != description.Width || desc.Height != description.Height || desc.Format != fmt)
                {
                    texture.Dispose(context);
                    texture[context] = new DX11DynamicTexture2D(context, description.Width, description.Height, fmt);
                }
            }
            else
            {
                texture[context] = new DX11DynamicTexture2D(context, description.Width, description.Height, fmt);
            }

            desc = texture[context].Resource.Description;

            int pixelSizeInBytes = description.Format.GetPixelSizeInBytes();
            int dataLength = desc.Width * desc.Height * pixelSizeInBytes;

            var t = texture[context];

            switch (description.DataType)
            {
                case TextureDescriptionDataType.IntPtr:
                    WriteToTexture(pixelSizeInBytes, dataLength, context, t, description.GetDataPointer());
                    break;
                case TextureDescriptionDataType.Array:
                case TextureDescriptionDataType.Spread:
                    var pinnedArray = GCHandle.Alloc(description.GetDataArray(), GCHandleType.Pinned);
                    try
                    {
                        WriteToTexture(pixelSizeInBytes, dataLength, context, t, pinnedArray.AddrOfPinnedObject());
                    }
                    finally
                    {
                        pinnedArray.Free();
                    }
                    break;
                case TextureDescriptionDataType.Stream:
                    WriteToTextureStream(pixelSizeInBytes, dataLength, context, t, description.GetDataStream());
                    break;
            }
        }

        private static void WriteToTexture(int stride, int dataLength, DX11RenderContext context, DX11Texture2D t, IntPtr ptr)
        {
            var ctx = context.CurrentDeviceContext;
            var db = ctx.MapSubresource(t.Resource, 0, 0, MapMode.WriteDiscard, SlimDX.Direct3D11.MapFlags.None);

            try
            {
                if (db.RowPitch == stride)
                {
                    Memory.Copy(db.Data.DataPointer, ptr, (uint)dataLength);
                }
                else
                {
                    int dataScanSize = stride * t.Width;
                    var textureScanSize = db.RowPitch;

                    var sourcePointer = ptr;
                    var destPointer = db.Data.DataPointer;

                    //copy line by line
                    for (int i = 0; i < t.Height; i++)
                    {
                        Memory.Copy(destPointer.Move(textureScanSize * i), sourcePointer.Move(dataScanSize * i), (uint)dataScanSize);
                    }
                }

            }
            finally
            {
                ctx.UnmapSubresource(t.Resource, 0);
            }
        }

        private static void WriteToTextureStream(int stride, int dataLength, DX11RenderContext context, DX11Texture2D t, Stream stream)
        {
            stream.Position = 0;
            var ctx = context.CurrentDeviceContext;
            var db = ctx.MapSubresource(t.Resource, 0, 0, MapMode.WriteDiscard, SlimDX.Direct3D11.MapFlags.None);

            try
            {
                int dataScanSize = stride * t.Width;
                var textureScanSize = db.RowPitch;

                int pos = 0;
                var lineBuffer = new byte[dataScanSize];

                //line by line
                for (int i = 0; i < t.Height; i++)
                {
                    stream.Read(lineBuffer, 0, dataScanSize);
                    db.Data.Write(lineBuffer, 0, dataScanSize);

                    //advance destination one row pitch
                    pos += textureScanSize;
                    db.Data.Position = pos;
                }
            }
            finally
            {
                ctx.UnmapSubresource(t.Resource, 0);
            }
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
            this.FTextureOutput[0].Dispose(context);
        }


        #region IDisposable Members
        public void Dispose()
        {
            if (this.FTextureOutput.SliceCount > 0)
            {
                if (this.FTextureOutput[0] != null)
                {
                    this.FTextureOutput[0].Dispose();
                }
            }

        }

        public void OnImportsSatisfied()
        {
            this.FTextureOutput.Resize(0, () => new DX11Resource<DX11Texture2D>(), r => r?.Dispose());
        }
        #endregion
    }
}
