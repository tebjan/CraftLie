using System;
using System.Linq;
using System.Collections.Generic;
using FeralTic.DX11.Geometry;
using VL.Core;
using SharpDX;

namespace CraftLie
{

    public class DrawTextDescription : DrawDescription
    {
        public readonly string Text;
        public readonly float Size;
        public readonly string FontName;

        public static readonly DrawTextDescription Default = new DrawTextDescription(Matrix.Identity, Color4.White);

        public DrawTextDescription(Matrix transformation, Color4 color, BlendMode blendMode = BlendMode.TextDefault, string text = "CraftLie", float size = 32, string fontName = "Arial")
        {
            Text = text;
            Size = size;
            FontName = fontName;
            Color = color;
            Transformation = transformation;
            Blending = blendMode;
        }

        public void SetLayerOrder(int layerOrder)
        {
            LayerOrder = layerOrder;
        }
    }

}