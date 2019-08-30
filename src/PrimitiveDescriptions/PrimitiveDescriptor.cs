using FeralTic.DX11.Geometry;
using System;
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
        Polygon,
        Sphere,
        Cylinder,
        Tube,
        Line,
        MeshJoin,
        Sprites,
        Text
    }

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

    public abstract class GeometryDescriptor
    {
        public Guid GeomId = Guid.NewGuid();
        public readonly PrimitiveType Type;
        public GeometryDescriptor(PrimitiveType type)
        {
            Type = type;
        }

        public static readonly Quad UnitQuad = new Quad() { Size = new SlimDX.Vector2(1) };
        public static readonly Box UnitBox = new Box() { Size = new SlimDX.Vector3(1) };

        public static readonly List<SlimDX.Vector2> UnitTri2D = new List<SlimDX.Vector2>(3) { new SlimDX.Vector2(-0.5f, -0.5f), new SlimDX.Vector2(0.5f, -0.5f), new SlimDX.Vector2(0, 0.5f) };

        public static readonly List<SlimDX.Vector3> UnitLine = new List<SlimDX.Vector3>(2) { new SlimDX.Vector3(-0.5f, 0, 0), new SlimDX.Vector3(0.5f, 0, 0) };
        public static readonly List<SlimDX.Vector3> UnitLineNormals = new List<SlimDX.Vector3>(2) { new SlimDX.Vector3(0, 1, 0), new SlimDX.Vector3(0, 1, 0) };

        public static readonly List<SlimDX.Vector3> UnitTri = new List<SlimDX.Vector3>(3) { new SlimDX.Vector3(-0.5f, -0.5f, 0), new SlimDX.Vector3(0.5f, -0.5f, 0), new SlimDX.Vector3(0, 0.5f, 0) };

        public virtual GeometryDescriptor DeepCopy() { throw new NotImplementedException(); }

        public static readonly List<SlimDX.Vector3> UnitTriNormals = new List<SlimDX.Vector3>(3) { new SlimDX.Vector3(0, -1, 0), new SlimDX.Vector3(0, -1, 0), new SlimDX.Vector3(0, -1, 0) };

        public static readonly List<SlimDX.Vector2> UnitTriTex = new List<SlimDX.Vector2>(3) { new SlimDX.Vector2(0, 1), new SlimDX.Vector2(1, 1), new SlimDX.Vector2(0.5f, 0) };
        public static readonly int[] UnitTriIndices = new int[] { 0, 1, 2 };
    }

    public class QuadDescriptor : GeometryDescriptor
    {
        public readonly Quad Settings;

        public QuadDescriptor()
            : base(PrimitiveType.Quad)
        {
            Settings = GeometryDescriptor.UnitQuad;
        }

        public override GeometryDescriptor DeepCopy()
        {
            return new QuadDescriptor();
        }
    }

    public class RoundQuadDescriptor : GeometryDescriptor
    {
        public readonly RoundRect Settings;

        public RoundQuadDescriptor(float cornerRadius = 0.1f, float aspect = 1, bool enableCenter = true, int cornerResolution = 6)
            : base(PrimitiveType.RoundQuad)
        {
            var radius = cornerRadius > 0 ? 0.5f - cornerRadius : 0.5f;
            var size = aspect > 1 ? new SlimDX.Vector2(radius, radius / aspect) : new SlimDX.Vector2(radius * aspect, radius);
            Settings = new RoundRect() { InnerRadius = size, OuterRadius = cornerRadius * 0.5f, EnableCenter = enableCenter, CornerResolution = cornerResolution };
        }
    }

    public class BoxDescriptor : GeometryDescriptor
    {
        public readonly Box Settings;

        public BoxDescriptor()
            : base(PrimitiveType.Box)
        {
            Settings = GeometryDescriptor.UnitBox;
        }
    }

    public class DiscDescriptor : GeometryDescriptor
    {
        public readonly Segment Settings;

        public DiscDescriptor(float phase, float cycles = 1, float innerRadius = 0, int resolution = 15)
            : base(PrimitiveType.Disc)
        {
            Settings = new Segment() { Cycles = cycles, Phase = phase, InnerRadius = innerRadius, Resolution = resolution, Flat = false };
        }
    }

    public class SphereDescriptor : GeometryDescriptor
    {
        public readonly Sphere Settings;

        public SphereDescriptor(float cyclesX = 1, float cyclesY = 1, int resolutionX = 15, int resolutionY = 15)
            : base(PrimitiveType.Sphere)
        {
            Settings = new Sphere() { CyclesX = cyclesX, CyclesY = cyclesY, Radius = 0.5f, ResX = resolutionX, ResY = resolutionY };
        }
    }

    public class CylinderDescriptor : GeometryDescriptor
    {
        public readonly Cylinder Settings;

        public CylinderDescriptor(float cycles = 1, float radiusRatio = 0.5f, int resolutionX = 15, int resolutionY = 1, bool centerY = true, bool caps = true)
            : base(PrimitiveType.Cylinder)
        {
            Settings = new Cylinder() { Cycles = cycles, Radius1 = radiusRatio, Radius2 = 1-radiusRatio, ResolutionX = resolutionX, ResolutionY = resolutionY, Length = 1, CenterY = centerY, Caps = caps };
        }
    }

    public class TubeDescriptor : GeometryDescriptor
    {
        public readonly SegmentZ Settings;

        public TubeDescriptor(float phase = 0, float cycles = 1, float innerRadius = 0.5f, int resolution = 15)
            : base(PrimitiveType.Tube)
        {
            Settings = new SegmentZ() { Phase = phase, Cycles = cycles, InnerRadius = innerRadius, Resolution = resolution, Z = 0.5f };
        }
    }


    public class LineDescriptor : GeometryDescriptor
    {
        public readonly List<SlimDX.Vector3> Positions;
        public readonly List<SlimDX.Vector3> Directions;
        public readonly bool IsClosed;

        public LineDescriptor()
            : base(PrimitiveType.Line)
        {
            Positions = GeometryDescriptor.UnitLine.ToList();
            Directions = GeometryDescriptor.UnitLineNormals.ToList();
        }

        public LineDescriptor(IReadOnlyList<Xenko.Core.Mathematics.Vector3> positions, bool isClosed)
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

        public override GeometryDescriptor DeepCopy()
        {
            return new LineDescriptor(Positions.ToList(), Directions.ToList(), IsClosed);
        }

        private LineDescriptor(List<SlimDX.Vector3> positions, List<SlimDX.Vector3> normals, bool isClosed)
            : base(PrimitiveType.Line)
        {

        }
    }

    public class PolygonDescriptor : GeometryDescriptor
    {
        public readonly List<SlimDX.Vector2> Positions;

        public PolygonDescriptor() : base(PrimitiveType.Polygon)
        {
            Positions = GeometryDescriptor.UnitTri2D.ToList();
        }

        public PolygonDescriptor(IReadOnlyList<Xenko.Core.Mathematics.Vector2> positions)
            : base(PrimitiveType.Polygon)
        {
            if (positions != null && positions.Count > 1)
            {
                Positions = positions.Select(v => new SlimDX.Vector2(v.X, v.Y)).ToList();
            }
            else
            {
                Positions = GeometryDescriptor.UnitTri2D;
            }
        }

        public override GeometryDescriptor DeepCopy()
        {
            return new PolygonDescriptor(Positions.ToList());

        }

        private PolygonDescriptor(List<SlimDX.Vector2> positions)
            : base(PrimitiveType.Polygon)
        {
            Positions = positions;
        }
    }

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

        public MeshJoinDescriptor(IReadOnlyList<Xenko.Core.Mathematics.Vector3> positions, IReadOnlyList<Xenko.Core.Mathematics.Vector3> normals, IReadOnlyList<Xenko.Core.Mathematics.Vector2> textureCoords, IReadOnlyList<int> indices, MeshTopology topology = MeshTopology.TriangleList)
            : base(PrimitiveType.MeshJoin)
        {

            if (positions != null && positions.Count > 1)
            {
                Positions = positions.Select(v => new SlimDX.Vector3(v.X, v.Y, v.Z)).ToList();
                Directions = normals != null && normals.Any() ? normals.Select(v => new SlimDX.Vector3(v.X, v.Y, v.Z)).ToList() : GeometryDescriptor.UnitTriNormals;
                Tex = textureCoords != null && textureCoords.Any() ? textureCoords.Select(v => new SlimDX.Vector2(v.X, v.Y)).ToList() : GeometryDescriptor.UnitTriTex;
                Indices = indices != null && indices.Any() ? indices.ToArray() : GeometryDescriptor.UnitTriIndices;
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

    public class SpritesDescriptor : GeometryDescriptor
    {
        public static readonly SpritesDescriptor Default = new SpritesDescriptor();

        public SpritesDescriptor()
            : base(PrimitiveType.Sprites)
        {
        }
    }

    public class TextDescriptor : GeometryDescriptor
    {
        public static readonly TextDescriptor Default = new TextDescriptor("CraftLie", "Arial", 32, 0);

        public readonly string Text;

        public readonly string FontName;
        public readonly float FontSize;
        public readonly float Extrude;
        public readonly HorizontalTextAlignment TextAlignment;
        public readonly VerticalTextAlignment ParagraphAlignment;

        public TextDescriptor(string text = "CraftLie", string fontName = "Arial", float fontSize = 32, float extrude = 0, HorizontalTextAlignment textAlignment = HorizontalTextAlignment.Center, VerticalTextAlignment paragraphAlignment = VerticalTextAlignment.Center)
            : base(PrimitiveType.Text)
        {
            Text = text;
            FontName = fontName;
            FontSize = fontSize;
            Extrude = extrude;
            TextAlignment = textAlignment;
            ParagraphAlignment = paragraphAlignment;
        }

        public override GeometryDescriptor DeepCopy()
        {
            return new TextDescriptor(Text, FontName, FontSize, Extrude, TextAlignment, ParagraphAlignment);
        }
    }

}