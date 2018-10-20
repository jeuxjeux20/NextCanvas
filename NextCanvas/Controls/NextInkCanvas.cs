﻿using NextCanvas.Controls.Content;
using NextCanvas.Ink;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace NextCanvas.Controls
{
    /// <summary>
    /// Logique d'interaction pour NextInkCanvas.xaml
    /// lol i'm funny i called it NEXT XDDDDDD
    /// </summary>
    public class NextInkCanvas : InkCanvas
    {
        public NextInkCanvas()
        {
            DynamicRenderer = new NextDynamicRenderer();
            PreferredPasteFormats = new List<InkCanvasClipboardFormat>
            {
                InkCanvasClipboardFormat.InkSerializedFormat,
                InkCanvasClipboardFormat.Xaml,
                InkCanvasClipboardFormat.Text
            };
            CommandManager.RegisterClassCommandBinding(typeof(NextInkCanvas), new CommandBinding(ApplicationCommands.Delete, DeleteCommandExecuted, CanExecuteDeleteCommand));
        }

        private static void DeleteCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var canvas = (NextInkCanvas) sender;
            if (e.Command == ApplicationCommands.Delete)
            {
                canvas.DeleteSelection();
            }
        }

        private static void CanExecuteDeleteCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            var canvas = (NextInkCanvas) sender;
            if (e.Command == ApplicationCommands.Delete)
            {
                e.CanExecute = canvas.GetSelectedElements().Any() || canvas.GetSelectedStrokes().Any();
            }
        }
        public void DeleteSelection()
        {
            var list = GetSelectedStrokes();
            Strokes.Remove(list);
            var elements = GetSelectedElements();
            if (elements.Any())
            {
                for (var i = elements.Count - 1; i >= 0; i--)
                {
                    RemoveChild(this, ((FrameworkElement)elements[i]).DataContext);
                }
            }
        }
        // ReSharper disable once RedundantOverriddenMember
        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            //// Remove the original stroke and add a custom stroke.
            //this.Strokes.Remove(e.Stroke);
            //var customStroke = new SquareStroke(e.Stroke.StylusPoints, e.Stroke.DrawingAttributes);
            //this.Strokes.Add(customStroke);

            //// Pass the custom stroke to base class' OnStrokeCollected method.
            //InkCanvasStrokeCollectedEventArgs args =
            //    new InkCanvasStrokeCollectedEventArgs(customStroke);
            //base.OnStrokeCollected(args);
            base.OnStrokeCollected(e);
        }
        protected override void OnStrokeErasing(InkCanvasStrokeErasingEventArgs e)
        {
            if (e.Stroke is SquareStroke stroke)
            {
                stroke.DisableSquare = true;
            }
            base.OnStrokeErasing(e);
        }
        // ReSharper disable once InconsistentNaming
        public StylusShape EraserShapeDP
        {
            get => (StylusShape)GetValue(EraserShapeDPProperty);
            set => SetValue(EraserShapeDPProperty, value);
        }
        // Using a DependencyProperty as the backing store for EraserShapeDP.  This enables animation, styling, binding, etc...
        // ReSharper disable once InconsistentNaming
        public static readonly DependencyProperty EraserShapeDPProperty =
            DependencyProperty.Register("EraserShapeDP", typeof(StylusShape), typeof(NextInkCanvas), new PropertyMetadata((sender, e) =>
            {
                ((NextInkCanvas)sender).EraserShape = e.NewValue as StylusShape ?? throw new InvalidOperationException();
            }));
        // ReSharper disable once InconsistentNaming
        public bool UseCustomCursorDP
        {
            get => (bool)GetValue(UseCustomCursorDPProperty);
            set => SetValue(UseCustomCursorDPProperty, value);
        }

        // Using a DependencyProperty as the backing store for UseCustomCursorDP.  This enables animation, styling, binding, etc...
        // ReSharper disable once InconsistentNaming
        public static readonly DependencyProperty UseCustomCursorDPProperty =
            DependencyProperty.Register("UseCustomCursorDP", typeof(bool), typeof(NextInkCanvas), new PropertyMetadata((sender, e) =>
            {
                ((NextInkCanvas)sender).UseCustomCursor = (bool)e.NewValue;
            }));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(NextInkCanvas), new FrameworkPropertyMetadata(null, (sender, e) =>
            {
                if (e.OldValue == e.NewValue)
                {
                    return;
                }
                var casted = (NextInkCanvas)sender;
                if (e.OldValue is INotifyCollectionChanged old)
                {
                    old.CollectionChanged -= casted.ItemsSourceItemChanged;
                }
                casted.Children.Clear();
                if (e.NewValue is INotifyCollectionChanged newish)
                {
                    newish.CollectionChanged += casted.ItemsSourceItemChanged;
                }
                foreach (var item in (IEnumerable)e.NewValue)
                {
                    AddChild(casted, item);
                }
            }));

        private static void AddChild(InkCanvas canvas, object item)
        {
            var element = new ContentElementRenderer();
            canvas.Children.Add(element);
            element.Initialize(item);
        }

        private bool isInternal;
        private static void RemoveChild(NextInkCanvas canvas, object item)
        {
            if (canvas.ItemsSource is IList l)
            {
                canvas.isInternal = true;
                l.Remove(item);
            }
        }
        private static FrameworkElement GetElementFromDataContext(NextInkCanvas canvas, object item)
        {
            return canvas.Children.Cast<FrameworkElement>().First(e => e.DataContext == item);
        }
        private static void RemoveVisualChild(NextInkCanvas canvas, UIElement item)
        {
            canvas.Children.Remove(item);
        }
        private static void RemoveVisualChild(NextInkCanvas canvas, object dataContext)
        {
            RemoveVisualChild(canvas, GetElementFromDataContext(canvas, dataContext));
        }
        private void ItemsSourceItemChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    AddChild(this, item);
                }
            }
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (isInternal)
                    {
                        RemoveVisualChild(this, item);
                    }
                    else
                    {
                        RemoveChild(this, item);
                    }
                }
            }

            isInternal = false;
        }
    }
}
