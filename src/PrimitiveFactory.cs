using FeralTic.DX11;
using FeralTic.DX11.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeralTic.DX11.Geometry;
using SlimDX;
using SlimDX.Direct3D11;

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
                case PrimitiveType.Cylinder:
                    return context.Primitives.Cylinder(((CylinderDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Tube:
                    return context.Primitives.SegmentZ(((TubeDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Line:
                    var lineDesc = (LineDescriptor)descriptor;
                    return LineStrip3d(context, lineDesc.Positions, lineDesc.Directions, false);
                    break;
                case PrimitiveType.Sprites:
                    return CreateNullGeometry(context);
                    break;
                default:
                    var settings = new Quad() { Size = new SlimDX.Vector2(1) };
                    return context.Primitives.QuadNormals(settings);
                    break;
            }
        }

        private static IDX11Geometry CreateNullGeometry(DX11RenderContext context)
        {
            DX11NullGeometry geom = new DX11NullGeometry(context);
            geom.Topology = SlimDX.Direct3D11.PrimitiveTopology.PointList;
            geom.InputLayout = new SlimDX.Direct3D11.InputElement[0];
            geom.HasBoundingBox = false;

            return geom;
        }

        public static DX11VertexGeometry LineStrip3d(DX11RenderContext context, List<Vector3> points, List<Vector3> directions, bool loop)
        {
            //Use direction verctor as normal, useful when we have analytical derivatives for direction
            DX11VertexGeometry geom = new DX11VertexGeometry(context);

            int ptcnt = Math.Max(points.Count, directions.Count);

            int vcount = loop ? ptcnt + 1 : ptcnt;

            Pos3Norm3Tex2Vertex[] verts = new Pos3Norm3Tex2Vertex[vcount];

            float inc = loop ? 1.0f / (float)vcount : 1.0f / ((float)vcount + 1.0f);

            float curr = 0.0f;


            for (int i = 0; i < ptcnt; i++)
            {
                verts[i].Position = points[i % points.Count];
                verts[i].Normals = directions[i % directions.Count];
                verts[i].TexCoords.X = curr;
                curr += inc;
            }

            if (loop)
            {
                verts[ptcnt].Position = points[0];
                verts[ptcnt].Normals = directions[0];
                verts[ptcnt].TexCoords.X = 1.0f;
            }


            DataStream ds = new DataStream(vcount * Pos3Norm3Tex2Vertex.VertexSize, true, true);
            ds.Position = 0;
            ds.WriteRange(verts);
            ds.Position = 0;

            var vbuffer = new SlimDX.Direct3D11.Buffer(context.Device, ds, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = (int)ds.Length,
                Usage = ResourceUsage.Default
            });

            ds.Dispose();

            geom.VertexBuffer = vbuffer;
            geom.InputLayout = Pos3Norm3Tex2Vertex.Layout;
            geom.Topology = PrimitiveTopology.LineStrip;
            geom.VerticesCount = vcount;
            geom.VertexSize = Pos3Norm3Tex2Vertex.VertexSize;

            geom.HasBoundingBox = false;

            return geom;
        }
    }
}
