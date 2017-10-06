using FeralTic.DX11;
using FeralTic.DX11.Geometry;
using FeralTic.DX11.Resources;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace CraftLie
{
    [Type]
    public class DynamicBufferDescription<TBuffer>
        where TBuffer : struct
    {
        public readonly IReadOnlyList<TBuffer> Data;

        [Node]
        public DynamicBufferDescription(IReadOnlyList<TBuffer> data)
        {
            Data = data;
        } 
    }

}
