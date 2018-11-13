﻿#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using NextCanvas.Controls.Content;
using NextCanvas.Ink;
using NextCanvas.Models;
using NextCanvas.Properties;
using NextCanvas.ViewModels.Content;

#endregion

namespace NextCanvas.Controls
{
    /// <summary>
    ///     Logique d'interaction pour NextInkCanvas.xaml
    ///     lol i'm funny i called it NEXT XDDDDDD
    /// </summary>
    public class NextInkCanvas : InkCanvas
    {
        // Using a DependencyProperty as the backing store for ScrollViewerReferent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScrollViewerReferentProperty =
            DependencyProperty.Register("ScrollViewerReferent", typeof(ScrollViewer), typeof(NextInkCanvas),
                new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for EraserShapeDP.  This enables animation, styling, binding, etc...
        // ReSharper disable once InconsistentNaming
        public static readonly DependencyProperty EraserShapeDPProperty =
            DependencyProperty.Register("EraserShapeDP", typeof(StylusShape), typeof(NextInkCanvas),
                new PropertyMetadata((sender, e) =>
                {
                    ((NextInkCanvas)sender).EraserShape =
                        e.NewValue as StylusShape ?? throw new InvalidOperationException();
                }));

        // Using a DependencyProperty as the backing store for UseCustomCursorDP.  This enables animation, styling, binding, etc...
        // ReSharper disable once InconsistentNaming
        public static readonly DependencyProperty UseCustomCursorDPProperty =
            DependencyProperty.Register("UseCustomCursorDP", typeof(bool), typeof(NextInkCanvas),
                new PropertyMetadata((sender, e) => { ((NextInkCanvas)sender).UseCustomCursor = (bool)e.NewValue; }));

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<ContentElementViewModel>),
                typeof(NextInkCanvas),
                new FrameworkPropertyMetadata(null, (sender, e) =>
                {
                    if (e.OldValue == e.NewValue)
                    {
                        return;
                    }

                    if (e.NewValue == null)
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


        public StrokeDelegate<Stroke> CustomStrokeInvocator
        {
            get => (StrokeDelegate<Stroke>)GetValue(CustomStrokeInvocatorProperty);
            set => SetValue(CustomStrokeInvocatorProperty, value);
        }

        // Using a DependencyProperty as the backing store for CustomStrokeInvocator.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CustomStrokeInvocatorProperty =
            DependencyProperty.Register("CustomStrokeInvocator", typeof(StrokeDelegate<Stroke>), typeof(NextInkCanvas), new PropertyMetadata(null));


        public ObservableCollection<ContentElementViewModel> SelectedItems
        {
            get => (ObservableCollection<ContentElementViewModel>)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectedItems.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(ObservableCollection<ContentElementViewModel>), typeof(NextInkCanvas), new FrameworkPropertyMetadata(new ObservableCollection<ContentElementViewModel>(),
                (o, e) =>
                {
                    void SelectedItemsChangedExternally(object sender, NotifyCollectionChangedEventArgs args)
                    {
                        ((NextInkCanvas)o).UpdateSelection();
                    }
                    if (e.OldValue is ObservableCollection<ContentElementViewModel> old)
                    {
                        old.CollectionChanged -= SelectedItemsChangedExternally;
                    }
                    var collection = (ObservableCollection<ContentElementViewModel>)e.NewValue;
                    var canvas = (NextInkCanvas)o;
                    UpdateSelection(canvas, collection);
                    collection.CollectionChanged += SelectedItemsChangedExternally;
                }));



        private static void UpdateSelection(NextInkCanvas canvas, ObservableCollection<ContentElementViewModel> c)
        {
            if (canvas.isSelectionInternal)
            {
                canvas.isSelectionInternal = canvas.shouldAutoSet;
                canvas.shouldAutoSet = false;
                return;
            }
            var elements = new List<FrameworkElement>();
            foreach (var element in c)
            {
                var item = GetElementFromDataContext(canvas, element);
                if (item != null)
                {
                    elements.Add(item);
                }
            }
            canvas.isSelectionInternal = true;
            canvas.shouldAutoSet = true;
            if (elements.Any())
            canvas.Select(elements);
        }

        private void UpdateSelection()
        {
            UpdateSelection(this, SelectedItems);
        }

        private bool isInternal;
        private bool isSelectionInternal;
        private bool shouldAutoSet;
        private MemoryStream lastDataObject;
        private double pasteDiff;

        public NextInkCanvas()
        {
            DynamicRenderer = new NextDynamicRenderer();
            SelectionHelper = new SelectionWrapper(SelectChildren);
            PreferredPasteFormats = new List<InkCanvasClipboardFormat>
            {
                InkCanvasClipboardFormat.InkSerializedFormat
            };
            CommandManager.RegisterClassCommandBinding(typeof(NextInkCanvas),
                new CommandBinding(ApplicationCommands.Delete, CommandExecuted, CanExecuteCommand));
            CommandManager.RegisterClassCommandBinding(typeof(NextInkCanvas),
                new CommandBinding(ApplicationCommands.Paste, CommandExecuted, CanExecuteCommand));
            CommandManager.RegisterClassCommandBinding(typeof(NextInkCanvas),
                new CommandBinding(ApplicationCommands.SelectAll, CommandExecuted, CanExecuteCommand));
        }

        public SelectionWrapper SelectionHelper { get; }


        public ScrollViewer ScrollViewerReferent
        {
            get => (ScrollViewer)GetValue(ScrollViewerReferentProperty);
            set => SetValue(ScrollViewerReferentProperty, value);
        }

        // ReSharper disable once InconsistentNaming
        public StylusShape EraserShapeDP
        {
            get => (StylusShape)GetValue(EraserShapeDPProperty);
            set => SetValue(EraserShapeDPProperty, value);
        }

        // ReSharper disable once InconsistentNaming
        public bool UseCustomCursorDP
        {
            get => (bool)GetValue(UseCustomCursorDPProperty);
            set => SetValue(UseCustomCursorDPProperty, value);
        }

        public ObservableCollection<ContentElementViewModel> ItemsSource
        {
            get => (ObservableCollection<ContentElementViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public void SelectChildren(object dataContext)
        {
            Select(new[] { GetElementFromDataContext(this, dataContext) });
        }

        protected override void OnSelectionChanged(EventArgs e)
        {
            var elements = GetSelectedElements();
            SelectedItems.Clear();
            if (elements.Any())
            {
                foreach (var element in elements)
                {
                    shouldAutoSet = false;
                    isSelectionInternal = true;
                    if (GetDataContextFromElement(element as FrameworkElement) is ContentElementViewModel item)
                    {
                        SelectedItems.Add(item);
                    }
                }
            }
            if (elements.Count == 1)
            {
                var element = elements[0];
                var highestZIndex = Children.Cast<UIElement>().Select(el => (int)el.GetValue(Panel.ZIndexProperty))
                    .OrderByDescending(i => i).First();
                var zIndex = (int)element.GetValue(Panel.ZIndexProperty);
                if (highestZIndex == 0 || zIndex != highestZIndex)
                {
                    element.SetValue(Panel.ZIndexProperty, highestZIndex + 1);
                }
                element.Focus();
                if (element is ContentElementRenderer render)
                {
                    render.FocusChild();
                }
            }
            base.OnSelectionChanged(e);
        }

        protected override void OnSelectionMoving(InkCanvasSelectionEditingEventArgs e)
        {
            var elements = GetSelectedElements().OfType<FrameworkElement>();
            var browsers = elements.Select(GetDataContextFromElement).OfType<WebBrowserElementViewModel>();
            var models = browsers as WebBrowserElementViewModel[] ?? browsers.ToArray();
            if (!models.Any() || e.OldRectangle.Y > e.NewRectangle.Y)
            {
                base.OnSelectionMoving(e);
                return;
            }
            // Don't make a web browser overflow or the window will be covered with it and it's ugly :c
            var highest = models.OrderByDescending(w => w.Top + w.Height).First();
            var bottom = highest.Top + highest.Height - e.OldRectangle.Y + e.NewRectangle.Y;
            var height = (ScrollViewerReferent?.ActualHeight ?? ActualHeight);
            if (bottom > height)
            {
                var rect = e.NewRectangle;
                rect.Y -= bottom - height;
                e.NewRectangle = rect;
            }
            base.OnSelectionMoving(e);
        }

        private static void CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var canvas = (NextInkCanvas)sender;
            if (e.Command == ApplicationCommands.Delete)
            {
                canvas.DeleteSelection();
            }

            if (e.Command == ApplicationCommands.Paste)
            {
                canvas.Paste();
            }

            if (e.Command == ApplicationCommands.SelectAll)
            {
                var elements = canvas.Children;
                var strokes = canvas.Strokes;
                canvas.Select(strokes, elements.OfType<UIElement>());
            }
        }

        /// <summary>
        ///     Pastes the clipboard content to the *CENTER*.
        /// </summary>
        public new void Paste()
        {
            UpdatePasteDifference();
            if (!CanPaste())
            {
                return;
            }

            double horizontalOffset = 0, verticalOffset = 0;
            double width = ActualWidth, height = ActualHeight;
            if (ScrollViewerReferent != null) // sees if there is any useful scroll viewers.
            {
                horizontalOffset = ScrollViewerReferent.ContentHorizontalOffset;
                verticalOffset = ScrollViewerReferent.ContentVerticalOffset;
                width = ScrollViewerReferent.ActualWidth;
                height = ScrollViewerReferent.ActualHeight;
            }

            Paste(new Point(horizontalOffset + width / 2 + pasteDiff, verticalOffset + height / 2 + pasteDiff));
        }

        private void UpdatePasteDifference()
        {
            if (!(Clipboard.GetData(StrokeCollection.InkSerializedFormat) is MemoryStream data))
            {
                pasteDiff = 0;
                return;
            }

            if (lastDataObject == null)
            {
                pasteDiff = 0;
                lastDataObject = data;
                return;
            }

            if (data.Length == lastDataObject.Length
            ) // It's less heavier than having to create two stream readers and blah blah blah
            {
                pasteDiff += 5;
            }
            else
            {
                pasteDiff = 0;
            }

            lastDataObject = data;
        }

        private static void CanExecuteCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            var canvas = (NextInkCanvas)sender;
            if (e.Command == ApplicationCommands.Delete)
            {
                e.CanExecute = canvas.GetSelectedElements().Any() || canvas.GetSelectedStrokes().Any();
            }

            if (e.Command == ApplicationCommands.Paste)
            {
                e.CanExecute = canvas.CanPaste();
            }

            if (e.Command == ApplicationCommands.SelectAll)
            {
                e.CanExecute = true;
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
        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            if (CustomStrokeInvocator == null)
            {
                base.OnStrokeCollected(e);
                return;
            }
            // Remove the original stroke and add a custom stroke.
            Strokes.Remove(e.Stroke);
            var customStroke = CustomStrokeInvocator(e.Stroke);
            // Store the type name, UwU
            var typeName = customStroke.GetType().FullName;
            if (typeName != null)
            {
                customStroke.AddPropertyData(AssemblyInfo.Guid, typeName);
            }

            Strokes.Add(customStroke);
            // Pass the custom stroke to base class' OnStrokeCollected method.
            var args = new InkCanvasStrokeCollectedEventArgs(customStroke);
            base.OnStrokeCollected(args);

        }
        protected override void OnStrokeErasing(InkCanvasStrokeErasingEventArgs e)
        {
            if (e.Stroke is SquareStroke stroke)
            {
                stroke.DisableSquare = true;
            }

            base.OnStrokeErasing(e);
        }

        private static void AddChild(InkCanvas canvas, object item)
        {
            var element = new ContentElementRenderer();
            canvas.Children.Add(element);
            element.Initialize(item);
        }

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
            return canvas.Children.Cast<FrameworkElement>().FirstOrDefault(e => e.DataContext == item);
        }
        private static object GetDataContextFromElement(NextInkCanvas canvas, FrameworkElement element)
        {
            return canvas.ItemsSource.First(e => element.DataContext == e);
        }

        private object GetDataContextFromElement(FrameworkElement element)
        {
            return GetDataContextFromElement(this, element);
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