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
        protected Pin<DX11Resource<DX11DynamicTexture2D>> FTextureOutput;

        [Output("Is Valid")]
        protected ISpread<bool> FValid;

        public void Evaluate(int SpreadMax)
        {
            if (this.FDataIn.Any(d => d != null && d.Set))
            {
                this.FTextureOutput.Resize(FDataIn.SliceCount, () => new DX11Resource<DX11DynamicTexture2D>(), r => r?.Dispose());
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

        private void SetupTexture(int slice, DX11RenderContext context, DX11Resource<DX11DynamicTexture2D> texture, DynamicTextureDescription description)
        {
            try
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
#if DEBUG
                    texture[context].Resource.DebugName = "DynamicTexture";
#endif
                }

                desc = texture[context].Resource.Description;

                int pixelSizeInBytes = description.Format.GetPixelSizeInBytes();
                int dataLength = desc.Width * desc.Height * pixelSizeInBytes;

                var t = texture[context];
                if (description.DataType == TextureDescriptionDataType.IntPtr)
                {
                    WriteToTexture(pixelSizeInBytes, dataLength, t, description.GetDataPointer());
                }
                else
                {
                    var pinnedArray = GCHandle.Alloc(description.GetDataArray(), GCHandleType.Pinned);
                    try
                    {
                        WriteToTexture(pixelSizeInBytes, dataLength, t, pinnedArray.AddrOfPinnedObject());
                    }
                    finally
                    {
                        pinnedArray.Free();
                    }

                }

            }
            catch (Exception)
            {
                FValid[slice] = false;
            }
        }

        private static void WriteToTexture(int stride, int dataLength, DX11DynamicTexture2D t, IntPtr ptr)
        {
            if (t.GetRowPitch() == t.Width * stride)
            {
                t.WriteData(ptr, dataLength);
            }
            else
            {
                t.WriteDataPitch(ptr, dataLength, stride);
            }
        }

        /// <summary>
        /// Copies to local buffer and increments the index. Tries to use Array.Copy();
        /// </summary>
        protected static void CopyToLocalBuffer<T>(VL.Lib.Collections.Spread<T> source, byte[] destination, int dataLength)
            where T : struct
        {
            var structSize = Marshal.SizeOf<T>();

            if (dataLength > structSize * source.Count)
                return;

            var data = source.GetInternalArray();
            System.Buffer.BlockCopy(data, 0, destination, 0, dataLength);
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
            this.FTextureOutput.Resize(0, () => new DX11Resource<DX11DynamicTexture2D>(), r => r?.Dispose());
        }
        #endregion
    }
}
