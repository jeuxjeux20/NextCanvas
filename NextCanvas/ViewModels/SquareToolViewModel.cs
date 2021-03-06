﻿using System;
using System.Windows.Ink;
using System.Windows.Input;
using NextCanvas.Ink;

namespace NextCanvas.ViewModels
{
    public class SquareToolViewModel : StrokeToolViewModel
    {
        public SquareToolViewModel(SquareTool model = null, Uri icon = null) : base(model, icon)
        {
            DemoHeight = 70;
            DemoWidth = 90;
        }

        public new SquareTool Model => (SquareTool)base.Model;
        protected override Tool BuildDefaultModel()
        {
            return new SquareTool();
        }

        private int _marginWidth = 4;

        public int MarginWidth
        {
            get => _marginWidth;
            set { _marginWidth = value; OnPropertyChanged(nameof(MarginWidth)); OnPropertyChanged(nameof(DemoStroke)); }
        }
        private int _marginHeight = 15;

        public int MarginHeight
        {
            get => _marginHeight;
            set { _marginHeight = value; OnPropertyChanged(nameof(MarginHeight)); OnPropertyChanged(nameof(DemoStroke)); }
        }

        public override StrokeCollection DemoStroke => new StrokeCollection
        {
            new Stroke(new StylusPointCollection
            {
                new StylusPoint(MarginWidth, MarginHeight),
                new StylusPoint(DemoWidth - MarginWidth, MarginHeight),
                new StylusPoint(DemoWidth - MarginWidth, DemoHeight - MarginHeight),
                new StylusPoint(MarginWidth, DemoHeight - MarginHeight),
                new StylusPoint(MarginWidth, MarginHeight)
            }, DrawingAttributes.SetFitToCurve(false))
        };
    }
}
