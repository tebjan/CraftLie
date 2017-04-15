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
    public class DrawDescription : IDisposable
    {
        public GeometryDescriptor GeometryDescriptor;
        public Matrix Transformation;
        public Color4 Color;
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
    public enum ShadingType
    {
        Constant,
        PhongDirectional
    }

    [Type]
    public class DrawGeometryDescription : DrawDescription
    {
        public ShadingType Shading;
        public IReadOnlyList<Matrix> InstanceTransformations;
        public IReadOnlyList<Color4> InstanceColors;
        public int InstanceCount;

        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly DrawGeometryDescription Default = new DrawGeometryDescription(null, Matrix.Identity, new Color4(0, 1, 0, 1), "", ShadingType.Constant, new List<Matrix>(), new List<Color4>());

        public DrawGeometryDescription()
            : this(null)
        {
        }

        [Node]
        public DrawGeometryDescription(GeometryDescriptor geometryDescriptor)
        {
            GeometryDescriptor = geometryDescriptor ?? new BoxDescriptor();
        }

        public DrawGeometryDescription(GeometryDescriptor geometryDescriptor, 
            Matrix transformation, 
            Color4 color,
            string texturePath,
            ShadingType shading,
            IReadOnlyList<Matrix> instanceTransformations,
            IReadOnlyList<Color4> instanceColors)
            : this(geometryDescriptor)
        {
            Update(transformation, color, texturePath, shading, instanceTransformations, instanceColors);
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
            ShadingType shading,
            IReadOnlyList<Matrix> instanceTransformations,
            IReadOnlyList<Color4> instanceColors)
        {
            Transformation = transformation;
            Color = color;
            TexturePath = texturePath;
            Shading = shading;

            if (instanceColors == null)
                instanceColors = new List<Color4>(1) { Color4.White };

            if (instanceTransformations == null)
                instanceTransformations =  new List<Matrix>(1) { Matrix.Identity };

            InstanceTransformations = instanceTransformations;
            InstanceColors = instanceColors;
            InstanceCount = Math.Max(Math.Max(instanceTransformations.Count, instanceColors.Count), 1);
        }
        
        [Node]
        public void Transform(Matrix transformation)
        {
            Matrix.Multiply(ref Transformation, ref transformation, out Transformation);
        }      
    }

}
