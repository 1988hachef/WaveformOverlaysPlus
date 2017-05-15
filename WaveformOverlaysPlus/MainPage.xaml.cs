using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Printing;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Printing;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel;
using WaveformOverlaysPlus.Helpers;
using Windows.Storage.FileProperties;
using WaveformOverlaysPlus.Controls;
using Windows.UI;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Microsoft.Services.Store.Engagement;
using Windows.UI.Xaml.Documents;
using Windows.UI.Text;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Input.Inking;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Diagnostics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System.Numerics;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace WaveformOverlaysPlus
{
    public class StoredImage
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        ObservableCollection<StoredImage> imageCollection;

        string ColorChangerBox;
        string currentToolChosen;

        #region For Custom Ink Rendering and Erase
        private readonly List<InkStrokeContainer> _strokes = new List<InkStrokeContainer>();
        private InkSynchronizer _inkSynchronizer;
        private bool _isErasing;
        private Point _lastPoint;
        private int _deferredDryDelay;
        private IReadOnlyList<InkStroke> _pendingDry;
        #endregion

        #region For Ink
        InkPresenter _inkPresenter;
        #endregion

        #region For Printing
        private PrintManager printMan;
        private PrintDocument printDoc;
        private IPrintDocumentSource printDocSource;
        #endregion

        #region For Sharing
        DataTransferManager dataTransferManager;
        #endregion

        #region For Arrow and Line
        Line lineForArrow;
        double topLineLimiter;
        double bottomLineLimiter;
        double leftLineLimiter;
        double rightLineLimiter;
        double firstY;
        double firstX;
        #endregion

        #region Dependency Properties

        public double currentSizeSelected
        {
            get { return (double)GetValue(currentSizeSelectedProperty); }
            set { SetValue(currentSizeSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for currentSizeSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty currentSizeSelectedProperty =
            DependencyProperty.Register("currentSizeSelected", typeof(double), typeof(MainPage), new PropertyMetadata(3));

        public double currentFontSize
        {
            get { return (double)GetValue(currentFontSizeProperty); }
            set
            {
                value = value == 1 ? 14 :
                        value == 2 ? 18 :
                        value == 6 ? 24 :
                        value == 10 ? 36 :
                        18;

                SetValue(currentFontSizeProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for currentFontSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty currentFontSizeProperty =
            DependencyProperty.Register("currentFontSize", typeof(double), typeof(MainPage), new PropertyMetadata(18));

        #endregion

        public MainPage()
        {
            this.InitializeComponent();

            // Set visibility of Feedback, if device supports Feedback
            if (StoreServicesFeedbackLauncher.IsSupported())
            {
                menuFeedback.Visibility = Visibility.Visible;
            }

            // Initialize _inkPresenter
            _inkPresenter = inkCanvas.InkPresenter;
            _inkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;

            // Initialize imageCollection
            imageCollection = new ObservableCollection<StoredImage>();
        }

        #region OnNavigated
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Register for PrintTaskRequested event
            printMan = PrintManager.GetForCurrentView();
            printMan.PrintTaskRequested += PrintTaskRequested;

            // Build a PrintDocument and register for callbacks
            printDoc = new PrintDocument();
            printDocSource = printDoc.DocumentSource;
            printDoc.Paginate += Paginate;
            printDoc.GetPreviewPage += GetPreviewPage;
            printDoc.AddPages += AddPages;

            // Register the current page as a share source.
            dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.ShareImageHandler);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            dataTransferManager.DataRequested -= ShareImageHandler;
            printMan.PrintTaskRequested -= PrintTaskRequested;
            printDoc.Paginate -= Paginate;
            printDoc.GetPreviewPage -= GetPreviewPage;
            printDoc.AddPages -= AddPages;
        }
        #endregion

        #region New and Exit
        private void menuNew_Click(object sender, RoutedEventArgs e)
        {
            gridMain.Children.Clear();

            if (!(gridMain.Background.Equals(Colors.White)))
            {
                gridMain.Background = new SolidColorBrush(Colors.White);
            }
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
        #endregion

        #region Open

        private async void menuOpen_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".bmp");
            openPicker.FileTypeFilter.Add(".gif");
            openPicker.FileTypeFilter.Add(".png");
            StorageFile imgFile = await openPicker.PickSingleFileAsync();

            if (imgFile != null)
            {
                double _height = 0;
                double _width = 0;

                Image image = new Image();
                image.Stretch = Stretch.Fill;

                // Set the image source and get the image width and height
                using (IRandomAccessStream IRASstream = await imgFile.OpenAsync(FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(IRASstream);
                    _height = bitmapImage.PixelHeight;
                    _width = bitmapImage.PixelWidth;
                    image.Source = bitmapImage;
                }

                if (_width < 42 || _height < 42)
                {
                    MessageDialog tooSmallMessage = new MessageDialog("Image too small. Please choose a larger image.");
                    await tooSmallMessage.ShowAsync();
                }
                else
                {
                    double scale = 1;

                    if (_width > gridMain.ActualWidth || _height > gridMain.ActualHeight)
                    {
                        scale = Math.Min(gridMain.ActualWidth / _width, gridMain.ActualHeight / _height);
                        _width = (_width * scale) - 1;
                        _height = (_height * scale) - 1;
                    }

                    string name = imgFile.Name;
                    string path = "ms-appdata:///local/" + name;

                    PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
                    paintObject.Width = _width;
                    paintObject.Height = _height;
                    paintObject.Content = image;
                    paintObject.ImageFileName = name;
                    paintObject.ImageFilePath = path;
                    paintObject.ImageScale = scale;
                    paintObject.OpacitySliderIsVisible = true;
                    paintObject.Unloaded += PaintObject_Unloaded;

                    gridMain.Children.Add(paintObject);

                    await imgFile.CopyAsync(ApplicationData.Current.LocalFolder, name, NameCollisionOption.ReplaceExisting);

                    imageCollection.Add(new StoredImage { FileName = name, FilePath = path });
                }
            }
        }

        #endregion

        #region Save

        private async void menuSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add(".png Portable Network Graphics", new List<string>() { ".png" });
                savePicker.FileTypeChoices.Add(".bmp Bitmap", new List<string>() { ".bmp" });
                savePicker.FileTypeChoices.Add(".jpg Joint Photographic Experts Group", new List<string>() { ".jpg" });
                savePicker.FileTypeChoices.Add(".gif Graphical Interchange Format", new List<string>() { ".gif" });
                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    await ImageUtils.CaptureElementToFile(gridForOverall, file);
                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("The Save File method ran into a problem.    " + ex.Message);
                await dialog.ShowAsync();
            }
        }

        #endregion

        #region Print

        private async void PrintButtonClick(object sender, RoutedEventArgs e)
        {
            gridCover.Visibility = Visibility.Visible;

            RenderTargetBitmap rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(gridForOverall);
            IBuffer pixelBuffer = await rtb.GetPixelsAsync();
            byte[] pixels = pixelBuffer.ToArray();
            WriteableBitmap wb = new WriteableBitmap(rtb.PixelWidth, rtb.PixelHeight);
            using (Stream stream = wb.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixels, 0, pixels.Length);
            }

            imageForPrint.Source = wb;
            gridForPrint.Visibility = Visibility.Visible;

            if (PrintManager.IsSupported())
            {
                try
                {
                    // Show print UI
                    await PrintManager.ShowPrintUIAsync();
                }
                catch
                {
                    // Printing cannot proceed at this time
                    ContentDialog noPrintingDialog = new ContentDialog()
                    {
                        Title = "Printing error",
                        Content = "\nSorry, printing can't proceed at this time.",
                        PrimaryButtonText = "OK"
                    };
                    await noPrintingDialog.ShowAsync();
                }
                finally
                {
                    imageForPrint.ClearValue(Image.SourceProperty);
                    gridForPrint.Visibility = Visibility.Collapsed;
                    gridCover.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Printing is not supported on this device
                ContentDialog noPrintingDialog = new ContentDialog()
                {
                    Title = "Printing not supported",
                    Content = "\nSorry, printing is not supported on this device.",
                    PrimaryButtonText = "OK"
                };
                await noPrintingDialog.ShowAsync();

                imageForPrint.ClearValue(Image.SourceProperty);
                gridForPrint.Visibility = Visibility.Collapsed;
                gridCover.Visibility = Visibility.Collapsed;
            }
        }

        private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            // Create the PrintTask.
            // Defines the title and delegate for PrintTaskSourceRequested
            var printTask = args.Request.CreatePrintTask("Print", PrintTaskSourceRequrested);

            // Handle PrintTask.Completed to catch failed print jobs
            printTask.Completed += PrintTaskCompleted;
        }

        private void PrintTaskSourceRequrested(PrintTaskSourceRequestedArgs args)
        {
            // Set the document source.
            args.SetSource(printDocSource);
        }

        private void Paginate(object sender, PaginateEventArgs e)
        {
            // As I only want to print one Rectangle, so I set the count to 1
            printDoc.SetPreviewPageCount(1, PreviewPageCountType.Final);
        }

        private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            // Provide a UIElement as the print preview.
            printDoc.SetPreviewPage(e.PageNumber, this.gridForPrint);
        }

        private void AddPages(object sender, AddPagesEventArgs e)
        {
            printDoc.AddPage(this.gridForPrint);

            // Indicate that all of the print pages have been provided
            printDoc.AddPagesComplete();
        }

        private async void PrintTaskCompleted(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            // Notify the user when the print operation fails.
            if (args.Completion == PrintTaskCompletion.Failed)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    ContentDialog noPrintingDialog = new ContentDialog()
                    {
                        Title = "Printing error",
                        Content = "\nSorry, failed to print.",
                        PrimaryButtonText = "OK"
                    };
                    await noPrintingDialog.ShowAsync();

                    imageForPrint.ClearValue(Image.SourceProperty);
                    gridForPrint.Visibility = Visibility.Collapsed;
                    gridCover.Visibility = Visibility.Collapsed;
                });
            }

            if (args.Completion == PrintTaskCompletion.Abandoned ||
                args.Completion == PrintTaskCompletion.Canceled ||
                args.Completion == PrintTaskCompletion.Submitted)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    imageForPrint.ClearValue(Image.SourceProperty);
                    gridForPrint.Visibility = Visibility.Collapsed;
                    gridCover.Visibility = Visibility.Collapsed;
                });
            }
        }

        #endregion

        #region Share

        private async void ShareImageHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            request.Data.Properties.Title = "Share Image";
            request.Data.Properties.Description = "share an image.";

            // Because we are making async calls in the DataRequested event handler,
            //  we need to get the deferral first.
            DataRequestDeferral deferral = request.GetDeferral();

            // Make sure we always call Complete on the deferral.
            try
            {
                // Get the file
                StorageFile thumbnailFile = await ApplicationData.Current.LocalFolder.GetFileAsync("thumbnail.png");
                request.Data.Properties.Thumbnail = RandomAccessStreamReference.CreateFromFile(thumbnailFile);

                StorageFile shareFile = await ApplicationData.Current.LocalFolder.GetFileAsync("shareFile.png");
                request.Data.SetBitmap(RandomAccessStreamReference.CreateFromFile(shareFile));
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void menuShare_Click(object sender, RoutedEventArgs e)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(gridForOverall);
            IBuffer pixelBuffer = await rtb.GetPixelsAsync();
            byte[] pixels = pixelBuffer.ToArray();
            WriteableBitmap wb = new WriteableBitmap(rtb.PixelWidth, rtb.PixelHeight);
            using (Stream stream = wb.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixels, 0, pixels.Length);
            }

            StorageFile thumbnailFile = await ImageUtils.WriteableBitmapToStorageFile(wb, "thumbnail.png");
            StorageFile shareFile = await ImageUtils.WriteableBitmapToStorageFile(wb, "shareFile.png");

            DataTransferManager.ShowShareUI();
        }




        #endregion

        #region Copy and Paste

        private async void menuCopy_Click(object sender, RoutedEventArgs e)
        {
            StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("TempImgFile", CreationCollisionOption.ReplaceExisting);
            await ImageUtils.CaptureElementToFile(gridForOverall, file);

            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(file));

            Clipboard.SetContent(dataPackage);
        }

        private async void menuPaste_Click(object sender, RoutedEventArgs e)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                IRandomAccessStreamReference imageReceived = null;
                try
                {
                    imageReceived = await dataPackageView.GetBitmapAsync();
                }
                catch (Exception ex)
                {
                    var dialog = new MessageDialog("Error retrieving image from Clipboard: " + ex.Message).ShowAsync();
                }

                if (imageReceived != null)
                {
                    double _height = 0;
                    double _width = 0;

                    Image image = new Image();
                    image.Stretch = Stretch.Fill;

                    // Set the image source and get the image width and height
                    using (var imageStream = await imageReceived.OpenReadAsync())
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(imageStream);
                        _height = bitmapImage.PixelHeight;
                        _width = bitmapImage.PixelWidth;
                        image.Source = bitmapImage;
                    }

                    if (_width < 40 || _height < 40)
                    {
                        var dialog = new MessageDialog("Image too small. Please choose a larger image.").ShowAsync();
                    }

                    if (_width > gridMain.ActualWidth || _height > gridMain.ActualHeight)
                    {
                        double scale = Math.Min(gridMain.ActualWidth / _width, gridMain.ActualHeight / _height);
                        _width = (_width * scale) - 1;
                        _height = (_height * scale) - 1;
                    }

                    PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
                    paintObject.Width = _width;
                    paintObject.Height = _height;
                    paintObject.Content = image;
                    paintObject.OpacitySliderIsVisible = true;

                    gridMain.Children.Add(paintObject);
                }
            }
            else
            {
                var dialog = new MessageDialog("Bitmap format is not available in clipboard").ShowAsync();
            }
        }

        #endregion

        #region Help menu items

        private async void menuFeedback_Click(object sender, RoutedEventArgs e)
        {
            var launcher = StoreServicesFeedbackLauncher.GetDefault();
            await launcher.LaunchAsync();
        }

        private async void menuPrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            Uri uri = new Uri(@"https://1drv.ms/u/s!AlsPP0wI1WI76nbgs0LoEttkRVnD");
            bool success = await Windows.System.Launcher.LaunchUriAsync(uri);

            if (success == false)
            {
                var dialog = await new MessageDialog("Webpage failed to open. If this continues to happen, please use the Feedback button to report the problem.").ShowAsync();
            }
        }

        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            gridCover.Visibility = Visibility.Visible;
            gridDialog.Visibility = Visibility.Visible;
            btnDialogOK.Visibility = Visibility.Visible;

            TextBlock title = new TextBlock();
            title.Text = "Pressure Waveform Overlays \n";
            title.FontWeight = FontWeights.Bold;

            TextBlock body = new TextBlock();
            body.Text = "Version 1.0 \n" +
                        "Copyright \u00A9 2017 Steven McGrew \n" +
                        "All rights reserved";

            spanelText.Children.Add(title);
            spanelText.Children.Add(body);
        }

        private void btnDialogOK_Click(object sender, RoutedEventArgs e)
        {
            gridCover.Visibility = Visibility.Collapsed;
            gridDialog.Visibility = Visibility.Collapsed;
            btnDialogOK.Visibility = Visibility.Collapsed;

            spanelText.Children.Clear();
        }

        #endregion

        #region Custom Ink Rendering with Erase

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial ink stroke attributes.
            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            drawingAttributes.DrawAsHighlighter = false;
            drawingAttributes.PenTip = PenTipShape.Circle;

            SolidColorBrush initialBrush = new SolidColorBrush(Colors.Blue);
            if (borderForStrokeColor != null)
            {
                initialBrush = borderForStrokeColor.Background as SolidColorBrush;
            }
            drawingAttributes.Color = initialBrush.Color;

            if (rbSize10 != null)
            {
                drawingAttributes.Size = new Size(currentSizeSelected, currentSizeSelected);
            }

            drawingAttributes.IgnorePressure = false;
            drawingAttributes.FitToCurve = true;
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);

            var display = DisplayInformation.GetForCurrentView();

            // 1. Activate custom drawing 
            _inkSynchronizer = _inkPresenter.ActivateCustomDrying();

            // 2. add use custom drawing when strokes are collected
            _inkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            _inkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

            var unprocessedInput = _inkPresenter.UnprocessedInput;
            unprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;
            unprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
            unprocessedInput.PointerReleased += UnprocessedInput_PointerReleased;
            unprocessedInput.PointerExited += UnprocessedInput_PointerExited;
            unprocessedInput.PointerLost += UnprocessedInput_PointerLost;
        }

        private void PenOrEraser_Clicked(object sender, RoutedEventArgs e)
        {
            UnBindLast();

            inkCanvas.Visibility = Visibility.Visible;

            if (currentToolChosen == "pen")
            {
                _inkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
            }
            else
            {
                _inkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
            }
        }

        private void UnprocessedInput_PointerLost(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (_isErasing)
            {
                args.Handled = true;
            }

            _isErasing = false;
        }

        private void UnprocessedInput_PointerExited(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (_isErasing)
            {
                args.Handled = true;
            }

            _isErasing = true;
        }

        private void UnprocessedInput_PointerReleased(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (_isErasing)
            {
                args.Handled = true;
            }

            _isErasing = false;
        }

        private void UnprocessedInput_PointerMoved(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (!_isErasing)
            {
                return;
            }

            var invalidate = false;

            foreach (var item in _strokes.ToArray())
            {
                var rect = item.SelectWithLine(_lastPoint, args.CurrentPoint.Position);

                if (rect.IsEmpty)
                {
                    continue;
                }

                if (rect.Width * rect.Height > 0)
                {
                    _strokes.Remove(item);

                    invalidate = true;
                }
            }

            _lastPoint = args.CurrentPoint.Position;

            args.Handled = true;

            if (invalidate)
            {
                DrawingCanvas.Invalidate();
            }
        }

        private void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            _lastPoint = args.CurrentPoint.Position;

            args.Handled = true;

            _isErasing = true;
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            _pendingDry = _inkSynchronizer.BeginDry();

            var container = new InkStrokeContainer();

            foreach (var stroke in _pendingDry)
            {
                container.AddStroke(stroke.Clone());
            }

            _strokes.Add(container);

            DrawingCanvas.Invalidate();
        }

        private void DrawCanvas(CanvasControl sender, CanvasDrawEventArgs args)
        {
            DrawInk(args.DrawingSession);

            if (_pendingDry != null && _deferredDryDelay == 0)
            {
                args.DrawingSession.DrawInk(_pendingDry);

                _deferredDryDelay = 1;

                CompositionTarget.Rendering += DeferEndDry;
            }
        }

        private void DeferEndDry(object sender, object e)
        {
            Debug.Assert(_pendingDry != null);

            if (_deferredDryDelay > 0)
            {
                _deferredDryDelay--;
            }
            else
            {
                CompositionTarget.Rendering -= DeferEndDry;
                _pendingDry = null;

                _inkSynchronizer.EndDry();
            }
        }

        private void DrawInk(CanvasDrawingSession session)
        {
            session.Clear(DrawingCanvas.ClearColor);

            foreach (var item in _strokes)
            {
                var strokes = item.GetStrokes();

                using (var list = new CanvasCommandList(session))
                {
                    using (var listSession = list.CreateDrawingSession())
                    {
                        listSession.DrawInk(strokes);
                    }

                    using (var shadowEffect = new ShadowEffect
                    {
                        ShadowColor = Colors.DarkRed,
                        Source = list,
                    })
                    {
                        session.DrawImage(shadowEffect, new Vector2(2, 2));
                    }
                }

                session.DrawInk(strokes);
            }
        }

        // This is how to redraw all strokes, if you needed to change them or add/remove an effect.
        void RedrawAllStrokes()
        {
            _pendingDry = _inkSynchronizer.BeginDry();
            var container = new InkStrokeContainer();
            foreach (var stroke in _pendingDry)
            {
                container.AddStroke(stroke.Clone());
            }
            _strokes.Add(container);
            DrawingCanvas.Invalidate();
        }

        #endregion

        #region Arrow and Line

        private void ArrowOrLine_Clicked(object sender, RoutedEventArgs e)
        {
            UnBindLast();
        }

        private void gridForOtherInput_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            (sender as Grid).CapturePointer(e.Pointer);

            lineForArrow = new Line();
            if (currentToolChosen == "arrow")
            {
                lineForArrow.StrokeEndLineCap = PenLineCap.Triangle;
            }
            lineForArrow.Stroke = borderForStrokeColor.Background;
            lineForArrow.StrokeThickness = currentSizeSelected;
            lineForArrow.X1 = e.GetCurrentPoint(gridMain).RawPosition.X;
            lineForArrow.Y1 = e.GetCurrentPoint(gridMain).RawPosition.Y;
            lineForArrow.X2 = e.GetCurrentPoint(gridMain).RawPosition.X;
            lineForArrow.Y2 = e.GetCurrentPoint(gridMain).RawPosition.Y;
            gridMain.Children.Add(lineForArrow);
            BringToFront(lineForArrow);

            // Set these variables for use in PointerMoved event
            topLineLimiter = currentSizeSelected / 2;
            bottomLineLimiter = gridMain.ActualHeight - (currentSizeSelected / 2);
            leftLineLimiter = currentSizeSelected / 2;
            rightLineLimiter = gridMain.ActualWidth - (currentSizeSelected / 2);
            firstY = e.GetCurrentPoint(gridMain).RawPosition.Y;
            firstX = e.GetCurrentPoint(gridMain).RawPosition.X;

            // Add the pointer moved event
            gridForOtherInput.PointerMoved += gridForOtherInput_PointerMoved;
        }

        void BringToFront(UIElement element)
        {
            int myZ = Canvas.GetZIndex(element);
            int ZWeAreChecking = 0;
            int maxZ = 0;

            for (int i = 0; i < gridMain.Children.Count; i++)
            {
                UIElement childWeAreChecking = gridMain.Children[i] as UIElement;
                ZWeAreChecking = Canvas.GetZIndex(childWeAreChecking);
                if (maxZ < ZWeAreChecking)
                {
                    maxZ = ZWeAreChecking;
                }
            }

            if (myZ < maxZ)
            {
                Canvas.SetZIndex(element, maxZ + 1);
            }
        }

        private void gridForOtherInput_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (lineForArrow != null)
            {
                var secondY = e.GetCurrentPoint(gridMain).RawPosition.Y;
                var secondX = e.GetCurrentPoint(gridMain).RawPosition.X;

                var yChange = secondY - firstY;
                var xChange = secondX - firstX;

                var yAdjust = yChange + firstY;
                var xAdjust = xChange + firstX;

                if (yAdjust >= topLineLimiter && yAdjust <= bottomLineLimiter)
                {
                    lineForArrow.Y2 = e.GetCurrentPoint(gridMain).RawPosition.Y;
                }

                if (xAdjust >= leftLineLimiter && xAdjust <= rightLineLimiter)
                {
                    lineForArrow.X2 = e.GetCurrentPoint(gridMain).RawPosition.X;
                }
            }
        }

        private void gridForOtherInput_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // Remove the pointer moved event
            gridForOtherInput.PointerMoved -= gridForOtherInput_PointerMoved;

            if (currentToolChosen == "arrow")
            {
                // Determine the length to make the arrow head lines
                int arrowHeadLength =
                    currentSizeSelected == 1 ? 10
                  : currentSizeSelected == 2 ? 8
                  : currentSizeSelected == 6 ? 6
                  : currentSizeSelected == 10 ? 4
                  : 12;

                Point ptA = new Point(lineForArrow.X1, lineForArrow.Y1);
                Point ptB = new Point(lineForArrow.X2, lineForArrow.Y2);

                // Find the arrow shaft unit vector.
                float vx = (float)(ptB.X - ptA.X);
                float vy = (float)(ptB.Y - ptA.Y);
                float dist = (float)Math.Sqrt(vx * vx + vy * vy);
                vx /= dist;
                vy /= dist;

                var length = arrowHeadLength;
                float ax = length * (-vy - vx);
                float ay = length * (vx - vy);
                Point pointArrow1 = new Point(ptB.X + ax, ptB.Y + ay);
                Point pointArrow2 = new Point(ptB.X - ay, ptB.Y + ax);

                Polyline arrowHead = new Polyline();
                arrowHead.StrokeLineJoin = PenLineJoin.Miter;
                arrowHead.Stroke = borderForStrokeColor.Background;
                arrowHead.Fill = borderForStrokeColor.Background;
                arrowHead.StrokeThickness = currentSizeSelected;
                arrowHead.Points.Add(pointArrow1);
                arrowHead.Points.Add(ptB);
                arrowHead.Points.Add(pointArrow2);
                arrowHead.Points.Add(pointArrow1);
                arrowHead.Points.Add(ptB);

                gridMain.Children.Add(arrowHead);
                BringToFront(arrowHead);

                (sender as Grid).ReleasePointerCapture(e.Pointer);
            }
        }

        #endregion

        #region Adding TextBox, Rectangles, or Ellipse

        private void text_Click(object sender, RoutedEventArgs e)
        {
            UnBindLast();
            TextBox textBox = new TextBox();
            textBox.Style = App.Current.Resources["styleTextBox"] as Style;
            Bind(textBox);

            PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
            paintObject.Content = textBox;
            gridMain.Children.Add(paintObject);

            textBox.SizeChanged += TextBox_SizeChanged;
        }

        private void TextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TextBox tBox = sender as TextBox;

            if (tBox.MinHeight == 1) tBox.Padding = new Thickness(6, 0, 6, 2);
            if (tBox.MinHeight == 2) tBox.Padding = new Thickness(6, 0, 6, 2);
            if (tBox.MinHeight == 6) tBox.Padding = new Thickness(14, 2, 14, 6);
            if (tBox.MinHeight == 10) tBox.Padding = new Thickness(16, 2, 16, 10);
        }

        private void ellipse_Click(object sender, RoutedEventArgs e)
        {
            UnBindLast();
            Ellipse ell = new Ellipse();
            Bind(ell);

            PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
            paintObject.Width = 200;
            paintObject.Height = 200;
            paintObject.Content = ell;
            paintObject.OpacitySliderIsVisible = true;
            gridMain.Children.Add(paintObject);
        }

        private void RectOrRoundRect_Clicked(object sender, RoutedEventArgs e)
        {
            UnBindLast();
            Rectangle rectangle = new Rectangle();
            if (currentToolChosen == "roundedRectangle")
            {
                rectangle.RadiusX = 20;
                rectangle.RadiusY = 20;
            }
            Bind(rectangle);

            PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
            paintObject.Width = 200;
            paintObject.Height = 200;
            paintObject.Content = rectangle;
            paintObject.OpacitySliderIsVisible = true;
            gridMain.Children.Add(paintObject);
        }

        #endregion

        #region Bind and UnBindLast

        void Bind(FrameworkElement target)
        {
            if (target is Shape) // Rectangle, Ellipse, Line, or Polyline
            {
                Binding bindStroke = new Binding();
                bindStroke.Source = borderForStrokeColor;
                bindStroke.Path = new PropertyPath("Background");
                target.SetBinding(Shape.StrokeProperty, bindStroke);

                Binding bindFill = new Binding();
                bindFill.Source = borderForFillColor;
                bindFill.Path = new PropertyPath("Background");
                target.SetBinding(Shape.FillProperty, bindFill);

                Binding bindStrokeSize = new Binding();
                bindStrokeSize.Source = this;
                bindStrokeSize.Path = new PropertyPath("currentSizeSelected");
                target.SetBinding(Shape.StrokeThicknessProperty, bindStrokeSize);
            }

            if (target is TextBox)
            {
                Binding bindStroke = new Binding();
                bindStroke.Source = borderForStrokeColor;
                bindStroke.Path = new PropertyPath("Background");
                target.SetBinding(BorderBrushProperty, bindStroke);

                Binding bindStrokeSize = new Binding();
                bindStrokeSize.Source = this;
                bindStrokeSize.Path = new PropertyPath("currentSizeSelected");
                target.SetBinding(MinHeightProperty, bindStrokeSize);

                Binding bindFill = new Binding();
                bindFill.Source = borderForFillColor;
                bindFill.Path = new PropertyPath("Background");
                target.SetBinding(BackgroundProperty, bindFill);

                Binding bindTextColor = new Binding();
                bindTextColor.Source = borderForTextColor;
                bindTextColor.Path = new PropertyPath("Background");
                target.SetBinding(ForegroundProperty, bindTextColor);

                Binding bindFontSize = new Binding();
                bindFontSize.Source = this;
                bindFontSize.Path = new PropertyPath("currentFontSize");
                target.SetBinding(FontSizeProperty, bindFontSize);
            }
        }

        void UnBindLast()
        {
            if (gridMain != null && gridMain.Children.Count > 0)
            {
                var lastChild = gridMain.Children.LastOrDefault();

                if (lastChild is PaintObjectTemplatedControl)
                {
                    var child = lastChild as PaintObjectTemplatedControl;
                    var content = child.Content;

                    if (content is Shape) // Rectangle or Ellipse
                    {
                        Shape s = content as Shape;
                        s.Stroke = s.Stroke;
                        s.Fill = s.Fill;
                        s.StrokeThickness = s.StrokeThickness;
                    }

                    if (content is TextBox)
                    {
                        TextBox t = content as TextBox;
                        t.Background = t.Background;
                        t.Foreground = t.Foreground;
                        t.BorderBrush = t.BorderBrush;
                        t.MinHeight = t.MinHeight; //MinHeight property is used for BorderThickness Binding because the border in the custom textbox style is accomplished by a rectangle strokethickness which is a double not a Thickness
                        t.FontSize = t.FontSize;
                    }
                }
                //if (lastChild is Polyline)
                //{
                //    var p = lastChild as Polyline;
                //    p.Stroke = p.Stroke;
                //    p.Fill = p.Fill;
                //    p.StrokeThickness = p.StrokeThickness;

                //    if (gridMain.Children.Count >= 2)
                //    {
                //        var secondToLastChild = gridMain.Children[gridMain.Children.Count - 2];

                //        if (secondToLastChild is Line)
                //        {
                //            var l = secondToLastChild as Line;
                //            l.Stroke = l.Stroke;
                //            l.Fill = l.Fill;
                //            l.StrokeThickness = l.StrokeThickness;
                //        }
                //    }

                //}
                //if (lastChild is Line)
                //{
                //    var l = lastChild as Line;
                //    l.Stroke = l.Stroke;
                //    l.Fill = l.Fill;
                //    l.StrokeThickness = l.StrokeThickness;
                //}
            }
        }

        #endregion

        #region Size and Color selections

        private void sizes_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            double size = Convert.ToDouble(radioButton.Tag);

            currentSizeSelected = size;
            currentFontSize = size;

            if (inkCanvas != null)
            {
                InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                drawingAttributes.Size = new Size(size, size);
                inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
            }
        }

        private void color_Click(object sender, RoutedEventArgs e)
        {
            InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();

            Button colorButton = sender as Button;
            SolidColorBrush chosenColor = colorButton.Background as SolidColorBrush;

            if (colorButton.Name != null)
            {
                if (colorButton.Name == "btnTransparent")
                {
                    switch (ColorChangerBox)
                    {
                        case "strokeColorRB":
                            strokeX.Visibility = Visibility.Visible;
                            borderForStrokeColor.Background = chosenColor;
                            drawingAttributes.Color = chosenColor.Color;
                            break;
                        case "fillColorRB":
                            fillX.Visibility = Visibility.Visible;
                            borderForFillColor.Background = chosenColor;
                            break;
                        case "textColorRB":
                            textX.Visibility = Visibility.Visible;
                            borderForTextColor.Background = chosenColor;
                            break;
                        case "pageColorRB":
                            borderForPageColor.Background = new SolidColorBrush(Colors.White);
                            break;
                    }
                }
                else
                {
                    switch (ColorChangerBox)
                    {
                        case "strokeColorRB":
                            strokeX.Visibility = Visibility.Collapsed;
                            borderForStrokeColor.Background = chosenColor;
                            drawingAttributes.Color = chosenColor.Color;
                            break;
                        case "fillColorRB":
                            fillX.Visibility = Visibility.Collapsed;
                            borderForFillColor.Background = chosenColor;
                            break;
                        case "textColorRB":
                            textX.Visibility = Visibility.Collapsed;
                            borderForTextColor.Background = chosenColor;
                            break;
                        case "pageColorRB":
                            borderForPageColor.Background = chosenColor;
                            break;
                    }
                }
            }
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
        }

        private void colorChanger_Checked(object sender, RoutedEventArgs e)
        {
            var colorChangerBoxButton = sender as RadioButton;
            ColorChangerBox = colorChangerBoxButton.Name;
        }

        #endregion

        private void tool_Checked(object sender, RoutedEventArgs e)
        {
            string name = (sender as RadioButton).Name;
            switch (name)
            {
                case "cursor":
                    currentToolChosen = "cursor";
                    UnBindLast();
                    break;
                case "text":
                    currentToolChosen = "text";
                    break;
                case "arrow":
                    currentToolChosen = "arrow";
                    gridForOtherInput.Visibility = Visibility.Visible;
                    break;
                case "ellipse":
                    currentToolChosen = "ellipse";
                    break;
                case "roundedRectangle":
                    currentToolChosen = "roundedRectangle";
                    break;
                case "rectangle":
                    currentToolChosen = "rectangle";
                    break;
                case "line":
                    currentToolChosen = "line";
                    gridForOtherInput.Visibility = Visibility.Visible;
                    break;
                case "eraser":
                    currentToolChosen = "eraser";
                    break;
                case "crop":
                    currentToolChosen = "crop";
                    break;
                case "pen":
                    currentToolChosen = "pen";
                    break;
            }
            // Collapse stuff that may be in the way
            if (currentToolChosen != "arrow" && currentToolChosen != "line")
            {
                if (gridForOtherInput != null)
                {
                    if (gridForOtherInput.Visibility == Visibility.Visible)
                    {
                        gridForOtherInput.Visibility = Visibility.Collapsed;
                    }
                }
            }
            if (currentToolChosen != "pen" && currentToolChosen != "eraser")
            {
                if (inkCanvas != null)
                {
                    if (inkCanvas.Visibility == Visibility.Visible)
                    {
                        inkCanvas.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private async void crop_Click(object sender, RoutedEventArgs e)
        {
            if (imageCollection.Count == 0)
            {
                var dialog = await new MessageDialog("Please open an image first.").ShowAsync();
            }
            else if (imageCollection.Count == 1)
            {
                gridForCrop.Visibility = Visibility.Visible;
                var path = imageCollection[0].FilePath;
                LoadImageIntoCropper(path);
            }
            else if (imageCollection.Count > 1)
            {
                gridForCrop.Visibility = Visibility.Visible;
                gridviewImages.ItemsSource = imageCollection;
                gridviewImages.Visibility = Visibility.Visible;
                btnCrop.IsEnabled = false;
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            btnCrop.IsEnabled = false;
            btnBack.IsEnabled = false;
            tblockFileName.Text = "Please choose an image";
            if (gridImageContainer.Children.Count > 1)
            {
                gridImageContainer.Children.RemoveAt(1);
            }
            gridviewImages.Visibility = Visibility.Visible;
        }

        private async void btnCrop_Click(object sender, RoutedEventArgs e)
        {
            Image imageMain = null;
            string fileName = tblockFileName.Text;

            foreach (var child in gridImageContainer.Children)
            {
                if (child is Image)
                {
                    imageMain = child as Image;
                }
            }

            // Get the top left point of the crop area
            GeneralTransform gt = rectCropArea.TransformToVisual(imageMain);
            Point cropPt = gt.TransformPoint(new Point(0, 0));

            try
            {
                StorageFile cropFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);

                using (IRandomAccessStream streamForFile = await cropFile.OpenReadAsync())
                {
                    BitmapDecoder bitmapDecoder = await BitmapDecoder.CreateAsync(streamForFile);

                    // Figure out the current scale of the image compared to the saved image
                    double scale = imageMain.ActualWidth / bitmapDecoder.PixelWidth;

                    // Set the size and point of the crop area with the scale factored in
                    double cropWidth = rectCropArea.ActualWidth / scale;
                    double cropHeight = rectCropArea.ActualHeight / scale;
                    double cropX = cropPt.X / scale;
                    double cropY = cropPt.Y / scale;

                    using (InMemoryRandomAccessStream streamForNewImage = new InMemoryRandomAccessStream())
                    {
                        BitmapEncoder bitmapEncoder = await BitmapEncoder.CreateForTranscodingAsync(streamForNewImage, bitmapDecoder);

                        BitmapBounds bitmapBounds = new BitmapBounds();
                        bitmapBounds.Height = (uint)cropHeight;
                        bitmapBounds.Width = (uint)cropWidth;
                        bitmapBounds.X = (uint)cropX;
                        bitmapBounds.Y = (uint)cropY;
                        bitmapEncoder.BitmapTransform.Bounds = bitmapBounds;

                        try
                        {
                            await bitmapEncoder.FlushAsync();
                        }
                        catch (Exception ex)
                        {
                            var dialog = new MessageDialog("Encoder error." + ex.Message);
                            await dialog.ShowAsync();
                        }

                        // Render the stream to the screen
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.SetSource(streamForNewImage);
                        foreach(var thing in gridMain.Children)
                        {
                            if (thing is PaintObjectTemplatedControl)
                            {
                                var control = thing as PaintObjectTemplatedControl;
                                if (control.Content is Image)
                                {
                                    if (control.ImageFileName == fileName)
                                    {
                                        var img = control.Content as Image;
                                        img.Source = bitmapImage;
                                        control.Width = bitmapImage.PixelWidth * control.ImageScale;
                                        control.Height = bitmapImage.PixelHeight * control.ImageScale;

                                        StorageFile file1 = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                                        using (var fileStream1 = await file1.OpenAsync(FileAccessMode.ReadWrite))
                                        {
                                            await RandomAccessStream.CopyAndCloseAsync(streamForNewImage.GetInputStreamAt(0), fileStream1.GetOutputStreamAt(0));
                                        }

                                        RemoveImageFromCollection(control);
                                        string newName = file1.Name;
                                        string newPath = "ms-appdata:///local/" + newName;
                                        control.ImageFileName = newName;
                                        control.ImageFilePath = newPath;
                                        imageCollection.Add(new StoredImage { FileName = newName, FilePath = newPath });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var dialog = await new MessageDialog("The cropping method ran into a problem. Cannot crop image.    " + ex.Message).ShowAsync();
            }

            CloseCropper();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseCropper();
        }

        private void gridviewImages_ItemClick(object sender, ItemClickEventArgs e)
        {
            StoredImage storedImage = e.ClickedItem as StoredImage;
            string filePath = storedImage.FilePath;
            LoadImageIntoCropper(filePath);
            gridviewImages.Visibility = Visibility.Collapsed;
            btnBack.IsEnabled = true;
            btnCrop.IsEnabled = true;
        }

        async void LoadImageIntoCropper(string filePath)
        {
            string fileName = filePath.Substring(20);

            StorageFile imgFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
            var properties = await imgFile.Properties.GetImagePropertiesAsync();

            if (properties.Width < 42 || properties.Height < 42)
            {
                MessageDialog tooSmallMessage = new MessageDialog("Image too small. Please choose a larger image.");
                await tooSmallMessage.ShowAsync();
            }
            else
            {
                tblockFileName.Text = fileName;

                double height = 0;
                double width = 0;

                Image img = new Image();
                using (IRandomAccessStream IRASstream = await imgFile.OpenAsync(FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(IRASstream);
                    height = bitmapImage.PixelHeight;
                    width = bitmapImage.PixelWidth;
                    img.Source = bitmapImage;
                }

                if (width > gridCropping.ActualWidth || height > gridCropping.ActualHeight)
                {
                    double scale = Math.Min(gridCropping.ActualWidth / width, gridCropping.ActualHeight / height);
                    width = (width * scale) - 1;
                    height = (height * scale) - 1;
                }

                img.Width = width;
                img.Height = height;

                if (width < 82 || height < 82)
                {
                    controlCropOutline.Width = 41;
                    controlCropOutline.Height = 41;
                }
                else
                {
                    controlCropOutline.Width = width / 2;
                    controlCropOutline.Height = height / 2;
                }

                gridImageContainer.Children.Add(img);
                gridImageContainer.Width = width;
                gridImageContainer.Height = height;
                controlCropOutline.transform_myControl.TranslateX = 0;
                controlCropOutline.transform_myControl.TranslateY = 0;

                foreach (Rectangle r in gridCrop.Children)
                {
                    if (r.Visibility == Visibility.Collapsed)
                    {
                        r.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void PaintObject_Unloaded(object sender, RoutedEventArgs e)
        {
            var paintObj = sender as PaintObjectTemplatedControl;
            RemoveImageFromCollection(paintObj);
        }

        private void PaintObjectTemplatedControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            foreach (Rectangle r in gridCrop.Children)
            {
                if (r.Width == 12)
                {
                    r.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void PaintObjectTemplatedControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            foreach (Rectangle r in gridCrop.Children)
            {
                if (r.Visibility == Visibility.Collapsed)
                {
                    r.Visibility = Visibility.Visible;
                }
            }
        }

        void CloseCropper()
        {
            btnCrop.IsEnabled = true;
            btnBack.IsEnabled = false;
            tblockFileName.Text = "Please choose an image";
            if (gridImageContainer.Children.Count > 1)
            {
                gridImageContainer.Children.RemoveAt(1);
            }
            gridviewImages.Visibility = Visibility.Collapsed;
            gridForCrop.Visibility = Visibility.Collapsed;
        }

        void RemoveImageFromCollection(PaintObjectTemplatedControl paintObj)
        {
            if (paintObj.Content is Image)
            {
                int index = 0;
                int pos = 0;

                foreach (StoredImage storedImage in imageCollection)
                {
                    if (storedImage.FileName == paintObj.ImageFileName)
                    {
                        pos = index;
                    }
                    else
                    {
                        index++;
                    }
                }

                imageCollection.RemoveAt(pos);
            }
        }
    }
}
