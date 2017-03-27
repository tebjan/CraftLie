using FeralTic.DX11.Geometry;
using SlimDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace CraftLie
{
    [Type(IsImmutable = true)]
    public class BufferDrawer
    {
        public readonly IReadOnlyList<BufferGeometry> Geometries;

        [Node]
        public BufferDrawer(IReadOnlyList<BufferGeometry> geometries)
        {
            Geometries = geometries;
        }

        public static BufferDrawer Unite(BufferDrawer input, BufferDrawer input2)
        {
            return new BufferDrawer(input.Geometries.Concat(input2.Geometries).ToList());
        }

        public static BufferDrawer Unite(IEnumerable<BufferDrawer> input)
        {
            return new BufferDrawer(GetGeometries(input));
        }

        static IReadOnlyList<BufferGeometry> GetGeometries(IEnumerable<BufferDrawer> input)
        {
            return input.SelectMany(d => d.Geometries).ToList();
        }

        static IEnumerable<BufferGeometry> GetGeometries()
        {
            yield return BufferGeometry.Default;
        }

        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly BufferDrawer Default = new BufferDrawer(GetGeometries().ToList());
    }
}
