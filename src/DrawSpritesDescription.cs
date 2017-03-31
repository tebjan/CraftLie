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

    [Type]
    public class DrawSpritesDescription : DrawDescriptionBase
    {
        public IReadOnlyList<Vector3> Positions;
        public IReadOnlyList<Vector2> Sizes;
        public IReadOnlyList<Color4> Colors;

        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly DrawSpritesDescription Default = new DrawSpritesDescription(
            Matrix.Identity,
            new List<Vector3>(),
            new List<Vector2>(),
            new List<Color4>());

        public int SpriteCount;

        [Node]
        public DrawSpritesDescription()
        {
            GeometryDescriptor = new SpritesDescriptor();
        }

        public DrawSpritesDescription(Matrix transformation, IReadOnlyList<Vector3> positions, IReadOnlyList<Vector2> sizes, IReadOnlyList<Color4> colors, string texturePath = "")
        {
            GeometryDescriptor = new SpritesDescriptor();

            Update(transformation, positions, sizes, colors, texturePath);
        }

        [Node]
        public void Update(
            Matrix transformation,
            IReadOnlyList<Vector3> positions, 
            IReadOnlyList<Vector2> sizes, 
            IReadOnlyList<Color4> colors, 
            string texturePath = "")
        {

            Transformation = transformation;
            TexturePath = texturePath;

            if (positions == null)
                positions = new List<Vector3>();

            if (sizes == null)
                sizes = new List<Vector2>();

            if (colors == null)
                colors = new List<Color4>();

            Positions = positions;
            Sizes = sizes;
            Colors = colors;
            SpriteCount = Math.Max(Math.Max(Positions.Count, Sizes.Count), Colors.Count);
        }
    }

}