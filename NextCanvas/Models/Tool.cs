﻿#region

using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

#endregion

namespace NextCanvas
{
    public class Tool : INamedObject
    {
        private bool? _hasDemo;

        public Tool()
        {
            DrawingAttributes.FitToCurve = true;
            DrawingAttributes.Color = Group.Color;
        }
        public DrawingAttributes DrawingAttributes { get; set; } = new DrawingAttributes();
        public bool HasColor { get; set; } = true;
        public string Name { get; set; } = "Tool";
        public object LargeIcon { get; set; }
        public object SmallIcon { get; set; }
        public ToolGroup Group { get; set; } = new ToolGroup();
        public Cursor Cursor { get; set; } = Cursors.Pen;

        public bool HasDemo
        {
            get => _hasDemo ?? HasColor;
            set => _hasDemo = value;
        }

        public bool HasSize => Mode != InkCanvasEditingMode.None && Mode != InkCanvasEditingMode.Select;
        public InkCanvasEditingMode Mode { get; set; } = InkCanvasEditingMode.Ink;
    }
}