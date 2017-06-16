using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DWriteFactory = SharpDX.DirectWrite.Factory;
using D2DFactory = SharpDX.Direct2D1.Factory;
using D2DGeometry = SharpDX.Direct2D1.Geometry;
using SharpDX.DirectWrite;
using SharpDX;
using VVVV.DX11.Nodes;
using SharpDX.Direct2D1;

namespace CraftLie
{
    public class Extruder
    {
        private D2DFactory factory;

        public Extruder(D2DFactory factory)
        {
            this.factory = factory;
        }

        const float sc_flatteningTolerance = .1f;

        private D2DGeometry FlattenGeometry(D2DGeometry geometry, float tolerance)
        {
            PathGeometry path = new PathGeometry(this.factory);

            GeometrySink sink = path.Open();

            geometry.Simplify(GeometrySimplificationOption.Lines, tolerance, sink);

            sink.Close();
            sink.Dispose();

            return path;
        }

        private D2DGeometry OutlineGeometry(D2DGeometry geometry)
        {
            PathGeometry path = new PathGeometry(this.factory);

            GeometrySink sink = path.Open();

            geometry.Outline(sink);

            sink.Close();
            sink.Dispose();

            return path;
        }


        public IEnumerable<Pos3Norm3VertexSDX> GetVertices(D2DGeometry geometry, float height = 1f)
        {

            List<Pos3Norm3VertexSDX> vertices = new List<Pos3Norm3VertexSDX>();

            //Empty mesh
            if (geometry == null)
            {
                Pos3Norm3VertexSDX zero = new Pos3Norm3VertexSDX();
                vertices.Add(zero);
                vertices.Add(zero);
                vertices.Add(zero);
                return vertices;
            }

            if (height > 0)
            {
                D2DGeometry flattenedGeometry = this.FlattenGeometry(geometry, sc_flatteningTolerance);
                D2DGeometry outlinedGeometry = this.OutlineGeometry(flattenedGeometry);

                //Add snap here

                var sink = new ExtrudingSink(vertices, height);

                outlinedGeometry.Simplify(GeometrySimplificationOption.Lines, sink);

                var bounds = flattenedGeometry.GetBounds();
                var scaling = Math.Min(1 / Math.Abs(bounds.Bottom - bounds.Top), 1);

                outlinedGeometry.Tessellate(sink);

                outlinedGeometry.Dispose();
                flattenedGeometry.Dispose();

                sink.Dispose();

                return vertices.Select(v => v.Scale(scaling).AssignTexCd()).Reverse(); 
            }
            else
            {
                D2DGeometry flattenedGeometry = this.FlattenGeometry(geometry, sc_flatteningTolerance);

                //Add snap here
                var sink = new FlatSink(vertices);

                var bounds = flattenedGeometry.GetBounds();
                var scaling = Math.Min(1 / Math.Abs(bounds.Bottom - bounds.Top), 1);

                flattenedGeometry.Tessellate(sink);

                flattenedGeometry.Dispose();
                sink.Dispose();

                return vertices.Select(v => v.Scale(scaling).AssignTexCd()).Reverse();
            }
        }
    }


}
