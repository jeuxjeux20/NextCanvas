﻿using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;

namespace NextCanvas.Controls
{
    public class NextDynamicRenderer : DynamicRenderer
    {
        // ReSharper disable once RedundantOverriddenMember
        protected override void OnDraw(
            DrawingContext drawingContext,
            StylusPointCollection stylusPoints,
            Geometry geometry,
            Brush fillBrush)
        {
            //if (stylusPoints.Count >= 2)
            //{
            //    StylusPoint minX, minY, maxX, maxY;
            //    Ink.SquareStroke.GetMinPoints(stylusPoints, out minX, out minY, out maxX, out maxY);
            //    drawingContext.DrawRectangle(null, new Pen(fillBrush, 2), new System.Windows.Rect(new System.Windows.Point(minX.X, minY.Y), new System.Windows.Point(maxX.X, maxY.Y)));
            //}
            base.OnDraw(drawingContext, stylusPoints, geometry, fillBrush);
        }
    }
}