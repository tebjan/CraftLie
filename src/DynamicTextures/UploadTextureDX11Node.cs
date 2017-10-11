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

    [PluginInfo(Name = "UploadTexture", Category = "DX11.Texture", Author = "vux, tonfilm")]
    public unsafe class UploadTextureDX11Node : IPluginEvaluate, IDX11ResourceHost, IDisposable, IPartImportsSatisfiedNotification
    {
        [Import()]
        protected ILogger logger;

        [Input("Data", DefaultValue = 0, AutoValidate = false)]
        protected ISpread<DynamicTextureDescription> FInData;

        [Input("Apply", IsBang = true, DefaultValue = 1)]
        protected ISpread<bool> FApply;

        [Config("Suppress Warning", DefaultValue = 0)]
        protected ISpread<bool> FSuppressWarning;

        [Output("Texture Out")]
        protected Pin<DX11Resource<DX11DynamicTexture2D>> FTextureOutput;

        [Output("Is Valid")]
        protected ISpread<bool> FValid;

        private bool FInvalidate;

        public void Evaluate(int SpreadMax)
        {
            if (this.FApply[0])
            {
                this.FInData.Sync();
                this.FInvalidate = true;
                this.FTextureOutput.Resize(FInData.SliceCount, () => new DX11Resource<DX11DynamicTexture2D>(), r => r?.Dispose());
                this.FValid.SliceCount = FInData.SliceCount;
            }        
        }

        public unsafe void Update(DX11RenderContext context)
        {           
            if (this.FTextureOutput.SliceCount == 0 || !this.FInvalidate)
            {
                return;
            }

            for (int i = 0; i < FTextureOutput.SliceCount; i++)
            {
                FValid[i] = false;
                SetupTexture(i, context, FTextureOutput[i], FInData[i]);
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

                int stride = GetPixelSizeInBytes(description.Format);
                int dataLength = desc.Width * desc.Height * stride;

                var t = texture[context];
                if (description.DataType == TextureDescriptionDataType.IntPtr)
                {
                    WriteToTexture(stride, dataLength, t, description.GetDataPointer());
                }
                else
                {
                    var pinnedArray = GCHandle.Alloc(description.GetDataArray(), GCHandleType.Pinned);
                    try
                    {
                        WriteToTexture(stride, dataLength, t, pinnedArray.AddrOfPinnedObject());
                    }
                    finally
                    {
                        pinnedArray.Free();
                    }

                }

                this.FInvalidate = false;

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

        private int GetPixelSizeInBytes(TextureDescriptionFormat format)
        {
            const int floatSizeInBytes = 4;
            const int shortSizeInBytes = 2;
            const int unormSizeInBytes = 1;
            switch (format)
            {
                case TextureDescriptionFormat.R32G32B32A32_Float:
                    return floatSizeInBytes * 4;

                case TextureDescriptionFormat.R8G8B8A8_UNorm:
                case TextureDescriptionFormat.B8G8R8A8_UNorm:
                    return unormSizeInBytes * 4;

                case TextureDescriptionFormat.R32_Float:
                    return floatSizeInBytes * 1;

                case TextureDescriptionFormat.R8_UNorm:
                    return unormSizeInBytes * 1;

               //no default case to get an error here when someone adds more formats
            }

            throw new NotImplementedException("Wrong texture format or texture format not implementd.");
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
