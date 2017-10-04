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
    public class DynamicBufferDescription : IDisposable
    {
        readonly IReadOnlyList<float> FData;

        [Node]
        public DynamicBufferDescription(IReadOnlyList<float> data)
        {
            FData = data;
        } 

        [Node]
        public void Dispose()
        {
        }
    }

}
