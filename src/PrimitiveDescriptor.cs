using FeralTic.DX11.Geometry;
using VL.Core;

namespace CraftLie
{
    public enum PrimitiveType
    {
        Quad,
        Box,
        Disc,
        Sphere,
        Tube
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
        public DiscDescriptor(float phase, float cycles = 1, float innerRadius = 0, int resolution = 12)
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
        public SphereDescriptor(float cyclesX = 1, float cyclesY = 1, int resolutionX = 12, int resolutionY = 12)
            : base(PrimitiveType.Sphere)
        {
            Settings = new Sphere() { CyclesX = cyclesX, CyclesY = cyclesY, Radius = 1, ResX = resolutionX, ResY = resolutionY };
        }
    }

    [Type(IsImmutable = true)]
    public class TubeDescriptor : GeometryDescriptor
    {
        public readonly SegmentZ Settings;

        [Node]
        public TubeDescriptor(float phase = 0, float cycles = 1, float innerRadius = 0.5f, int resolution = 12)
            : base(PrimitiveType.Tube)
        {
            Settings = new SegmentZ() { Phase = phase, Cycles = cycles, InnerRadius = innerRadius, Resolution = resolution, Z = 1 };
        }
    }
}