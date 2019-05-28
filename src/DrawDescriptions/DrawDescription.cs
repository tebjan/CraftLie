using FeralTic.DX11;
using FeralTic.DX11.Geometry;
using FeralTic.DX11.Resources;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace CraftLie
{
    public enum TransformationSpace
    {
        World,
        View,
        Projection
    }

    public enum BlendMode
    {
        Disabled,
        Add,
        Blend,
        Multiply,
        AlphaAdd,
        TextDefault,
        Keep
    }

    public class DrawDescription : IDisposable
    {
        public GeometryDescriptor GeometryDescriptor;
        public Guid DrawId = Guid.NewGuid();
        public Matrix Transformation;
        public Color4 Color;
        public TransformationSpace Space = TransformationSpace.World;
        public BlendMode Blending = BlendMode.Blend;
        public string TexturePath;
        public int LayerOrder;

        public IDX11Geometry GetGeometry(DX11RenderContext context)
        {
            IDX11Geometry geo;
            if (!GeometryCache.TryGetValue(DrawId, out var cache))
            {
                GeometryCache[DrawId] = new Dictionary<DX11RenderContext, IDX11Geometry>();
                geo = CreateGeo(context);
            }
            else
            {
                if (!cache.TryGetValue(context, out geo))
                {
                    geo = CreateGeo(context);
                }
            }    

            return geo;
        }

        protected IDX11Geometry CreateGeo(DX11RenderContext context)
        {
            IDX11Geometry geo = PrimitiveFactory.GetGeometry(context, GeometryDescriptor);
            GeometryCache[DrawId][context] = geo;
            return geo;
        }

        public void SetSpace(TransformationSpace space)
        {
            Space = space;
        }

        public void Transform(Matrix transformation)
        {
            Matrix.Multiply(ref Transformation, ref transformation, out Transformation);

            //var pos = new Vector4(ClipRect.X, ClipRect.Y, 0, 1);
            //var size = new Vector4(ClipRect.Width, ClipRect.Height, 0, 0);

            //Vector4.Transform(ref pos, ref transformation, out pos);
            //Vector4.Transform(ref size, ref transformation, out size);

            //ClipRect = new RectangleF(pos.X, pos.Y, size.X, size.Y);
        }

        public void Dispose()
        {
            DisposeGeometry();
            GeometryCache.Remove(DrawId);
        }

        protected void DisposeGeometry()
        {
            try
            {
                if (GeometryCache.TryGetValue(DrawId, out var cache))
                {
                    foreach(var geo in cache.Values)
                    {
                        try
                        {
                            geo?.Dispose();
                        }
                        catch (Exception)
                        {
                            //safe dispose
                        }
                        cache.Clear();
                    }
                }

            }
            catch (Exception)
            {
                //safe dispose
            }
        }

        static readonly Dictionary<Guid, Dictionary<DX11RenderContext, IDX11Geometry>> GeometryCache = new Dictionary<Guid, Dictionary<DX11RenderContext, IDX11Geometry>>();
    }

}
