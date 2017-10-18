using FeralTic.DX11;
using FeralTic.DX11.Geometry;
using FeralTic.DX11.Resources;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Collections;

namespace CraftLie
{
    public enum BufferDescriptionDataType
    {
        IntPtr,
        Array,
        Spread,
        Stream
    }

    public abstract class DynamicBufferDescription
    {
        public readonly bool Set;
        public readonly BufferDescriptionDataType DataType;
        public readonly long DataSizeInBytes;
        public readonly int Stride;

        public DynamicBufferDescription(BufferDescriptionDataType dataType, long dataSizeInBytes, int stride, bool set)
        {
            DataSizeInBytes = Math.Max(dataSizeInBytes, 0);
            DataType = dataType;
            Stride = stride;
            Set = set;
        }

        public virtual IntPtr GetDataPointer() => IntPtr.Zero;
        public virtual Array GetDataArray() => new byte[0];
        public virtual Stream GetDataStream() => new MemoryStream();
    }

    public class DynamicBufferDescription<TBuffer> : DynamicBufferDescription
        where TBuffer : struct
    {
        public DynamicBufferDescription(BufferDescriptionDataType dataType, long dataSizeInBytes, int stride, bool set)
            : base(dataType, dataSizeInBytes, stride, set)
        {
        }

        protected static readonly int StructSizeInBytes = Marshal.SizeOf(typeof(TBuffer));
    }

    public class DynamicBufferDescriptionSpread<TBuffer> : DynamicBufferDescription<TBuffer>
        where TBuffer : struct
    {
        public readonly Spread<TBuffer> Data;

        public DynamicBufferDescriptionSpread(Spread<TBuffer> data, bool set = true)
            : this(data, StructSizeInBytes, set)
        {
        }

        public DynamicBufferDescriptionSpread(Spread<TBuffer> data, int strideInBytes = 1, bool set = true)
            : base(BufferDescriptionDataType.Spread, StructSizeInBytes * data.Count, strideInBytes, set)
        {
            Data = data;
        }

        public override Array GetDataArray() => Data.GetInternalArray();
    }

    public class DynamicBufferDescriptionArray<TBuffer> : DynamicBufferDescription<TBuffer>
        where TBuffer : struct
    {
        public readonly TBuffer[] Data;

        public DynamicBufferDescriptionArray(TBuffer[] data, bool set = true)
            : this(data, StructSizeInBytes, set)
        {
        }

        public DynamicBufferDescriptionArray(TBuffer[] data, int strideInBytes = 1, bool set = true)
            : base(BufferDescriptionDataType.Array, StructSizeInBytes * data.Length, strideInBytes, set)
        {
            Data = data;
        }

        public override Array GetDataArray() => Data;
    }

    public class DynamicBufferDescriptionIntPtr : DynamicBufferDescription<byte>
    {
        public readonly IntPtr Data;

        public DynamicBufferDescriptionIntPtr(IntPtr data, long dataSizeInBytes = 1, bool set = true)
            : this(data, dataSizeInBytes, StructSizeInBytes, set)
        {
        }

        public DynamicBufferDescriptionIntPtr(IntPtr data, long dataSizeInBytes, int strideInBytes = 1, bool set = true)
            : base(BufferDescriptionDataType.IntPtr, dataSizeInBytes, strideInBytes, set)
        {
            Data = data;
        }

        public override IntPtr GetDataPointer() => Data;
    }

    public class DynamicBufferDescriptionStream : DynamicBufferDescription<byte>
    {
        public readonly Stream Data;

        public DynamicBufferDescriptionStream(Stream data, bool set = true)
            : this(data, StructSizeInBytes, set)
        {
        }

        public DynamicBufferDescriptionStream(Stream data, int strideInBytes = 1, bool set = true)
            : base(BufferDescriptionDataType.Stream, data.Length, strideInBytes, set)
        {
            Data = data;
        }

        public override Stream GetDataStream() => Data;
    }

}
