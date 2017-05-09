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

namespace WaveformOverlaysPlus
{
    public sealed partial class MainPage : Page
    {
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
            set
            {
                value = value == 1 ? 2 :
                        value == 2 ? 3 :
                        value == 6 ? 4 :
                        value == 10 ? 6 :
                        4;

                SetValue(currentSizeSelectedProperty, value);
            }
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

                if (_width < 40 || _height < 40)
                {
                    MessageDialog tooSmallMessage = new MessageDialog("Image too small. Please choose a larger image.");
                    await tooSmallMessage.ShowAsync();
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

                await imgFile.CopyAsync(ApplicationData.Current.LocalCacheFolder, "desiredNewName", NameCollisionOption.ReplaceExisting);
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

        #region Ink and Eraser

        //  How to update inkCanvas drawingAttributes
        ///  
        //if (inkCanvas != null)
        //{
        //    InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
        //    drawingAttributes.DrawAsHighlighter = false;
        //    drawingAttributes.PenTip = PenTipShape.Circle;
        //    drawingAttributes.Color = Colors.DodgerBlue;
        //    drawingAttributes.Size = new Size(2, 2);
        //    drawingAttributes.IgnorePressure = false;
        //    drawingAttributes.FitToCurve = true;
        //    inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
        //}

        void AddInk()
        {
            UnBindLast();

            inkCanvas.Visibility = Visibility.Visible;
            _inkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
        }

        void AddEraser()
        {
            inkCanvas.Visibility = Visibility.Visible;
            _inkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
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

        private void gridForOtherInput_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            UnBindLast();

            (sender as Grid).CapturePointer(e.Pointer);

            lineForArrow = new Line();
            if (currentToolChosen == "arrow")
            {
                lineForArrow.StrokeEndLineCap = PenLineCap.Triangle;
            }

            Bind(lineForArrow);

            lineForArrow.X1 = e.GetCurrentPoint(gridMain).RawPosition.X;
            lineForArrow.Y1 = e.GetCurrentPoint(gridMain).RawPosition.Y;
            lineForArrow.X2 = e.GetCurrentPoint(gridMain).RawPosition.X;
            lineForArrow.Y2 = e.GetCurrentPoint(gridMain).RawPosition.Y;
            gridMain.Children.Add(lineForArrow);

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
                    currentSizeSelected == 2 ? 13
                  : currentSizeSelected == 3 ? 13
                  : currentSizeSelected == 4 ? 14
                  : currentSizeSelected == 6 ? 15
                  : 15;

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
                arrowHead.StrokeThickness = 6;
                arrowHead.Points.Add(pointArrow1);
                arrowHead.Points.Add(ptB);
                arrowHead.Points.Add(pointArrow2);
                arrowHead.Points.Add(pointArrow1);
                arrowHead.Points.Add(ptB);
                Bind(arrowHead);

                gridMain.Children.Add(arrowHead);

                (sender as Grid).ReleasePointerCapture(e.Pointer);
            }
        }

        #endregion

        private void tool_Checked(object sender, RoutedEventArgs e)
        {
            // Collapse stuff that may be in the way
            if (inkCanvas != null)
            {
                inkCanvas.Visibility = Visibility.Collapsed;
            }
            if (gridForOtherInput != null)
            {
                gridForOtherInput.Visibility = Visibility.Collapsed;
            }

            string name = (sender as RadioButton).Name;
            switch (name)
            {
                case "cursor":
                    currentToolChosen = "cursor";
                    SwitchToCursorMode();
                    break;
                case "text":
                    currentToolChosen = "text";
                    AddTextBox();
                    break;
                case "arrow":
                    currentToolChosen = "arrow";
                    SwitchToArrowAndLineMode();
                    break;
                case "ellipse":
                    currentToolChosen = "ellipse";
                    AddEllipse();
                    break;
                case "roundedRectangle":
                    currentToolChosen = "roundedRectangle";
                    AddRoundedRectangle();
                    break;
                case "rectangle":
                    currentToolChosen = "rectangle";
                    AddRectangle();
                    break;
                case "line":
                    currentToolChosen = "line";
                    SwitchToArrowAndLineMode();
                    break;
                case "eraser":
                    currentToolChosen = "eraser";
                    AddEraser();
                    break;
                case "crop":
                    currentToolChosen = "crop";

                    break;
                case "pen":
                    currentToolChosen = "pen";
                    AddInk();
                    break;
            }
        }

        void AddTextBox()
        {
            UnBindLast();
            TextBox textBox = new TextBox();
            textBox.Style = App.Current.Resources["styleTextBox"] as Style;
            Bind(textBox);

            PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
            paintObject.Content = textBox;
            gridMain.Children.Add(paintObject);
        }

        void AddRectangle()
        {
            UnBindLast();
            Rectangle rectangle = new Rectangle();
            Bind(rectangle);

            PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
            paintObject.Width = 200;
            paintObject.Height = 200;
            paintObject.Content = rectangle;
            gridMain.Children.Add(paintObject);
        }

        void AddRoundedRectangle()
        {
            UnBindLast();
            Rectangle rectangle = new Rectangle();
            rectangle.RadiusX = 20;
            rectangle.RadiusY = 20;
            Bind(rectangle);

            PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
            paintObject.Width = 200;
            paintObject.Height = 200;
            paintObject.Content = rectangle;
            gridMain.Children.Add(paintObject);
        }

        void AddEllipse()
        {
            UnBindLast();
            Ellipse ell = new Ellipse();
            Bind(ell);

            PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
            paintObject.Width = 200;
            paintObject.Height = 200;
            paintObject.Content = ell;
            gridMain.Children.Add(paintObject);
        }

        void SwitchToArrowAndLineMode()
        {
            UnBindLast();
            gridForOtherInput.Visibility = Visibility.Visible;
        }

        void SwitchToCursorMode()
        {
            UnBindLast();
        }

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
            if (gridMain.Children.Count > 0)
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
                if (lastChild is Polyline)
                {
                    var p = lastChild as Polyline;
                    p.Stroke = p.Stroke;
                    p.Fill = p.Fill;
                    p.StrokeThickness = p.StrokeThickness;

                    if (gridMain.Children.Count >= 2)
                    {
                        var secondToLastChild = gridMain.Children[gridMain.Children.Count - 2];

                        if (secondToLastChild is Line)
                        {
                            var l = secondToLastChild as Line;
                            l.Stroke = l.Stroke;
                            l.Fill = l.Fill;
                            l.StrokeThickness = l.StrokeThickness;
                        }
                    }
                        
                }
                if (lastChild is Line)
                {
                    var l = lastChild as Line;
                    l.Stroke = l.Stroke;
                    l.Fill = l.Fill;
                    l.StrokeThickness = l.StrokeThickness;
                }
            }
        }
    }
}
