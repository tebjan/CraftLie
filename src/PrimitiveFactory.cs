using FeralTic.DX11;
using FeralTic.DX11.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeralTic.DX11.Geometry;

namespace CraftLie
{
    public static class PrimitiveFactory
    {
        public static IDX11Geometry GetGeometry(DX11RenderContext context, GeometryDescriptor descriptor)
        {
            switch (descriptor.Type)
            {
                case PrimitiveType.Quad:
                    return context.Primitives.QuadNormals(((QuadDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.RoundQuad:
                    return context.Primitives.RoundRect(((RoundQuadDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Box:
                    return context.Primitives.Box(((BoxDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Disc:
                    return context.Primitives.Segment(((DiscDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Sphere:
                    return context.Primitives.Sphere(((SphereDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Tube:
                    return context.Primitives.SegmentZ(((TubeDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Line:
                    return context.Primitives.LineStrip3d(((LineDescriptor)descriptor).Settings, false);
                    break;
                default:
                    var settings = new Quad() { Size = new SlimDX.Vector2(1) };
                    return context.Primitives.QuadNormals(settings);
                    break;
            }

            
        }
    }
}
