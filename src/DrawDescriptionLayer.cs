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
    public class DrawDescriptionLayer
    {
        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly DrawDescriptionLayer Default = new DrawDescriptionLayer(
            GetDefaultDrawDescription().ToList(),
            GetDefaultSpritesDescriptor().ToList(),
            GetDefaultTextDescriptor().ToList());

        public readonly IReadOnlyList<DrawGeometryDescription> GeometryDescriptions;
        public readonly IReadOnlyList<DrawTextDescription> TextDescriptions;
        public readonly IReadOnlyList<DrawSpritesDescription> SpritesDescriptions;

        [Node]
        public DrawDescriptionLayer(IReadOnlyList<DrawGeometryDescription> geometries, IReadOnlyList<DrawSpritesDescription> sprites, IReadOnlyList<DrawTextDescription> texts)
        {
            GeometryDescriptions = geometries;
            SpritesDescriptions = sprites;
            TextDescriptions = texts;
        }

        public static DrawDescriptionLayer Concat(DrawDescriptionLayer input, DrawDescriptionLayer input2)
        {
            return new DrawDescriptionLayer(
                input.GeometryDescriptions.Concat(input2.GeometryDescriptions).ToList(),
                input.SpritesDescriptions.Concat(input2.SpritesDescriptions).ToList(),
                input.TextDescriptions.Concat(input2.TextDescriptions).ToList());
        }

        public static DrawDescriptionLayer Unite(IEnumerable<DrawDescriptionLayer> input)
        {
            return new DrawDescriptionLayer(
                input.SelectMany(d => d.GeometryDescriptions).ToList(),
                input.SelectMany(d => d.SpritesDescriptions).ToList(),
                input.SelectMany(d => d.TextDescriptions).ToList());
        }

        static IEnumerable<DrawGeometryDescription> GetDefaultDrawDescription()
        {
            yield return DrawGeometryDescription.Default;
        }

        static IEnumerable<DrawTextDescription> GetDefaultTextDescriptor()
        {
            yield return DrawTextDescription.Default;
        }

        static IEnumerable<DrawSpritesDescription> GetDefaultSpritesDescriptor()
        {
            yield return DrawSpritesDescription.Default;
        }
    }
}
