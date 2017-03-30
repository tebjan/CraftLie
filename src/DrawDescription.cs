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
    public class DrawDescriptionBase : IDisposable
    {
        public GeometryDescriptor GeometryDescriptor;
        public string TexturePath;

        public IDX11Geometry GetGeometry(DX11RenderContext context)
        {
            IDX11Geometry geo;
            if (!GeometryCache.TryGetValue(context, out geo))
            {
                geo = PrimitiveFactory.GetGeometry(context, GeometryDescriptor);
                GeometryCache[context] = geo;
            }

            return geo;
        }

        [Node]
        public void Dispose()
        {
            DisposeGeometry();
        }

        protected void DisposeGeometry()
        {
            try
            {
                foreach (var geo in GeometryCache.Values)
                {
                    try
                    {
                        geo?.Dispose();
                    }
                    catch (Exception)
                    {
                        //safe dispose
                    }
                }

                GeometryCache.Clear();
            }
            catch (Exception)
            {
                //safe dispose
            }
        }

        readonly Dictionary<DX11RenderContext, IDX11Geometry> GeometryCache = new Dictionary<DX11RenderContext, IDX11Geometry>();
    }

    [Type]
    public class DrawDescription : DrawDescriptionBase
    {
        public Matrix Transformation;
        public IReadOnlyList<Matrix> InstanceTransformations;
        public IReadOnlyList<Color4> InstanceColors;
        public int InstanceCount;

        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly DrawDescription Default = new DrawDescription(null, Matrix.Identity, new Color4(0, 1, 0, 1), "", new List<Matrix>(), new List<Color4>());

        public DrawDescription()
            : this(null)
        {
        }

        [Node]
        public DrawDescription(GeometryDescriptor geometryDescriptor)
        {
            GeometryDescriptor = geometryDescriptor ?? new BoxDescriptor();
        }

        public DrawDescription(GeometryDescriptor geometryDescriptor, 
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
        public void UpdateGeometry(GeometryDescriptor geometryDescriptor)
        {
            if (geometryDescriptor != GeometryDescriptor)
            {
                DisposeGeometry();
                GeometryDescriptor = geometryDescriptor;
            }
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
    }

    [Type(IsImmutable = true)]
    public class DrawSpritesDescription : DrawDescriptionBase
    {
        public IReadOnlyList<Vector3> Positions;
        public IReadOnlyList<Vector2> Sizes;
        public IReadOnlyList<Color4> Colors;

        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly DrawSpritesDescription Default = new DrawSpritesDescription(
            new List<Vector3>() { Vector3.Zero },
            new List<Vector2>() { Vector2.One },
            new List<Color4>() { Color4.White });

        public int SpriteCount;

        [Node]
        public DrawSpritesDescription(SpritesDescriptor geometryDescriptor)
        {
            GeometryDescriptor = geometryDescriptor ?? new SpritesDescriptor();
        }

        [Node]
        public DrawSpritesDescription(IReadOnlyList<Vector3> positions, IReadOnlyList<Vector2> sizes, IReadOnlyList<Color4> colors, string texturePath = "")
        {
            Positions = positions;
            Sizes = sizes;
            Colors = colors;
            TexturePath = texturePath;
        }

        [Node]
        public void Update(
            IReadOnlyList<Vector3> positions, 
            IReadOnlyList<Vector2> sizes, 
            IReadOnlyList<Color4> colors, 
            string texturePath = "")
        {

            TexturePath = texturePath;

            if (positions == null)
                positions = new List<Vector3>() { Vector3.Zero };

            if (sizes == null)
                sizes = new List<Vector2>() { Vector2.One };

            if (colors == null)
                colors = new List<Color4>() { Color4.White };

            Positions = positions;
            Sizes = sizes;
            Colors = colors;
            var newSpriteCount = Math.Max(Math.Max(Positions.Count, Sizes.Count), Colors.Count);

            if (newSpriteCount != SpriteCount)
            {
                DisposeGeometry();
                GeometryDescriptor = new SpritesDescriptor() { SpriteCount = newSpriteCount };
                SpriteCount = newSpriteCount;
            }
        }
    }
}
