﻿#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Fluent;
using NextCanvas.Content;
using NextCanvas.Content.ViewModels;
using NextCanvas.Extensibility.Content;
using NextCanvas.Interactivity;
using NextCanvas.Interactivity.Dialogs;
using NextCanvas.Interactivity.Multimedia;
using NextCanvas.Interactivity.Progress;
using NextCanvas.Properties;
using NextCanvas.Serialization;
using NextCanvas.Utilities;
using NextCanvas.Utilities.Content;

#endregion

namespace NextCanvas.ViewModels
{
    public class MainWindowViewModel : ViewModelBase<MainWindowModel>
    {
        private DocumentViewModel _document;

        private ElementCreationContext _elementCreationContext;
        public IInteractionProvider<IErrorInteraction> ErrorProvider { get; set; }
        public IInteractionProvider<IModifyObjectInteraction> ModifyProvider { get; set; }
        private int _selectedToolIndex;

        public MainWindowViewModel(MainWindowModel model = null) : base(model)
        {
            Initialize();
        }

        public MainWindowViewModel() : this(null)
        {

        }

        public DocumentViewModel CurrentDocument
        {
            get => _document;
            set
            {
                if (_document != null)
                {
                    _document.Pages.CollectionChanged -= PagesChanged;
                }

                _document = value;
                Model.Document = _document.Model;
                Subscribe();
                OnPropertyChanged(nameof(CurrentDocument));
                UpdatePageManipulation();
                UpdatePageText();
                UpdateSelectedPage();
            }
        }

        private int _selectedPageIndex;
        public int SelectedPageIndex
        {
            get => _selectedPageIndex;
            set
            {
                if (value > CurrentDocument.Pages.Count - 1) throw new IndexOutOfRangeException("ur out of range");
                _selectedPageIndex = value;
                UpdateSelectedPage();
            }
        }
        private void UpdateSelectedPage()
        {
            if (_selectedPageIndex >= CurrentDocument.Pages.Count)
            {
                _selectedPageIndex = CurrentDocument.Pages.Count - 1;
            }
            OnPropertyChanged(nameof(SelectedPageIndex));
            OnPropertyChanged(nameof(SelectedPage));
            UpdatePageText();
            UpdatePageManipulation();
        }

        public PageViewModel SelectedPage
        {
            get => CurrentDocument.Pages[_selectedPageIndex];
            set => SelectedPageIndex = CurrentDocument.Pages.IndexOf(value);
        }

        public ObservableCollection<Color> FavoriteColors => SettingsManager.Settings.FavoriteColors;
        public ObservableViewModelCollection<ToolViewModel, Tool> Tools => SettingsManager.Settings.Tools;
        public ObservableCollection<ContentElementViewModel> SelectedElements { get; set; } = new ObservableCollection<ContentElementViewModel>();
        public string SavePath { get; set; }
        public string OpenPath { get; set; }
        public string OpenImagePath { get; set; }

        public int SelectedToolIndex
        {
            get => _selectedToolIndex;
            set
            {
                if ((value < 0 || value >= Tools.Count) && Tools.Count != 0) return;
                _selectedToolIndex = value;
                OnPropertyChanged(nameof(SelectedToolIndex));
                OnPropertyChanged(nameof(SelectedTool));
                OnPropertyChanged(nameof(IsSelectTool));
                ToolViewModel.UpdateCursorIfEraser(SelectedTool);
            }
        }
        // better use this tho
        public ToolViewModel SelectedTool
        {
            get
            {
                if (IsSelectTool)
                {
                    return null;
                }
                var tool = Tools[SelectedToolIndex];
                var color = tool.DrawingAttributes.Color;
                if (!ColorGallery.StandardThemeColors.Contains(color) && !FavoriteColors.Contains(color))
                    FavoriteColors.Add(color);

                return tool;
            }
            set
            {
                if (value is null)
                {
                    IsSelectTool = true;
                    return;
                }
                else
                {
                    IsSelectTool = false;
                }
                SelectedToolIndex = Tools.IndexOf(value);
            }
        }

        private bool _isSelectTool;
        public bool IsSelectTool
        {
            get => _isSelectTool;
            set
            {
                _isSelectTool = value;
                OnPropertyChanged(nameof(IsSelectTool));
                OnPropertyChanged(nameof(SelectedTool));
                SwitchToSelectToolCommand.RaiseCanExecuteChanged();
            }
        }

        public ElementCreationContext ElementCreationContext
        {
            get => _elementCreationContext;
            set
            {
                _elementCreationContext = value;
                OnPropertyChanged(nameof(ElementCreationContext));
            }
        }

        // Pages
        public DelegateCommand PreviousPageCommand { get; private set; }
        public DelegateCommand NextPageCommand { get; private set; }
        public DelegateCommand NewPageCommand { get; private set; }
        public DelegateCommand DeletePageCommand { get; private set; }

        public DelegateCommand ExtendPageCommand { get; private set; }

        // Tools
        public DelegateCommand SetToolByNameCommand { get; private set; }

        // File saving
        public DelegateCommand SaveCommand { get; private set; }

        public DelegateCommand OpenCommand { get; private set; }

        // Content
        public DelegateCommand CreateTextBoxCommand { get; private set; }
        public DelegateCommand SwitchToSelectToolCommand { get; private set; }
        public DelegateCommand CreateImageCommand { get; private set; }
        public DelegateCommand CreateScreenshotCommand { get; private set; }
        public DelegateCommand CreateWebBrowserCommand { get; private set; }
        public string PageDisplayText => SelectedPageIndex + 1 + "/" + CurrentDocument.Pages.Count;

        private DocumentReader DocumentReader { get; } = new DocumentReader();
        private DocumentSaver DocumentSaver { get; } = new DocumentSaver();

        private void Subscribe() // To my youtube channel XD
        {
            _document.Pages.CollectionChanged += PagesChanged;
        }

        private void UpdatePageManipulation()
        {
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
            DeletePageCommand.RaiseCanExecuteChanged();
        }

        private void PagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SelectedPageIndex > 0 && e.OldStartingIndex >= SelectedPageIndex) SelectedPageIndex = e.OldStartingIndex - 1; // To avoid 3/2 for example
            UpdateSelectedPage();
        }

        private void UpdatePageText()
        {
            OnPropertyChanged(nameof(PageDisplayText));
        }
        public List<ContentAddonEntry> ContentAddonEntries { get; private set; }
        public bool HasContentAddons => ContentAddonEntries?.Any() ?? false;
        private void Initialize()
        {
            _document = new DocumentViewModel(Model.Document);
            Subscribe();
            // Create commands
            PreviousPageCommand = new DelegateCommand(o => ChangePage(Direction.Backwards),
                o => CanChangePage(Direction.Backwards));
            NextPageCommand = new DelegateCommand(o => ChangePage(Direction.Forwards),
                o => CanChangePage(Direction.Forwards));
            NewPageCommand = new DelegateCommand(CreateNewPage);
            DeletePageCommand = new DelegateCommand(DeletePage, o => CurrentDocument.CanDeletePage);
            ExtendPageCommand = new DelegateCommand(ExtendPage);
            SetToolByNameCommand = new DelegateCommand(SetToolByName, IsNameValid);
            SwitchToSelectToolCommand = new DelegateCommand(SwitchToSelectTool, CanSwitchToSelectTool);
            SaveCommand = new DelegateCommand(async o => await SaveDocument(o));
            OpenCommand = new DelegateCommand(async o => await OpenDocument(o));
            CreateTextBoxCommand = new DelegateCommand(CreateTextBox);
            CreateImageCommand = new DelegateCommand(CreateImage);
            CreateScreenshotCommand = new DelegateCommand(CreateScreenShot);
            CreateWebBrowserCommand = new DelegateCommand(CreateWebBrowser);
#if DEBUG
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
#endif
            if (App.GetCurrent().Addons.Any())
            {
                ContentAddonEntries = App.GetCurrent().Addons.SelectMany(a => a.ResolvedAddonElements)
                    .Where(a => a.IsContentElement())
                    .Select(a => new ContentAddonEntry
                    {
                        AddElementCommand = new DelegateCommand(_ =>
                        {
                            var e = a.CreateContentElement();
                            ProcessItem(e);
                        }),
                        AddonData = a.Attribute as ContentAddonElementAttribute
                    }).ToList();
            }
#if DEBUG
            }
#endif
            OnPropertyChanged(nameof(HasContentAddons));
        }
        private void CenterElement(ContentElementViewModel v, ElementCreationContext context)
        {
            v.Left = GetCenterLeft(context, v.Width);
            v.Top = GetCenterTop(context, v.Height);
        }

        private double GetCenterLeft(ElementCreationContext context, double width)
        {
            return context.ContentHorizontalOffset + context.VisibleWidth / 2 - width / 2;
        }

        private double GetCenterTop(ElementCreationContext context, double height)
        {
            return context.ContentVerticalOffset + context.VisibleHeight / 2 - height / 2;
        }

        private void CreateTextBox()
        {
            var element = new TextBoxElementViewModel();
            ProcessItem(element);
        }

        private void ProcessItem(ContentElementViewModel element)
        {
            if (ElementCreationContext == null)
            {
                AddElementToSelectedPage(element);
                return;
            }
            CenterElement(element, ElementCreationContext);
            AddElementToSelectedPage(element);
            SelectedTool = GetSelectTool();
            ElementCreationContext.Selection.Select(element);
        }

        private void CreateImage()
        {
            if (OpenImagePath == null) return;

            ResourceViewModel resource;
            using (var fileStream = File.Open(OpenImagePath, FileMode.Open, FileAccess.Read))
            {
                resource = CurrentDocument.AddResource(fileStream);
            } // Release the file, we already copied it. 

            AddImage(resource);
        }

        private void AddImage(ResourceViewModel resource)
        {
            var element = new ImageElementViewModel(new ImageElement(resource.Model));
            if (ElementCreationContext != null)
            {
                // Sets the width and the height to the image's default dimensions. Resizes if the page or visible height is too small.
                // Tries to :
                // 1. Stretch the whole visible height.
                // 2. Keeps image's max dimensions.
                // 3. Tries to not exceed the page's dimensions.
                var cappedHeight = (ElementCreationContext.VisibleHeight - 25).Cap(100);
                var cappedWidth = (ElementCreationContext.VisibleWidth - 25).Cap(100);
                element.Width = Math.Min(Math.Min(cappedWidth, SelectedPage.Width), element.Image.PixelWidth);
                element.Height = Math.Min(Math.Min(cappedHeight, SelectedPage.Height),
                    element.Image.PixelHeight);
            }
            ProcessItem(element);
        }

        private void CreateScreenShot(object interaction)
        {
            if (!(interaction is IInteractionProvider<IScreenshotInteraction> interact)) return;
            var screenshotter = interact.CreateInteraction();
            screenshotter.ActionComplete += Screenshotter_ActionComplete;
            screenshotter.ShowInteraction();
            // yes.
        }

        private void Screenshotter_ActionComplete(object sender, ScreenshotTakenEventArgs e)
        {
            var resource = CurrentDocument.AddResource(e.ImageData, "Screenshot" + DateTime.Now.ToString("s").Replace(":", ".") + e.ImageExtension);
            AddImage(resource);
        }

        private void CreateWebBrowser(object interaction)
        {
            if (!(interaction is IInteractionProvider<IModifyObjectInteraction> interact)) return;
            var editor = interact.CreateInteraction();
            var browserInstance =
                new WebBrowserElementViewModel(SettingsManager.Settings.GetDefaultValue<WebBrowserElement>());
            editor.ObjectToModify = browserInstance;
            editor.HeaderStart = EditorResources.ModifyObject_CreateHeader;
            editor.IsObjectCreation = true;
            editor.ActionComplete += (sender, args) => { ProcessItem(browserInstance); };
            editor.ShowInteraction();
        }

        // Pages things

        private void AddElementToSelectedPage(ContentElementViewModel element)
        {
            SelectedPage.Elements.Add(element);
        }

        private void SwitchToSelectTool()
        {
            IsSelectTool = true;
        }

        private bool CanSwitchToSelectTool()
        {
            return !IsSelectTool;
        }

        public void SelectionHandler(object sender, InkCanvasSelectionChangingEventArgs e)
        {
            if (e.GetSelectedElements().Count + e.GetSelectedStrokes().Count > 0) SwitchToSelectTool();
        }
        private ToolViewModel GetSelectTool()
        {
            return !IsThereAnySelectTools() ? null : Tools.First(t => t.Mode == InkCanvasEditingMode.Select);
        }

        private bool IsThereAnySelectTools()
        {
            return Tools.Any(t => t.Mode == InkCanvasEditingMode.Select);
        }

        // The following shall be replaced with some zippy archives and resources and isf and blah and wow and everything you need to be gud
        // Oh wait, it is now XD
        private async Task SaveDocument(object progress = null)
        {
            if (SavePath == null) return;

            try
            {
                var progressInteractionProcessed = GetProgressInteraction(progress);
                await DocumentSaver.SaveCompressedDocument(CurrentDocument.Model, SavePath,
                    progressInteractionProcessed);
            }
            catch (Exception e)
            {
                if (ErrorProvider == null) throw;
                var error = ErrorProvider.CreateInteraction();
                error.Content = string.Format(ErrorResources.PassBy_Content, e.Message);
                error.Title = ErrorResources.Error;
                error.ShowInteraction();
            }
            finally
            {
                SavePath = null;
            }
        }

        private static IProgressInteraction GetProgressInteraction(object progress)
        {
            IProgressInteraction progressInteractionProcessed;
            if (progress is IInteractionProvider<IProgressInteraction> provider)
                progressInteractionProcessed = provider.CreateInteraction();
            else
                throw new ArgumentNullException(nameof(progress));

            return progressInteractionProcessed;
        }

        private async Task OpenDocument(object progress = null)
        {
            if (OpenPath == null) return;
            try
            {
                if (!(progress is IInteractionProvider<IProgressInteraction> provider)) return;
                using (var fileStream = File.Open(OpenPath, FileMode.Open))
                {
                    _document.Dispose();
                    CurrentDocument =
                        new DocumentViewModel(await DocumentReader.TryOpenDocument(fileStream,
                            provider.CreateInteraction()));
                }
            }
            catch (Exception e)
            {
                if (ErrorProvider == null) throw;
                var error = ErrorProvider.CreateInteraction();
                error.Content = string.Format(ErrorResources.PassBy_Content, e.Message);
                error.Title = ErrorResources.Error;
                error.ShowInteraction();
            }
            finally
            {
                OpenPath = null;
            }
        }

        private void SetToolByName(object name)
        {
            SetToolByName(name.ToString());
        }

        private void SetToolByName(string name)
        {
            if (!IsNameValid(name)) return;

            SelectedTool = Tools.First(t => t.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        private bool IsNameValid(object name)
        {
            return IsNameValid(name.ToString());
        }

        private bool IsNameValid(string name)
        {
            return Tools.Any(t => t.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        private void DeletePage(int index)
        {
            if (CurrentDocument.CanDeletePage)
            {
                CurrentDocument.Pages.RemoveAt(index);
            }
        }

        private void DeletePage(object interaction)
        {
            if (!CurrentDocument.CanDeletePage) return;
            if (!(interaction is IInteractionProvider<IUserRequestInteraction> userRequest)) return;
            var interact = userRequest.CreateInteraction();
            interact.Title = DialogResources.DeletePageTitle;
            interact.Content = DialogResources.DeletePageContent;
            interact.ActionComplete += (sender, args) =>
            {
                if (!args.IsAccept) return;
                DeletePage(SelectedPageIndex);
            };
            interact.ShowInteraction();
        }
        private void ExtendPage(object direction)
        {
            ExtendPage(direction.ToString());
        }

        private void ExtendPage(string direction)
        {
            if (direction.Equals("Right", StringComparison.InvariantCultureIgnoreCase))
                SelectedPage.Width += 350;

            if (direction.Equals("Bottom", StringComparison.InvariantCultureIgnoreCase))
                SelectedPage.Height += 350;
        }

        private void ChangePage(Direction direction)
        {
            if (CanChangePage(direction)) SelectedPageIndex += (int)direction;
        }

        public void ChangePage(int index)
        {
            if (CanChangePage(index)) SelectedPageIndex = index;
        }

        private void CreateNewPage()
        {
            CurrentDocument.Pages.Insert(SelectedPageIndex + 1, new PageViewModel());
            ChangePage(Direction.Forwards);
        }

        private bool CanChangePage(Direction direction)
        {
            return direction == Direction.Forwards && SelectedPageIndex + 1 != CurrentDocument.Pages.Count ||
                   direction == Direction.Backwards && SelectedPageIndex - 1 >= 0;
        }

        private bool CanChangePage(int index)
        {
            return _document.Pages.Count > index;
        }

        private enum Direction
        {
            Forwards = 1,
            Backwards = -1
        }

        public class ContentAddonEntry
        {
            public ICommand AddElementCommand { get; set; }
            public ContentAddonElementAttribute AddonData { get; set; }
        }
    }
}