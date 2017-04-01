using FeralTic.DX11.Geometry;
using System.Collections.Generic;
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
        Sprites
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
            Settings = new Sphere() { CyclesX = cyclesX, CyclesY = cyclesY, Radius = 1, ResX = resolutionX, ResY = resolutionY };
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
            Settings = new SegmentZ() { Phase = phase, Cycles = cycles, InnerRadius = innerRadius, Resolution = resolution, Z = 1 };
        }
    }

    [Type(IsImmutable = true)]
    public class LineDescriptor : GeometryDescriptor
    {
        public readonly List<SlimDX.Vector3> Settings;

        [Node]
        public LineDescriptor()
            : base(PrimitiveType.Line)
        {
            Settings = GeometryDescriptor.UnitLine;
        }
    }

    [Type(IsImmutable = true)]
    public class SpritesDescriptor : GeometryDescriptor
    {
        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly SpritesDescriptor Default = new SpritesDescriptor();

        public int SpriteCount;

        [Node]
        public SpritesDescriptor()
            : base(PrimitiveType.Sprites)
        {
        }
    }
}