using System;
using System.Linq;
using System.Collections.Generic;
using FeralTic.DX11;
using FeralTic.DX11.Geometry;
using FeralTic.DX11.Resources;
using Xenko.Core.Mathematics;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace CraftLie
{

    public class DrawSpritesDescription : DrawDescription
    {
        public IReadOnlyCollection<Vector3> Positions;
        public IReadOnlyCollection<Vector2> Sizes;
        public IReadOnlyCollection<Color4> Colors;

        static readonly IReadOnlyCollection<Vector3> NoPositions = new List<Vector3>(0);
        static readonly IReadOnlyCollection<Vector2> NoSizes = new List<Vector2>(0);
        static readonly IReadOnlyCollection<Color4> NoColors = new List<Color4>(0);

        static readonly IReadOnlyCollection<Vector2> DefaultSizes = new List<Vector2>(1) { new Vector2(0.01f) };
        static readonly IReadOnlyCollection<Color4> DefaultColors = new List<Color4>(1) { Color4.White };

        public static readonly DrawSpritesDescription Default = new DrawSpritesDescription(
            Matrix.Identity,
            BlendMode.Blend,
            NoPositions,
            NoSizes,
            NoColors);

        public int SpriteCount;

        public DrawSpritesDescription()
        {
            GeometryDescriptor = new SpritesDescriptor();
        }

        public void SetLayerOrder(int layerOrder)
        {
            LayerOrder = layerOrder;
        }

        public DrawSpritesDescription(Matrix transformation, BlendMode blendMode, IReadOnlyCollection<Vector3> positions, IReadOnlyCollection<Vector2> sizes, IReadOnlyCollection<Color4> colors, string texturePath = "")
        {
            GeometryDescriptor = new SpritesDescriptor();

            Update(transformation, blendMode, positions, sizes, colors, texturePath);
        }

        public void Update(
            Matrix transformation,
            BlendMode blendMode = BlendMode.Blend,
            IReadOnlyCollection<Vector3> positions = null,
            IReadOnlyCollection<Vector2> sizes = null,
            IReadOnlyCollection<Color4> colors = null, 
            string texturePath = "")
        {

            Transformation = transformation;
            TexturePath = texturePath;
            Space = TransformationSpace.World; //always set default, can be changed by Within node
            Blending = blendMode;

            if (positions == null)
                positions = NoPositions;

            if (sizes == null || sizes.Count < 1)
            {
                if (positions.Count > 0)
                    sizes = DefaultSizes;
                else
                    sizes = NoSizes;
            }

            if (colors == null || colors.Count < 1)
            {
                if (positions.Count > 0)
                    colors = DefaultColors;
                else
                    colors = NoColors;
            }

            Positions = positions;
            Sizes = sizes;
            Colors = colors;
            SpriteCount = Math.Max(Math.Max(Positions.Count, Sizes.Count), Colors.Count);
        }
    }

}