using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using InputElement = SlimDX.Direct3D11.InputElement;

namespace CraftLie
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Pos3Norm3VertexSDX
    {
        public Vector3 Position;
        public Vector3 Normals;
        public Vector2 TexCoord;

        private static SlimDX.Direct3D11.InputElement[] layout;

        public static SlimDX.Direct3D11.InputElement[] Layout
        {
            get
            {
                if (layout == null)
                {
                    layout = new InputElement[]
                    {
                            new InputElement("POSITION", 0, SlimDX.DXGI.Format.R32G32B32_Float, 0, 0),
                            new InputElement("NORMAL", 0, SlimDX.DXGI.Format.R32G32B32_Float, 12, 0),
                            new InputElement("TEXCOORD", 0, SlimDX.DXGI.Format.R32G32_Float, 24, 0),
                    };
                }
                return layout;
            }
        }

        public static int VertexSize
        {
            get { return Marshal.SizeOf(typeof(Pos3Norm3VertexSDX)); }
        }

        internal Pos3Norm3VertexSDX Scale(float scaling)
        {
            Vector3.Multiply(ref Position, scaling, out Position);
            return this;
        }

        internal Pos3Norm3VertexSDX AssignTexCd()
        {
            TexCoord = (Vector2)Position;
            return this;
        }
    }
}