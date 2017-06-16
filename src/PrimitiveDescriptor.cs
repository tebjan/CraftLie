using FeralTic.DX11.Geometry;
using System.Collections.Generic;
using System.Linq;
using VL.Core;

namespace CraftLie
{
    public enum PrimitiveType
    {
        Quad,
        RoundQuad,
        Box,
        Disc,
        Sphere,
        Cylinder,
        Tube,
        Line,
        MeshJoin,
        Sprites,
        Text
    }

    [Type]
    public enum ParagraphAlignment
    {
        //
        // Summary:
        //     The top of the text flow is aligned to the top edge of the layout box.
        Near = 0,
        //
        // Summary:
        //     The bottom of the text flow is aligned to the bottom edge of the layout box.
        Far = 1,
        //
        // Summary:
        //     The center of the flow is aligned to the center of the layout box.
        Center = 2
    }

    [Type]
    public enum TextAlignment
    {
        //
        // Summary:
        //     The leading edge of the paragraph text is aligned to the leading edge of the
        //     layout box.
        Leading = 0,
        //
        // Summary:
        //     The trailing edge of the paragraph text is aligned to the trailing edge of the
        //     layout box.
        Trailing = 1,
        //
        // Summary:
        //     The center of the paragraph text is aligned to the center of the layout box.
        Center = 2,
        //
        // Summary:
        //     Align text to the leading side, and also justify text to fill the lines.
        Justified = 3
    }

    [Type]
    //
    // Summary:
    //     Specifies how the pipeline should interpret vertex data bound to the input assembler
    //     stage.
    public enum MeshTopology
    {
        //
        // Summary:
        //     The IA stage has not been initialized with a primitive topology. The IA stage
        //     will not function properly unless a primitive topology is defined.
        Undefined = 0,
        //
        // Summary:
        //     Interpret the vertex data as a list of points.
        PointList = 1,
        //
        // Summary:
        //     Interpret the vertex data as a list of lines.
        LineList = 2,
        //
        // Summary:
        //     Interpret the vertex data as a line strip.
        LineStrip = 3,
        //
        // Summary:
        //     Interpret the vertex data as a list of triangles.
        TriangleList = 4,
        //
        // Summary:
        //     Interpret the vertex data as a triangle strip.
        TriangleStrip = 5
    }

    [Type(IsImmutable = true)]
    public abstract class GeometryDescriptor
    {
        public readonly PrimitiveType Type;
        public GeometryDescriptor(PrimitiveType type)
        {
            Type = type;
        }

        public static readonly Quad UnitQuad = new Quad() { Size = new SlimDX.Vector2(1) };
        public static readonly Box UnitBox = new Box() { Size = new SlimDX.Vector3(1) };
        public static readonly List<SlimDX.Vector3> UnitLine = new List<SlimDX.Vector3>(2) { new SlimDX.Vector3(-0.5f, 0, 0), new SlimDX.Vector3(0.5f, 0, 0) };
        public static readonly List<SlimDX.Vector3> UnitLineNormals = new List<SlimDX.Vector3>(2) { new SlimDX.Vector3(0, 1, 0), new SlimDX.Vector3(0, 1, 0) };

        public static readonly List<SlimDX.Vector3> UnitTri = new List<SlimDX.Vector3>(3) { new SlimDX.Vector3(-0.5f, -0.5f, 0), new SlimDX.Vector3(0.5f, -0.5f, 0), new SlimDX.Vector3(0, 0.5f, 0) };
        public static readonly List<SlimDX.Vector3> UnitTriNormals = new List<SlimDX.Vector3>(3) { new SlimDX.Vector3(0, -1, 0), new SlimDX.Vector3(0, -1, 0), new SlimDX.Vector3(0, -1, 0) };
        public static readonly List<SlimDX.Vector2> UnitTriTex = new List<SlimDX.Vector2>(3) { new SlimDX.Vector2(0, 1), new SlimDX.Vector2(1, 1), new SlimDX.Vector2(0.5f, 0) };
        public static readonly int[] UnitTriIndices = new int[] { 0, 1, 2 };
    }

    [Type(IsImmutable = true)]
    public class QuadDescriptor : GeometryDescriptor
    {
        public readonly Quad Settings;

        [Node]
        public QuadDescriptor()
            : base(PrimitiveType.Quad)
        {
            Settings = GeometryDescriptor.UnitQuad;
        }
    }

    [Type(IsImmutable = true)]
    public class RoundQuadDescriptor : GeometryDescriptor
    {
        public readonly RoundRect Settings;

        [Node]
        public RoundQuadDescriptor(float cornerRadius = 0.1f, bool enableCenter = true, int cornerResolution = 6)
            : base(PrimitiveType.RoundQuad)
        {
            var size = new SlimDX.Vector2(cornerRadius > 0 ? 0.5f - cornerRadius : 0.5f);
            Settings = new RoundRect() { InnerRadius = size, OuterRadius = cornerRadius * 0.5f, EnableCenter = enableCenter, CornerResolution = cornerResolution };
        }
    }

    [Type(IsImmutable = true)]
    public class BoxDescriptor : GeometryDescriptor
    {
        public readonly Box Settings;

        [Node]
        public BoxDescriptor()
            : base(PrimitiveType.Box)
        {
            Settings = GeometryDescriptor.UnitBox;
        }
    }

    [Type(IsImmutable = true)]
    public class DiscDescriptor : GeometryDescriptor
    {
        public readonly Segment Settings;

        [Node]
        public DiscDescriptor(float phase, float cycles = 1, float innerRadius = 0, int resolution = 15)
            : base(PrimitiveType.Disc)
        {
            Settings = new Segment() { Cycles = cycles, Phase = phase, InnerRadius = innerRadius, Resolution = resolution, Flat = false };
        }
    }

    [Type(IsImmutable = true)]
    public class SphereDescriptor : GeometryDescriptor
    {
        public readonly Sphere Settings;

        [Node]
        public SphereDescriptor(float cyclesX = 1, float cyclesY = 1, int resolutionX = 15, int resolutionY = 15)
            : base(PrimitiveType.Sphere)
        {
            Settings = new Sphere() { CyclesX = cyclesX, CyclesY = cyclesY, Radius = 0.5f, ResX = resolutionX, ResY = resolutionY };
        }
    }

    [Type(IsImmutable = true)]
    public class CylinderDescriptor : GeometryDescriptor
    {
        public readonly Cylinder Settings;

        [Node]
        public CylinderDescriptor(float cycles = 1, float radiusRatio = 0.5f, int resolutionX = 15, int resolutionY = 1, bool centerY = true, bool caps = true)
            : base(PrimitiveType.Cylinder)
        {
            Settings = new Cylinder() { Cycles = cycles, Radius1 = radiusRatio, Radius2 = 1-radiusRatio, ResolutionX = resolutionX, ResolutionY = resolutionY, Length = 1, CenterY = centerY, Caps = caps };
        }
    }

    [Type(IsImmutable = true)]
    public class TubeDescriptor : GeometryDescriptor
    {
        public readonly SegmentZ Settings;

        [Node]
        public TubeDescriptor(float phase = 0, float cycles = 1, float innerRadius = 0.5f, int resolution = 15)
            : base(PrimitiveType.Tube)
        {
            Settings = new SegmentZ() { Phase = phase, Cycles = cycles, InnerRadius = innerRadius, Resolution = resolution, Z = 0.5f };
        }
    }

    [Type(IsImmutable = true)]
    public class LineDescriptor : GeometryDescriptor
    {
        public readonly List<SlimDX.Vector3> Positions;
        public readonly List<SlimDX.Vector3> Directions;
        public readonly bool IsClosed;

        [Node]
        public LineDescriptor()
            : base(PrimitiveType.Line)
        {
            Positions = GeometryDescriptor.UnitLine;
            Directions = GeometryDescriptor.UnitLineNormals;
        }

        [Node(Version = "Poly")]
        public LineDescriptor(IReadOnlyList<SharpDX.Vector3> positions, bool isClosed)
            : base(PrimitiveType.Line)
        {
            IsClosed = isClosed;

            if (positions != null && positions.Count > 1)
            {
                Positions = positions.Select(v => new SlimDX.Vector3(v.X, v.Y, v.Z)).ToList();
                Directions = positions.Select(_ => new SlimDX.Vector3(0, 1, 0)).ToList();
            }
            else
            {
                Positions = GeometryDescriptor.UnitLine;
                Directions = GeometryDescriptor.UnitLineNormals;
            }
        }
    }

    [Type(IsImmutable = true)]
    public class MeshJoinDescriptor : GeometryDescriptor
    {
        public readonly List<SlimDX.Vector3> Positions;
        public readonly List<SlimDX.Vector3> Directions;
        public readonly List<SlimDX.Vector2> Tex;
        public readonly int[] Indices;
        public readonly MeshTopology Topology;

        //[Node]
        //public MeshJoinDescriptor()
        //    : base(PrimitiveType.MeshJoin)
        //{
        //    Positions = GeometryDescriptor.UnitTri;
        //    Directions = GeometryDescriptor.UnitTriNormals;
        //    Tex = GeometryDescriptor.UnitTriTex;
        //    Indices = GeometryDescriptor.UnitTriIndices;
        //}

        [Node]
        public MeshJoinDescriptor(IReadOnlyList<SharpDX.Vector3> positions, IReadOnlyList<SharpDX.Vector3> normals, IReadOnlyList<SharpDX.Vector2> textureCoords, IReadOnlyList<int> indices, MeshTopology topology = MeshTopology.TriangleList)
            : base(PrimitiveType.MeshJoin)
        {

            if (positions != null && positions.Count > 1)
            {
                Positions = positions.Select(v => new SlimDX.Vector3(v.X, v.Y, v.Z)).ToList();
                Directions = normals?.Select(v => new SlimDX.Vector3(v.X, v.Y, v.Z)).ToList() ?? GeometryDescriptor.UnitTriNormals;
                Tex = textureCoords?.Select(v => new SlimDX.Vector2(v.X, v.Y)).ToList() ?? GeometryDescriptor.UnitTriTex;
                Indices = indices?.ToArray() ?? GeometryDescriptor.UnitTriIndices;
            }
            else
            {
                Positions = GeometryDescriptor.UnitTri;
                Directions = GeometryDescriptor.UnitTriNormals;
                Tex = GeometryDescriptor.UnitTriTex;
                Indices = GeometryDescriptor.UnitTriIndices;
            }

            Topology = topology;
        }
    }

    [Type(IsImmutable = true)]
    public class SpritesDescriptor : GeometryDescriptor
    {
        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly SpritesDescriptor Default = new SpritesDescriptor();

        [Node]
        public SpritesDescriptor()
            : base(PrimitiveType.Sprites)
        {
        }
    }

    [Type(IsImmutable = true)]
    public class TextDescriptor : GeometryDescriptor
    {
        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly TextDescriptor Default = new TextDescriptor("CraftLie", "Arial", 32, 0, TextAlignment.Center, ParagraphAlignment.Center);

        public readonly string Text;

        public readonly string FontName;
        public readonly float FontSize;
        public readonly float Extrude;
        public readonly TextAlignment TextAlignment;
        public readonly ParagraphAlignment ParagraphAlignment;

        [Node]
        public TextDescriptor(string text = "CraftLie", string fontName = "Arial", float fontSize = 32, float extrude = 0, TextAlignment textAlignment = TextAlignment.Center, ParagraphAlignment paragraphAlignment = ParagraphAlignment.Center)
            : base(PrimitiveType.Text)
        {
            Text = text;
            FontName = fontName;
            FontSize = fontSize;
            Extrude = extrude;
            TextAlignment = textAlignment;
            ParagraphAlignment = paragraphAlignment;
        }
    }

}