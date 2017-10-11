#region usings
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;
using VVVV.Utils;
using CraftLie;

#endregion usings

namespace VVVV.Nodes
{
    

    #region PluginInfo
    [PluginInfo(Name = "UploadTexture", Category = "EX9.Texture", Help = "Copies the data from a VL DynamicTextureDescription to a texture", Author = "tonfilm")]
    #endregion PluginInfo
    public class UploadTextureDX9Node : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        public class TextureInfo
        {
            public DynamicTextureDescription Description;

            public TextureInfo(DynamicTextureDescription desc)
            {
                Description = desc;
            }
        }

        [Input("Data")]
        public ISpread<DynamicTextureDescription> FDataIn;

        [Output("Texture Out")]
        public ISpread<TextureResource<TextureInfo>> FTextureOut;

        [Import]
        public ILogger FLogger;

        public void OnImportsSatisfied()
        {
            //spreads have a length of one by default, change it
            //to zero so ResizeAndDispose works properly.
            FTextureOut.SliceCount = 0;
        }

        //called when data for any output pin is requested
        public void Evaluate(int spreadMax)
        {
            FTextureOut.ResizeAndDispose(spreadMax, CreateTextureResource);
            for (int i = 0; i < spreadMax; i++)
            {
                var textureResource = FTextureOut[i];
                var info = textureResource.Metadata.Description;
                var desc = FDataIn[i];

                //recreate textures if resolution or format was changed
                if (info == null || desc == null || info.Width != desc.Width || info.Height != desc.Height || info.Format != desc.Format)
                {
                    textureResource?.Dispose();
                    textureResource = CreateTextureResource(i);
                }
                else
                {
                    textureResource.Metadata.Description = desc;
                    textureResource.NeedsUpdate = desc.Set;
                }

                FTextureOut[i] = textureResource;
            }
        }

        TextureResource<TextureInfo> CreateTextureResource(int slice)
        {
            var desciption = FDataIn[slice] ?? new DynamicTextureDescriptionArray<float>(new float[] { 1 }, 1, 1, TextureDescriptionFormat.R32_Float);
            return TextureResource.Create(new TextureInfo(desciption), CreateTexture, UpdateTexture);
        }

        //this method gets called, when Reinitialize() was called in evaluate,
        //or a graphics device asks for its data
        Texture CreateTexture(TextureInfo info, Device device)
        {
            var description = info.Description;
            var pool = Pool.Managed;
            var usage = Usage.None;

            if (device is DeviceEx)
            {
                pool = Pool.Default;
                usage = Usage.Dynamic;
            }

            return new Texture(device, Math.Max(description.Width, 1), Math.Max(description.Height, 1), 1, usage, GetDX9Format(description.Format), pool);
        }

        //this method gets called, when Update() was called in evaluate,
        //or a graphics device asks for its texture, here you fill the texture with the actual data
        //this is called for each renderer, careful here with multiscreen setups, in that case
        //calculate the pixels in evaluate and just copy the data to the device texture here
        unsafe void UpdateTexture(TextureInfo info, Texture texture)
        {
            var description = info.Description;
            var rect = texture.LockRectangle(0, LockFlags.None);
            var textureScanSize = rect.Pitch;

            int pixelSizeInBytes = description.Format.GetPixelSizeInBytes();
            int dataScanSize = description.Width * pixelSizeInBytes;
            int dataLength = dataScanSize * description.Height;

            try
            {
                if (description.DataType == TextureDescriptionDataType.IntPtr)
                {
                    var sourcePointer = description.GetDataPointer();
                    var destPointer = rect.Data.DataPointer;

                    if (textureScanSize == dataScanSize)
                    {
                        Memory.Copy(destPointer, sourcePointer, (uint)dataLength);
                    }
                    else
                    {
                        //copy line by line
                        for (int i = 0; i < description.Height; i++)
                        {
                            Memory.Copy(destPointer.Move(textureScanSize * i), sourcePointer.Move(dataScanSize * i), (uint)dataScanSize);
                        }
                    }

                }
                else
                {

                    var pinnedArray = GCHandle.Alloc(description.GetDataArray(), GCHandleType.Pinned);
                    var sourcePointer = pinnedArray.AddrOfPinnedObject();
                    var destPointer = rect.Data.DataPointer;

                    try
                    {
                        if (textureScanSize == dataScanSize)
                        {
                           Memory.Copy(destPointer, sourcePointer, (uint)dataLength);
                        }
                        else
                        {
                            //copy line by line
                            for (int i = 0; i < description.Height; i++)
                            {
                                Memory.Copy(destPointer.Move(textureScanSize * i), sourcePointer.Move(dataScanSize * i), (uint)dataScanSize);
                            }
                        }
                    }
                    finally
                    {
                        pinnedArray.Free();
                    } 
                }
            }
            finally
            {
                texture.UnlockRectangle(0);
            }        
        }

        private Format GetDX9Format(TextureDescriptionFormat format)
        {
            switch (format)
            {
                case TextureDescriptionFormat.R32G32B32A32_Float:
                    return Format.A32B32G32R32F;

                case TextureDescriptionFormat.R8G8B8A8_UNorm:
                    return Format.A8B8G8R8;

                case TextureDescriptionFormat.R32_Float:
                    return Format.R32F;

                case TextureDescriptionFormat.R8_UNorm:
                    return Format.L8;

                case TextureDescriptionFormat.B8G8R8A8_UNorm:
                    return Format.A8R8G8B8;
            }

            throw new NotImplementedException("Unknown DX9 texture format");
        }
    }
}
