using FeralTic.DX11;
using FeralTic.DX11.Geometry;
using FeralTic.DX11.Resources;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Collections;

namespace CraftLie
{
    public enum RawBufferDescriptionDataType
    {
        IntPtr,
        Array,
        Spread,
        Stream
    }

    public class DynamicRawBufferDescription
    {
        public readonly bool Set;
        public readonly RawBufferDescriptionDataType DataType;
        public readonly long DataSizeInBytes;

        internal DynamicRawBufferDescription(long dataSizeInBytes, RawBufferDescriptionDataType dataType, bool set)
        {
            DataSizeInBytes = Math.Max(dataSizeInBytes, 0);
            DataType = dataType;
        }

        public virtual IntPtr GetDataPointer() => IntPtr.Zero;
        public virtual Array GetDataArray() => new byte[0];
        public virtual Stream GetDataStream() => new MemoryStream();
    }

    public class DynamicRawBufferDescriptionIntPtr : DynamicRawBufferDescription
    {
        public readonly IntPtr Data;

        public DynamicRawBufferDescriptionIntPtr(IntPtr data, long dataSizeInBytes, bool set = true)
            : base(dataSizeInBytes, RawBufferDescriptionDataType.IntPtr, set)
        {
            Data = data;
        }

        public override IntPtr GetDataPointer() => Data;
    }

    public class DynamicRawBufferDescriptionArray : DynamicRawBufferDescription
    {
        public readonly byte[] Data;

        public DynamicRawBufferDescriptionArray(byte[] data, bool set = true)
            : base(data.LongLength, RawBufferDescriptionDataType.Array, set)
        {
            Data = data;
        }

        public override Array GetDataArray() => Data;
    }

    public class DynamicRawBufferDescriptionSpread : DynamicRawBufferDescription
    {
        public readonly Spread<byte> Data;

        public DynamicRawBufferDescriptionSpread(Spread<byte> data, bool set = true)
            : base(data.Count, RawBufferDescriptionDataType.Spread, set)
        {
            Data = data;
        }

        public override Array GetDataArray() => Data.GetInternalArray();
    }

    public class DynamicRawBufferDescriptionStream : DynamicRawBufferDescription
    {
        public readonly Stream Data;

        public DynamicRawBufferDescriptionStream(Stream data, bool set = true)
            : base(data.Length, RawBufferDescriptionDataType.Stream, set)
        {
            Data = data;
        }

        public override Stream GetDataStream() => Data;
    }
}
