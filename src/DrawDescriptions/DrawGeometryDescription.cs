using System;
using System.Linq;
using System.Collections.Generic;
using FeralTic.DX11;
using FeralTic.DX11.Geometry;
using FeralTic.DX11.Resources;
using SharpDX;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace CraftLie
{
    public enum ShadingType
    {
        Constant,
        PhongDirectional
    }

    public class DrawGeometryDescription : DrawDescription
    {
        public static readonly DrawGeometryDescription Default;
        static readonly IReadOnlyCollection<Matrix> Identity;
        static readonly IReadOnlyCollection<Color4> White;

        static DrawGeometryDescription()
        {
            Identity = new Matrix[] { Matrix.Identity };
            White = new Color4[] { Color4.White };
            Default = new DrawGeometryDescription(null, Matrix.Identity, new Color4(0, 1, 0, 1), "", BlendMode.Blend, ShadingType.Constant, new List<Matrix>(), new List<Color4>());
        }

        public ShadingType Shading;
        public IReadOnlyCollection<Matrix> InstanceTransformations;
        public IReadOnlyCollection<Color4> InstanceColors;
        public int InstanceCount;

        public DrawGeometryDescription()
            : this(null)
        {
        }

        public DrawGeometryDescription(GeometryDescriptor geometryDescriptor)
        {
            GeometryDescriptor = geometryDescriptor ?? new BoxDescriptor();
        }

        public void SetLayerOrder(int layerOrder)
        {
            LayerOrder = layerOrder;
        }

        public DrawGeometryDescription(GeometryDescriptor geometryDescriptor, 
            Matrix transformation, 
            Color4 color,
            string texturePath,
            BlendMode blendMode,
            ShadingType shading,
            IReadOnlyCollection<Matrix> instanceTransformations,
            IReadOnlyCollection<Color4> instanceColors)
            : this(geometryDescriptor)
        {
            Update(transformation, color, texturePath, blendMode, shading, instanceTransformations, instanceColors);
        }

        public void UpdateGeometry(GeometryDescriptor geometryDescriptor)
        {
            if (geometryDescriptor != GeometryDescriptor)
            {
                DisposeGeometry();
                GeometryDescriptor = geometryDescriptor;
            }
        }

        public DrawGeometryDescription DeepCopy()
        {
            var result = new DrawGeometryDescription(this.GeometryDescriptor.DeepCopy());
            result.Update(this.Transformation, this.Color, this.TexturePath, this.Blending, this.Shading, this.InstanceTransformations.ToList(), this.InstanceColors.ToList());
            return result;
        }

        public void Update(
            Matrix transformation,
            Color4 color,
            string texturePath,
            BlendMode blendMode = BlendMode.Blend,
            ShadingType shading = ShadingType.Constant,
            IReadOnlyCollection<Matrix> instanceTransformations = null,
            IReadOnlyCollection<Color4> instanceColors = null)
        {
            Transformation = transformation;
            Color = color;
            TexturePath = texturePath;
            Blending = blendMode;
            Shading = shading;
            Space = TransformationSpace.World; //always set default, can be changed by Within node

            if (instanceColors == null || instanceColors.Count == 0)
                instanceColors = White;

            if (instanceTransformations == null || instanceTransformations.Count == 0)
                instanceTransformations = Identity;

            InstanceTransformations = instanceTransformations;
            InstanceColors = instanceColors;
            InstanceCount = Math.Max(Math.Max(instanceTransformations.Count, instanceColors.Count), 1);
        }   
    }

}