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

namespace VVVV.DX11.Nodes
{
    [PluginInfo(Name = "UploadBuffer", Category = "DX11.Buffer", Version = "Int", Author = "tonfilm")]
    public class UploadIntBufferNode : UploadBufferNode<int> { }

    [PluginInfo(Name = "UploadBuffer", Category = "DX11.Buffer", Version = "UInt", Author = "tonfilm")]
    public class UploadUIntBufferNode : UploadBufferNode<uint> { }

    [PluginInfo(Name = "UploadBuffer", Category = "DX11.Buffer", Version = "Float", Author = "tonfilm")]
    public class UploadFloatBufferNode : UploadBufferNode<float> { }

    [PluginInfo(Name = "UploadBuffer", Category = "DX11.Buffer", Version = "2d", Author = "tonfilm")]
    public class UploadVector2BufferNode : UploadBufferNode<Vector2> { }

    [PluginInfo(Name = "UploadBuffer", Category = "DX11.Buffer", Version = "3d", Author = "tonfilm")]
    public class UploadVector3BufferNode : UploadBufferNode<Vector3> { }

    [PluginInfo(Name = "UploadBuffer", Category = "DX11.Buffer", Version = "4d", Author = "tonfilm")]
    public class UploadVector4BufferNode : UploadBufferNode<Vector4> { }

    [PluginInfo(Name = "UploadBuffer", Category = "DX11.Buffer", Version = "Color", Author = "tonfilm")]
    public class UploadColorBufferNode : UploadBufferNode<Color4> { }

    [PluginInfo(Name = "UploadBuffer", Category = "DX11.Buffer", Version = "Transform", Author = "tonfilm")]
    public class UploadMatrixBufferNode : UploadBufferNode<Matrix>
    {
        protected override void CopyData(IDiffSpread<DynamicBufferDescription<Matrix>> descriptions)
        {
            var writeIndex = 0;

            foreach (var desc in descriptions.Where(d => d != null))
            {
                foreach (var trans in desc.Data)
                {
                    trans.Transpose();
                    FLocalDataBuffer[writeIndex++] = trans;
                }
            }
        }
    }

    public class UploadBufferNode<TBuffer> : IPluginEvaluate, IDX11ResourceHost, IPartImportsSatisfiedNotification, IDisposable 
        where TBuffer : struct
    {
        [Import()]
        protected IPluginHost2 pluginHost;

        [Input("Buffer Description", Order = 5)]
        protected IDiffSpread<DynamicBufferDescription<TBuffer>> FBufferDescriptionIn;

        [Input("Keep In Memory", DefaultValue = 0, Order = 6)]
        protected ISpread<bool> FKeep;

        [Input("Preferred Buffer Type", DefaultValue = 0, Order = 6, Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<DX11BufferUploadType> FBufferType;

        //geometry

        [Output("Buffer")]
        protected ISpread<DX11Resource<IDX11ReadableStructureBuffer>> FBufferOutput;

        [Output("Is Valid")]
        protected ISpread<bool> FValid;

        DX11BufferUploadType currentBufferType = DX11BufferUploadType.Dynamic;

        public void Evaluate(int SpreadMax)
        {
            //buffer outputs
            if (this.FBufferDescriptionIn[0].Set)
            {
                if (this.FBufferDescriptionIn.SliceCount > 0)
                {
                    this.FBufferOutput.SliceCount = 1;

                    this.FValid.SliceCount = 1;

                    //create geometry buffer resources
                    if (this.FBufferOutput[0] == null)
                    {
                        this.FBufferOutput[0] = new DX11Resource<IDX11ReadableStructureBuffer>();
                    }
                }
                else //no output
                {
                    //geos

                    this.FBufferOutput.SafeDisposeAll();
                    this.FBufferOutput.SliceCount = 0;

                    this.FValid.SliceCount = 0;
                }

                //mark buffers changed
                this.FBufferOutput.Stream.IsChanged = true;

                //set all normal output pins and get the total counts
                UpdateNormalPins();
            }

        }

        protected int FTotalDataCount;

        protected TBuffer[] FLocalDataBuffer = new TBuffer[4096];

        private void UpdateNormalPins()
        {
            FTotalDataCount = 0;

            foreach (var desc in FBufferDescriptionIn)
            {
                FTotalDataCount += desc.Data.Count;
            }
        }


        public void Update(DX11RenderContext context)
        {
            if (this.FBufferOutput.SliceCount == 0) { return; }

            var newContext = !this.FBufferOutput[0].Contains(context);

            if (this.FBufferDescriptionIn[0].Set || newContext)
            {
                var bufferTypeChanged = this.currentBufferType != this.FBufferType[0];

                //refresh buffers?
                CheckBufferDispose<TBuffer>(context, this.FBufferOutput[0], FTotalDataCount, bufferTypeChanged);

                PrepareLocalBufferData(context);

                //make new buffers?
                CreateBuffer<TBuffer>(FBufferOutput[0], context, FTotalDataCount, FLocalDataBuffer);

            }

            this.FValid[0] = true;
            this.currentBufferType = this.FBufferType[0];

            //write to buffers
            bool needContextCopy = this.FBufferType[0] != DX11BufferUploadType.Immutable;
            if (needContextCopy)
            {
                try
                {
                    WriteToBuffer(FBufferOutput[0], context, FLocalDataBuffer, FTotalDataCount);
                }
                catch (Exception ex)
                {
                    this.pluginHost.Log(TLogType.Error, ex.Message);
                }
            }
        }

        private static void CheckBufferDispose<T>(DX11RenderContext context, DX11Resource<IDX11ReadableStructureBuffer> bufferResource, int bufferCount, bool bufferTypeChanged)
            where T : struct
        {
            if (bufferResource.Contains(context))
            {
                if (bufferResource[context].ElementCount < bufferCount
                    || bufferTypeChanged
                    || bufferResource[context] is DX11ImmutableStructuredBuffer<T>)
                {
                    bufferResource.Dispose(context);
                }
            }
        }

        /// <summary>
        /// Creates a buffer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bufferResource">The buffer resource</param>
        /// <param name="context">The DX11 context.</param>
        /// <param name="count">The required count. Gets blown up to the next power of 2.</param>
        /// <param name="bufferToCopy">The buffer to copy in case of immutable buffer type</param>
        private void CreateBuffer<T>(DX11Resource<IDX11ReadableStructureBuffer> bufferResource, DX11RenderContext context, int count, T[] bufferToCopy)
            where T : struct
        {
            if (!bufferResource.Contains(context))
            {
                count = NextUpperPow2(count);
                if (this.FBufferType[0] == DX11BufferUploadType.Dynamic)
                {
                    bufferResource[context] = new DX11DynamicStructuredBuffer<T>(context, count);
                }
                else if (this.FBufferType[0] == DX11BufferUploadType.Default)
                {
                    bufferResource[context] = new DX11CopyDestStructuredBuffer<T>(context, count);
                }
                else
                {
                    bufferResource[context] = new DX11ImmutableStructuredBuffer<T>(context.Device, bufferToCopy, count);
                }
            }
        }

        private void WriteToBuffer<T>(DX11Resource<IDX11ReadableStructureBuffer> bufferResource, DX11RenderContext context, T[] bufferToCopy, int elementCount)
            where T : struct
        {
            if (elementCount > 0)
            {
                if (this.FBufferType[0] == DX11BufferUploadType.Dynamic)
                {
                    DX11DynamicStructuredBuffer<T> b = (DX11DynamicStructuredBuffer<T>)bufferResource[context];
                    b.WriteData(bufferToCopy, 0, elementCount);
                }
                else if (this.FBufferType[0] == DX11BufferUploadType.Default)
                {
                    DX11CopyDestStructuredBuffer<T> b = (DX11CopyDestStructuredBuffer<T>)bufferResource[context];
                    b.WriteData(bufferToCopy, 0, elementCount);
                }
            }
        }

        private void PrepareLocalBufferData(DX11RenderContext context)
        {
            //make sure arrays are big enough
            EnsureArraySize(ref this.FLocalDataBuffer, FTotalDataCount);

            CopyData(FBufferDescriptionIn);         
        }

        protected virtual void CopyData(IDiffSpread<DynamicBufferDescription<TBuffer>> descriptions)
        {
            var writeIndex = 0;

            foreach (var desc in descriptions.Where(d => d != null))
            {
                CopyToLocalBuffer(desc.Data, FLocalDataBuffer, ref writeIndex);
            }
        }

        /// <summary>
        /// Copies to local buffer and increments the index. Tries to use Array.Copy();
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination array.</param>
        /// <param name="destinationStartIndex">Start index in the destination array.</param>
        protected static void CopyToLocalBuffer<T>(IReadOnlyList<T> source, T[] destination, ref int destinationStartIndex)
            where T : struct
        {
            var collection = source as ICollection<T>;
            if (collection != null)
            {
                //direct copy
                collection.CopyTo(destination, destinationStartIndex);
                destinationStartIndex += collection.Count;
                return;
            }
            else 
            {
                foreach (var pos in source)
                {
                    //iteration
                    destination[destinationStartIndex++] = pos;
                }
            }
        }

        RGBAColor ToRGBAColor(Color4 color)
        {
            return new RGBAColor(color.Red, color.Green, color.Blue, color.Alpha);
        }

        SlimDXMatrix ToSlimDXMatrix(ref Matrix m)
        {
            return new SlimDXMatrix()
            {
                M11 = m.M11, M12 = m.M12, M13 = m.M13, M14 = m.M14,
                M21 = m.M21, M22 = m.M22, M23 = m.M23, M24 = m.M24,
                M31 = m.M31, M32 = m.M32, M33 = m.M33, M34 = m.M34,
                M41 = m.M41, M42 = m.M42, M43 = m.M43, M44 = m.M44
            };
        }

        private Vector4D ToVector4(ref RectangleF clipRect)
        {
            return new Vector4D(clipRect.Left, clipRect.Top, clipRect.Right, clipRect.Bottom);
        }

        void EnsureArraySize<T>(ref T[] array, int minimumSize)
        {
            var newSize = array.Length;
            if (newSize < minimumSize)
            {
                do
                {
                    newSize = newSize << 1;
                }
                while (newSize < minimumSize);

                array = new T[newSize];
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
            this.FBufferOutput.SliceCount = 1;
        }
    }
}
