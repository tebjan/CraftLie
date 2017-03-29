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
        public static readonly DrawDescriptionLayer Default = new DrawDescriptionLayer(GetDefault().ToList());

        public readonly IReadOnlyList<DrawDescription> DrawDescriptions;

        [Node]
        public DrawDescriptionLayer(IReadOnlyList<DrawDescription> geometries)
        {
            DrawDescriptions = geometries;
        }

        public static DrawDescriptionLayer Concat(DrawDescriptionLayer input, DrawDescriptionLayer input2)
        {
            return new DrawDescriptionLayer(input.DrawDescriptions.Concat(input2.DrawDescriptions).ToList());
        }

        public static DrawDescriptionLayer Unite(IEnumerable<DrawDescriptionLayer> input)
        {
            return new DrawDescriptionLayer(input.SelectMany(d => d.DrawDescriptions).ToList());
        }

        static IEnumerable<DrawDescription> GetDefault()
        {
            yield return DrawDescription.Default;
        }
    }
}
