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
    public class DrawDescriptionBuffer
    {
        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly DrawDescriptionBuffer Default = new DrawDescriptionBuffer(GetDefault().ToList());

        public readonly IReadOnlyList<DrawDescription> DrawDescriptions;

        [Node]
        public DrawDescriptionBuffer(IReadOnlyList<DrawDescription> geometries)
        {
            DrawDescriptions = geometries;
        }

        public static DrawDescriptionBuffer Unite(DrawDescriptionBuffer input, DrawDescriptionBuffer input2)
        {
            return new DrawDescriptionBuffer(input.DrawDescriptions.Concat(input2.DrawDescriptions).ToList());
        }

        public static DrawDescriptionBuffer Unite(IEnumerable<DrawDescriptionBuffer> input)
        {
            return new DrawDescriptionBuffer(input.SelectMany(d => d.DrawDescriptions).ToList());
        }

        static IEnumerable<DrawDescription> GetDefault()
        {
            yield return DrawDescription.Default;
        }
    }
}
