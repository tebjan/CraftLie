using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using SlimDX;
using SlimDX.Direct3D11;

using Buffer = SlimDX.Direct3D11.Buffer;


namespace FeralTic.DX11.Resources
{
    public class DX11CopyDestStructuredVLBuffer<T> : DX11StructuredBuffer<T>, IDX11ReadableStructureBuffer where T : struct
    {
        public ShaderResourceView SRV { get; protected set; }

        private DX11RenderContext context;

        public DX11CopyDestStructuredVLBuffer(DX11RenderContext context, int cnt)
            : this(context, cnt, Marshal.SizeOf(typeof(T)))
        {
        }

        public DX11CopyDestStructuredVLBuffer(DX11RenderContext context, int cnt, int stride)
        {
            this.context = context;
            this.Size = cnt * Marshal.SizeOf(typeof(T));
            this.ElementCount = cnt;
            this.Stride = stride;

            BufferDescription bd = new BufferDescription()
            {
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                SizeInBytes = this.Size,
                StructureByteStride = stride,
                Usage = ResourceUsage.Default
            };

            this.Buffer = new Buffer(context.Device, bd);
            this.SRV = new ShaderResourceView(context.Device, this.Buffer);
        }

        public DX11CopyDestStructuredVLBuffer(DX11RenderContext context, Buffer buffer, int cnt) //Dynamic default buffer
        {
            this.context = context;
            this.Size = cnt;
            this.Buffer = buffer;
            this.SRV = new ShaderResourceView(context.Device, this.Buffer);
            this.Stride = Marshal.SizeOf(typeof(T));
        }

        public DX11CopyDestStructuredVLBuffer(DX11RenderContext context, IntPtr initial, int cnt) //Dynamic default buffer
        {
            this.context = context;
            this.Size = cnt * Marshal.SizeOf(typeof(T));
            this.ElementCount = cnt;
            this.Stride = Marshal.SizeOf(typeof(T));

            BufferDescription bd = new BufferDescription()
            {
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                SizeInBytes = this.Size,
                StructureByteStride = Marshal.SizeOf(typeof(T)),
                Usage = ResourceUsage.Immutable
            };

            DataStream ds = new DataStream(initial, this.Size, true, false);
            ds.Position = 0;

            this.Buffer = new Buffer(context.Device, ds, bd);
            this.SRV = new ShaderResourceView(context.Device, this.Buffer);
        }

        public void WriteData(T[] data)
        {
            WriteData(data, 0, data.Length);
        }

        public void WriteData(T[] data, int offset, long count)
        {
            using (var ds = new DataStream(data, true, true))
            {
                DataBox db = new DataBox(0, 0, ds);
                this.context.CurrentDeviceContext.UpdateSubresource(db, this.Buffer, 0);
            }
        }

        public void WriteData(IntPtr ptr, long length)
        {
            using (var ds = new DataStream(ptr, length, true, true))
            {
                DataBox db = new DataBox(0, 0, ds);
                this.context.CurrentDeviceContext.UpdateSubresource(db, this.Buffer, 0);
            }
        }

        protected override void OnDispose()
        {
            if (this.SRV != null) { this.SRV.Dispose(); }
        }
    }
}
