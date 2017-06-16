using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.DX11.Nodes;

namespace CraftLie
{
    public class FlatSink : GeometrySink, TessellationSink
    {
        private List<Pos3Norm3VertexSDX> vertices;

        public FlatSink(List<Pos3Norm3VertexSDX> vertices)
        {
            this.vertices = vertices;
        }

        public void AddBeziers(BezierSegment[] beziers)
        {

        }

        public void AddLines(SharpDX.Mathematics.Interop.RawVector2[] pointsRef)
        {

        }

        public void BeginFigure(SharpDX.Mathematics.Interop.RawVector2 startPoint, FigureBegin figureBegin)
        {
        }

        public void Close()
        {

        }

        public void EndFigure(FigureEnd figureEnd)
        {
           
        }

        public void SetFillMode(FillMode fillMode)
        {

        }

        public void SetSegmentFlags(PathSegment vertexFlags)
        {

        }

        public IDisposable Shadow
        {
            get;
            set;
        }

        public void Dispose()
        {
            Shadow.Dispose();
        }

        public void AddTriangles(Triangle[] triangles)
        {
            for (int i = 0; i < triangles.Length; i++)
            {
                Triangle tri = triangles[i];

                vertices.Add(new Pos3Norm3VertexSDX() { Position = new Vector3(tri.Point1.X, tri.Point1.Y, 0), Normals = new Vector3(0.0f, 0.0f, 1.0f) });
                vertices.Add(new Pos3Norm3VertexSDX() { Position = new Vector3(tri.Point2.X, tri.Point2.Y, 0), Normals = new Vector3(0.0f, 0.0f, 1.0f) });
                vertices.Add(new Pos3Norm3VertexSDX() { Position = new Vector3(tri.Point3.X, tri.Point3.Y, 0), Normals = new Vector3(0.0f, 0.0f, 1.0f) });
            }


        }

        public void AddArc(ArcSegment arc)
        {

        }

        public void AddBezier(BezierSegment bezier)
        {

        }

        public void AddLine(SharpDX.Mathematics.Interop.RawVector2 point)
        {

        }

        public void AddQuadraticBezier(QuadraticBezierSegment bezier)
        {

        }

        public void AddQuadraticBeziers(QuadraticBezierSegment[] beziers)
        {

        }
    }

}
