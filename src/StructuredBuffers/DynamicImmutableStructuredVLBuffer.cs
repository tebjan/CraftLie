using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SlimDX.Direct3D11;

using Buffer = SlimDX.Direct3D11.Buffer;
using System.IO;

namespace FeralTic.DX11.Resources
{

    public class DX11ImmutableStructuredVLBuffer<T> : DX11StructuredBuffer<T>, IDX11ReadableStructureBuffer where T : struct
    {
        public ShaderResourceView SRV { get; protected set; }

        public DX11ImmutableStructuredVLBuffer(Device dev, Array initialData, int elementCount)
            : this(dev, initialData, elementCount, Marshal.SizeOf(typeof(T)))
        {
        }

        public DX11ImmutableStructuredVLBuffer(Device dev, Array initialData, int elementCount, int stride)
        {
            this.Stride = stride;
            this.Size = elementCount * this.Stride;
            this.ElementCount = elementCount;

            BufferDescription bd = new BufferDescription()
            {
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                SizeInBytes = this.Size,
                StructureByteStride = stride,
                Usage = ResourceUsage.Immutable
            };

            DataStream ds = new DataStream(initialData, true, true);
            this.Buffer = new Buffer(dev, ds, bd);
            this.SRV = new ShaderResourceView(dev, this.Buffer);
        }

        public DX11ImmutableStructuredVLBuffer(Device dev, IntPtr initialData, int elementCount)
            : this(dev, initialData, elementCount, Marshal.SizeOf(typeof(T)))
        {
        }

        public DX11ImmutableStructuredVLBuffer(Device dev, IntPtr initialData, int elementCount, int stride)
        {
            this.Stride = stride;
            this.Size = elementCount * this.Stride;
            this.ElementCount = elementCount;

            BufferDescription bd = new BufferDescription()
            {
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                SizeInBytes = this.Size,
                StructureByteStride = stride,
                Usage = ResourceUsage.Immutable
            };

            DataStream ds = new DataStream(initialData, elementCount, true, true);
            this.Buffer = new Buffer(dev, ds, bd);
            this.SRV = new ShaderResourceView(dev, this.Buffer);
        }

        public DX11ImmutableStructuredVLBuffer(Device dev, Stream initialData, int elementCount)
            : this(dev, initialData, elementCount, Marshal.SizeOf(typeof(T)))
        {
        }

        public DX11ImmutableStructuredVLBuffer(Device dev, Stream initialData, int elementCount, int stride)
        {
            this.Stride = stride;
            this.Size = elementCount * this.Stride;
            this.ElementCount = elementCount;

            BufferDescription bd = new BufferDescription()
            {
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                SizeInBytes = this.Size,
                StructureByteStride = stride,
                Usage = ResourceUsage.Immutable
            };

            DataStream ds = new DataStream(elementCount, true, true);
            initialData.CopyTo(ds);
            this.Buffer = new Buffer(dev, ds, bd);
            this.SRV = new ShaderResourceView(dev, this.Buffer);
        }

        protected override void OnDispose()
        {
            if (this.SRV != null) { this.SRV.Dispose(); }
        }
    }
}
