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
    public class DrawDescription : IDisposable
    {
        public readonly AbstractPrimitiveDescriptor GeometryDescriptor;
        public Matrix Transformation;
        public string TexturePath;
        public IReadOnlyList<Matrix> InstanceTransformations;
        public IReadOnlyList<Color4> InstanceColors;
        public int InstanceCount;

        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly DrawDescription Default = new DrawDescription(new Box(), Matrix.Identity, new Color4(0, 1, 0, 1), "", new List<Matrix>(), new List<Color4>());

        [Node]
        public DrawDescription()
            : this(null)
        {
        }

        public DrawDescription(AbstractPrimitiveDescriptor geometryDescriptor)
        {
            var box = new Box();
            box.Size = new SlimDX.Vector3(1);
            GeometryDescriptor = geometryDescriptor ?? box;
        }

        public DrawDescription(AbstractPrimitiveDescriptor geometryDescriptor, 
            Matrix transformation, 
            Color4 color,
            string texturePath,
            IReadOnlyList<Matrix> instanceTransformations,
            IReadOnlyList<Color4> instanceColors)
            : this(geometryDescriptor)
        {
            Update(transformation, color, texturePath, instanceTransformations, instanceColors);
        }

        [Node]
        public void Update(
            Matrix transformation,
            Color4 color,
            string texturePath,
            IReadOnlyList<Matrix> instanceTransformations,
            IReadOnlyList<Color4> instanceColors)
        {
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

        public DX11IndexedGeometry GetGeometry(DX11RenderContext context)
        {
            DX11IndexedGeometry geo;
            if (!GeometryCache.TryGetValue(context, out geo))
            {
                var settings = new Box();
                settings.Size = new SlimDX.Vector3(1);
                geo = context.Primitives.Box(settings);
                GeometryCache[context] = geo;
            }

            return geo;
        }

        [Node]
        public void Dispose()
        {
            try
            {
                foreach (var geo in GeometryCache.Values)
                {
                    geo.Dispose();
                }
            }
            catch (Exception)
            {
                //safe dispose
            }
        }

        readonly Dictionary<DX11RenderContext, DX11IndexedGeometry> GeometryCache = new Dictionary<DX11RenderContext, DX11IndexedGeometry>();
    }
}
