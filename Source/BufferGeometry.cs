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
    [Type(IsImmutable = true)]
    public class BufferGeometry
    {
        public readonly AbstractPrimitiveDescriptor Geometry;
        public readonly Matrix Transformation;
        public readonly string TexturePath;
        public readonly IReadOnlyList<Matrix> InstanceTransformations;
        public readonly IReadOnlyList<Color4> InstanceColors;

        public readonly int InstanceCount;

        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly BufferGeometry Default = new BufferGeometry(new Box(), Matrix.Identity, new Color4(0, 1, 0, 1), "", new List<Matrix>(), new List<Color4>());

        [Node]
        public BufferGeometry(
            Matrix transformation,
            Color4 color,
            string texturePath,
            IReadOnlyList<Matrix> instanceTransformations,
            IReadOnlyList<Color4> instanceColors)
            : this(null, transformation, color, texturePath, instanceTransformations, instanceColors)
        {
        }

        public BufferGeometry(AbstractPrimitiveDescriptor geometryDescription, 
            Matrix transformation, 
            Color4 color,
            string texturePath,
            IReadOnlyList<Matrix> instanceTransformations,
            IReadOnlyList<Color4> instanceColors)
        {
            var box = new Box(); box.Size = new SlimDX.Vector3(1);
            Geometry = geometryDescription ?? box;
            Transformation = transformation;
            TexturePath = texturePath;

            if (instanceColors == null)
                instanceColors = new List<Color4>();

            if (instanceTransformations == null)
                instanceTransformations = Enumerable.Repeat(Matrix.Identity, 1).ToList();

            InstanceTransformations = instanceTransformations;

            InstanceColors = instanceColors.Count > 0 ? instanceColors : Enumerable.Repeat(color, 1).ToList();

            InstanceCount = Math.Max(Math.Max(instanceTransformations.Count, instanceColors.Count), 1);
        }



        public DX11IndexedGeometry GetGeom(DX11RenderContext context)
        {

            DX11IndexedGeometry geo;
            if (!GeometryCache.TryGetValue(Geometry.PrimitiveType, out geo))
            {
                var settings = new Box();
                settings.Size = new SlimDX.Vector3(1);
                geo = context.Primitives.Box(settings);
                GeometryCache[Geometry.PrimitiveType] = geo;
            }


            return geo;
        }


        static Dictionary<string, DX11IndexedGeometry> GeometryCache = new Dictionary<string, DX11IndexedGeometry>();
    }
}
