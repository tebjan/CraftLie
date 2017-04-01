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

        public readonly IReadOnlyList<DrawDescription> DrawDescriptions;
        public readonly IReadOnlyList<TextDescriptor> TextDescriptions;
        public readonly IReadOnlyList<DrawSpritesDescription> SpritesDescriptions;

        [Node]
        public DrawDescriptionLayer(IReadOnlyList<DrawDescription> geometries, IReadOnlyList<DrawSpritesDescription> sprites, IReadOnlyList<TextDescriptor> texts)
        {
            DrawDescriptions = geometries;
            SpritesDescriptions = sprites;
            TextDescriptions = texts;
        }

        public static DrawDescriptionLayer Concat(DrawDescriptionLayer input, DrawDescriptionLayer input2)
        {
            return new DrawDescriptionLayer(
                input.DrawDescriptions.Concat(input2.DrawDescriptions).ToList(),
                input.SpritesDescriptions.Concat(input2.SpritesDescriptions).ToList(),
                input.TextDescriptions.Concat(input2.TextDescriptions).ToList());
        }

        public static DrawDescriptionLayer Unite(IEnumerable<DrawDescriptionLayer> input)
        {
            return new DrawDescriptionLayer(
                input.SelectMany(d => d.DrawDescriptions).ToList(),
                input.SelectMany(d => d.SpritesDescriptions).ToList(),
                input.SelectMany(d => d.TextDescriptions).ToList());
        }

        static IEnumerable<DrawDescription> GetDefaultDrawDescription()
        {
            yield return DrawDescription.Default;
        }

        static IEnumerable<TextDescriptor> GetDefaultTextDescriptor()
        {
            yield return TextDescriptor.Default;
        }

        static IEnumerable<DrawSpritesDescription> GetDefaultSpritesDescriptor()
        {
            yield return DrawSpritesDescription.Default;
        }
    }
}
