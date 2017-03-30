using System;
using System.Linq;
using System.Collections.Generic;
using FeralTic.DX11.Geometry;
using VL.Core;
using SharpDX;

namespace CraftLie
{

    [Type(IsImmutable = true)]
    public class TextDescriptor
    {
        public readonly string Text;
        public readonly float Size;
        public readonly string FontName;
        public readonly Color4 Color;
        public readonly Matrix Transformation;

        [Node(Hidden = true, IsDefaultValue = true)]
        public static readonly TextDescriptor Default = new TextDescriptor(Matrix.Identity, Color4.White);

        [Node]
        public TextDescriptor(Matrix transformation, Color4 color, string text = "CraftLie", float size = 32, string fontName = "Arial")
        {
            Text = text;
            Size = size;
            FontName = fontName;
            Color = color;
            Transformation = transformation;
        }
    }

}