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
using System.Runtime.InteropServices;

namespace VVVV.DX11.Nodes
{
    [PluginInfo(Name = "UploadBuffer", Category = "DX11.Buffer", Version = "Raw", Author = "tonfilm")]
    public class UploadRawBufferNode : IPluginEvaluate, IDX11ResourceHost, IPartImportsSatisfiedNotification, IDisposable
    {
        [Import()]
        protected IPluginHost2 pluginHost;

        [Input("Buffer Description", Order = 5)]
        protected IDiffSpread<DynamicRawBufferDescription> FBufferDescriptionIn;

        [Input("Keep In Memory", DefaultValue = 0, Order = 6)]
        protected ISpread<bool> FKeep;

        [Input("Apply", IsBang = true, DefaultValue = 1, Order = 7)]
        protected ISpread<bool> FApply;

        //geometry

        [Output("Buffer")]
        protected ISpread<DX11Resource<DX11DynamicRawBuffer>> FBufferOutput;

        [Output("Is Valid")]
        protected ISpread<bool> FValid;

        DX11BufferUploadType currentBufferType = DX11BufferUploadType.Dynamic;
        private bool FInvalidate;
        private bool FFirst = true;
        private int FBufferInSpreadMax;

        protected virtual bool NeedConvert { get { return false; } }


        int FOldGeometryOutCount = 0;
        int FOldSpritesGeometryOutCount = 0;

        public void Evaluate(int SpreadMax)
        {
            //buffer outputs
            if (this.FApply[0] || this.FFirst)
            {
                if (this.FBufferDescriptionIn.SliceCount > 0)
                {
                    this.FBufferOutput.Resize(FBufferDescriptionIn.SliceCount, () => new DX11Resource<DX11DynamicRawBuffer>(), b => b?.Dispose());

                    this.FValid.SliceCount = FBufferDescriptionIn.SliceCount;
                }
                else //no output
                {
                    //geos

                    this.FBufferOutput.SafeDisposeAll();
                    this.FBufferOutput.SliceCount = 0;

                    this.FValid.SliceCount = 0;
                }

                this.FBufferInSpreadMax = this.FBufferDescriptionIn.SliceCount;
                this.FInvalidate = true;
                this.FFirst = false;

                //mark buffers changed
                this.FBufferOutput.Stream.IsChanged = true;
            }

        }

        public void Update(DX11RenderContext context)
        {
            if (this.FBufferOutput.SliceCount == 0 || !this.FInvalidate)
            {
                return;
            }

            for (int i = 0; i < FBufferOutput.SliceCount; i++)
            {
                FValid[i] = false;
                SetupBuffer(i, context, FBufferOutput[i], FBufferDescriptionIn[i]);
            }

            FInvalidate = false;
        }

        private void SetupBuffer(int slice, DX11RenderContext context, DX11Resource<DX11DynamicRawBuffer> buffer, DynamicRawBufferDescription description)
        {
            if (!buffer.Contains(context))
            {
                //refresh buffers?
                if (buffer.Contains(context))
                {
                    if (buffer[context].Size < description.DataSizeInBytes)
                    {
                        buffer.Dispose(context);
                    }
                }

                //make new buffers?
                if (!buffer.Contains(context))
                {
                    var count = NextUpperPow2((int)description.DataSizeInBytes);
                    buffer[context] = new DX11DynamicRawBuffer(context, count);
                }

            }

            this.FValid[slice] = true;

            //write to buffers
            var b = buffer[context];
            if (description.DataType == RawBufferDescriptionDataType.IntPtr)
            {
                b.WriteData(description.GetDataPointer(), (int)description.DataSizeInBytes);
            }
            else
            {
                var pinnedArray = GCHandle.Alloc(description.GetDataArray(), GCHandleType.Pinned);
                try
                {
                    b.WriteData(pinnedArray.AddrOfPinnedObject(), (int)description.DataSizeInBytes);
                }
                finally
                {
                    pinnedArray.Free();
                }
            }
        }

        int NextUpperPow2(int count)
        {
            var pow2 = 2;
            while (pow2 < count)
            {
                pow2 = pow2 << 1;
            }
            return pow2;
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
            if (force || !this.FKeep[0])
            {
                this.FBufferOutput.SafeDisposeAll(context);
            }
        }

        public void Dispose()
        {
            this.FBufferOutput.SafeDisposeAll();
        }

        public void OnImportsSatisfied()
        {
            this.FBufferOutput.Resize(0, () => new DX11Resource<DX11DynamicRawBuffer>(), b => b?.Dispose());
        }
    }
}
