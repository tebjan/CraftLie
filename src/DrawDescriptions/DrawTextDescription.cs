using System;
using System.Linq;
using System.Collections.Generic;
using FeralTic.DX11.Geometry;
using VL.Core;
using SharpDX;

namespace CraftLie
{
    public enum VerticalTextAlignment
    {
        //
        // Summary:
        //     The top of the text flow is aligned to the top edge of the layout box.
        Top = 0,
        //
        // Summary:
        //     The bottom of the text flow is aligned to the bottom edge of the layout box.
        Bottom = 1,
        //
        // Summary:
        //     The center of the flow is aligned to the center of the layout box.
        Center = 2
    }

    public enum HorizontalTextAlignment
    {
        //
        // Summary:
        //     The leading edge of the paragraph text is aligned to the leading edge of the
        //     layout box.
        Left = 0,
        //
        // Summary:
        //     The trailing edge of the paragraph text is aligned to the trailing edge of the
        //     layout box.
        Right = 1,
        //
        // Summary:
        //     The center of the paragraph text is aligned to the center of the layout box.
        Center = 2,
        //
        // Summary:
        //     Align text to the leading side, and also justify text to fill the lines.
        Justified = 3
    }

    public enum FontWeight
    {
        Thin = 100,
        UltraLight = 200,
        ExtraLight = 200,
        Light = 300,
        Normal = 400,
        Regular = 400,
        Medium = 500,
        SemiBold = 600,
        DemiBold = 600,
        Bold = 700,
        UltraBold = 800,
        ExtraBold = 800,
        Heavy = 900,
        Black = 900,
        UltraBlack = 950,
        ExtraBlack = 950
    }

    public enum FontStyle
    {
        Normal = 0,
        Oblique = 1,
        Italic = 2
    }

    public class DrawTextDescription : DrawDescription
    {
        public string Text;
        public float Size;
        public string FontName;
        public FontWeight Weight;
        public FontStyle Style;
        public HorizontalTextAlignment HorizontalAlignment;
        public VerticalTextAlignment VerticalAlignment;
        public float TextWidth;


        public static readonly DrawTextDescription Default = new DrawTextDescription(Matrix.Identity, Color4.White);

        public DrawTextDescription DeepCopy()
        {
            return new DrawTextDescription(this.Transformation, this.Color, this.Blending, this.Text, this.Size, this.FontName, this.Weight, this.Style, this.HorizontalAlignment, this.VerticalAlignment, this.TextWidth);
        }

        public DrawTextDescription(Matrix transformation, Color4 color, BlendMode blendMode = BlendMode.TextDefault,
            string text = "CraftLie",
            float size = 12,
            string fontName = "Arial",
            FontWeight weight = FontWeight.Normal,
            FontStyle style = FontStyle.Normal,
            HorizontalTextAlignment horizontalAlignment = HorizontalTextAlignment.Left,
            VerticalTextAlignment verticalAlignment = VerticalTextAlignment.Top,
            float textWidth = 100)
        {

            Transformation = transformation;
            Color = color;
            Blending = blendMode;

            Text = text;
            Size = size;
            FontName = fontName;
            Weight = weight;
            Style = style;
            HorizontalAlignment = horizontalAlignment;
            VerticalAlignment = verticalAlignment;
            TextWidth = textWidth;
        }

        public void Update(Matrix transformation, Color4 color, BlendMode blendMode = BlendMode.TextDefault,
            string text = "CraftLie",
            float size = 12,
            string fontName = "Arial",
            FontWeight weight = FontWeight.Normal,
            FontStyle style = FontStyle.Normal,
            HorizontalTextAlignment horizontalAlignment = HorizontalTextAlignment.Left,
            VerticalTextAlignment verticalAlignment = VerticalTextAlignment.Top,
            float textWidth = 100)
        {

            Transformation = transformation;
            Color = color;
            Blending = blendMode;

            Text = text;
            Size = size;
            FontName = fontName;
            Weight = weight;
            Style = style;
            HorizontalAlignment = horizontalAlignment;
            VerticalAlignment = verticalAlignment;
            TextWidth = textWidth;
        }

        public void SetLayerOrder(int layerOrder)
        {
            LayerOrder = layerOrder;
        }
    }
}