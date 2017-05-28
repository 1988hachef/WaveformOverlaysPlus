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
using Windows.UI.Xaml.Markup;

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
        string nameOfFile;
        Grid gridCylinderIdOverlay;
        TextBlock label;
        TextBlock label1;
        TextBlock label2;
        TextBlock label3;
        TextBlock label4;
        TextBlock label5;
        TextBlock label6;
        TextBlock label7;
        TextBlock label8;
        TextBlock label9;
        TextBlock label10;
        TextBlock label11;

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

        #region For Rulers

        string gripName;
        Line rulerLine;
        Shape gripShape;
        CompositeTransform rulerTransform;
        double UnitsPerX;
        double UnitsPerY;
        double amountBetweenVs;
        double amountBetweenHs;
        double VstartValue;
        double HstartValue;

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
            gridCover.Visibility = Visibility.Visible;
            gridBranding.Visibility = Visibility.Visible;

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
            finally
            {
                gridBranding.Visibility = Visibility.Collapsed;
                gridCover.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Print

        private async void PrintButtonClick(object sender, RoutedEventArgs e)
        {
            gridCover.Visibility = Visibility.Visible;
            gridBranding.Visibility = Visibility.Visible;

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
                gridBranding.Visibility = Visibility.Collapsed;
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
                    gridBranding.Visibility = Visibility.Collapsed;
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
                    gridBranding.Visibility = Visibility.Collapsed;
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
                gridBranding.Visibility = Visibility.Collapsed;
                gridCover.Visibility = Visibility.Collapsed;

                deferral.Complete();
            }
        }

        private async void menuShare_Click(object sender, RoutedEventArgs e)
        {
            gridCover.Visibility = Visibility.Visible;
            gridBranding.Visibility = Visibility.Visible;

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
            gridCover.Visibility = Visibility.Visible;
            gridBranding.Visibility = Visibility.Visible;

            StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("TempImgFile", CreationCollisionOption.ReplaceExisting);
            await ImageUtils.CaptureElementToFile(gridForOverall, file);

            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(file));

            Clipboard.SetContent(dataPackage);

            gridBranding.Visibility = Visibility.Collapsed;
            gridCover.Visibility = Visibility.Collapsed;
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

        private async void Page_Loaded(object sender, RoutedEventArgs e)
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

            // Delete old files from LocalFolder
            var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();

            foreach (var file in files)
            {
                await file.DeleteAsync(StorageDeleteOption.Default);
            }

            // Set these to initial values
            gridCompressionOverlay.Width = gridMain.ActualWidth;
            gridCompressionOverlay.Height = gridMain.ActualHeight;
            SetAmountBetween(tboxHpos);
            SetAmountBetween(tboxVpos);
            

            transformExh.TranslateX = 140 / UnitsPerX;
            gridExhOverlap.Width = 230 / UnitsPerX;
            transformInt.TranslateX = 350 / UnitsPerX;
            gridIntOverlap.Width = 235 / UnitsPerX;

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
            lineForArrow.IsHitTestVisible = false;
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
                arrowHead.IsHitTestVisible = false;
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

        #region Crop

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

        #endregion

        #region Ruler Manipulations

        void SetUnitsPerX()
        {
            GeneralTransform gt = lineVruler720.TransformToVisual(lineVrulerZero);
            Point pt = gt.TransformPoint(new Point(0, 0));

            if (pt.X != 0)
            {
                UnitsPerX = amountBetweenVs / pt.X;
            }
            else
            {
                UnitsPerX = 0;
            }
        }

        void SetUnitsPerY()
        {
            GeneralTransform gt = lineHrulerPres.TransformToVisual(lineHrulerZero);
            Point pt = gt.TransformPoint(new Point(0, 0));

            if (pt.Y != 0)
            {
                UnitsPerY = amountBetweenHs / pt.Y;
            }
            else
            {
                UnitsPerY = 0;
            }
        }

        void SetTextofPink(bool IsColoredRuler)
        {
            if (IsColoredRuler == true)
            {
                if (gripName != null)
                {
                    GeneralTransform gt1 = rulerLine.TransformToVisual(rectZeroDegrees);
                    Point point = gt1.TransformPoint(new Point(0, 0));

                    if (gripName == "rectV1") { tblockPink1.Text = (Math.Round((point.X * UnitsPerX) + VstartValue)).ToString(); }
                    if (gripName == "rectV2") { tblockPink2.Text = (Math.Round((point.X * UnitsPerX) + VstartValue)).ToString(); }
                    if (tblockPink1.Text != "--" && tblockPink2.Text != "--") { tblockPinkDelta.Text = Math.Round(Math.Abs((Convert.ToDouble(tblockPink1.Text) - Convert.ToDouble(tblockPink2.Text)))).ToString(); }
                }
            }
            else
            {
                if (tblockPink1.Text != "--")
                {
                    GeneralTransform gt1 = lineV1.TransformToVisual(rectZeroDegrees);
                    Point point = gt1.TransformPoint(new Point(0, 0));

                    tblockPink1.Text = (Math.Round((point.X * UnitsPerX) + VstartValue)).ToString();
                }
                if (tblockPink2.Text != "--")
                {
                    GeneralTransform gt1 = lineV2.TransformToVisual(rectZeroDegrees);
                    Point point = gt1.TransformPoint(new Point(0, 0));

                    tblockPink2.Text = (Math.Round((point.X * UnitsPerX) + VstartValue)).ToString();
                }
                if (tblockPink1.Text != "--" && tblockPink2.Text != "--")
                {
                    tblockPinkDelta.Text = Math.Round(Math.Abs((Convert.ToDouble(tblockPink1.Text) - Convert.ToDouble(tblockPink2.Text)))).ToString();
                }
            }
        }

        void SetTextofPurple(bool IsColoredRuler)
        {
            if (IsColoredRuler == true)
            {
                if (gripName != null)
                {
                    GeneralTransform gt1 = rulerLine.TransformToVisual(lineHrulerZero);
                    Point point = gt1.TransformPoint(new Point(0, 0));

                    if (gripName == "rectH1") { tblockPurple1.Text = (Math.Round((point.Y * UnitsPerY) + HstartValue)).ToString(); }
                    if (gripName == "rectH2") { tblockPurple2.Text = (Math.Round((point.Y * UnitsPerY) + HstartValue)).ToString(); }
                    if (tblockPurple1.Text != "--" && tblockPurple2.Text != "--") { tblockPurpleDelta.Text = Math.Round(Math.Abs((Convert.ToDouble(tblockPurple1.Text) - Convert.ToDouble(tblockPurple2.Text)))).ToString(); }
                }
            }
            else
            {
                if (tblockPurple1.Text != "--")
                {
                    GeneralTransform gt1 = lineH1.TransformToVisual(lineHrulerZero);
                    Point point = gt1.TransformPoint(new Point(0, 0));

                    tblockPurple1.Text = (Math.Round((point.Y * UnitsPerY) + HstartValue)).ToString();
                }
                if (tblockPurple2.Text != "--")
                {
                    GeneralTransform gt1 = lineH2.TransformToVisual(lineHrulerZero);
                    Point point = gt1.TransformPoint(new Point(0, 0));

                    tblockPurple2.Text = (Math.Round((point.Y * UnitsPerY) + HstartValue)).ToString();
                }
                if (tblockPurple1.Text != "--" && tblockPurple2.Text != "--")
                {
                    tblockPurpleDelta.Text = Math.Round(Math.Abs((Convert.ToDouble(tblockPurple1.Text) - Convert.ToDouble(tblockPurple2.Text)))).ToString();
                }
            }
        }

        private void tbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = sender as TextBox;
            SetAmountBetween(textbox);
        }

        void SetAmountBetween(TextBox textbox)
        {
            if (textbox.Name.StartsWith("tboxV"))
            {
                try
                {
                    double low = Convert.ToDouble(tboxVzero.Text);
                    double high = Convert.ToDouble(tboxVpos.Text);
                    amountBetweenVs = high - low;
                    VstartValue = low;
                }
                catch
                {
                    amountBetweenVs = 0;
                    VstartValue = 0;
                }

                SetUnitsPerX();
                SetTextofPink(false);
            }
            else if (textbox.Name.StartsWith("tboxH"))
            {
                try
                {
                    double low = Convert.ToDouble(tboxHzero.Text);
                    double high = Convert.ToDouble(tboxHpos.Text);
                    amountBetweenHs = high - low;
                    HstartValue = low;
                }
                catch
                {
                    amountBetweenHs = 0;
                    HstartValue = 0;
                }

                SetUnitsPerY();
                SetTextofPurple(false);
            }
        }

        void MoveSideToSide(Shape shape, CompositeTransform transform, ManipulationDeltaRoutedEventArgs e)
        {
            GeneralTransform gt = shape.TransformToVisual(gridMain);
            Point prisonerTopLeftPoint = gt.TransformPoint(new Point(0, 0));

            double left = prisonerTopLeftPoint.X;
            double right = left + shape.ActualWidth;
            double leftAdjust = left + e.Delta.Translation.X;
            double rightAdjust = right + e.Delta.Translation.X;

            if ((leftAdjust >= 0) && (rightAdjust <= gridMain.ActualWidth))
            {
                if (gripName == "polygonVzero")
                {
                    var minWidth = (prisonerTopLeftPoint.X + gridCompressionOverlay.ActualWidth) - 5;
                    if (rightAdjust <= minWidth)
                    {
                        transformComp.TranslateX += e.Delta.Translation.X;
                        gridCompressionOverlay.Width = gridCompressionOverlay.ActualWidth - e.Delta.Translation.X;

                        transform.TranslateX += e.Delta.Translation.X;
                    }
                }
                else if (gripName == "polygonV720")
                {
                    var minWidth = (prisonerTopLeftPoint.X - gridCompressionOverlay.ActualWidth) + 5;
                    if (leftAdjust >= minWidth)
                    {
                        gridCompressionOverlay.Width = gridCompressionOverlay.ActualWidth + e.Delta.Translation.X;

                        transform.TranslateX += e.Delta.Translation.X;
                    }
                }
                else
                {
                    transform.TranslateX += e.Delta.Translation.X;
                }
            }

            // Set text and units
            if (gripName.StartsWith("r"))
            {
                SetTextofPink(true);
            }
            else
            {
                SetUnitsPerX();
                SetTextofPink(false);
                SetEVOText();
                SetEVCText();
                SetIVOText();
                SetIVCText();
            }
        }

        void MoveUpAndDown(Shape shape, CompositeTransform transform, ManipulationDeltaRoutedEventArgs e)
        {
            GeneralTransform gt = shape.TransformToVisual(gridMain);
            Point prisonerTopLeftPoint = gt.TransformPoint(new Point(0, 0));

            double top = prisonerTopLeftPoint.Y;
            double bottom = top + shape.ActualHeight;
            double topAdjust = top + e.Delta.Translation.Y;
            double bottomAdjust = bottom + e.Delta.Translation.Y;

            if ((topAdjust >= 0) && (bottomAdjust <= gridMain.ActualHeight))
            {
                transform.TranslateY += e.Delta.Translation.Y;
            }

            if (gripName.StartsWith("r"))
            {
                SetTextofPurple(true);
            }
            else
            {
                SetUnitsPerY();
                SetTextofPurple(false);
            }
        }

        private void vertical_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (gripName.StartsWith("r"))
            {
                MoveSideToSide(gripShape, rulerTransform, e);
            }
            else
            {
                MoveSideToSide(rulerLine, rulerTransform, e);
            }
        }

        private void horizontal_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (gripName.StartsWith("r"))
            {
                MoveUpAndDown(gripShape, rulerTransform, e);
            }
            else
            {
                MoveUpAndDown(rulerLine, rulerTransform, e);
            }
        }

        private void rulers_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            gripShape = sender as Shape;
            gripName = gripShape.Name;
            Grid rulerContainer;

            if (gripName.StartsWith("r"))
            {
                rulerContainer = gripShape.Parent as Grid;
                rulerLine = rulerContainer.Children[0] as Line;
                gridDelta.Visibility = Visibility.Visible;
                rulerLine.Visibility = Visibility.Visible;
            }
            else
            {
                var parent = gripShape.Parent as StackPanel;
                rulerContainer = parent.Parent as Grid;
                rulerLine = rulerContainer.Children[0] as Line;
                rulerLine.Stroke = new SolidColorBrush(Colors.Gray);
            }
            rulerTransform = rulerContainer.RenderTransform as CompositeTransform;
        }

        private void rulers_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            GeneralTransform gt;
            Point p;

            if (gripName.StartsWith("r"))
            {
                gt = gripShape.TransformToVisual(gridMain);
                p = gt.TransformPoint(new Point(0, 0));

                if ((p.X < 2 && p.Y > (gridMain.ActualHeight - 12)) ||
                    (p.X < 2 && p.Y < 2) ||
                    (p.X > (gridMain.ActualWidth - 12) && p.Y > (gridMain.ActualHeight - 12)))
                {
                    rulerLine.Visibility = Visibility.Collapsed;
                    if (rulerLine.Name.StartsWith("lineV"))
                    {
                        tblockPinkDelta.Text = "--";

                        if (rulerLine.Name == "lineV1") { tblockPink1.Text = "--"; }
                        if (rulerLine.Name == "lineV2") { tblockPink2.Text = "--"; }
                    }
                    else if (rulerLine.Name.StartsWith("lineH"))
                    {
                        tblockPurpleDelta.Text = "--";

                        if (rulerLine.Name == "lineH1") { tblockPurple1.Text = "--"; }
                        if (rulerLine.Name == "lineH2") { tblockPurple2.Text = "--"; }
                    }
                }

                if (lineV1.Visibility == Visibility.Collapsed &&
                    lineV2.Visibility == Visibility.Collapsed &&
                    lineH1.Visibility == Visibility.Collapsed &&
                    lineH2.Visibility == Visibility.Collapsed)
                {
                    gridDelta.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                gt = rulerLine.TransformToVisual(gridMain);
                p = gt.TransformPoint(new Point(0, 0));

                if ((p.X < 2 && p.Y > (gridMain.ActualHeight - 2)) ||
                    (p.X > (gridMain.ActualWidth - 2) && p.Y < 2) ||
                    (p.X < 2 && p.Y < 2))
                {
                    rulerLine.Stroke = new SolidColorBrush(Colors.Transparent);
                }
            }
        }

        #endregion

        #region Comp overlays

        private void btnComp_Click(object sender, RoutedEventArgs e)
        {
            if (gridCompressionOverlay.Opacity < .5)
            {
                gridCompressionOverlay.Opacity = 1.0;
            }
            else
            {
                gridCompressionOverlay.Opacity = .001;
            }
        }

        private void btnOverlap_Click(object sender, RoutedEventArgs e)
        {
            if (gridIntOverlap.Visibility == Visibility.Collapsed)
            {
                gridIntOverlap.Visibility = Visibility.Visible;
                gridExhOverlap.Visibility = Visibility.Visible;
                tblockExh.Foreground = new SolidColorBrush(Colors.Red);
                tblockInt.Foreground = new SolidColorBrush(Colors.Blue);
            }
            else
            {
                gridIntOverlap.Visibility = Visibility.Collapsed;
                gridExhOverlap.Visibility = Visibility.Collapsed;
                tblockExh.Foreground = new SolidColorBrush(Colors.Black);
                tblockInt.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void spEVO_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            GeneralTransform gt = rectEVO.TransformToVisual(gridToContainOthers);
            Point p = gt.TransformPoint(new Point(0, 0));

            var currentXposition = p.X;
            var xAdjust = currentXposition + e.Delta.Translation.X;
            var rightLimit = currentXposition + rectRed.ActualWidth - 60;

            var currentYposition = p.Y;
            var yAdjust = currentYposition + e.Delta.Translation.Y;
            var bottomLimit = gridToContainOthers.ActualHeight - gridIntOverlap.ActualHeight - 45;

            if (xAdjust > 25 && xAdjust < rightLimit)
            {
                transformExh.TranslateX += e.Delta.Translation.X;
                gridExhOverlap.Width -= e.Delta.Translation.X;

                SetEVOText();
            }
            if (yAdjust > 1 && yAdjust < bottomLimit)
            {
                gridExhOverlap.Height -= e.Delta.Translation.Y;
            }
        }

        void SetEVOText()
        {
            GeneralTransform gt = rectEVO.TransformToVisual(rectZeroDegrees);
            Point p = gt.TransformPoint(new Point(0, 0));

            var numForText = (p.X * UnitsPerX) + VstartValue;

            if (numForText <= 180)
            {
                tblockExhOpen.Text = Math.Round(180 - numForText).ToString();
                if (tblockEVO.Text != "\u00BA BBC")
                {
                    tblockEVO.Text = "\u00BA BBC";
                }
            }
            else
            {
                tblockExhOpen.Text = Math.Round(numForText - 180).ToString();
                if (tblockEVO.Text != "\u00BA ABC")
                {
                    tblockEVO.Text = "\u00BA ABC";
                }
            }
        }

        private void spEVC_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            GeneralTransform gt = rectEVC.TransformToVisual(gridToContainOthers);
            Point p = gt.TransformPoint(new Point(0, 0));

            var currentXposition = p.X;
            var xAdjust = currentXposition + e.Delta.Translation.X;
            var leftLimit = currentXposition - rectRed.ActualWidth + 60;

            var currentYposition = p.Y;
            var yAdjust = currentYposition + e.Delta.Translation.Y;
            var bottomLimit = gridToContainOthers.ActualHeight - gridIntOverlap.ActualHeight - 45;

            if (xAdjust > leftLimit && xAdjust < gridToContainOthers.ActualWidth - 35)
            {
                gridExhOverlap.Width += e.Delta.Translation.X;

                SetEVCText();
            }
            if (yAdjust > 1 && yAdjust < bottomLimit)
            {
                gridExhOverlap.Height -= e.Delta.Translation.Y;
            }
        }

        void SetEVCText()
        {
            GeneralTransform gt = rectEVC.TransformToVisual(rectZeroDegrees);
            Point p = gt.TransformPoint(new Point(0, 0));

            var numForText = (p.X * UnitsPerX) + VstartValue;

            if (numForText >= 360)
            {
                tblockExhClose.Text = Math.Round(numForText - 360).ToString();
                if (tblockEVC.Text != "\u00BA ATC")
                {
                    tblockEVC.Text = "\u00BA ATC";
                }
            }
            else
            {
                tblockExhClose.Text = Math.Round(360 - numForText).ToString();
                if (tblockEVC.Text != "\u00BA BTC")
                {
                    tblockEVC.Text = "\u00BA BTC";
                }
            }
        }

        private void spIVO_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            GeneralTransform gt = rectIVO.TransformToVisual(gridToContainOthers);
            Point p = gt.TransformPoint(new Point(0, 0));

            var currentXposition = p.X;
            var xAdjust = currentXposition + e.Delta.Translation.X;
            var rightLimit = currentXposition + rectBlue.ActualWidth - 60;

            var currentYposition = p.Y;
            var yAdjust = currentYposition + e.Delta.Translation.Y;
            var topLimit = gridToContainOthers.ActualHeight - gridExhOverlap.ActualHeight + 45;

            if (xAdjust > 25 && xAdjust < rightLimit)
            {
                transformInt.TranslateX += e.Delta.Translation.X;
                gridIntOverlap.Width -= e.Delta.Translation.X;

                SetIVOText();
            }
            if (yAdjust > topLimit && yAdjust < gridToContainOthers.ActualHeight - 75)
            {
                gridIntOverlap.Height -= e.Delta.Translation.Y;
            }
        }

        void SetIVOText()
        {
            GeneralTransform gt = rectIVO.TransformToVisual(rectZeroDegrees);
            Point p = gt.TransformPoint(new Point(0, 0));

            var numForText = (p.X * UnitsPerX) + VstartValue;

            if (numForText <= 360)
            {
                tblockIntOpen.Text = Math.Round(360 - numForText).ToString();
                if (tblockIVO.Text != "\u00BA BTC")
                {
                    tblockIVO.Text = "\u00BA BTC";
                }
            }
            else
            {
                tblockIntOpen.Text = Math.Round(numForText - 360).ToString();
                if (tblockIVO.Text != "\u00BA ATC")
                {
                    tblockIVO.Text = "\u00BA ATC";
                }
            }
        }

        private void spIVC_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            GeneralTransform gt = rectIVC.TransformToVisual(gridToContainOthers);
            Point p = gt.TransformPoint(new Point(0, 0));

            var currentXposition = p.X;
            var xAdjust = currentXposition + e.Delta.Translation.X;
            var leftLimit = currentXposition - rectBlue.ActualWidth + 60;

            var currentYposition = p.Y;
            var yAdjust = currentYposition + e.Delta.Translation.Y;
            var topLimit = gridToContainOthers.ActualHeight - gridExhOverlap.ActualHeight + 45;

            if (xAdjust > leftLimit && xAdjust < gridToContainOthers.ActualWidth - 35)
            {
                gridIntOverlap.Width += e.Delta.Translation.X;

                SetIVCText();
            }
            if (yAdjust > topLimit && yAdjust < gridToContainOthers.ActualHeight - 75)
            {
                gridIntOverlap.Height -= e.Delta.Translation.Y;
            }
        }

        void SetIVCText()
        {
            GeneralTransform gt = rectIVC.TransformToVisual(rectZeroDegrees);
            Point p = gt.TransformPoint(new Point(0, 0));

            var numForText = (p.X * UnitsPerX) + VstartValue;

            if (numForText >= 540)
            {
                tblockIntClose.Text = Math.Round(numForText - 540).ToString();
                if (tblockIVC.Text != "\u00BA ABC")
                {
                    tblockIVC.Text = "\u00BA ABC";
                }
            }
            else
            {
                tblockIntClose.Text = Math.Round(540 - numForText).ToString();
                if (tblockIVC.Text != "\u00BA BBC")
                {
                    tblockIVC.Text = "\u00BA BBC";
                }
            }
        }

        #endregion

        #region Cyl overlay

        private void btnCyl_Click(object sender, RoutedEventArgs e)
        {
            gridCylinderIDSelections.Visibility = Visibility.Visible;
        }

        private void btnCancelCylinderID_Click(object sender, RoutedEventArgs e)
        {
            gridCylinderIDSelections.Visibility = Visibility.Collapsed;
        }

        private void comboBoxCylinders_DropDownClosed(object sender, object e)
        {
            ComboBox cb = sender as ComboBox;
            int cbNum = Convert.ToInt32(cb.SelectionBoxItem);

            if (comboBoxCylinders.SelectedItem == null)
            {
                return;
            }
            else
            {
                // Reset and clear
                comboBox1.Visibility = Visibility.Collapsed; comboBox2.Visibility = Visibility.Collapsed; comboBox3.Visibility = Visibility.Collapsed; comboBox4.Visibility = Visibility.Collapsed; comboBox5.Visibility = Visibility.Collapsed; comboBox6.Visibility = Visibility.Collapsed; comboBox7.Visibility = Visibility.Collapsed; comboBox8.Visibility = Visibility.Collapsed; comboBox9.Visibility = Visibility.Collapsed; comboBox10.Visibility = Visibility.Collapsed; comboBox11.Visibility = Visibility.Collapsed; comboBox12.Visibility = Visibility.Collapsed;
                textBox1.Visibility = Visibility.Collapsed; textBox2.Visibility = Visibility.Collapsed; textBox3.Visibility = Visibility.Collapsed; textBox4.Visibility = Visibility.Collapsed; textBox5.Visibility = Visibility.Collapsed; textBox6.Visibility = Visibility.Collapsed; textBox7.Visibility = Visibility.Collapsed; textBox8.Visibility = Visibility.Collapsed;
                textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                comboBoxSync.Items.Clear();

                if (cbNum == 1)
                {
                    textBox1.Visibility = Visibility.Visible;
                    textBox1.Text = "1";

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.SelectedIndex = 0;

                    nameOfFile = "OneGridXamlText.txt";
                }
                else if (cbNum == 2)
                {
                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);

                    nameOfFile = "TwoGridXamlText.txt";
                }
                else if (cbNum == 3)
                {
                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);

                    nameOfFile = "ThreeGridXamlText.txt";
                }
                else if (cbNum == 4)
                {
                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;
                    textBox4.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);

                    nameOfFile = "FourGridXamlText.txt";
                }
                else if (cbNum == 5)
                {
                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;
                    textBox4.Visibility = Visibility.Visible;
                    textBox5.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);

                    nameOfFile = "FiveGridXamlText.txt";
                }
                else if (cbNum == 6)
                {
                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;
                    textBox4.Visibility = Visibility.Visible;
                    textBox5.Visibility = Visibility.Visible;
                    textBox6.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);

                    nameOfFile = "SixGridXamlText.txt";
                }
                else if (cbNum == 7)
                {
                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;
                    textBox4.Visibility = Visibility.Visible;
                    textBox5.Visibility = Visibility.Visible;
                    textBox6.Visibility = Visibility.Visible;
                    textBox7.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);

                    nameOfFile = "SevenGridXamlText.txt";
                }
                else if (cbNum == 8)
                {
                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;
                    textBox4.Visibility = Visibility.Visible;
                    textBox5.Visibility = Visibility.Visible;
                    textBox6.Visibility = Visibility.Visible;
                    textBox7.Visibility = Visibility.Visible;
                    textBox8.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);
                    comboBoxSync.Items.Add(8);

                    nameOfFile = "EightGridXamlText.txt";
                }
                else if (cbNum == 9)
                {
                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);
                    comboBoxSync.Items.Add(8);
                    comboBoxSync.Items.Add(9);

                    comboBox1.Visibility = Visibility.Visible;
                    comboBox2.Visibility = Visibility.Visible;
                    comboBox3.Visibility = Visibility.Visible;
                    comboBox4.Visibility = Visibility.Visible;
                    comboBox5.Visibility = Visibility.Visible;
                    comboBox6.Visibility = Visibility.Visible;
                    comboBox7.Visibility = Visibility.Visible;
                    comboBox8.Visibility = Visibility.Visible;
                    comboBox9.Visibility = Visibility.Visible;

                    nameOfFile = "NineGridXamlText.txt";
                }
                else if (cbNum == 10)
                {
                    comboBox1.Visibility = Visibility.Visible;
                    comboBox2.Visibility = Visibility.Visible;
                    comboBox3.Visibility = Visibility.Visible;
                    comboBox4.Visibility = Visibility.Visible;
                    comboBox5.Visibility = Visibility.Visible;
                    comboBox6.Visibility = Visibility.Visible;
                    comboBox7.Visibility = Visibility.Visible;
                    comboBox8.Visibility = Visibility.Visible;
                    comboBox9.Visibility = Visibility.Visible;
                    comboBox10.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);
                    comboBoxSync.Items.Add(8);
                    comboBoxSync.Items.Add(9);
                    comboBoxSync.Items.Add(10);

                    nameOfFile = "TenGridXamlText.txt";
                }
                else if (cbNum == 11)
                {
                    comboBox1.Visibility = Visibility.Visible;
                    comboBox2.Visibility = Visibility.Visible;
                    comboBox3.Visibility = Visibility.Visible;
                    comboBox4.Visibility = Visibility.Visible;
                    comboBox5.Visibility = Visibility.Visible;
                    comboBox6.Visibility = Visibility.Visible;
                    comboBox7.Visibility = Visibility.Visible;
                    comboBox8.Visibility = Visibility.Visible;
                    comboBox9.Visibility = Visibility.Visible;
                    comboBox10.Visibility = Visibility.Visible;
                    comboBox11.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);
                    comboBoxSync.Items.Add(8);
                    comboBoxSync.Items.Add(9);
                    comboBoxSync.Items.Add(10);
                    comboBoxSync.Items.Add(11);

                    nameOfFile = "ElevenGridXamlText.txt";
                }
                else if (cbNum == 12)
                {
                    comboBox1.Visibility = Visibility.Visible;
                    comboBox2.Visibility = Visibility.Visible;
                    comboBox3.Visibility = Visibility.Visible;
                    comboBox4.Visibility = Visibility.Visible;
                    comboBox5.Visibility = Visibility.Visible;
                    comboBox6.Visibility = Visibility.Visible;
                    comboBox7.Visibility = Visibility.Visible;
                    comboBox8.Visibility = Visibility.Visible;
                    comboBox9.Visibility = Visibility.Visible;
                    comboBox10.Visibility = Visibility.Visible;
                    comboBox11.Visibility = Visibility.Visible;
                    comboBox12.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);
                    comboBoxSync.Items.Add(8);
                    comboBoxSync.Items.Add(9);
                    comboBoxSync.Items.Add(10);
                    comboBoxSync.Items.Add(11);
                    comboBoxSync.Items.Add(12);

                    nameOfFile = "TwelveGridXamlText.txt";
                }
            }

            if (cbNum == 1)
            {
                btnGoCylinderID.Focus(FocusState.Programmatic);
            }
            else
            {
                comboBoxSync.Focus(FocusState.Programmatic);
            }
        }

        private async void btnGoCylinderID_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxCylinders.SelectionBoxItem == null || comboBoxSync.SelectionBoxItem == null)
            {
                var dialog = new MessageDialog("You must have a selection for NUMBER OF CYLINDERS and SYNC");
                await dialog.ShowAsync();
            }
            else
            {
                try
                {
                    gridCylinderIDSelections.Visibility = Visibility.Collapsed;

                    // Get your file
                    StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(BaseUri, "/MyXamlTextFiles/" + nameOfFile));

                    // Read your file and set it to a string variable
                    string myXamlString = await FileIO.ReadTextAsync(file);

                    // Create the object from that string
                    object myAdditionalElement = XamlReader.Load(myXamlString);

                    // Initialize a new instance of it
                    gridCylinderIdOverlay = myAdditionalElement as Grid;

                    // Create the paintObject and set the xaml file object as the content
                    PaintObjectTemplatedControl paintObjectCylID = new PaintObjectTemplatedControl();
                    paintObjectCylID.Width = gridMain.ActualWidth / 2;
                    paintObjectCylID.Height = gridMain.ActualHeight / 2;
                    paintObjectCylID.Opacity = 0.6;
                    paintObjectCylID.OpacitySliderIsVisible = true;
                    paintObjectCylID.Content = gridCylinderIdOverlay;

                    gridMain.Children.Add(paintObjectCylID);

                    // Create event handler
                    paintObjectCylID.Unloaded += PaintObjectCylID_Unloaded;

                    // Show and move thumb to lower right of grid, and show color key
                    colorKey.Visibility = Visibility.Visible;

                    // Set some variables to use for the SetGridLabels method
                    string sync = comboBoxSync.SelectionBoxItem.ToString();
                    int cylinders = Convert.ToInt32(comboBoxCylinders.SelectionBoxItem);

                    if (cylinders == 1)
                    {
                        return; // The label on OneGridXamlText is already defined as 1
                    }
                    else
                    {
                        SetGridLabels(sync, cylinders);
                    }
                }
                catch (Exception ex)
                {
                    var dialog = new MessageDialog("A problem occured when trying to load overlay.   " + ex.Message);
                    await dialog.ShowAsync();

                    StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                    logger.Log("Show CylinderID Overlay exception: " + ex.Message);
                }
            }
        }

        private void PaintObjectCylID_Unloaded(object sender, RoutedEventArgs e)
        {
            var count = 0;

            foreach (var child in gridMain.Children)
            {
                if (child is PaintObjectTemplatedControl)
                {
                    var currentChild = child as PaintObjectTemplatedControl;
                    if (currentChild.Content is Grid)
                    {
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                if (colorKey.Visibility == Visibility.Visible)
                {
                    colorKey.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void SetGridLabels(string sync, int cylinders)
        {
            try
            {
                if (cylinders == 2)
                {
                    FrameworkElement labelA = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label" select someElement as FrameworkElement).FirstOrDefault();
                    label = labelA as TextBlock;

                    FrameworkElement labelB = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label1" select someElement as FrameworkElement).FirstOrDefault();
                    label1 = labelB as TextBlock;

                    if (textBox1.Text == sync)
                    {
                        label1.Text = textBox1.Text;
                        label.Text = textBox2.Text;
                    }
                    else if (textBox2.Text == sync)
                    {
                        label1.Text = textBox2.Text;
                        label.Text = textBox1.Text;
                    }
                }

                if (cylinders == 3)
                {
                    FrameworkElement labelA = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label" select someElement as FrameworkElement).FirstOrDefault();
                    label = labelA as TextBlock;

                    FrameworkElement labelB = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label1" select someElement as FrameworkElement).FirstOrDefault();
                    label1 = labelB as TextBlock;

                    FrameworkElement labelC = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label2" select someElement as FrameworkElement).FirstOrDefault();
                    label2 = labelC as TextBlock;

                    if (textBox1.Text == sync)
                    {
                        label2.Text = textBox1.Text;
                        label1.Text = textBox2.Text;
                        label.Text = textBox3.Text;

                    }
                    else if (textBox2.Text == sync)
                    {
                        label2.Text = textBox2.Text;
                        label1.Text = textBox3.Text;
                        label.Text = textBox1.Text;
                    }
                    else if (textBox3.Text == sync)
                    {
                        label2.Text = textBox3.Text;
                        label1.Text = textBox1.Text;
                        label.Text = textBox2.Text;
                    }
                }

                if (cylinders == 4)
                {
                    FrameworkElement labelA = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label" select someElement as FrameworkElement).FirstOrDefault();
                    label = labelA as TextBlock;

                    FrameworkElement labelB = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label1" select someElement as FrameworkElement).FirstOrDefault();
                    label1 = labelB as TextBlock;

                    FrameworkElement labelC = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label2" select someElement as FrameworkElement).FirstOrDefault();
                    label2 = labelC as TextBlock;

                    FrameworkElement labelD = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label3" select someElement as FrameworkElement).FirstOrDefault();
                    label3 = labelD as TextBlock;

                    if (textBox1.Text == sync)
                    {
                        label3.Text = textBox1.Text;
                        label2.Text = textBox2.Text;
                        label1.Text = textBox3.Text;
                        label.Text = textBox4.Text;

                    }
                    else if (textBox2.Text == sync)
                    {
                        label3.Text = textBox2.Text;
                        label2.Text = textBox3.Text;
                        label1.Text = textBox4.Text;
                        label.Text = textBox1.Text;
                    }
                    else if (textBox3.Text == sync)
                    {
                        label3.Text = textBox3.Text;
                        label2.Text = textBox4.Text;
                        label1.Text = textBox1.Text;
                        label.Text = textBox2.Text;
                    }
                    else if (textBox4.Text == sync)
                    {
                        label3.Text = textBox4.Text;
                        label2.Text = textBox1.Text;
                        label1.Text = textBox2.Text;
                        label.Text = textBox3.Text;
                    }
                }

                if (cylinders == 5)
                {
                    FrameworkElement labelA = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label" select someElement as FrameworkElement).FirstOrDefault();
                    label = labelA as TextBlock;

                    FrameworkElement labelB = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label1" select someElement as FrameworkElement).FirstOrDefault();
                    label1 = labelB as TextBlock;

                    FrameworkElement labelC = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label2" select someElement as FrameworkElement).FirstOrDefault();
                    label2 = labelC as TextBlock;

                    FrameworkElement labelD = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label3" select someElement as FrameworkElement).FirstOrDefault();
                    label3 = labelD as TextBlock;

                    FrameworkElement labelE = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label4" select someElement as FrameworkElement).FirstOrDefault();
                    label4 = labelE as TextBlock;

                    if (textBox1.Text == sync)
                    {
                        label4.Text = textBox1.Text;
                        label3.Text = textBox2.Text;
                        label2.Text = textBox3.Text;
                        label1.Text = textBox4.Text;
                        label.Text = textBox5.Text;

                    }
                    else if (textBox2.Text == sync)
                    {
                        label4.Text = textBox2.Text;
                        label3.Text = textBox3.Text;
                        label2.Text = textBox4.Text;
                        label1.Text = textBox5.Text;
                        label.Text = textBox1.Text;
                    }
                    else if (textBox3.Text == sync)
                    {
                        label4.Text = textBox3.Text;
                        label3.Text = textBox4.Text;
                        label2.Text = textBox5.Text;
                        label1.Text = textBox1.Text;
                        label.Text = textBox2.Text;
                    }
                    else if (textBox4.Text == sync)
                    {
                        label4.Text = textBox4.Text;
                        label3.Text = textBox5.Text;
                        label2.Text = textBox1.Text;
                        label1.Text = textBox2.Text;
                        label.Text = textBox3.Text;
                    }
                    else if (textBox5.Text == sync)
                    {
                        label4.Text = textBox5.Text;
                        label3.Text = textBox1.Text;
                        label2.Text = textBox2.Text;
                        label1.Text = textBox3.Text;
                        label.Text = textBox4.Text;
                    }
                }

                if (cylinders == 6)
                {
                    FrameworkElement labelA = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label" select someElement as FrameworkElement).FirstOrDefault();
                    label = labelA as TextBlock;

                    FrameworkElement labelB = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label1" select someElement as FrameworkElement).FirstOrDefault();
                    label1 = labelB as TextBlock;

                    FrameworkElement labelC = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label2" select someElement as FrameworkElement).FirstOrDefault();
                    label2 = labelC as TextBlock;

                    FrameworkElement labelD = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label3" select someElement as FrameworkElement).FirstOrDefault();
                    label3 = labelD as TextBlock;

                    FrameworkElement labelE = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label4" select someElement as FrameworkElement).FirstOrDefault();
                    label4 = labelE as TextBlock;

                    FrameworkElement labelF = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label5" select someElement as FrameworkElement).FirstOrDefault();
                    label5 = labelF as TextBlock;

                    if (textBox1.Text == sync)
                    {
                        label5.Text = textBox1.Text;
                        label4.Text = textBox2.Text;
                        label3.Text = textBox3.Text;
                        label2.Text = textBox4.Text;
                        label1.Text = textBox5.Text;
                        label.Text = textBox6.Text;

                    }
                    else if (textBox2.Text == sync)
                    {
                        label5.Text = textBox2.Text;
                        label4.Text = textBox3.Text;
                        label3.Text = textBox4.Text;
                        label2.Text = textBox5.Text;
                        label1.Text = textBox6.Text;
                        label.Text = textBox1.Text;
                    }
                    else if (textBox3.Text == sync)
                    {
                        label5.Text = textBox3.Text;
                        label4.Text = textBox4.Text;
                        label3.Text = textBox5.Text;
                        label2.Text = textBox6.Text;
                        label1.Text = textBox1.Text;
                        label.Text = textBox2.Text;
                    }
                    else if (textBox4.Text == sync)
                    {
                        label5.Text = textBox4.Text;
                        label4.Text = textBox5.Text;
                        label3.Text = textBox6.Text;
                        label2.Text = textBox1.Text;
                        label1.Text = textBox2.Text;
                        label.Text = textBox3.Text;
                    }
                    else if (textBox5.Text == sync)
                    {
                        label5.Text = textBox5.Text;
                        label4.Text = textBox6.Text;
                        label3.Text = textBox1.Text;
                        label2.Text = textBox2.Text;
                        label1.Text = textBox3.Text;
                        label.Text = textBox4.Text;
                    }
                    else if (textBox6.Text == sync)
                    {
                        label5.Text = textBox6.Text;
                        label4.Text = textBox1.Text;
                        label3.Text = textBox2.Text;
                        label2.Text = textBox3.Text;
                        label1.Text = textBox4.Text;
                        label.Text = textBox5.Text;
                    }
                }

                if (cylinders == 7)
                {
                    FrameworkElement labelA = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label" select someElement as FrameworkElement).FirstOrDefault();
                    label = labelA as TextBlock;

                    FrameworkElement labelB = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label1" select someElement as FrameworkElement).FirstOrDefault();
                    label1 = labelB as TextBlock;

                    FrameworkElement labelC = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label2" select someElement as FrameworkElement).FirstOrDefault();
                    label2 = labelC as TextBlock;

                    FrameworkElement labelD = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label3" select someElement as FrameworkElement).FirstOrDefault();
                    label3 = labelD as TextBlock;

                    FrameworkElement labelE = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label4" select someElement as FrameworkElement).FirstOrDefault();
                    label4 = labelE as TextBlock;

                    FrameworkElement labelF = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label5" select someElement as FrameworkElement).FirstOrDefault();
                    label5 = labelF as TextBlock;

                    FrameworkElement labelG = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label6" select someElement as FrameworkElement).FirstOrDefault();
                    label6 = labelG as TextBlock;

                    if (textBox1.Text == sync)
                    {
                        label6.Text = textBox1.Text;
                        label5.Text = textBox2.Text;
                        label4.Text = textBox3.Text;
                        label3.Text = textBox4.Text;
                        label2.Text = textBox5.Text;
                        label1.Text = textBox6.Text;
                        label.Text = textBox7.Text;

                    }
                    else if (textBox2.Text == sync)
                    {
                        label6.Text = textBox2.Text;
                        label5.Text = textBox3.Text;
                        label4.Text = textBox4.Text;
                        label3.Text = textBox5.Text;
                        label2.Text = textBox6.Text;
                        label1.Text = textBox7.Text;
                        label.Text = textBox1.Text;
                    }
                    else if (textBox3.Text == sync)
                    {
                        label6.Text = textBox3.Text;
                        label5.Text = textBox4.Text;
                        label4.Text = textBox5.Text;
                        label3.Text = textBox6.Text;
                        label2.Text = textBox7.Text;
                        label1.Text = textBox1.Text;
                        label.Text = textBox2.Text;
                    }
                    else if (textBox4.Text == sync)
                    {
                        label6.Text = textBox4.Text;
                        label5.Text = textBox5.Text;
                        label4.Text = textBox6.Text;
                        label3.Text = textBox7.Text;
                        label2.Text = textBox1.Text;
                        label1.Text = textBox2.Text;
                        label.Text = textBox3.Text;
                    }
                    else if (textBox5.Text == sync)
                    {
                        label6.Text = textBox5.Text;
                        label5.Text = textBox6.Text;
                        label4.Text = textBox7.Text;
                        label3.Text = textBox1.Text;
                        label2.Text = textBox2.Text;
                        label1.Text = textBox3.Text;
                        label.Text = textBox4.Text;
                    }
                    else if (textBox6.Text == sync)
                    {
                        label6.Text = textBox6.Text;
                        label5.Text = textBox7.Text;
                        label4.Text = textBox1.Text;
                        label3.Text = textBox2.Text;
                        label2.Text = textBox3.Text;
                        label1.Text = textBox4.Text;
                        label.Text = textBox5.Text;
                    }
                    else if (textBox7.Text == sync)
                    {
                        label6.Text = textBox7.Text;
                        label5.Text = textBox1.Text;
                        label4.Text = textBox2.Text;
                        label3.Text = textBox3.Text;
                        label2.Text = textBox4.Text;
                        label1.Text = textBox5.Text;
                        label.Text = textBox6.Text;
                    }
                }

                if (cylinders == 8)
                {
                    FrameworkElement labelA = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label" select someElement as FrameworkElement).FirstOrDefault();
                    label = labelA as TextBlock;

                    FrameworkElement labelB = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label1" select someElement as FrameworkElement).FirstOrDefault();
                    label1 = labelB as TextBlock;

                    FrameworkElement labelC = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label2" select someElement as FrameworkElement).FirstOrDefault();
                    label2 = labelC as TextBlock;

                    FrameworkElement labelD = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label3" select someElement as FrameworkElement).FirstOrDefault();
                    label3 = labelD as TextBlock;

                    FrameworkElement labelE = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label4" select someElement as FrameworkElement).FirstOrDefault();
                    label4 = labelE as TextBlock;

                    FrameworkElement labelF = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label5" select someElement as FrameworkElement).FirstOrDefault();
                    label5 = labelF as TextBlock;

                    FrameworkElement labelG = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label6" select someElement as FrameworkElement).FirstOrDefault();
                    label6 = labelG as TextBlock;

                    FrameworkElement labelH = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label7" select someElement as FrameworkElement).FirstOrDefault();
                    label7 = labelH as TextBlock;

                    if (textBox1.Text == sync)
                    {
                        label7.Text = textBox1.Text;
                        label6.Text = textBox2.Text;
                        label5.Text = textBox3.Text;
                        label4.Text = textBox4.Text;
                        label3.Text = textBox5.Text;
                        label2.Text = textBox6.Text;
                        label1.Text = textBox7.Text;
                        label.Text = textBox8.Text;

                    }
                    else if (textBox2.Text == sync)
                    {
                        label7.Text = textBox2.Text;
                        label6.Text = textBox3.Text;
                        label5.Text = textBox4.Text;
                        label4.Text = textBox5.Text;
                        label3.Text = textBox6.Text;
                        label2.Text = textBox7.Text;
                        label1.Text = textBox8.Text;
                        label.Text = textBox1.Text;
                    }
                    else if (textBox3.Text == sync)
                    {
                        label7.Text = textBox3.Text;
                        label6.Text = textBox4.Text;
                        label5.Text = textBox5.Text;
                        label4.Text = textBox6.Text;
                        label3.Text = textBox7.Text;
                        label2.Text = textBox8.Text;
                        label1.Text = textBox1.Text;
                        label.Text = textBox2.Text;
                    }
                    else if (textBox4.Text == sync)
                    {
                        label7.Text = textBox4.Text;
                        label6.Text = textBox5.Text;
                        label5.Text = textBox6.Text;
                        label4.Text = textBox7.Text;
                        label3.Text = textBox8.Text;
                        label2.Text = textBox1.Text;
                        label1.Text = textBox2.Text;
                        label.Text = textBox3.Text;
                    }
                    else if (textBox5.Text == sync)
                    {
                        label7.Text = textBox5.Text;
                        label6.Text = textBox6.Text;
                        label5.Text = textBox7.Text;
                        label4.Text = textBox8.Text;
                        label3.Text = textBox1.Text;
                        label2.Text = textBox2.Text;
                        label1.Text = textBox3.Text;
                        label.Text = textBox4.Text;
                    }
                    else if (textBox6.Text == sync)
                    {
                        label7.Text = textBox6.Text;
                        label6.Text = textBox7.Text;
                        label5.Text = textBox8.Text;
                        label4.Text = textBox1.Text;
                        label3.Text = textBox2.Text;
                        label2.Text = textBox3.Text;
                        label1.Text = textBox4.Text;
                        label.Text = textBox5.Text;
                    }
                    else if (textBox7.Text == sync)
                    {
                        label7.Text = textBox7.Text;
                        label6.Text = textBox8.Text;
                        label5.Text = textBox1.Text;
                        label4.Text = textBox2.Text;
                        label3.Text = textBox3.Text;
                        label2.Text = textBox4.Text;
                        label1.Text = textBox5.Text;
                        label.Text = textBox6.Text;
                    }
                    else if (textBox8.Text == sync)
                    {
                        label7.Text = textBox8.Text;
                        label6.Text = textBox1.Text;
                        label5.Text = textBox2.Text;
                        label4.Text = textBox3.Text;
                        label3.Text = textBox4.Text;
                        label2.Text = textBox5.Text;
                        label1.Text = textBox6.Text;
                        label.Text = textBox7.Text;
                    }
                }

                if (cylinders == 9)
                {
                    FrameworkElement labelA = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label" select someElement as FrameworkElement).FirstOrDefault();
                    label = labelA as TextBlock;

                    FrameworkElement labelB = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label1" select someElement as FrameworkElement).FirstOrDefault();
                    label1 = labelB as TextBlock;

                    FrameworkElement labelC = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label2" select someElement as FrameworkElement).FirstOrDefault();
                    label2 = labelC as TextBlock;

                    FrameworkElement labelD = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label3" select someElement as FrameworkElement).FirstOrDefault();
                    label3 = labelD as TextBlock;

                    FrameworkElement labelE = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label4" select someElement as FrameworkElement).FirstOrDefault();
                    label4 = labelE as TextBlock;

                    FrameworkElement labelF = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label5" select someElement as FrameworkElement).FirstOrDefault();
                    label5 = labelF as TextBlock;

                    FrameworkElement labelG = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label6" select someElement as FrameworkElement).FirstOrDefault();
                    label6 = labelG as TextBlock;

                    FrameworkElement labelH = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label7" select someElement as FrameworkElement).FirstOrDefault();
                    label7 = labelH as TextBlock;

                    FrameworkElement labelI = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label8" select someElement as FrameworkElement).FirstOrDefault();
                    label8 = labelI as TextBlock;

                    if (Convert.ToString(comboBox1.SelectionBoxItem) == sync)
                    {
                        label8.Text = comboBox1.SelectionBoxItem.ToString();
                        label7.Text = comboBox2.SelectionBoxItem.ToString();
                        label6.Text = comboBox3.SelectionBoxItem.ToString();
                        label5.Text = comboBox4.SelectionBoxItem.ToString();
                        label4.Text = comboBox5.SelectionBoxItem.ToString();
                        label3.Text = comboBox6.SelectionBoxItem.ToString();
                        label2.Text = comboBox7.SelectionBoxItem.ToString();
                        label1.Text = comboBox8.SelectionBoxItem.ToString();
                        label.Text = comboBox9.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox2.SelectionBoxItem) == sync)
                    {
                        label8.Text = comboBox2.SelectionBoxItem.ToString();
                        label7.Text = comboBox3.SelectionBoxItem.ToString();
                        label6.Text = comboBox4.SelectionBoxItem.ToString();
                        label5.Text = comboBox5.SelectionBoxItem.ToString();
                        label4.Text = comboBox6.SelectionBoxItem.ToString();
                        label3.Text = comboBox7.SelectionBoxItem.ToString();
                        label2.Text = comboBox8.SelectionBoxItem.ToString();
                        label1.Text = comboBox9.SelectionBoxItem.ToString();
                        label.Text = comboBox1.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox3.SelectionBoxItem) == sync)
                    {
                        label8.Text = comboBox3.SelectionBoxItem.ToString();
                        label7.Text = comboBox4.SelectionBoxItem.ToString();
                        label6.Text = comboBox5.SelectionBoxItem.ToString();
                        label5.Text = comboBox6.SelectionBoxItem.ToString();
                        label4.Text = comboBox7.SelectionBoxItem.ToString();
                        label3.Text = comboBox8.SelectionBoxItem.ToString();
                        label2.Text = comboBox9.SelectionBoxItem.ToString();
                        label1.Text = comboBox1.SelectionBoxItem.ToString();
                        label.Text = comboBox2.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox4.SelectionBoxItem) == sync)
                    {
                        label8.Text = comboBox4.SelectionBoxItem.ToString();
                        label7.Text = comboBox5.SelectionBoxItem.ToString();
                        label6.Text = comboBox6.SelectionBoxItem.ToString();
                        label5.Text = comboBox7.SelectionBoxItem.ToString();
                        label4.Text = comboBox8.SelectionBoxItem.ToString();
                        label3.Text = comboBox9.SelectionBoxItem.ToString();
                        label2.Text = comboBox1.SelectionBoxItem.ToString();
                        label1.Text = comboBox2.SelectionBoxItem.ToString();
                        label.Text = comboBox3.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox5.SelectionBoxItem) == sync)
                    {
                        label8.Text = comboBox5.SelectionBoxItem.ToString();
                        label7.Text = comboBox6.SelectionBoxItem.ToString();
                        label6.Text = comboBox7.SelectionBoxItem.ToString();
                        label5.Text = comboBox8.SelectionBoxItem.ToString();
                        label4.Text = comboBox9.SelectionBoxItem.ToString();
                        label3.Text = comboBox1.SelectionBoxItem.ToString();
                        label2.Text = comboBox2.SelectionBoxItem.ToString();
                        label1.Text = comboBox3.SelectionBoxItem.ToString();
                        label.Text = comboBox4.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox6.SelectionBoxItem) == sync)
                    {
                        label8.Text = comboBox6.SelectionBoxItem.ToString();
                        label7.Text = comboBox7.SelectionBoxItem.ToString();
                        label6.Text = comboBox8.SelectionBoxItem.ToString();
                        label5.Text = comboBox9.SelectionBoxItem.ToString();
                        label4.Text = comboBox1.SelectionBoxItem.ToString();
                        label3.Text = comboBox2.SelectionBoxItem.ToString();
                        label2.Text = comboBox3.SelectionBoxItem.ToString();
                        label1.Text = comboBox4.SelectionBoxItem.ToString();
                        label.Text = comboBox5.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox7.SelectionBoxItem) == sync)
                    {
                        label8.Text = comboBox7.SelectionBoxItem.ToString();
                        label7.Text = comboBox8.SelectionBoxItem.ToString();
                        label6.Text = comboBox9.SelectionBoxItem.ToString();
                        label5.Text = comboBox1.SelectionBoxItem.ToString();
                        label4.Text = comboBox2.SelectionBoxItem.ToString();
                        label3.Text = comboBox3.SelectionBoxItem.ToString();
                        label2.Text = comboBox4.SelectionBoxItem.ToString();
                        label1.Text = comboBox5.SelectionBoxItem.ToString();
                        label.Text = comboBox6.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox8.SelectionBoxItem) == sync)
                    {
                        label8.Text = comboBox8.SelectionBoxItem.ToString();
                        label7.Text = comboBox9.SelectionBoxItem.ToString();
                        label6.Text = comboBox1.SelectionBoxItem.ToString();
                        label5.Text = comboBox2.SelectionBoxItem.ToString();
                        label4.Text = comboBox3.SelectionBoxItem.ToString();
                        label3.Text = comboBox4.SelectionBoxItem.ToString();
                        label2.Text = comboBox5.SelectionBoxItem.ToString();
                        label1.Text = comboBox6.SelectionBoxItem.ToString();
                        label.Text = comboBox7.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox9.SelectionBoxItem) == sync)
                    {
                        label8.Text = comboBox9.SelectionBoxItem.ToString();
                        label7.Text = comboBox1.SelectionBoxItem.ToString();
                        label6.Text = comboBox2.SelectionBoxItem.ToString();
                        label5.Text = comboBox3.SelectionBoxItem.ToString();
                        label4.Text = comboBox4.SelectionBoxItem.ToString();
                        label3.Text = comboBox5.SelectionBoxItem.ToString();
                        label2.Text = comboBox6.SelectionBoxItem.ToString();
                        label1.Text = comboBox7.SelectionBoxItem.ToString();
                        label.Text = comboBox8.SelectionBoxItem.ToString();
                    }
                }

                if (cylinders == 10)
                {
                    FrameworkElement labelA = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label" select someElement as FrameworkElement).FirstOrDefault();
                    label = labelA as TextBlock;

                    FrameworkElement labelB = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label1" select someElement as FrameworkElement).FirstOrDefault();
                    label1 = labelB as TextBlock;

                    FrameworkElement labelC = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label2" select someElement as FrameworkElement).FirstOrDefault();
                    label2 = labelC as TextBlock;

                    FrameworkElement labelD = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label3" select someElement as FrameworkElement).FirstOrDefault();
                    label3 = labelD as TextBlock;

                    FrameworkElement labelE = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label4" select someElement as FrameworkElement).FirstOrDefault();
                    label4 = labelE as TextBlock;

                    FrameworkElement labelF = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label5" select someElement as FrameworkElement).FirstOrDefault();
                    label5 = labelF as TextBlock;

                    FrameworkElement labelG = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label6" select someElement as FrameworkElement).FirstOrDefault();
                    label6 = labelG as TextBlock;

                    FrameworkElement labelH = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label7" select someElement as FrameworkElement).FirstOrDefault();
                    label7 = labelH as TextBlock;

                    FrameworkElement labelI = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label8" select someElement as FrameworkElement).FirstOrDefault();
                    label8 = labelI as TextBlock;

                    FrameworkElement labelJ = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label9" select someElement as FrameworkElement).FirstOrDefault();
                    label9 = labelJ as TextBlock;

                    if (Convert.ToString(comboBox1.SelectionBoxItem) == sync)
                    {
                        label9.Text = comboBox1.SelectionBoxItem.ToString();
                        label8.Text = comboBox2.SelectionBoxItem.ToString();
                        label7.Text = comboBox3.SelectionBoxItem.ToString();
                        label6.Text = comboBox4.SelectionBoxItem.ToString();
                        label5.Text = comboBox5.SelectionBoxItem.ToString();
                        label4.Text = comboBox6.SelectionBoxItem.ToString();
                        label3.Text = comboBox7.SelectionBoxItem.ToString();
                        label2.Text = comboBox8.SelectionBoxItem.ToString();
                        label1.Text = comboBox9.SelectionBoxItem.ToString();
                        label.Text = comboBox10.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox2.SelectionBoxItem) == sync)
                    {
                        label9.Text = comboBox2.SelectionBoxItem.ToString();
                        label8.Text = comboBox3.SelectionBoxItem.ToString();
                        label7.Text = comboBox4.SelectionBoxItem.ToString();
                        label6.Text = comboBox5.SelectionBoxItem.ToString();
                        label5.Text = comboBox6.SelectionBoxItem.ToString();
                        label4.Text = comboBox7.SelectionBoxItem.ToString();
                        label3.Text = comboBox8.SelectionBoxItem.ToString();
                        label2.Text = comboBox9.SelectionBoxItem.ToString();
                        label1.Text = comboBox10.SelectionBoxItem.ToString();
                        label.Text = comboBox1.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox3.SelectionBoxItem) == sync)
                    {
                        label9.Text = comboBox3.SelectionBoxItem.ToString();
                        label8.Text = comboBox4.SelectionBoxItem.ToString();
                        label7.Text = comboBox5.SelectionBoxItem.ToString();
                        label6.Text = comboBox6.SelectionBoxItem.ToString();
                        label5.Text = comboBox7.SelectionBoxItem.ToString();
                        label4.Text = comboBox8.SelectionBoxItem.ToString();
                        label3.Text = comboBox9.SelectionBoxItem.ToString();
                        label2.Text = comboBox10.SelectionBoxItem.ToString();
                        label1.Text = comboBox1.SelectionBoxItem.ToString();
                        label.Text = comboBox2.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox4.SelectionBoxItem) == sync)
                    {
                        label9.Text = comboBox4.SelectionBoxItem.ToString();
                        label8.Text = comboBox5.SelectionBoxItem.ToString();
                        label7.Text = comboBox6.SelectionBoxItem.ToString();
                        label6.Text = comboBox7.SelectionBoxItem.ToString();
                        label5.Text = comboBox8.SelectionBoxItem.ToString();
                        label4.Text = comboBox9.SelectionBoxItem.ToString();
                        label3.Text = comboBox10.SelectionBoxItem.ToString();
                        label2.Text = comboBox1.SelectionBoxItem.ToString();
                        label1.Text = comboBox2.SelectionBoxItem.ToString();
                        label.Text = comboBox3.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox5.SelectionBoxItem) == sync)
                    {
                        label9.Text = comboBox5.SelectionBoxItem.ToString();
                        label8.Text = comboBox6.SelectionBoxItem.ToString();
                        label7.Text = comboBox7.SelectionBoxItem.ToString();
                        label6.Text = comboBox8.SelectionBoxItem.ToString();
                        label5.Text = comboBox9.SelectionBoxItem.ToString();
                        label4.Text = comboBox10.SelectionBoxItem.ToString();
                        label3.Text = comboBox1.SelectionBoxItem.ToString();
                        label2.Text = comboBox2.SelectionBoxItem.ToString();
                        label1.Text = comboBox3.SelectionBoxItem.ToString();
                        label.Text = comboBox4.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox6.SelectionBoxItem) == sync)
                    {
                        label9.Text = comboBox6.SelectionBoxItem.ToString();
                        label8.Text = comboBox7.SelectionBoxItem.ToString();
                        label7.Text = comboBox8.SelectionBoxItem.ToString();
                        label6.Text = comboBox9.SelectionBoxItem.ToString();
                        label5.Text = comboBox10.SelectionBoxItem.ToString();
                        label4.Text = comboBox1.SelectionBoxItem.ToString();
                        label3.Text = comboBox2.SelectionBoxItem.ToString();
                        label2.Text = comboBox3.SelectionBoxItem.ToString();
                        label1.Text = comboBox4.SelectionBoxItem.ToString();
                        label.Text = comboBox5.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox7.SelectionBoxItem) == sync)
                    {
                        label9.Text = comboBox7.SelectionBoxItem.ToString();
                        label8.Text = comboBox8.SelectionBoxItem.ToString();
                        label7.Text = comboBox9.SelectionBoxItem.ToString();
                        label6.Text = comboBox10.SelectionBoxItem.ToString();
                        label5.Text = comboBox1.SelectionBoxItem.ToString();
                        label4.Text = comboBox2.SelectionBoxItem.ToString();
                        label3.Text = comboBox3.SelectionBoxItem.ToString();
                        label2.Text = comboBox4.SelectionBoxItem.ToString();
                        label1.Text = comboBox5.SelectionBoxItem.ToString();
                        label.Text = comboBox6.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox8.SelectionBoxItem) == sync)
                    {
                        label9.Text = comboBox8.SelectionBoxItem.ToString();
                        label8.Text = comboBox9.SelectionBoxItem.ToString();
                        label7.Text = comboBox10.SelectionBoxItem.ToString();
                        label6.Text = comboBox1.SelectionBoxItem.ToString();
                        label5.Text = comboBox2.SelectionBoxItem.ToString();
                        label4.Text = comboBox3.SelectionBoxItem.ToString();
                        label3.Text = comboBox4.SelectionBoxItem.ToString();
                        label2.Text = comboBox5.SelectionBoxItem.ToString();
                        label1.Text = comboBox6.SelectionBoxItem.ToString();
                        label.Text = comboBox7.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox9.SelectionBoxItem) == sync)
                    {
                        label9.Text = comboBox9.SelectionBoxItem.ToString();
                        label8.Text = comboBox10.SelectionBoxItem.ToString();
                        label7.Text = comboBox1.SelectionBoxItem.ToString();
                        label6.Text = comboBox2.SelectionBoxItem.ToString();
                        label5.Text = comboBox3.SelectionBoxItem.ToString();
                        label4.Text = comboBox4.SelectionBoxItem.ToString();
                        label3.Text = comboBox5.SelectionBoxItem.ToString();
                        label2.Text = comboBox6.SelectionBoxItem.ToString();
                        label1.Text = comboBox7.SelectionBoxItem.ToString();
                        label.Text = comboBox8.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox10.SelectionBoxItem) == sync)
                    {
                        label9.Text = comboBox10.SelectionBoxItem.ToString();
                        label8.Text = comboBox1.SelectionBoxItem.ToString();
                        label7.Text = comboBox2.SelectionBoxItem.ToString();
                        label6.Text = comboBox3.SelectionBoxItem.ToString();
                        label5.Text = comboBox4.SelectionBoxItem.ToString();
                        label4.Text = comboBox5.SelectionBoxItem.ToString();
                        label3.Text = comboBox6.SelectionBoxItem.ToString();
                        label2.Text = comboBox7.SelectionBoxItem.ToString();
                        label1.Text = comboBox8.SelectionBoxItem.ToString();
                        label.Text = comboBox9.SelectionBoxItem.ToString();
                    }
                }

                if (cylinders == 11)
                {
                    FrameworkElement labelA = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label" select someElement as FrameworkElement).FirstOrDefault();
                    label = labelA as TextBlock;

                    FrameworkElement labelB = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label1" select someElement as FrameworkElement).FirstOrDefault();
                    label1 = labelB as TextBlock;

                    FrameworkElement labelC = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label2" select someElement as FrameworkElement).FirstOrDefault();
                    label2 = labelC as TextBlock;

                    FrameworkElement labelD = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label3" select someElement as FrameworkElement).FirstOrDefault();
                    label3 = labelD as TextBlock;

                    FrameworkElement labelE = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label4" select someElement as FrameworkElement).FirstOrDefault();
                    label4 = labelE as TextBlock;

                    FrameworkElement labelF = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label5" select someElement as FrameworkElement).FirstOrDefault();
                    label5 = labelF as TextBlock;

                    FrameworkElement labelG = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label6" select someElement as FrameworkElement).FirstOrDefault();
                    label6 = labelG as TextBlock;

                    FrameworkElement labelH = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label7" select someElement as FrameworkElement).FirstOrDefault();
                    label7 = labelH as TextBlock;

                    FrameworkElement labelI = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label8" select someElement as FrameworkElement).FirstOrDefault();
                    label8 = labelI as TextBlock;

                    FrameworkElement labelJ = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label9" select someElement as FrameworkElement).FirstOrDefault();
                    label9 = labelJ as TextBlock;

                    FrameworkElement labelK = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label10" select someElement as FrameworkElement).FirstOrDefault();
                    label10 = labelK as TextBlock;

                    if (Convert.ToString(comboBox1.SelectionBoxItem) == sync)
                    {
                        label10.Text = comboBox1.SelectionBoxItem.ToString();
                        label9.Text = comboBox2.SelectionBoxItem.ToString();
                        label8.Text = comboBox3.SelectionBoxItem.ToString();
                        label7.Text = comboBox4.SelectionBoxItem.ToString();
                        label6.Text = comboBox5.SelectionBoxItem.ToString();
                        label5.Text = comboBox6.SelectionBoxItem.ToString();
                        label4.Text = comboBox7.SelectionBoxItem.ToString();
                        label3.Text = comboBox8.SelectionBoxItem.ToString();
                        label2.Text = comboBox9.SelectionBoxItem.ToString();
                        label1.Text = comboBox10.SelectionBoxItem.ToString();
                        label.Text = comboBox11.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox2.SelectionBoxItem) == sync)
                    {
                        label10.Text = comboBox2.SelectionBoxItem.ToString();
                        label9.Text = comboBox3.SelectionBoxItem.ToString();
                        label8.Text = comboBox4.SelectionBoxItem.ToString();
                        label7.Text = comboBox5.SelectionBoxItem.ToString();
                        label6.Text = comboBox6.SelectionBoxItem.ToString();
                        label5.Text = comboBox7.SelectionBoxItem.ToString();
                        label4.Text = comboBox8.SelectionBoxItem.ToString();
                        label3.Text = comboBox9.SelectionBoxItem.ToString();
                        label2.Text = comboBox10.SelectionBoxItem.ToString();
                        label1.Text = comboBox11.SelectionBoxItem.ToString();
                        label.Text = comboBox1.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox3.SelectionBoxItem) == sync)
                    {
                        label10.Text = comboBox3.SelectionBoxItem.ToString();
                        label9.Text = comboBox4.SelectionBoxItem.ToString();
                        label8.Text = comboBox5.SelectionBoxItem.ToString();
                        label7.Text = comboBox6.SelectionBoxItem.ToString();
                        label6.Text = comboBox7.SelectionBoxItem.ToString();
                        label5.Text = comboBox8.SelectionBoxItem.ToString();
                        label4.Text = comboBox9.SelectionBoxItem.ToString();
                        label3.Text = comboBox10.SelectionBoxItem.ToString();
                        label2.Text = comboBox11.SelectionBoxItem.ToString();
                        label1.Text = comboBox1.SelectionBoxItem.ToString();
                        label.Text = comboBox2.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox4.SelectionBoxItem) == sync)
                    {
                        label10.Text = comboBox4.SelectionBoxItem.ToString();
                        label9.Text = comboBox5.SelectionBoxItem.ToString();
                        label8.Text = comboBox6.SelectionBoxItem.ToString();
                        label7.Text = comboBox7.SelectionBoxItem.ToString();
                        label6.Text = comboBox8.SelectionBoxItem.ToString();
                        label5.Text = comboBox9.SelectionBoxItem.ToString();
                        label4.Text = comboBox10.SelectionBoxItem.ToString();
                        label3.Text = comboBox11.SelectionBoxItem.ToString();
                        label2.Text = comboBox1.SelectionBoxItem.ToString();
                        label1.Text = comboBox2.SelectionBoxItem.ToString();
                        label.Text = comboBox3.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox5.SelectionBoxItem) == sync)
                    {
                        label10.Text = comboBox5.SelectionBoxItem.ToString();
                        label9.Text = comboBox6.SelectionBoxItem.ToString();
                        label8.Text = comboBox7.SelectionBoxItem.ToString();
                        label7.Text = comboBox8.SelectionBoxItem.ToString();
                        label6.Text = comboBox9.SelectionBoxItem.ToString();
                        label5.Text = comboBox10.SelectionBoxItem.ToString();
                        label4.Text = comboBox11.SelectionBoxItem.ToString();
                        label3.Text = comboBox1.SelectionBoxItem.ToString();
                        label2.Text = comboBox2.SelectionBoxItem.ToString();
                        label1.Text = comboBox3.SelectionBoxItem.ToString();
                        label.Text = comboBox4.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox6.SelectionBoxItem) == sync)
                    {
                        label10.Text = comboBox6.SelectionBoxItem.ToString();
                        label9.Text = comboBox7.SelectionBoxItem.ToString();
                        label8.Text = comboBox8.SelectionBoxItem.ToString();
                        label7.Text = comboBox9.SelectionBoxItem.ToString();
                        label6.Text = comboBox10.SelectionBoxItem.ToString();
                        label5.Text = comboBox11.SelectionBoxItem.ToString();
                        label4.Text = comboBox1.SelectionBoxItem.ToString();
                        label3.Text = comboBox2.SelectionBoxItem.ToString();
                        label2.Text = comboBox3.SelectionBoxItem.ToString();
                        label1.Text = comboBox4.SelectionBoxItem.ToString();
                        label.Text = comboBox5.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox7.SelectionBoxItem) == sync)
                    {
                        label10.Text = comboBox7.SelectionBoxItem.ToString();
                        label9.Text = comboBox8.SelectionBoxItem.ToString();
                        label8.Text = comboBox9.SelectionBoxItem.ToString();
                        label7.Text = comboBox10.SelectionBoxItem.ToString();
                        label6.Text = comboBox11.SelectionBoxItem.ToString();
                        label5.Text = comboBox1.SelectionBoxItem.ToString();
                        label4.Text = comboBox2.SelectionBoxItem.ToString();
                        label3.Text = comboBox3.SelectionBoxItem.ToString();
                        label2.Text = comboBox4.SelectionBoxItem.ToString();
                        label1.Text = comboBox5.SelectionBoxItem.ToString();
                        label.Text = comboBox6.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox8.SelectionBoxItem) == sync)
                    {
                        label10.Text = comboBox8.SelectionBoxItem.ToString();
                        label9.Text = comboBox9.SelectionBoxItem.ToString();
                        label8.Text = comboBox10.SelectionBoxItem.ToString();
                        label7.Text = comboBox11.SelectionBoxItem.ToString();
                        label6.Text = comboBox1.SelectionBoxItem.ToString();
                        label5.Text = comboBox2.SelectionBoxItem.ToString();
                        label4.Text = comboBox3.SelectionBoxItem.ToString();
                        label3.Text = comboBox4.SelectionBoxItem.ToString();
                        label2.Text = comboBox5.SelectionBoxItem.ToString();
                        label1.Text = comboBox6.SelectionBoxItem.ToString();
                        label.Text = comboBox7.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox9.SelectionBoxItem) == sync)
                    {
                        label10.Text = comboBox9.SelectionBoxItem.ToString();
                        label9.Text = comboBox10.SelectionBoxItem.ToString();
                        label8.Text = comboBox11.SelectionBoxItem.ToString();
                        label7.Text = comboBox1.SelectionBoxItem.ToString();
                        label6.Text = comboBox2.SelectionBoxItem.ToString();
                        label5.Text = comboBox3.SelectionBoxItem.ToString();
                        label4.Text = comboBox4.SelectionBoxItem.ToString();
                        label3.Text = comboBox5.SelectionBoxItem.ToString();
                        label2.Text = comboBox6.SelectionBoxItem.ToString();
                        label1.Text = comboBox7.SelectionBoxItem.ToString();
                        label.Text = comboBox8.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox10.SelectionBoxItem) == sync)
                    {
                        label10.Text = comboBox10.SelectionBoxItem.ToString();
                        label9.Text = comboBox11.SelectionBoxItem.ToString();
                        label8.Text = comboBox1.SelectionBoxItem.ToString();
                        label7.Text = comboBox2.SelectionBoxItem.ToString();
                        label6.Text = comboBox3.SelectionBoxItem.ToString();
                        label5.Text = comboBox4.SelectionBoxItem.ToString();
                        label4.Text = comboBox5.SelectionBoxItem.ToString();
                        label3.Text = comboBox6.SelectionBoxItem.ToString();
                        label2.Text = comboBox7.SelectionBoxItem.ToString();
                        label1.Text = comboBox8.SelectionBoxItem.ToString();
                        label.Text = comboBox9.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox11.SelectionBoxItem) == sync)
                    {
                        label10.Text = comboBox11.SelectionBoxItem.ToString();
                        label9.Text = comboBox1.SelectionBoxItem.ToString();
                        label8.Text = comboBox2.SelectionBoxItem.ToString();
                        label7.Text = comboBox3.SelectionBoxItem.ToString();
                        label6.Text = comboBox4.SelectionBoxItem.ToString();
                        label5.Text = comboBox5.SelectionBoxItem.ToString();
                        label4.Text = comboBox6.SelectionBoxItem.ToString();
                        label3.Text = comboBox7.SelectionBoxItem.ToString();
                        label2.Text = comboBox8.SelectionBoxItem.ToString();
                        label1.Text = comboBox9.SelectionBoxItem.ToString();
                        label.Text = comboBox10.SelectionBoxItem.ToString();
                    }
                }

                if (cylinders == 12)
                {
                    FrameworkElement labelA = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label" select someElement as FrameworkElement).FirstOrDefault();
                    label = labelA as TextBlock;

                    FrameworkElement labelB = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label1" select someElement as FrameworkElement).FirstOrDefault();
                    label1 = labelB as TextBlock;

                    FrameworkElement labelC = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label2" select someElement as FrameworkElement).FirstOrDefault();
                    label2 = labelC as TextBlock;

                    FrameworkElement labelD = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label3" select someElement as FrameworkElement).FirstOrDefault();
                    label3 = labelD as TextBlock;

                    FrameworkElement labelE = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label4" select someElement as FrameworkElement).FirstOrDefault();
                    label4 = labelE as TextBlock;

                    FrameworkElement labelF = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label5" select someElement as FrameworkElement).FirstOrDefault();
                    label5 = labelF as TextBlock;

                    FrameworkElement labelG = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label6" select someElement as FrameworkElement).FirstOrDefault();
                    label6 = labelG as TextBlock;

                    FrameworkElement labelH = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label7" select someElement as FrameworkElement).FirstOrDefault();
                    label7 = labelH as TextBlock;

                    FrameworkElement labelI = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label8" select someElement as FrameworkElement).FirstOrDefault();
                    label8 = labelI as TextBlock;

                    FrameworkElement labelJ = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label9" select someElement as FrameworkElement).FirstOrDefault();
                    label9 = labelJ as TextBlock;

                    FrameworkElement labelK = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label10" select someElement as FrameworkElement).FirstOrDefault();
                    label10 = labelK as TextBlock;

                    FrameworkElement labelL = (from someElement in gridCylinderIdOverlay.Children where (someElement is FrameworkElement) && ((FrameworkElement)someElement).Name == "label11" select someElement as FrameworkElement).FirstOrDefault();
                    label11 = labelL as TextBlock;

                    if (Convert.ToString(comboBox1.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox1.SelectionBoxItem.ToString();
                        label10.Text = comboBox2.SelectionBoxItem.ToString();
                        label9.Text = comboBox3.SelectionBoxItem.ToString();
                        label8.Text = comboBox4.SelectionBoxItem.ToString();
                        label7.Text = comboBox5.SelectionBoxItem.ToString();
                        label6.Text = comboBox6.SelectionBoxItem.ToString();
                        label5.Text = comboBox7.SelectionBoxItem.ToString();
                        label4.Text = comboBox8.SelectionBoxItem.ToString();
                        label3.Text = comboBox9.SelectionBoxItem.ToString();
                        label2.Text = comboBox10.SelectionBoxItem.ToString();
                        label1.Text = comboBox11.SelectionBoxItem.ToString();
                        label.Text = comboBox12.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox2.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox2.SelectionBoxItem.ToString();
                        label10.Text = comboBox3.SelectionBoxItem.ToString();
                        label9.Text = comboBox4.SelectionBoxItem.ToString();
                        label8.Text = comboBox5.SelectionBoxItem.ToString();
                        label7.Text = comboBox6.SelectionBoxItem.ToString();
                        label6.Text = comboBox7.SelectionBoxItem.ToString();
                        label5.Text = comboBox8.SelectionBoxItem.ToString();
                        label4.Text = comboBox9.SelectionBoxItem.ToString();
                        label3.Text = comboBox10.SelectionBoxItem.ToString();
                        label2.Text = comboBox11.SelectionBoxItem.ToString();
                        label1.Text = comboBox12.SelectionBoxItem.ToString();
                        label.Text = comboBox1.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox3.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox3.SelectionBoxItem.ToString();
                        label10.Text = comboBox4.SelectionBoxItem.ToString();
                        label9.Text = comboBox5.SelectionBoxItem.ToString();
                        label8.Text = comboBox6.SelectionBoxItem.ToString();
                        label7.Text = comboBox7.SelectionBoxItem.ToString();
                        label6.Text = comboBox8.SelectionBoxItem.ToString();
                        label5.Text = comboBox9.SelectionBoxItem.ToString();
                        label4.Text = comboBox10.SelectionBoxItem.ToString();
                        label3.Text = comboBox11.SelectionBoxItem.ToString();
                        label2.Text = comboBox12.SelectionBoxItem.ToString();
                        label1.Text = comboBox1.SelectionBoxItem.ToString();
                        label.Text = comboBox2.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox4.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox4.SelectionBoxItem.ToString();
                        label10.Text = comboBox5.SelectionBoxItem.ToString();
                        label9.Text = comboBox6.SelectionBoxItem.ToString();
                        label8.Text = comboBox7.SelectionBoxItem.ToString();
                        label7.Text = comboBox8.SelectionBoxItem.ToString();
                        label6.Text = comboBox9.SelectionBoxItem.ToString();
                        label5.Text = comboBox10.SelectionBoxItem.ToString();
                        label4.Text = comboBox11.SelectionBoxItem.ToString();
                        label3.Text = comboBox12.SelectionBoxItem.ToString();
                        label2.Text = comboBox1.SelectionBoxItem.ToString();
                        label1.Text = comboBox2.SelectionBoxItem.ToString();
                        label.Text = comboBox3.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox5.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox5.SelectionBoxItem.ToString();
                        label10.Text = comboBox6.SelectionBoxItem.ToString();
                        label9.Text = comboBox7.SelectionBoxItem.ToString();
                        label8.Text = comboBox8.SelectionBoxItem.ToString();
                        label7.Text = comboBox9.SelectionBoxItem.ToString();
                        label6.Text = comboBox10.SelectionBoxItem.ToString();
                        label5.Text = comboBox11.SelectionBoxItem.ToString();
                        label4.Text = comboBox12.SelectionBoxItem.ToString();
                        label3.Text = comboBox1.SelectionBoxItem.ToString();
                        label2.Text = comboBox2.SelectionBoxItem.ToString();
                        label1.Text = comboBox3.SelectionBoxItem.ToString();
                        label.Text = comboBox4.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox6.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox6.SelectionBoxItem.ToString();
                        label10.Text = comboBox7.SelectionBoxItem.ToString();
                        label9.Text = comboBox8.SelectionBoxItem.ToString();
                        label8.Text = comboBox9.SelectionBoxItem.ToString();
                        label7.Text = comboBox10.SelectionBoxItem.ToString();
                        label6.Text = comboBox11.SelectionBoxItem.ToString();
                        label5.Text = comboBox12.SelectionBoxItem.ToString();
                        label4.Text = comboBox1.SelectionBoxItem.ToString();
                        label3.Text = comboBox2.SelectionBoxItem.ToString();
                        label2.Text = comboBox3.SelectionBoxItem.ToString();
                        label1.Text = comboBox4.SelectionBoxItem.ToString();
                        label.Text = comboBox5.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox7.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox7.SelectionBoxItem.ToString();
                        label10.Text = comboBox8.SelectionBoxItem.ToString();
                        label9.Text = comboBox9.SelectionBoxItem.ToString();
                        label8.Text = comboBox10.SelectionBoxItem.ToString();
                        label7.Text = comboBox11.SelectionBoxItem.ToString();
                        label6.Text = comboBox12.SelectionBoxItem.ToString();
                        label5.Text = comboBox1.SelectionBoxItem.ToString();
                        label4.Text = comboBox2.SelectionBoxItem.ToString();
                        label3.Text = comboBox3.SelectionBoxItem.ToString();
                        label2.Text = comboBox4.SelectionBoxItem.ToString();
                        label1.Text = comboBox5.SelectionBoxItem.ToString();
                        label.Text = comboBox6.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox8.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox8.SelectionBoxItem.ToString();
                        label10.Text = comboBox9.SelectionBoxItem.ToString();
                        label9.Text = comboBox10.SelectionBoxItem.ToString();
                        label8.Text = comboBox11.SelectionBoxItem.ToString();
                        label7.Text = comboBox12.SelectionBoxItem.ToString();
                        label6.Text = comboBox1.SelectionBoxItem.ToString();
                        label5.Text = comboBox2.SelectionBoxItem.ToString();
                        label4.Text = comboBox3.SelectionBoxItem.ToString();
                        label3.Text = comboBox4.SelectionBoxItem.ToString();
                        label2.Text = comboBox5.SelectionBoxItem.ToString();
                        label1.Text = comboBox6.SelectionBoxItem.ToString();
                        label.Text = comboBox7.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox9.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox9.SelectionBoxItem.ToString();
                        label10.Text = comboBox10.SelectionBoxItem.ToString();
                        label9.Text = comboBox11.SelectionBoxItem.ToString();
                        label8.Text = comboBox12.SelectionBoxItem.ToString();
                        label7.Text = comboBox1.SelectionBoxItem.ToString();
                        label6.Text = comboBox2.SelectionBoxItem.ToString();
                        label5.Text = comboBox3.SelectionBoxItem.ToString();
                        label4.Text = comboBox4.SelectionBoxItem.ToString();
                        label3.Text = comboBox5.SelectionBoxItem.ToString();
                        label2.Text = comboBox6.SelectionBoxItem.ToString();
                        label1.Text = comboBox7.SelectionBoxItem.ToString();
                        label.Text = comboBox8.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox10.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox10.SelectionBoxItem.ToString();
                        label10.Text = comboBox11.SelectionBoxItem.ToString();
                        label9.Text = comboBox12.SelectionBoxItem.ToString();
                        label8.Text = comboBox1.SelectionBoxItem.ToString();
                        label7.Text = comboBox2.SelectionBoxItem.ToString();
                        label6.Text = comboBox3.SelectionBoxItem.ToString();
                        label5.Text = comboBox4.SelectionBoxItem.ToString();
                        label4.Text = comboBox5.SelectionBoxItem.ToString();
                        label3.Text = comboBox6.SelectionBoxItem.ToString();
                        label2.Text = comboBox7.SelectionBoxItem.ToString();
                        label1.Text = comboBox8.SelectionBoxItem.ToString();
                        label.Text = comboBox9.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox11.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox11.SelectionBoxItem.ToString();
                        label10.Text = comboBox12.SelectionBoxItem.ToString();
                        label9.Text = comboBox1.SelectionBoxItem.ToString();
                        label8.Text = comboBox2.SelectionBoxItem.ToString();
                        label7.Text = comboBox3.SelectionBoxItem.ToString();
                        label6.Text = comboBox4.SelectionBoxItem.ToString();
                        label5.Text = comboBox5.SelectionBoxItem.ToString();
                        label4.Text = comboBox6.SelectionBoxItem.ToString();
                        label3.Text = comboBox7.SelectionBoxItem.ToString();
                        label2.Text = comboBox8.SelectionBoxItem.ToString();
                        label1.Text = comboBox9.SelectionBoxItem.ToString();
                        label.Text = comboBox10.SelectionBoxItem.ToString();
                    }
                    else if (Convert.ToString(comboBox12.SelectionBoxItem) == sync)
                    {
                        label11.Text = comboBox12.SelectionBoxItem.ToString();
                        label10.Text = comboBox1.SelectionBoxItem.ToString();
                        label9.Text = comboBox2.SelectionBoxItem.ToString();
                        label8.Text = comboBox3.SelectionBoxItem.ToString();
                        label7.Text = comboBox4.SelectionBoxItem.ToString();
                        label6.Text = comboBox5.SelectionBoxItem.ToString();
                        label5.Text = comboBox6.SelectionBoxItem.ToString();
                        label4.Text = comboBox7.SelectionBoxItem.ToString();
                        label3.Text = comboBox8.SelectionBoxItem.ToString();
                        label2.Text = comboBox9.SelectionBoxItem.ToString();
                        label1.Text = comboBox10.SelectionBoxItem.ToString();
                        label.Text = comboBox11.SelectionBoxItem.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("A problem occured when trying to set the grid labels.    " + ex.Message);
                await dialog.ShowAsync();
            }
        }

        private void comboBoxSync_DropDownClosed(object sender, object e)
        {
            if (textBox1.Visibility == Visibility.Visible)
            {
                textBox1.Focus(FocusState.Programmatic);
            }
        }

        private void textBox1_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (textBox2.Visibility == Visibility.Visible)
            {
                textBox2.Focus(FocusState.Programmatic);
            }
            else
            {
                btnGoCylinderID.Focus(FocusState.Programmatic);
            }
        }

        private void textBox2_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (textBox3.Visibility == Visibility.Visible)
            {
                textBox3.Focus(FocusState.Programmatic);
            }
            else
            {
                btnGoCylinderID.Focus(FocusState.Programmatic);
            }
        }

        private void textBox3_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (textBox4.Visibility == Visibility.Visible)
            {
                textBox4.Focus(FocusState.Programmatic);
            }
            else
            {
                btnGoCylinderID.Focus(FocusState.Programmatic);
            }
        }

        private void textBox4_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (textBox5.Visibility == Visibility.Visible)
            {
                textBox5.Focus(FocusState.Programmatic);
            }
            else
            {
                btnGoCylinderID.Focus(FocusState.Programmatic);
            }
        }

        private void textBox5_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (textBox6.Visibility == Visibility.Visible)
            {
                textBox6.Focus(FocusState.Programmatic);
            }
            else
            {
                btnGoCylinderID.Focus(FocusState.Programmatic);
            }
        }

        private void textBox6_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (textBox7.Visibility == Visibility.Visible)
            {
                textBox7.Focus(FocusState.Programmatic);
            }
            else
            {
                btnGoCylinderID.Focus(FocusState.Programmatic);
            }
        }

        private void textBox7_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (textBox8.Visibility == Visibility.Visible)
            {
                textBox8.Focus(FocusState.Programmatic);
            }
            else
            {
                btnGoCylinderID.Focus(FocusState.Programmatic);
            }
        }

        private void textBox8_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            btnGoCylinderID.Focus(FocusState.Programmatic);
        }

        private void textBox1_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                textBox1.Text = "";
            }
        }

        private void textBox2_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (textBox2.Text.Length > 0)
            {
                textBox2.Text = "";
            }
        }

        private void textBox3_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (textBox3.Text.Length > 0)
            {
                textBox3.Text = "";
            }
        }

        private void textBox4_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (textBox4.Text.Length > 0)
            {
                textBox4.Text = "";
            }
        }

        private void textBox5_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (textBox5.Text.Length > 0)
            {
                textBox5.Text = "";
            }
        }

        private void textBox6_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (textBox6.Text.Length > 0)
            {
                textBox6.Text = "";
            }
        }

        private void textBox7_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (textBox7.Text.Length > 0)
            {
                textBox7.Text = "";
            }
        }

        private void textBox8_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (textBox8.Text.Length > 0)
            {
                textBox8.Text = "";
            }
        }

        private void box_DropDownOpened(object sender, object e)
        {
            var box = sender as ComboBox;

            if (box.Items.Count > 0)
            {
                box.SelectedIndex = 0;
            }
        }

        #endregion
    }
}
