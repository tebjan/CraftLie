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
using SlimDX.Direct3D11;

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
            if (this.FBufferDescriptionIn.Any(d => d != null && d.Set))
            {
                if (this.FBufferDescriptionIn.SliceCount > 0)
                {
                    this.FBufferOutput.Resize(FBufferDescriptionIn.SliceCount, () => new DX11Resource<IDX11ReadableStructureBuffer>(), b => b?.Dispose());
                    this.FValid.SliceCount = FBufferDescriptionIn.SliceCount;
                }
                else //no output
                {

                    this.FBufferOutput.SafeDisposeAll();
                    this.FBufferOutput.SliceCount = 0;
                    this.FValid.SliceCount = 0;
                }

                //mark buffers changed
                this.FBufferOutput.Stream.IsChanged = true;
            }

        }


        public void Update(DX11RenderContext context)
        {

            for (int i = 0; i < FBufferOutput.SliceCount; i++)
            {
                if (FBufferDescriptionIn[i].Set)
                {
                    FValid[i] = false;
                    SetupBuffer(i, context, FBufferOutput[i], FBufferDescriptionIn[i]);
                }
            }
        }

        private void SetupBuffer(int slice, DX11RenderContext context, DX11Resource<IDX11ReadableStructureBuffer> buffer, DynamicBufferDescription<TBuffer> description)
        {
            //refresh buffers?
            if (buffer.Contains(context))
            {
                var res = buffer[context];
                if (res.Buffer.Description.SizeInBytes < description.DataSizeInBytes
                || currentBufferType != FBufferType[0]
                || res.Stride != description.Stride
                || res is DX11ImmutableStructuredVLBuffer<TBuffer>)
                {
                    buffer.Dispose(context);
                }
            }

            //make new buffers?
            if (!buffer.Contains(context))
            {
                CreateBuffer(buffer, context, description);
            }

            this.FValid[slice] = true;

            //write to buffers
            var b = buffer[context];

            switch (description.DataType)
            {
                case BufferDescriptionDataType.IntPtr:
                    var buff = b as DX11DynamicStructuredVLBuffer<TBuffer>;
                    if (buff != null)
                    {
                        buff.WriteData(description.GetDataPointer(), description.DataSizeInBytes);
                    }
                    else
                    {
                        var cdBuff = b as DX11CopyDestStructuredVLBuffer<TBuffer>;
                        cdBuff.WriteData(description.GetDataPointer(), description.DataSizeInBytes);
                    }
                    break;
                case BufferDescriptionDataType.Array:
                case BufferDescriptionDataType.Spread:
                    var pinnedArray = GCHandle.Alloc(description.GetDataArray(), GCHandleType.Pinned);
                    try
                    {
                        buff = b as DX11DynamicStructuredVLBuffer<TBuffer>;
                        if (buff != null)
                        {
                            buff.WriteData(pinnedArray.AddrOfPinnedObject(), description.DataSizeInBytes);
                        }
                        else
                        {
                            var cdBuff = b as DX11CopyDestStructuredVLBuffer<TBuffer>;
                            cdBuff.WriteData(pinnedArray.AddrOfPinnedObject(), description.DataSizeInBytes);
                        }

                    }
                    finally
                    {
                        pinnedArray.Free();
                    }
                    break;
                case BufferDescriptionDataType.Stream:
                    var stream = description.GetDataStream();
                    stream.Position = 0;
                    var ctx = context.CurrentDeviceContext;

                    buff = b as DX11DynamicStructuredVLBuffer<TBuffer>;
                    if (buff != null)
                    {
                        using (var ds = new SlimDX.DataStream((int)description.DataSizeInBytes, true, true))
                        {
                            stream.CopyTo(ds);
                            SlimDX.DataBox db = new SlimDX.DataBox(0, 0, ds);
                            ctx.UpdateSubresource(db, b.Buffer, 0);
                        }
                    }
                    else
                    {
                        var db = ctx.MapSubresource(b.Buffer, MapMode.WriteDiscard, MapFlags.None);
                        stream.CopyTo(db.Data);
                        ctx.UnmapSubresource(b.Buffer, 0);
                    }
                    break;
            }
        }


        /// <summary>
        /// Creates a buffer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private void CreateBuffer(DX11Resource<IDX11ReadableStructureBuffer> bufferResource, DX11RenderContext context, DynamicBufferDescription<TBuffer> description)
        {
            var count = 0;

            switch (description.DataType)
            {
                case BufferDescriptionDataType.IntPtr:
                case BufferDescriptionDataType.Stream:
                    count = (int)description.DataSizeInBytes;
                    break;

                case BufferDescriptionDataType.Array:
                case BufferDescriptionDataType.Spread:
                    count = description.GetDataArray().Length;
                    break;
            }

            count = NextUpperPow2(count);

            if (this.FBufferType[0] == DX11BufferUploadType.Dynamic)
            {
                bufferResource[context] = new DX11DynamicStructuredVLBuffer<TBuffer>(context, count, description.Stride);
            }
            else if (this.FBufferType[0] == DX11BufferUploadType.Default)
            {
                bufferResource[context] = new DX11CopyDestStructuredVLBuffer<TBuffer>(context, count, description.Stride);
            }
            else
            {
                count = (int)description.DataSizeInBytes;
                switch (description.DataType)
                {
                    case BufferDescriptionDataType.IntPtr:
                        bufferResource[context] = new DX11ImmutableStructuredVLBuffer<TBuffer>(context.Device, description.GetDataPointer(), count, description.Stride);
                        break;
                    case BufferDescriptionDataType.Array:
                    case BufferDescriptionDataType.Spread:
                        bufferResource[context] = new DX11ImmutableStructuredVLBuffer<TBuffer>(context.Device, description.GetDataArray(), count, description.Stride);
                        break;
                    case BufferDescriptionDataType.Stream:
                        bufferResource[context] = new DX11ImmutableStructuredVLBuffer<TBuffer>(context.Device, description.GetDataArray(), count, description.Stride);
                        break;
                    default:
                        break;
                }            
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
            this.FBufferOutput.Resize(0, () => new DX11Resource<IDX11ReadableStructureBuffer>(), b => b?.Dispose());
        }
    }
}
