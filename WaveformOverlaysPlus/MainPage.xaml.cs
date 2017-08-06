using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Printing;
using Windows.Storage;
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
using WaveformOverlaysPlus.Helpers;
using WaveformOverlaysPlus.Controls;
using Windows.UI;
using Windows.Graphics.Imaging;
using Microsoft.Services.Store.Engagement;
using Windows.UI.Text;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Input.Inking;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Diagnostics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System.Numerics;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Markup;
using WaveformOverlaysPlus.UndoRedoCommands;
using Windows.System;
using Windows.ApplicationModel;
using WaveformOverlaysPlus.Converters;

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
        
        Point workAreaStartPoint;
        bool mightNeedToSave = false;
        string ColorChangerBox;
        string currentToolChosen;
        string nameOfFile;

#region For Cyl ID overlay

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

#endregion

#region For Settings

        ApplicationDataContainer localSettings = null;
        const string lastStrokeColor = "lastStrokeColor";
        const string lastFillColor = "lastFillColor";
        const string lastTextColor = "lastTextColor";
        const string lastPageColor = "lastPageColor";
        const string lastCompLinesColor = "lastCompLinesColor";
        const string lastSizeSelected = "lastSizeSelected";
        const string lastColorBox = "lastColorBox";

#endregion

#region For UndoRedo

        UndoRedoManager.UnDoRedo _UndoRedo;
        Point pointStartOfManipulation;
        Point pointEndOfManipuluation;
        Point paintObjStart;
        Point paintObjEnd;
        double widthStart;
        double heightStart;
        double widthEnd;
        double heightEnd;

#endregion

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

        bool IsMouseDeviceRightClick = false;
        Line polyRulerLine;
        Grid rulerContainer;
        string gripName;
        Line rulerLine;
        Shape gripShape;
        CompositeTransform rulerTransform;
        decimal UnitsPerX;
        decimal UnitsPerY;
        decimal amountBetweenVs;
        decimal amountBetweenHs;
        decimal VstartValue = 0;
        decimal HstartValue = 0;

#endregion

#region Dependency Properties

        public double currentSizeSelected
        {
            get { return (double)GetValue(currentSizeSelectedProperty); }
            set { SetValue(currentSizeSelectedProperty, value); }
        }
        public static readonly DependencyProperty currentSizeSelectedProperty =
            DependencyProperty.Register("currentSizeSelected", typeof(double), typeof(MainPage), new PropertyMetadata(6.0));



        public double currentFontSize
        {
            get { return (double)GetValue(currentFontSizeProperty); }
            set
            {
                value = value == 1 ? 14.0 :
                        value == 2 ? 18.0 :
                        value == 6 ? 24.0 :
                        value == 10 ? 36.0 :
                        24.0;

                SetValue(currentFontSizeProperty, value);
            }
        }
        public static readonly DependencyProperty currentFontSizeProperty =
            DependencyProperty.Register("currentFontSize", typeof(double), typeof(MainPage), new PropertyMetadata(24.0));

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

            // Initialize UndoRedo
            _UndoRedo = new UndoRedoManager.UnDoRedo();

            // Initialize Settings storage
            localSettings = ApplicationData.Current.LocalSettings;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Delete old files from LocalFolder
            var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();

            if (files.Count > 0)
            {
                int initialFileCount = files.Count;
                int runningFileCount = files.Count;

                try
                {
                    foreach (var file in files)
                    {
                        await file.DeleteAsync(StorageDeleteOption.PermanentDelete);

                        var runningFileCheck = await ApplicationData.Current.LocalFolder.GetFilesAsync();
                        runningFileCount = runningFileCheck.Count;
                    }
                }
                catch (Exception ex)
                {
                    StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                    logger.Log("MyDeleteLocalFilesError" + " "
                               + initialFileCount.ToString() + " " 
                               + "files initially" + " "
                               + runningFileCount.ToString() + " "
                               + "files left" + " "
                               + ex.Message + " " 
                               + ex.StackTrace);
                }
            }

            // Set some initial values
            gridForOverall.Width = gridForOverall.ActualWidth;
            gridForOverall.Height = gridForOverall.ActualHeight;
            gridCompressionOverlay.Width = gridMain.ActualWidth;
            gridCompressionOverlay.Height = gridMain.ActualHeight;
            gridExhOverlap.Height = gridMain.ActualHeight * 0.6;
            gridIntOverlap.Height = (gridMain.ActualHeight * 0.6) - 46;
            gridImageContainer.Width = gridMain.ActualWidth;
            gridImageContainer.Height = gridMain.ActualHeight;
            SetAmountBetween(tboxHpos);
            SetAmountBetween(tboxVpos);
            lineHrulerPres.X2 = gridMain.ActualWidth;
            lineHrulerZero.X2 = gridMain.ActualWidth;
            lineVrulerZero.Y2 = gridMain.ActualHeight;
            lineVruler720.Y2 = gridMain.ActualHeight;
            lineV2.Y2 = gridMain.ActualHeight;
            lineV1.Y2 = gridMain.ActualHeight;
            lineH2.X2 = gridMain.ActualWidth;
            lineH1.X2 = gridMain.ActualWidth;

            transformExh.TranslateX = 140 / Convert.ToDouble(UnitsPerX);
            gridExhOverlap.Width = 230 / Convert.ToDouble(UnitsPerX);
            transformInt.TranslateX = 350 / Convert.ToDouble(UnitsPerX);
            gridIntOverlap.Width = 235 / Convert.ToDouble(UnitsPerX);

            GeneralTransform gt = gridForOverall.TransformToVisual(gridForOverall_0);
            Point pt = gt.TransformPoint(new Point(0, 0));
            workAreaStartPoint = pt;

            // Set user's previous settings
            RestoreUserSettings();

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
                    textColorRB.IsChecked = true;
                    currentToolChosen = "text";
                    break;
                case "arrow":
                    strokeColorRB.IsChecked = true;
                    currentToolChosen = "arrow";
                    gridForOtherInput.Visibility = Visibility.Visible;
                    break;
                case "ellipse":
                    strokeColorRB.IsChecked = true;
                    currentToolChosen = "ellipse";
                    break;
                case "roundedRectangle":
                    strokeColorRB.IsChecked = true;
                    currentToolChosen = "roundedRectangle";
                    break;
                case "rectangle":
                    strokeColorRB.IsChecked = true;
                    currentToolChosen = "rectangle";
                    break;
                case "line":
                    strokeColorRB.IsChecked = true;
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
                    strokeColorRB.IsChecked = true;
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

        //private async void menuMiniApp_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageDialog dialog = new MessageDialog(ex.Message);
        //        await dialog.ShowAsync();
        //    }
        //}

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

            // For shortcut keys
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            dataTransferManager.DataRequested -= ShareImageHandler;
            printMan.PrintTaskRequested -= PrintTaskRequested;
            printDoc.Paginate -= Paginate;
            printDoc.GetPreviewPage -= GetPreviewPage;
            printDoc.AddPages -= AddPages;
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
        }

#endregion

#region New and Exit

        private void menuNew_Click(object sender, RoutedEventArgs e)
        {
            if (gridMain.Children.Count == 0 && _strokes.Count == 0 || mightNeedToSave == false)
            {
                New();
            }
            else
            {
                gridCover.Visibility = Visibility.Visible;
                spSaveBeforeNewDialog.Visibility = Visibility.Visible;
            }
        }

        private void btnSaveBeforeNew_Click(object sender, RoutedEventArgs e)
        {
            spSaveBeforeNewDialog.Visibility = Visibility.Collapsed;
            Save();
        }

        private void btnDontSaveBeforeNew_Click(object sender, RoutedEventArgs e)
        {
            New();
        }

        private void btnCancelSaveBeforeNew_Click(object sender, RoutedEventArgs e)
        {
            spSaveBeforeNewDialog.Visibility = Visibility.Collapsed;
            gridCover.Visibility = Visibility.Collapsed;
        }

        async void New()
        {
            try
            {
                Frame.Navigate(typeof(ResetPage));

                //var _Frame = Window.Current.Content as Frame;
                //_Frame.Navigate(_Frame.Content.GetType());
                //_Frame.GoBack(); // remove from BackStack
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to create a new page.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyNewError" + " " + ex.Message + " " + ex.StackTrace);
            }
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

#endregion

#region Open

        private void menuOpen_Click(object sender, RoutedEventArgs e)
        {
            Open();
        }

        async void Open()
        {
            try
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
                    UnBindLast();

                    spanelBusy.Visibility = Visibility.Visible;
                    tblockBusy.Text = "Opening...";
                    pRing.IsActive = true;

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

                        // Save to local storage and generate unique name in case two of the same image are opened.
                        StorageFile file2 = await imgFile.CopyAsync(ApplicationData.Current.LocalFolder, imgFile.Name, NameCollisionOption.GenerateUniqueName);

                        string name = file2.Name;
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
                        paintObject.ManipulationStarting += GeneralPaintObj_ManipStarting;
                        paintObject.ManipulationCompleted += GeneralPaintObj_ManipCompleted;
                        paintObject.Closing += GeneralPaintObject_Closing;
                        paintObject.Z_Order_Changed += GeneralPaintObject_Z_Order_Changed;

                        gridMain.Children.Add(paintObject);

                        _UndoRedo.InsertInUnDoRedoForAddRemoveElement(true, paintObject, gridMain);
                        ManageUndoRedoButtons();

                        imageCollection.Add(new StoredImage { FileName = name, FilePath = path });
                    }
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to open the file.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyOpenError" + " " + ex.Message + " " + ex.StackTrace);
            }
            finally
            {
                pRing.IsActive = false;
                tblockBusy.Text = "";
                spanelBusy.Visibility = Visibility.Collapsed;
            }
        }

#endregion

#region Save

        private void menuSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        async void Save()
        {
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
                    spanelBusy.Visibility = Visibility.Visible;
                    tblockBusy.Text = "Saving...";
                    pRing.IsActive = true;

                    await ImageUtils.CaptureElementToFile(gridForOverall_0, file);
                    mightNeedToSave = false;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to save the file.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MySaveError" + " " + ex.Message + " " + ex.StackTrace);
            }
            finally
            {
                gridBranding.Visibility = Visibility.Collapsed;

                pRing.IsActive = false;
                tblockBusy.Text = "";
                spanelBusy.Visibility = Visibility.Collapsed;
            }
        }

#endregion

#region Print

        private async void PrintButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Print();
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to print the file.\n\n"
                                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyPrintError" + " " + ex.Message + " " + ex.StackTrace);
            }
        }

        async void Print()
        {
            gridBranding.Visibility = Visibility.Visible;

            RenderTargetBitmap rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(gridForOverall_0);
            IBuffer pixelBuffer = await rtb.GetPixelsAsync();
            byte[] pixels = pixelBuffer.ToArray();
            WriteableBitmap wb = new WriteableBitmap(rtb.PixelWidth, rtb.PixelHeight);
            using (Stream stream = wb.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixels, 0, pixels.Length);
            }

            imageForPrint.Source = wb;
            gridCover.Visibility = Visibility.Visible;
            gridForPrint.Visibility = Visibility.Visible;

            if (gridForPrint.ActualWidth > 1055)
            {
                gridForPrint.Width = 1055;
            }

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
                    gridCover.Visibility = Visibility.Collapsed;
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
                    gridCover.Visibility = Visibility.Collapsed;
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
            request.Data.Properties.Title = "Share image";
            request.Data.Properties.Description = "from Pressure Waveform Overlays";

            // Because we are making async calls in the DataRequested event handler,
            //  we need to get the deferral first.
            DataRequestDeferral deferral = request.GetDeferral(); // Make sure we always call Complete on the deferral.

            try
            {
                // Get the file
                StorageFile thumbnailFile = await ApplicationData.Current.TemporaryFolder.GetFileAsync("thumbnail.png");
                request.Data.Properties.Thumbnail = RandomAccessStreamReference.CreateFromFile(thumbnailFile);

                StorageFile shareFile = await ApplicationData.Current.TemporaryFolder.GetFileAsync("shareFile.png");
                request.Data.SetBitmap(RandomAccessStreamReference.CreateFromFile(shareFile));
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to get the file for sharing.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyGetFileForShareError" + " " + ex.Message + " " + ex.StackTrace);
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
            try
            {
                gridCover.Visibility = Visibility.Visible;
                gridBranding.Visibility = Visibility.Visible;

                RenderTargetBitmap rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(gridForOverall_0);
                IBuffer pixelBuffer = await rtb.GetPixelsAsync();
                byte[] pixels = pixelBuffer.ToArray();
                WriteableBitmap wb = new WriteableBitmap(rtb.PixelWidth, rtb.PixelHeight);
                using (Stream stream = wb.PixelBuffer.AsStream())
                {
                    await stream.WriteAsync(pixels, 0, pixels.Length);
                }
                
                StorageFile shareFile = await ImageUtils.WriteableBitmapToTemporaryFile(wb, "shareFile.png");

                StorageFile thumbnailFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("thumbnail.png", CreationCollisionOption.ReplaceExisting);
                await ImageUtils.CreateThumbnailFromFile(shareFile, 160, thumbnailFile);

                DataTransferManager.ShowShareUI();
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when preparing to share.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyPrepareToShareError" + " " + ex.Message + " " + ex.StackTrace);
            }
        }

#endregion

#region Copy and Paste

        private void menuCopy_Click(object sender, RoutedEventArgs e)
        {
            Copy();
        }

        async void Copy()
        {
            try
            {
                spanelBusy.Visibility = Visibility.Visible;
                tblockBusy.Text = "Copying...";
                pRing.IsActive = true;

                gridBranding.Visibility = Visibility.Visible;

                StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("TempImgFile.png", CreationCollisionOption.ReplaceExisting);
                await ImageUtils.CaptureElementToFile(gridForOverall_0, file);

                DataPackage dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(file));

                Clipboard.SetContent(dataPackage);

                gridBranding.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to copy.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyCopyError" + " " + ex.Message + " " + ex.StackTrace);
            }
            finally
            {
                pRing.IsActive = false;
                tblockBusy.Text = "";
                spanelBusy.Visibility = Visibility.Collapsed;
            }
        }

        private void menuPaste_Click(object sender, RoutedEventArgs e)
        {
            Paste();
        }

        async void Paste()
        {
            try
            {
                DataPackageView dataView = Clipboard.GetContent();

                if (!(dataView.Contains(StandardDataFormats.StorageItems)) && !(dataView.Contains(StandardDataFormats.Bitmap)))
                {
                    await new MessageDialog("Sorry, could not Paste the items. Only images can be pasted.").ShowAsync();
                }
                else
                {
                    if (dataView.Contains(StandardDataFormats.StorageItems))
                    {
                        ProcessDataPackageViewForImages(dataView);
                    }

                    if (dataView.Contains(StandardDataFormats.Bitmap))
                    {
                        PasteBitmapFromDataPackageView(dataView);
                    }
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to paste.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyPasteError" + " " + ex.Message + " " + ex.StackTrace);
            }
        }

        async void PasteBitmapFromDataPackageView(DataPackageView dataView)
        {
            string newName;
            string newPath;
            double newScale = 1.0;

            IRandomAccessStreamReference imageStreamReference = null;
            try
            {
                imageStreamReference = await dataView.GetBitmapAsync();
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("Error retrieving image from Clipboard: " + ex.Message).ShowAsync();
            }

            if (imageStreamReference != null)
            {
                UnBindLast();

                spanelBusy.Visibility = Visibility.Visible;
                tblockBusy.Text = "Loading...";
                pRing.IsActive = true;

                double _height = 0;
                double _width = 0;

                Image image = new Image();
                image.Stretch = Stretch.Fill;

                // Set the image source and get the image width and height
                using (IRandomAccessStreamWithContentType imageStream = await imageStreamReference.OpenReadAsync())
                {
                    WriteableBitmap wb = new WriteableBitmap(1, 1);
                    await wb.SetSourceAsync(imageStream);
                    _height = wb.PixelHeight;
                    _width = wb.PixelWidth;
                    image.Source = wb;

                    // Save to local storage
                    string nameForFile = "_image.png";
                    if (!(String.IsNullOrEmpty(dataView.Properties.Title) || String.IsNullOrWhiteSpace(dataView.Properties.Title)))
                    {
                        nameForFile = dataView.Properties.Title;
                    }
                    StorageFile sFile = await ImageUtils.WriteableBitmapToStorageFile(wb, nameForFile);

                    newName = sFile.Name;
                    newPath = "ms-appdata:///local/" + newName;
                }

                if (_width < 40 || _height < 40)
                {
                    var dialog = new MessageDialog("Image too small. Please choose a larger image.").ShowAsync();
                }

                if (_width > gridMain.ActualWidth || _height > gridMain.ActualHeight)
                {
                    newScale = Math.Min(gridMain.ActualWidth / _width, gridMain.ActualHeight / _height);
                    _width = (_width * newScale) - 1;
                    _height = (_height * newScale) - 1;
                }

                PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
                paintObject.Width = _width;
                paintObject.Height = _height;
                paintObject.Content = image;
                paintObject.ImageFileName = newName;
                paintObject.ImageFilePath = newPath;
                paintObject.ImageScale = newScale;
                paintObject.OpacitySliderIsVisible = true;
                paintObject.Unloaded += PaintObject_Unloaded;
                paintObject.ManipulationStarting += GeneralPaintObj_ManipStarting;
                paintObject.ManipulationCompleted += GeneralPaintObj_ManipCompleted;
                paintObject.Closing += GeneralPaintObject_Closing;
                paintObject.Z_Order_Changed += GeneralPaintObject_Z_Order_Changed;

                gridMain.Children.Add(paintObject);

                _UndoRedo.InsertInUnDoRedoForAddRemoveElement(true, paintObject, gridMain);
                ManageUndoRedoButtons();

                imageCollection.Add(new StoredImage { FileName = newName, FilePath = newPath });

                pRing.IsActive = false;
                tblockBusy.Text = "";
                spanelBusy.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

#region Help menu items

        private async void menuHelp_Click(object sender, RoutedEventArgs e)
        {
            Uri uri = new Uri(@"https://youtu.be/YF63-zWaduc");
            bool success = await Launcher.LaunchUriAsync(uri);

            if (success == false)
            {
                var dialog = await new MessageDialog("Webpage failed to open. If this continues to happen, please use the Feedback button to report the problem.").ShowAsync();
            }
        }

        private async void menuFeedback_Click(object sender, RoutedEventArgs e)
        {
            var launcher = StoreServicesFeedbackLauncher.GetDefault();
            await launcher.LaunchAsync();
        }

        private async void menuPrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            Uri uri = new Uri(@"https://1drv.ms/u/s!AlsPP0wI1WI76nbgs0LoEttkRVnD");
            bool success = await Launcher.LaunchUriAsync(uri);

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
            body.Text = "Version " + GetAppVersion() + "\n" +
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

        string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            ushort[] versionProperties =
            {
                version.Major,
                version.Minor,
                version.Build,
                version.Revision
            };

            return String.Join(".", versionProperties);
        }

#endregion

#region Custom Ink Rendering with Erase

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

                    // for undo redo
                    _UndoRedo.InsertInUnDoRedoForEraseStroke(_strokes, item, DrawingCanvas);
                    ManageUndoRedoButtons();
                    //
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

            // for undo redo
            _UndoRedo.InsertInUnDoRedoForDrawStroke(_strokes, container, DrawingCanvas);
            ManageUndoRedoButtons();
            //
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

                // For adding effects
                using (var list = new CanvasCommandList(session))
                {
                    // First draw the thing we want to apply effects to
                    using (var listSession = list.CreateDrawingSession())
                    {
                        listSession.DrawInk(strokes);
                    }

                    // Then draw the effects
                    using (var shadowEffect = new ShadowEffect { ShadowColor = Colors.DimGray, Source = list, })
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
            (sender as UIElement).CapturePointer(e.Pointer);

            lineForArrow = new Line();
            lineForArrow.StrokeStartLineCap = PenLineCap.Round;
            lineForArrow.StrokeEndLineCap = PenLineCap.Round;
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

            (sender as UIElement).ReleasePointerCapture(e.Pointer);

            InkStrokeBuilder strokeBuilder = new InkStrokeBuilder();
            List<Point> listOfPoints = new List<Point>();
            InkStrokeContainer strokeContainer = new InkStrokeContainer();
            InkStroke myNewStroke;
            InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
            Point ptA = new Point(lineForArrow.X1, lineForArrow.Y1);
            Point ptB = new Point(lineForArrow.X2, lineForArrow.Y2);

            if (currentToolChosen == "line")
            {
                listOfPoints.Add(ptA);
                listOfPoints.Add(ptB);

                drawingAttributes.IgnorePressure = true;
                strokeBuilder.SetDefaultDrawingAttributes(drawingAttributes);

                myNewStroke = strokeBuilder.CreateStroke(listOfPoints);
                strokeContainer.AddStroke(myNewStroke);
                _strokes.Add(strokeContainer);

                DrawingCanvas.Invalidate();
            }
            else // currentToolChosen == "arrow"
            {
                // Determine the length to make the arrow head lines
                int arrowHeadLength =
                    currentSizeSelected == 1 ? 8
                  : currentSizeSelected == 2 ? 10
                  : currentSizeSelected == 6 ? 12
                  : currentSizeSelected == 10 ? 14
                  : 8;

                // Get arrowhead points
                Point[] arrowheadPoints = GetArrowheadPoints(arrowHeadLength, ptA, ptB);
                Point ptC = arrowheadPoints[0];
                Point ptD = arrowheadPoints[1];

                listOfPoints.Add(ptA);
                listOfPoints.Add(ptB);
                listOfPoints.Add(ptC);
                listOfPoints.Add(ptB);
                listOfPoints.Add(ptD);

                drawingAttributes.IgnorePressure = true;
                strokeBuilder.SetDefaultDrawingAttributes(drawingAttributes);

                myNewStroke = strokeBuilder.CreateStroke(listOfPoints);
                strokeContainer.AddStroke(myNewStroke);
                _strokes.Add(strokeContainer);

                DrawingCanvas.Invalidate();
            }

            gridMain.Children.Remove(lineForArrow);

            _UndoRedo.InsertInUnDoRedoForDrawStroke(_strokes, strokeContainer, DrawingCanvas);
            ManageUndoRedoButtons();
        }

        private Point[] GetArrowheadPoints(int arrowheadLength, Point arrowshaftStartPoint, Point arrowshaftEndPoint)
        {
            // start and end points cannot be the same, otherwise the arrowhead points cannot be calculated.
            if (arrowshaftStartPoint == arrowshaftEndPoint)
            {
                arrowshaftEndPoint.X = arrowshaftStartPoint.X + 1;
            }

            // Find the arrow shaft unit vector.
            float vx = (float)(arrowshaftEndPoint.X - arrowshaftStartPoint.X);
            float vy = (float)(arrowshaftEndPoint.Y - arrowshaftStartPoint.Y);
            float dist = (float)Math.Sqrt(vx * vx + vy * vy);
            vx /= dist;
            vy /= dist;

            float ax = arrowheadLength * (-vy - vx);
            float ay = arrowheadLength * (vx - vy);

            return new Point[] { new Point(arrowshaftEndPoint.X + ax, arrowshaftEndPoint.Y + ay),
                                 new Point(arrowshaftEndPoint.X - ay, arrowshaftEndPoint.Y + ax)};
        }

#endregion

#region Adding TextBox, Rectangles, or Ellipse

        private void GeneralPaintObject_Z_Order_Changed(object sender, PaintObjectTemplatedControl.Z_Change_EventArgs e)
        {
            _UndoRedo.InsertInUnDoRedoForZ_Order_Change(sender as FrameworkElement, e.ChangeOfZ);
            ManageUndoRedoButtons();
        }

        private void GeneralPaintObject_Closing(object sender, EventArgs e)
        {
            _UndoRedo.InsertInUnDoRedoForAddRemoveElement(false, sender as FrameworkElement, gridMain);
            ManageUndoRedoButtons();
        }

        private void GeneralPaintObj_ManipStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            var paintObj = sender as FrameworkElement;
            var name = paintObj.Name;

            GeneralTransform gt = paintObj.TransformToVisual(gridMain);
            Point p = gt.TransformPoint(new Point(0, 0));
            paintObjStart = p;

            widthStart = paintObj.ActualWidth;
            heightStart = paintObj.ActualHeight;
        }

        private void GeneralPaintObj_ManipCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var paintObj = sender as FrameworkElement;
            var name = paintObj.Name;

            GeneralTransform gt = paintObj.TransformToVisual(gridMain);
            Point p = gt.TransformPoint(new Point(0, 0));
            paintObjEnd = p;

            widthEnd = paintObj.ActualWidth;
            heightEnd = paintObj.ActualHeight;

            var Xchange = paintObjEnd.X - paintObjStart.X;
            var Ychange = paintObjEnd.Y - paintObjStart.Y;
            var widthChange = widthEnd - widthStart;
            var heightChange = heightEnd - heightStart;

            if (Xchange >= 1 || Xchange <= -1 ||
                Ychange >= 1 || Ychange <= -1 ||
                widthChange >= 1 || widthChange <= -1 ||
                heightChange >= 1 || heightChange <= -1)
            {
                _UndoRedo.InsertInUnDoRedoForMoveOrResize(Xchange, Ychange, widthChange, heightChange, paintObj);
                ManageUndoRedoButtons();
            }
        }

        private void text_Click(object sender, RoutedEventArgs e)
        {
            UnBindLast();
            TextBox textBox = new TextBox();
            textBox.Style = App.Current.Resources["styleTextBox"] as Style;
            textBox.SizeChanged += TextBox_SizeChanged;
            Bind(textBox);

            PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
            paintObject.Content = textBox;
            paintObject.Closing += GeneralPaintObject_Closing;
            paintObject.ManipulationStarting += GeneralPaintObj_ManipStarting;
            paintObject.ManipulationCompleted += GeneralPaintObj_ManipCompleted;
            paintObject.Z_Order_Changed += GeneralPaintObject_Z_Order_Changed;

            gridMain.Children.Add(paintObject);

            _UndoRedo.InsertInUnDoRedoForAddRemoveElement(true, paintObject, gridMain);
            ManageUndoRedoButtons();
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
            paintObject.Closing += GeneralPaintObject_Closing;
            paintObject.ManipulationStarting += GeneralPaintObj_ManipStarting;
            paintObject.ManipulationCompleted += GeneralPaintObj_ManipCompleted;
            paintObject.Z_Order_Changed += GeneralPaintObject_Z_Order_Changed;

            gridMain.Children.Add(paintObject);

            _UndoRedo.InsertInUnDoRedoForAddRemoveElement(true, paintObject, gridMain);
            ManageUndoRedoButtons();
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
            paintObject.Closing += GeneralPaintObject_Closing;
            paintObject.ManipulationStarting += GeneralPaintObj_ManipStarting;
            paintObject.ManipulationCompleted += GeneralPaintObj_ManipCompleted;
            paintObject.Z_Order_Changed += GeneralPaintObject_Z_Order_Changed;

            gridMain.Children.Add(paintObject);

            _UndoRedo.InsertInUnDoRedoForAddRemoveElement(true, paintObject, gridMain);
            ManageUndoRedoButtons();
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

                    if (content is Grid && child.OpacitySliderIsVisible == false) // content is the labels grid
                    {
                        var labelGrid = content as Grid;

                        foreach(var textboxLabel in labelGrid.Children)
                        {
                            TextBox t = textboxLabel as TextBox;
                            t.Background = t.Background;
                            t.Foreground = t.Foreground;
                            t.BorderBrush = t.BorderBrush;
                            t.MinHeight = t.MinHeight; //MinHeight property is used for BorderThickness Binding because the border in the custom textbox style is accomplished by a rectangle strokethickness which is a double not a Thickness
                            t.FontSize = t.FontSize;
                        }
                    }
                }
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

            // Store the setting
            if (localSettings != null)
            {
                localSettings.Values[lastSizeSelected] = size;
            }
        }

        private void sizes_Clicked(object sender, RoutedEventArgs e)
        {
            // Only insert an undo command if called from a click from the user, not from an IsChecked change event. This distinction has to be made in order for undo/redo to behave properly.
            
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
                            localSettings.Values[lastStrokeColor] = chosenColor.Color.ToString();
                            break;
                        case "fillColorRB":
                            fillX.Visibility = Visibility.Visible;
                            borderForFillColor.Background = chosenColor;
                            localSettings.Values[lastFillColor] = chosenColor.Color.ToString();
                            break;
                        case "textColorRB":
                            textX.Visibility = Visibility.Visible;
                            borderForTextColor.Background = chosenColor;
                            localSettings.Values[lastTextColor] = chosenColor.Color.ToString();
                            break;
                        case "pageColorRB":
                            var whiteBrush = new SolidColorBrush(Colors.White);
                            borderForPageColor.Background = whiteBrush;
                            localSettings.Values[lastPageColor] = whiteBrush.Color.ToString();
                            break;
                        case "compLinesColorRB":
                            var grayBrush = new SolidColorBrush(Colors.Gray);
                            borderForCompLinesColor.Background = grayBrush;
                            localSettings.Values[lastCompLinesColor] = grayBrush.Color.ToString();
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
                            localSettings.Values[lastStrokeColor] = chosenColor.Color.ToString();
                            break;
                        case "fillColorRB":
                            fillX.Visibility = Visibility.Collapsed;
                            borderForFillColor.Background = chosenColor;
                            localSettings.Values[lastFillColor] = chosenColor.Color.ToString();
                            break;
                        case "textColorRB":
                            textX.Visibility = Visibility.Collapsed;
                            borderForTextColor.Background = chosenColor;
                            localSettings.Values[lastTextColor] = chosenColor.Color.ToString();
                            break;
                        case "pageColorRB":
                            borderForPageColor.Background = chosenColor;
                            localSettings.Values[lastPageColor] = chosenColor.Color.ToString();
                            break;
                        case "compLinesColorRB":
                            borderForCompLinesColor.Background = chosenColor;
                            localSettings.Values[lastCompLinesColor] = chosenColor.Color.ToString();
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

            if (localSettings != null)
            {
                localSettings.Values[lastColorBox] = ColorChangerBox;
            }
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
                gridCover.Visibility = Visibility.Visible;
                CropEnterStory.Begin();

                var path = imageCollection[0].FilePath;
                LoadImageIntoCropper(path);
                btnCrop.IsEnabled = true;
            }
            else if (imageCollection.Count > 1)
            {
                gridCover.Visibility = Visibility.Visible;
                CropEnterStory.Begin();

                gridviewImages.ItemsSource = imageCollection;
                gridviewImages.Visibility = Visibility.Visible;
                btnCrop.IsEnabled = false;
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            BackToImagesGridView();
        }

        void BackToImagesGridView()
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
            btnCrop.IsEnabled = false;

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
                            var dialog = new MessageDialog("BitmapEncoder error." + ex.Message);
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

                                        _UndoRedo.InsertInUnDoRedoForCrop(bitmapImage, cropFile, file1, control, imageCollection);
                                        ManageUndoRedoButtons();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to crop the image.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyCropError" + " " + ex.Message + " " + ex.StackTrace);
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
            try
            {
                string fileName = filePath.Substring(20);
                double height = 0;
                double width = 0;

                StorageFile imgFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);

                Image img = new Image();
                using (IRandomAccessStream IRASstream = await imgFile.OpenAsync(FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(IRASstream);
                    height = bitmapImage.PixelHeight;
                    width = bitmapImage.PixelWidth;
                    img.Source = bitmapImage;
                }

                if (width < 42 || height < 42)
                {
                    MessageDialog tooSmallMessage = new MessageDialog("Image too small. Please choose a larger image.");
                    await tooSmallMessage.ShowAsync();

                    if (imageCollection.Count < 2)
                    {
                        gridForCrop.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        BackToImagesGridView();
                    }
                }
                else
                {
                    tblockFileName.Text = fileName;

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
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to load image into cropper.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyLoadImageIntoCropperError" + " " + ex.Message + " " + ex.StackTrace);

                gridBranding.Visibility = Visibility.Collapsed;
                CloseCropper();
            }
        }

        private void PaintObject_Unloaded(object sender, RoutedEventArgs e)
        {
            var paintObj = sender as PaintObjectTemplatedControl;
            RemoveImageFromCollection(paintObj);
        }

        private void CropOutline_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            (sender as UIElement).CapturePointer(e.Pointer);

            foreach (Rectangle r in gridCrop.Children)
            {
                if (r.Width == 12)
                {
                    r.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void CropOutline_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var frameworkEl = sender as FrameworkElement;
            var pointerPoint = e.GetCurrentPoint(frameworkEl);
            var x = pointerPoint.Position.X;
            var y = pointerPoint.Position.Y;

            foreach (Rectangle r in gridCrop.Children)
            {
                if (r.Visibility == Visibility.Collapsed)
                {
                    r.Visibility = Visibility.Visible;
                }
            }

            if (x < 0 || x > frameworkEl.ActualWidth || y < 0 || y > frameworkEl.ActualHeight) // pointer point is outside of the FrameworkElement
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }

            frameworkEl.ReleasePointerCapture(e.Pointer);
            frameworkEl.ClearValue(PointerCapturesProperty);
        }

        void CloseCropper()
        {
            CropExitStory.Begin();

            btnCrop.IsEnabled = false;
            btnBack.IsEnabled = false;
            btnBack.Visibility = Visibility.Visible;
            btnCrop.Visibility = Visibility.Visible;
            btnSaveSelection.Visibility = Visibility.Collapsed;

            tblockFileName.Text = "Please choose an image";
            if (gridImageContainer.Children.Count > 1)
            {
                gridImageContainer.Children.RemoveAt(1);
            }
            gridBranding.Visibility = Visibility.Collapsed;
            gridviewImages.Visibility = Visibility.Collapsed;
            gridCover.Visibility = Visibility.Collapsed;
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
                decimal x = Convert.ToDecimal(pt.X);
                UnitsPerX = amountBetweenVs / x;
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
                decimal y = Convert.ToDecimal(pt.Y);
                UnitsPerY = amountBetweenHs / y;
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

                    decimal x = Convert.ToDecimal(point.X);

                    if (gripName == "rectV1") { tblockPink1.Text = (Math.Round(((x * UnitsPerX) + VstartValue), 1, MidpointRounding.AwayFromZero)).ToString(); }
                    if (gripName == "rectV2") { tblockPink2.Text = (Math.Round(((x * UnitsPerX) + VstartValue), 1, MidpointRounding.AwayFromZero)).ToString(); }
                    if (tblockPink1.Text != "--" && tblockPink2.Text != "--") { tblockPinkDelta.Text = (Math.Abs(Convert.ToDecimal(tblockPink1.Text) - Convert.ToDecimal(tblockPink2.Text))).ToString(); }
                }
            }
            else
            {
                if (tblockPink1.Text != "--")
                {
                    GeneralTransform gt1 = lineV1.TransformToVisual(rectZeroDegrees);
                    Point point = gt1.TransformPoint(new Point(0, 0));

                    decimal x = Convert.ToDecimal(point.X);

                    tblockPink1.Text = (Math.Round(((x * UnitsPerX) + VstartValue), 1, MidpointRounding.AwayFromZero)).ToString();
                }
                if (tblockPink2.Text != "--")
                {
                    GeneralTransform gt1 = lineV2.TransformToVisual(rectZeroDegrees);
                    Point point = gt1.TransformPoint(new Point(0, 0));

                    decimal x = Convert.ToDecimal(point.X);

                    tblockPink2.Text = (Math.Round(((x * UnitsPerX) + VstartValue), 1, MidpointRounding.AwayFromZero)).ToString();
                }
                if (tblockPink1.Text != "--" && tblockPink2.Text != "--")
                {
                    tblockPinkDelta.Text = (Math.Abs(Convert.ToDecimal(tblockPink1.Text) - Convert.ToDecimal(tblockPink2.Text))).ToString();
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

                    decimal y = Convert.ToDecimal(point.Y);

                    if (gripName == "rectH1") { tblockPurple1.Text = (Math.Round(((y * UnitsPerY) + HstartValue), 1, MidpointRounding.AwayFromZero)).ToString(); }
                    if (gripName == "rectH2") { tblockPurple2.Text = (Math.Round(((y * UnitsPerY) + HstartValue), 1, MidpointRounding.AwayFromZero)).ToString(); }
                    if (tblockPurple1.Text != "--" && tblockPurple2.Text != "--") { tblockPurpleDelta.Text = (Math.Abs(Convert.ToDecimal(tblockPurple1.Text) - Convert.ToDecimal(tblockPurple2.Text))).ToString(); }
                }
            }
            else
            {
                if (tblockPurple1.Text != "--")
                {
                    GeneralTransform gt1 = lineH1.TransformToVisual(lineHrulerZero);
                    Point point = gt1.TransformPoint(new Point(0, 0));

                    decimal y = Convert.ToDecimal(point.Y);

                    tblockPurple1.Text = (Math.Round(((y * UnitsPerY) + HstartValue), 1, MidpointRounding.AwayFromZero)).ToString();
                }
                if (tblockPurple2.Text != "--")
                {
                    GeneralTransform gt1 = lineH2.TransformToVisual(lineHrulerZero);
                    Point point = gt1.TransformPoint(new Point(0, 0));

                    decimal y = Convert.ToDecimal(point.Y);

                    tblockPurple2.Text = (Math.Round(((y * UnitsPerY) + HstartValue), 1, MidpointRounding.AwayFromZero)).ToString();
                }
                if (tblockPurple1.Text != "--" && tblockPurple2.Text != "--")
                {
                    tblockPurpleDelta.Text = (Math.Abs(Convert.ToDecimal(tblockPurple1.Text) - Convert.ToDecimal(tblockPurple2.Text))).ToString();
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
                    decimal low = Convert.ToDecimal(tboxVzero.Text);
                    decimal high = Convert.ToDecimal(tboxVpos.Text);

                    amountBetweenVs = high - low;
                    VstartValue = low;

                    SetUnitsPerX();
                    SetTextofPink(false);

                    if (gridExhOverlap.Visibility == Visibility.Visible) // If overlap overlays are visible
                    {
                        var low2 = (int)Math.Round(low);
                        var high2 = (int)Math.Round(high);

                        if (low2 != 0 || high2 != 720)
                        {
                            tblockExhOpen.Text = "--";
                            tblockExhClose.Text = "--";
                            tblockIntOpen.Text = "--";
                            tblockIntClose.Text = "--";
                        }
                        else
                        {
                            SetEVOText();
                            SetEVCText();
                            SetIVOText();
                            SetIVCText();
                        }
                    }
                }
                catch
                {
                    amountBetweenVs = 0;
                    VstartValue = 0;

                    SetUnitsPerX();
                    SetTextofPink(false);

                    tblockExhOpen.Text = "--";
                    tblockExhClose.Text = "--";
                    tblockIntOpen.Text = "--";
                    tblockIntClose.Text = "--";
                }
            }
            else if (textbox.Name.StartsWith("tboxH"))
            {
                try
                {
                    decimal low = Convert.ToDecimal(tboxHzero.Text);
                    decimal high = Convert.ToDecimal(tboxHpos.Text);
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
            GeneralTransform gt = shape.TransformToVisual(gridToContainOthers);
            Point prisonerTopLeftPoint = gt.TransformPoint(new Point(0, 0));

            double left = prisonerTopLeftPoint.X;
            double right = left + shape.ActualWidth;
            double leftAdjust = left + e.Delta.Translation.X;
            double rightAdjust = right + e.Delta.Translation.X;

            if ((leftAdjust >= 0) && (rightAdjust <= gridMain.ActualWidth))
            {
                if (gripName == "polygonVzero")
                {
                    var minWidth = (prisonerTopLeftPoint.X + gridCompressionOverlay.ActualWidth) - 30;
                    if (rightAdjust <= minWidth)
                    {
                        transformComp.TranslateX += e.Delta.Translation.X;
                        gridCompressionOverlay.Width = gridCompressionOverlay.ActualWidth - e.Delta.Translation.X;

                        transform.TranslateX += e.Delta.Translation.X;
                    }
                }
                else if (gripName == "polygonV720")
                {
                    var minWidth = (prisonerTopLeftPoint.X - gridCompressionOverlay.ActualWidth) + 30;
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

                try
                {
                    var low1 = Convert.ToDecimal(tboxVzero.Text);
                    var high1 = Convert.ToDecimal(tboxVpos.Text);

                    var low2 = (int)Math.Round(low1);
                    var high2 = (int)Math.Round(high1);

                    if (low2 != 0 || high2 != 720)
                    {
                        tblockExhOpen.Text = "--";
                        tblockExhClose.Text = "--";
                        tblockIntOpen.Text = "--";
                        tblockIntClose.Text = "--";
                    }
                    else
                    {
                        SetEVOText();
                        SetEVCText();
                        SetIVOText();
                        SetIVCText();
                    }
                }
                catch (Exception ex)
                {
                    StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                    logger.Log("MySetTextWhenMoveSideToSideError" + " " + ex.Message + " " + ex.StackTrace);
                }
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
            if (IsMouseDeviceRightClick)
            {
                e.Handled = true;
                return;
            }
            else
            {
                gripShape = sender as Shape;
                gripName = gripShape.Name;

                if (gripName.StartsWith("r")) // it is one of the colored ruler
                {
                    rulerContainer = gripShape.Parent as Grid;
                    rulerLine = rulerContainer.Children[0] as Line;
                    gridDelta.Visibility = Visibility.Visible;
                    rulerLine.Visibility = Visibility.Visible;
                }
                else // it is one of the other rulers
                {
                    var parent = gripShape.Parent as StackPanel;
                    rulerContainer = parent.Parent as Grid;
                    rulerLine = rulerContainer.Children[0] as Line;
                    rulerLine.Stroke = new SolidColorBrush(Colors.Gray);
                }
                rulerTransform = rulerContainer.RenderTransform as CompositeTransform;

                GeneralTransform gt = rulerContainer.TransformToVisual(gridToContainOthers);
                Point p = gt.TransformPoint(new Point(0, 0));
                pointStartOfManipulation = p;
            }
        }

        private void rulers_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            SetSomeStuffOnRulerManipulationCompleted();

            GeneralTransform gt = rulerContainer.TransformToVisual(gridToContainOthers);
            Point p = gt.TransformPoint(new Point(0, 0));
            pointEndOfManipuluation = p;

            var Xchange = pointEndOfManipuluation.X - pointStartOfManipulation.X;
            var Ychange = pointEndOfManipuluation.Y - pointStartOfManipulation.Y;

            if (Xchange >= 1 || Xchange <= -1 || Ychange >= 1 || Ychange <= -1)
            {
                _UndoRedo.InsertInUnDoRedoForMoveOrResize(Xchange, Ychange, 0, 0, rulerContainer);
                ManageUndoRedoButtons();
            }
        }

        void SetSomeStuffOnRulerManipulationCompleted()
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
                else
                {
                    if (rulerLine.Visibility == Visibility.Collapsed) { rulerLine.Visibility = Visibility.Visible; } 
                }

                if (lineV1.Visibility == Visibility.Collapsed &&
                    lineV2.Visibility == Visibility.Collapsed &&
                    lineH1.Visibility == Visibility.Collapsed &&
                    lineH2.Visibility == Visibility.Collapsed)
                {
                    gridDelta.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (gridDelta.Visibility == Visibility.Collapsed) { gridDelta.Visibility = Visibility.Visible; }
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
                else
                {
                    var gray = new SolidColorBrush(Colors.Gray);
                    var stroke = rulerLine.Stroke as SolidColorBrush;
                    if (stroke.Color != gray.Color) { rulerLine.Stroke = gray; }
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
                compLinesColorRB.Visibility = Visibility.Visible;
                compLinesColorRB.IsChecked = true;

                _UndoRedo.InsertInUnDoRedoForShowHideComp(true, gridCompressionOverlay, compLinesColorRB, strokeColorRB);
                ManageUndoRedoButtons();
            }
            else
            {
                gridCompressionOverlay.Opacity = .001;
                compLinesColorRB.Visibility = Visibility.Collapsed;
                strokeColorRB.IsChecked = true;

                _UndoRedo.InsertInUnDoRedoForShowHideComp(false, gridCompressionOverlay, compLinesColorRB, strokeColorRB);
                ManageUndoRedoButtons();
            }
        }

        private async void btnOverlap_Click(object sender, RoutedEventArgs e)
        {
            if (gridIntOverlap.Visibility == Visibility.Collapsed)
            {
                try
                {
                    var low1 = Convert.ToDecimal(tboxVzero.Text);
                    var high1 = Convert.ToDecimal(tboxVpos.Text);

                    var low2 = (int)Math.Round(low1);
                    var high2 = (int)Math.Round(high1);

                    if (low2 != 0 || high2 != 720)
                    {
                        await new MessageDialog("The degree rulers along the bottom must be set to 0 and 720").ShowAsync();
                    }
                    else
                    {
                        // Make visible
                        gridIntOverlap.Visibility = Visibility.Visible;
                        gridExhOverlap.Visibility = Visibility.Visible;
                        tblockExh.Foreground = new SolidColorBrush(Colors.Red);
                        tblockInt.Foreground = new SolidColorBrush(Colors.Blue);

                        // Get position of zero point of compression overlay for next step
                        GeneralTransform gt = rectZeroDegrees.TransformToVisual(gridToContainOthers);
                        Point zeroPoint = gt.TransformPoint(new Point(0, 0));

                        // Move to standard position in relation to compression overlay
                        transformExh.TranslateX = zeroPoint.X + (140 / Convert.ToDouble(UnitsPerX));
                        gridExhOverlap.Width = 230 / Convert.ToDouble(UnitsPerX);
                        transformInt.TranslateX = zeroPoint.X + (350 / Convert.ToDouble(UnitsPerX));
                        gridIntOverlap.Width = 235 / Convert.ToDouble(UnitsPerX);

                        _UndoRedo.InsertInUnDoRedoForShowHideOverlap(true, gridExhOverlap, gridIntOverlap, tblockExh, tblockInt);
                        ManageUndoRedoButtons();
                    }
                }
                catch (Exception ex)
                {
                    await new MessageDialog("Sorry, a problem occured when trying to show the valve overlap overlay.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                    StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                    logger.Log("MyValveOverlapError" + " " + ex.Message + " " + ex.StackTrace);

                    gridIntOverlap.Visibility = Visibility.Collapsed;
                    gridExhOverlap.Visibility = Visibility.Collapsed;
                    tblockExh.Foreground = new SolidColorBrush(Colors.Black);
                    tblockInt.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
            else
            {
                gridIntOverlap.Visibility = Visibility.Collapsed;
                gridExhOverlap.Visibility = Visibility.Collapsed;
                tblockExh.Foreground = new SolidColorBrush(Colors.Black);
                tblockInt.Foreground = new SolidColorBrush(Colors.Black);

                _UndoRedo.InsertInUnDoRedoForShowHideOverlap(false, gridExhOverlap, gridIntOverlap, tblockExh, tblockInt);
                ManageUndoRedoButtons();
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
            }
            if (yAdjust > 1 && yAdjust < bottomLimit)
            {
                gridExhOverlap.Height -= e.Delta.Translation.Y;
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
            }
            if (yAdjust > 1 && yAdjust < bottomLimit)
            {
                gridExhOverlap.Height -= e.Delta.Translation.Y;
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
            }
            if (yAdjust > topLimit && yAdjust < gridToContainOthers.ActualHeight - 75)
            {
                gridIntOverlap.Height -= e.Delta.Translation.Y;
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
            }
            if (yAdjust > topLimit && yAdjust < gridToContainOthers.ActualHeight - 75)
            {
                gridIntOverlap.Height -= e.Delta.Translation.Y;
            }
        }

        private void OverlapManip_Starting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var name = element.Name;

            if (name == "spEVO" || name == "spEVC")
            {
                GeneralTransform gt = gridExhOverlap.TransformToVisual(gridMain);
                Point p = gt.TransformPoint(new Point(0, 0));
                paintObjStart = p;

                widthStart = gridExhOverlap.ActualWidth;
                heightStart = gridExhOverlap.ActualHeight;
            }
            else if (name == "spIVO" || name == "spIVC")
            {
                GeneralTransform gt = gridIntOverlap.TransformToVisual(gridMain);
                Point p = gt.TransformPoint(new Point(0, 0));
                paintObjStart = p;

                widthStart = gridIntOverlap.ActualWidth;
                heightStart = gridIntOverlap.ActualHeight;
            }
        }

        private void OverlapManip_Completed(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var manipulator = sender as FrameworkElement;
            var manipulatorName = manipulator.Name;
            FrameworkElement uiElement = null;

            if (manipulatorName == "spEVO" || manipulatorName == "spEVC")
            {
                GeneralTransform gt = gridExhOverlap.TransformToVisual(gridMain);
                Point p = gt.TransformPoint(new Point(0, 0));
                paintObjEnd = p;

                widthEnd = gridExhOverlap.ActualWidth;
                heightEnd = gridExhOverlap.ActualHeight;

                uiElement = gridExhOverlap;
            }
            else if (manipulatorName == "spIVO" || manipulatorName == "spIVC")
            {
                GeneralTransform gt = gridIntOverlap.TransformToVisual(gridMain);
                Point p = gt.TransformPoint(new Point(0, 0));
                paintObjEnd = p;

                widthEnd = gridIntOverlap.ActualWidth;
                heightEnd = gridIntOverlap.ActualHeight;

                uiElement = gridIntOverlap;
            }

            var Xchange = paintObjEnd.X - paintObjStart.X;
            var widthChange = widthEnd - widthStart;
            var heightChange = heightEnd - heightStart;

            if (Xchange >= 1 || Xchange <= -1 ||
                widthChange >= 1 || widthChange <= -1 ||
                heightChange >= 1 || heightChange <= -1)
            {
                _UndoRedo.InsertInUnDoRedoForManipOverlap(Xchange, widthChange, heightChange, uiElement, manipulatorName);
                ManageUndoRedoButtons();
            }
        }

        private void gridExhOverlap_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                var low1 = Convert.ToDecimal(tboxVzero.Text);
                var high1 = Convert.ToDecimal(tboxVpos.Text);

                var low2 = (int)Math.Round(low1);
                var high2 = (int)Math.Round(high1);

                if (low2 != 0 || high2 != 720)
                {
                    tblockExhOpen.Text = "--";
                    tblockExhClose.Text = "--";
                    tblockIntOpen.Text = "--";
                    tblockIntClose.Text = "--";
                }
                else
                {
                    SetEVOText();
                    SetEVCText();
                }
            }
            catch (Exception ex)
            {
                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyExhGridSizeChangedError" + " " + ex.Message + " " + ex.StackTrace);
            }
        }

        private void gridIntOverlap_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                var low1 = Convert.ToDecimal(tboxVzero.Text);
                var high1 = Convert.ToDecimal(tboxVpos.Text);

                var low2 = (int)Math.Round(low1);
                var high2 = (int)Math.Round(high1);

                if (low2 != 0 || high2 != 720)
                {
                    tblockExhOpen.Text = "--";
                    tblockExhClose.Text = "--";
                    tblockIntOpen.Text = "--";
                    tblockIntClose.Text = "--";
                }
                else
                {
                    SetIVOText();
                    SetIVCText();
                }
            }
            catch (Exception ex)
            {
                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyIntGridSizeChangedError" + " " + ex.Message + " " + ex.StackTrace);
            }
        }

        void SetEVOText()
        {
            GeneralTransform gt = rectEVO.TransformToVisual(rectZeroDegrees);
            Point p = gt.TransformPoint(new Point(0, 0));

            decimal x = Convert.ToDecimal(p.X);
            decimal numForText = (x * UnitsPerX) + VstartValue;

            if (numForText <= 180)
            {
                tblockExhOpen.Text = (Math.Round(180 - numForText)).ToString();
                if (tblockEVO.Text != "\u00BA BBC")
                {
                    tblockEVO.Text = "\u00BA BBC";
                }
            }
            else
            {
                tblockExhOpen.Text = (Math.Round(numForText - 180)).ToString();
                if (tblockEVO.Text != "\u00BA ABC")
                {
                    tblockEVO.Text = "\u00BA ABC";
                }
            }
        }

        void SetEVCText()
        {
            GeneralTransform gt = rectEVC.TransformToVisual(rectZeroDegrees);
            Point p = gt.TransformPoint(new Point(0, 0));

            decimal x = Convert.ToDecimal(p.X);
            decimal numForText = (x * UnitsPerX) + VstartValue;

            if (numForText >= 360)
            {
                tblockExhClose.Text = (Math.Round(numForText - 360)).ToString();
                if (tblockEVC.Text != "\u00BA ATC")
                {
                    tblockEVC.Text = "\u00BA ATC";
                }
            }
            else
            {
                tblockExhClose.Text = (Math.Round(360 - numForText)).ToString();
                if (tblockEVC.Text != "\u00BA BTC")
                {
                    tblockEVC.Text = "\u00BA BTC";
                }
            }
        }

        void SetIVOText()
        {
            GeneralTransform gt = rectIVO.TransformToVisual(rectZeroDegrees);
            Point p = gt.TransformPoint(new Point(0, 0));

            decimal x = Convert.ToDecimal(p.X);
            var numForText = (x * UnitsPerX) + VstartValue;

            if (numForText <= 360)
            {
                tblockIntOpen.Text = (Math.Round(360 - numForText)).ToString();
                if (tblockIVO.Text != "\u00BA BTC")
                {
                    tblockIVO.Text = "\u00BA BTC";
                }
            }
            else
            {
                tblockIntOpen.Text = (Math.Round(numForText - 360)).ToString();
                if (tblockIVO.Text != "\u00BA ATC")
                {
                    tblockIVO.Text = "\u00BA ATC";
                }
            }
        }

        void SetIVCText()
        {
            GeneralTransform gt = rectIVC.TransformToVisual(rectZeroDegrees);
            Point p = gt.TransformPoint(new Point(0, 0));

            decimal x = Convert.ToDecimal(p.X);
            var numForText = (x * UnitsPerX) + VstartValue;

            if (numForText >= 540)
            {
                tblockIntClose.Text = (Math.Round(numForText - 540)).ToString();
                if (tblockIVC.Text != "\u00BA ABC")
                {
                    tblockIVC.Text = "\u00BA ABC";
                }
            }
            else
            {
                tblockIntClose.Text = (Math.Round(540 - numForText)).ToString();
                if (tblockIVC.Text != "\u00BA BBC")
                {
                    tblockIVC.Text = "\u00BA BBC";
                }
            }
        }

#endregion

#region Cyl ID overlay

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
                    UnBindLast();

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
                    paintObjectCylID.Unloaded += PaintObjectCylID_Unloaded;
                    paintObjectCylID.Loaded += PaintObjectCylID_Loaded;
                    paintObjectCylID.ManipulationStarting += GeneralPaintObj_ManipStarting;
                    paintObjectCylID.ManipulationCompleted += GeneralPaintObj_ManipCompleted;
                    paintObjectCylID.Closing += GeneralPaintObject_Closing;
                    paintObjectCylID.Z_Order_Changed += GeneralPaintObject_Z_Order_Changed;

                    gridMain.Children.Add(paintObjectCylID);

                    _UndoRedo.InsertInUnDoRedoForAddRemoveElement(true, paintObjectCylID, gridMain);
                    ManageUndoRedoButtons();

                    // Show color key
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
                    await new MessageDialog("Sorry, a problem occured when trying to load cylinder ID overlay.\n\n"
                                             + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                    StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                    logger.Log("MyCylIDError" + " " + ex.Message + " " + ex.StackTrace);
                }
            }
        }

        private void PaintObjectCylID_Loaded(object sender, RoutedEventArgs e)
        {
            if (colorKey.Visibility == Visibility.Collapsed)
            {
                colorKey.Visibility = Visibility.Visible;
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
                    if (currentChild.Content is Grid && currentChild.OpacitySliderIsVisible == true) // this determines if it is a Cylinder ID overlay
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

#region Undo Redo buttons

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void btnRedo_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        void Undo()
        {
            var topUndo = _UndoRedo.GetTopUndoCommand();
            var _topUndo = topUndo.ToString();

            _UndoRedo.Undo(1);
            ManageUndoRedoButtons();

            if (_topUndo.EndsWith("MoveOrResizeCommand"))
            {
                var moveCommand = topUndo as UndoRedoManager.MoveOrResizeCommand;
                var element = moveCommand._UiElement;
                SetStuffOnMoveCommand(element);

                // Below is for PaintOject with TextBox because the textbox resizes when typing near the edge and that screws up the Translate X and Y stuff.
                if (element.Parent == gridMain)
                {
                    try
                    {
                        var el = element as PaintObjectTemplatedControl;
                        if (el.Content is TextBox)
                        {
                            KeepPaintObjectInBounds(el);
                        }
                    }
                    catch
                    {
                        
                    }
                }
            }
            else if (_topUndo.EndsWith("AddRemoveElementCommand"))
            {
                var addRemoveCommand = topUndo as UndoRedoManager.AddRemoveElementCommand;
                var paintObject = addRemoveCommand._UiElement as PaintObjectTemplatedControl;
                var content = paintObject.Content;

                if (content is Image && addRemoveCommand._AddingElement == false)
                {
                    imageCollection.Add(new StoredImage { FileName = paintObject.ImageFileName, FilePath = paintObject.ImageFilePath });
                }
            }
        }

        void Redo()
        {
            var topRedo = _UndoRedo.GetTopRedoCommand();
            var _topRedo = topRedo.ToString();

            _UndoRedo.Redo(1);
            ManageUndoRedoButtons();

            if (_topRedo.EndsWith("MoveOrResizeCommand"))
            {
                var moveCommand = topRedo as UndoRedoManager.MoveOrResizeCommand;
                var element = moveCommand._UiElement;
                SetStuffOnMoveCommand(element);

                // Below is for PaintOject with TextBox because the textbox resizes when typing near the edge and that screws up the Translate X and Y stuff.
                if (element.Parent == gridMain)
                {
                    try
                    {
                        var el = element as PaintObjectTemplatedControl;
                        if (el.Content is TextBox)
                        {
                            KeepPaintObjectInBounds(el);
                        }
                    }
                    catch
                    {

                    }
                }
            }
            else if (_topRedo.EndsWith("AddRemoveElementCommand"))
            {
                var addRemoveCommand = topRedo as UndoRedoManager.AddRemoveElementCommand;
                var paintObject = addRemoveCommand._UiElement as PaintObjectTemplatedControl;
                var content = paintObject.Content;

                if (content is Image && addRemoveCommand._AddingElement == true)
                {
                    imageCollection.Add(new StoredImage { FileName = paintObject.ImageFileName, FilePath = paintObject.ImageFilePath });
                }
            }
        }

        void ManageUndoRedoButtons()
        {
            if (mightNeedToSave == false)
            {
                mightNeedToSave = true;
            }

            if (_UndoRedo.IsUndoPossible())
            {
                if (!btnUndo.IsEnabled) { btnUndo.IsEnabled = true; }
            }
            else
            {
                if (btnUndo.IsEnabled) { btnUndo.IsEnabled = false; }
            }

            if (_UndoRedo.IsRedoPossible())
            {
                if (!btnRedo.IsEnabled) { btnRedo.IsEnabled = true; }
            }
            else
            {
                if (btnRedo.IsEnabled) { btnRedo.IsEnabled = false; }
            }
        }

        void SetStuffOnMoveCommand(FrameworkElement element)
        {
            string name;

            if (String.IsNullOrEmpty(element.Name))
            {
                return;
            }
            else
            {
                name = element.Name;
            }

            if (String.IsNullOrEmpty(name))
            {
                return;
            }
            else
            {
                if (name == "gridHrulerPres")
                {
                    gripShape = polygonHpres;
                    gripName = gripShape.Name;
                    rulerLine = lineHrulerPres;

                    SetUnitsPerY();
                    SetTextofPurple(false);
                }
                else if (name == "gridHrulerZero")
                {
                    gripShape = polygonHzero;
                    gripName = gripShape.Name;
                    rulerLine = lineHrulerZero;

                    SetUnitsPerY();
                    SetTextofPurple(false);
                }
                else if (name == "gridVrulerZero")
                {
                    gripShape = polygonVzero;
                    gripName = gripShape.Name;
                    rulerLine = lineVrulerZero;

                    GeneralTransform gt = lineVrulerZero.TransformToVisual(gridToContainOthers);
                    Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

                    GeneralTransform gt1 = lineVruler720.TransformToVisual(gridToContainOthers);
                    Point TopLeftPoint1 = gt1.TransformPoint(new Point(0, 0));

                    transformComp.TranslateX = TopLeftPoint.X - 1; // Subtract 1 because of layout behavior
                    gridCompressionOverlay.Width = TopLeftPoint1.X - TopLeftPoint.X + 2; // Add 2 because of layout behavior

                    SetUnitsPerX();
                    SetTextofPink(false);

                    try
                    {
                        var low1 = Convert.ToDecimal(tboxVzero.Text);
                        var high1 = Convert.ToDecimal(tboxVpos.Text);

                        var low2 = (int)Math.Round(low1);
                        var high2 = (int)Math.Round(high1);

                        if (low2 != 0 || high2 != 720)
                        {
                            tblockExhOpen.Text = "--";
                            tblockExhClose.Text = "--";
                            tblockIntOpen.Text = "--";
                            tblockIntClose.Text = "--";
                        }
                        else
                        {
                            SetEVOText();
                            SetEVCText();
                            SetIVOText();
                            SetIVCText();
                        }
                    }
                    catch (Exception ex)
                    {
                        StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                        logger.Log("MySetValveTextOnMoveCommandError" + " " + ex.Message + " " + ex.StackTrace);
                    }
                }
                else if (name == "gridVruler720")
                {
                    gripShape = polygonV720;
                    gripName = gripShape.Name;
                    rulerLine = lineVruler720;

                    GeneralTransform gt = lineVrulerZero.TransformToVisual(gridToContainOthers);
                    Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

                    GeneralTransform gt1 = lineVruler720.TransformToVisual(gridToContainOthers);
                    Point TopLeftPoint1 = gt1.TransformPoint(new Point(0, 0));

                    gridCompressionOverlay.Width = TopLeftPoint1.X - TopLeftPoint.X + 2; // Add 2 because of layout behavior

                    SetUnitsPerX();
                    SetTextofPink(false);

                    try
                    {
                        var low1 = Convert.ToDecimal(tboxVzero.Text);
                        var high1 = Convert.ToDecimal(tboxVpos.Text);

                        var low2 = (int)Math.Round(low1);
                        var high2 = (int)Math.Round(high1);

                        if (low2 != 0 || high2 != 720)
                        {
                            tblockExhOpen.Text = "--";
                            tblockExhClose.Text = "--";
                            tblockIntOpen.Text = "--";
                            tblockIntClose.Text = "--";
                        }
                        else
                        {
                            SetEVOText();
                            SetEVCText();
                            SetIVOText();
                            SetIVCText();
                        }
                    }
                    catch (Exception ex)
                    {
                        StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                        logger.Log("MySetValveTextOnMoveCommandError" + " " + ex.Message + " " + ex.StackTrace);
                    }
                }
                else if (name == "gridVline2")
                {
                    gripShape = rectV2;
                    gripName = gripShape.Name;
                    rulerLine = lineV2;

                    SetTextofPink(true);
                }
                else if (name == "gridVline1")
                {
                    gripShape = rectV1;
                    gripName = gripShape.Name;
                    rulerLine = lineV1;

                    SetTextofPink(true);
                }
                else if (name == "gridHline1")
                {
                    gripShape = rectH1;
                    gripName = gripShape.Name;
                    rulerLine = lineH1;

                    SetTextofPurple(true);
                }
                else if (name == "gridHline2")
                {
                    gripShape = rectH2;
                    gripName = gripShape.Name;
                    rulerLine = lineH2;

                    SetTextofPurple(true);
                }

                SetSomeStuffOnRulerManipulationCompleted();
            }
        }

        void KeepPaintObjectInBounds(PaintObjectTemplatedControl paintOject)
        {
            var jail = gridMain;
            var prisoner = paintOject;

            //Get top left point of prisoner in relation to jail
            GeneralTransform gt = prisoner.TransformToVisual(jail);
            Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

            // Set these variables to represent the edges of prisoner
            double left = TopLeftPoint.X;
            double top = TopLeftPoint.Y;
            double right = left + prisoner.ActualWidth;
            double bottom = top + prisoner.ActualHeight;

            // Reposition to keep inside panel
            if (left < 0)
            {
                prisoner.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, 0);
            }
            else if ((right > jail.ActualWidth) && (left > 0))
            {
                double positionX_ToKeepInBounds = jail.ActualWidth - prisoner.ActualWidth;
                prisoner.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (positionX_ToKeepInBounds - prisoner.BorderThickness.Right));
            }

            if (top < 0)
            {
                prisoner.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, 0);
            }
            else if ((bottom > jail.ActualHeight) && (top > 0))
            {
                double positionY_ToKeepInBounds = jail.ActualHeight - prisoner.ActualHeight;
                prisoner.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, (positionY_ToKeepInBounds - prisoner.BorderThickness.Bottom));
            }
        }

#endregion

#region Cursor changes

        private void spToolButtons_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.PointerCursor.Type != CoreCursorType.Arrow)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }
        }

        private void spMenuButtons_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.PointerCursor.Type != CoreCursorType.Arrow)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }
        }

        private void HrulerGrips_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNorthSouth, 0);
        }

        private void HrulerGrips_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var rulerGrip = sender as UIElement;

            if (rulerGrip.PointerCaptures == null)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }
        }

        private void HrulerGrips_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Check for input device and set IsMouseDeviceRightClick value
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                var properties = e.GetCurrentPoint(this).Properties;
                if (properties.IsLeftButtonPressed)
                {
                    IsMouseDeviceRightClick = false;
                }
                else if (properties.IsRightButtonPressed)
                {
                    IsMouseDeviceRightClick = true;
                }
            }

            (sender as UIElement).CapturePointer(e.Pointer);
        }

        private void HrulerGrips_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var rulerGrip = sender as FrameworkElement;
            var pointerPoint = e.GetCurrentPoint(rulerGrip);
            var x = pointerPoint.Position.X;
            var y = pointerPoint.Position.Y;

            if (rulerGrip.Name.StartsWith("p")) // polygon shape is hard to determine if pointer is inside it, so just turn cursor back to arrow.
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }
            else // rectangle shape
            {
                if (x < 0 || x > rulerGrip.ActualWidth || y < 0 || y > rulerGrip.ActualHeight) // pointer point is outside of the grip
                {
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
                }
            }

            rulerGrip.ReleasePointerCapture(e.Pointer);
            rulerGrip.ClearValue(PointerCapturesProperty);
        }

        private void VrulerGrips_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeWestEast, 0);
        }

        private void VrulerGrips_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var rulerGrip = sender as UIElement;

            if (rulerGrip.PointerCaptures == null)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }
        }

        private void VrulerGrips_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Check for input device and set IsMouseDeviceRightClick value
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                var properties = e.GetCurrentPoint(this).Properties;
                if (properties.IsLeftButtonPressed)
                {
                    IsMouseDeviceRightClick = false;
                }
                else if (properties.IsRightButtonPressed)
                {
                    IsMouseDeviceRightClick = true;
                }
            }

            (sender as UIElement).CapturePointer(e.Pointer);
        }

        private void VrulerGrips_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var rulerGrip = sender as FrameworkElement;
            var pointerPoint = e.GetCurrentPoint(rulerGrip);
            var x = pointerPoint.Position.X;
            var y = pointerPoint.Position.Y;

            if (rulerGrip.Name.StartsWith("p")) // polygon shape is hard to determine if pointer is inside it, so just turn cursor back to arrow.
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }
            else // rectangle shape
            {
                if (x < 0 || x > rulerGrip.ActualWidth || y < 0 || y > rulerGrip.ActualHeight) // pointer point is outside of the grip
                {
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
                }
            }

            rulerGrip.ReleasePointerCapture(e.Pointer);
            rulerGrip.ClearValue(PointerCapturesProperty);
        }

        private void ValveOverlapText_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 1);
        }

        private void ValveOverlapText_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
        }

        #endregion

#region Shortcut Keys

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            var focusedElement = FocusManager.GetFocusedElement();
            
            if (focusedElement is TextBox) // Only allow some shortcut keys to be handled by the CoreWindow, because a TextBox has some of the same shortcut keys and only one thing should handle the shortcut action.
            {
                var currentStateOfCtrlKey = sender.GetAsyncKeyState(VirtualKey.Control);

                if (currentStateOfCtrlKey == CoreVirtualKeyStates.Down)
                {
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.Y: Redo(); break;
                        case VirtualKey.S: Save(); break;
                        case VirtualKey.N: New(); break;
                        case VirtualKey.O: Open(); break;
                        case VirtualKey.P: Print(); break;
                    }
                }
            }
            else // Allow all global shortcut keys
            {
                var currentStateOfCtrlKey = sender.GetAsyncKeyState(VirtualKey.Control);

                if (currentStateOfCtrlKey == CoreVirtualKeyStates.Down)
                {
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.V: Paste(); break;
                        case VirtualKey.Z: Undo(); break;
                        case VirtualKey.Y: Redo(); break;
                        case VirtualKey.S: Save(); break;
                        case VirtualKey.N: New(); break;
                        case VirtualKey.O: Open(); break;
                        case VirtualKey.C: Copy(); break;
                        case VirtualKey.P: Print(); break;
                    }
                }
            }
        }

#endregion

#region Right click flyout, copy/paste

        private void General_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Point position = e.GetPosition(gridToContainOthers);
            ShowCopyPasteFlyout(position);

            e.Handled = true;
        }

        void ShowCopyPasteFlyout(Point point)
        {
            transformRectForFlyoutPosition.TranslateX = point.X;
            transformRectForFlyoutPosition.TranslateY = point.Y;

            FlyoutBase flyout = FlyoutBase.GetAttachedFlyout(gridToContainOthers);
            flyout.Placement = FlyoutPlacementMode.Right;
            flyout.ShowAt(rectForFlyoutPosition);
        }

        private void flyoutCopy_Click(object sender, RoutedEventArgs e)
        {
            Copy();
        }

        private void flyoutPaste_Click(object sender, RoutedEventArgs e)
        {
            Paste();
        }

#endregion

#region Label maker

        private void btnCylLabels_Click(object sender, RoutedEventArgs e)
        {
            gridCover.Visibility = Visibility.Visible;
            spLabelChooser.Visibility = Visibility.Visible;
        }

        private async void labelChooserButtons_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UnBindLast();

                var btn = sender as Button;
                var numberOfLabels = Convert.ToInt32(btn.Content);

                Grid labelGrid = new Grid();
                labelGrid.Background = new SolidColorBrush(Colors.Transparent);

                if (numberOfLabels == 1)
                {
                    ColumnDefinition def1 = new ColumnDefinition() { Width = new GridLength(9) };
                    ColumnDefinition def2 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
                    ColumnDefinition def3 = new ColumnDefinition() { Width = new GridLength(9) };

                    labelGrid.ColumnDefinitions.Add(def1);
                    labelGrid.ColumnDefinitions.Add(def2);
                    labelGrid.ColumnDefinitions.Add(def3);

                    TextBox textbox = new TextBox() { Style = App.Current.Resources["styleTextBoxDividers"] as Style };
                    Bind(textbox);
                    textbox.SizeChanged += LabelTextBox_SizeChanged;
                    labelGrid.Children.Add(textbox);
                    Grid.SetColumn(textbox, 1);
                }
                else
                {
                    ColumnDefinition def0 = new ColumnDefinition() { Width = new GridLength(9) };
                    labelGrid.ColumnDefinitions.Add(def0);

                    for (int i = 1; i < numberOfLabels; i++)
                    {
                        ColumnDefinition def1 = new ColumnDefinition() { Width = GridLength.Auto };
                        ColumnDefinition def2 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };

                        labelGrid.ColumnDefinitions.Add(def1);
                        labelGrid.ColumnDefinitions.Add(def2);
                    }

                    ColumnDefinition defFinal1 = new ColumnDefinition() { Width = GridLength.Auto };
                    ColumnDefinition defFinal2 = new ColumnDefinition() { Width = new GridLength(9) };

                    labelGrid.ColumnDefinitions.Add(defFinal1);
                    labelGrid.ColumnDefinitions.Add(defFinal2);

                    int columnPosition = 1;
                    for (int i = 0; i < numberOfLabels; i++)
                    {
                        TextBox textbox = new TextBox() { Style = App.Current.Resources["styleTextBoxDividers"] as Style };
                        Bind(textbox);
                        textbox.SizeChanged += LabelTextBox_SizeChanged;
                        labelGrid.Children.Add(textbox);
                        Grid.SetColumn(textbox, columnPosition);

                        columnPosition += 2;
                    }
                }

                PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
                paintObject.Content = labelGrid;
                paintObject.Height = 60;
                paintObject.Width = numberOfLabels * 50;
                paintObject.MinWidth = 57;
                paintObject.Closing += GeneralPaintObject_Closing;
                paintObject.ManipulationStarting += GeneralPaintObj_ManipStarting;
                paintObject.ManipulationCompleted += GeneralPaintObj_ManipCompleted;
                paintObject.Z_Order_Changed += GeneralPaintObject_Z_Order_Changed;

                gridMain.Children.Add(paintObject);

                spLabelChooser.Visibility = Visibility.Collapsed;
                gridCover.Visibility = Visibility.Collapsed;

                _UndoRedo.InsertInUnDoRedoForAddRemoveElement(true, paintObject, gridMain);
                ManageUndoRedoButtons();
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to create labels.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyLabelMakerError" + " " + ex.Message + " " + ex.StackTrace);
            }
            
        }

        private void LabelTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TextBox tBox = sender as TextBox;

            if (tBox.MinHeight == 1)
            {
                tBox.Padding = new Thickness(2, 1, 2, 0);
                tBox.MinWidth = 18;
            }

            if (tBox.MinHeight == 2)
            {
                tBox.Padding = new Thickness(3, 1, 3, 0);
                tBox.MinWidth = 22;
            }

            if (tBox.MinHeight == 6)
            {
                tBox.Padding = new Thickness(4, 1, 5, 0);
                tBox.MinWidth = 29;
            }

            if (tBox.MinHeight == 10)
            {
                tBox.Padding = new Thickness(4, 1, 4, 0);
                tBox.MinWidth = 35;
            }
        }

        private void btnLabelCancel_Click(object sender, RoutedEventArgs e)
        {
            spLabelChooser.Visibility = Visibility.Collapsed;
            gridCover.Visibility = Visibility.Collapsed;
        }

#endregion

#region Drag and Drop

        private void gridToContainOthers_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.IsCaptionVisible = false;
            e.DragUIOverride.IsGlyphVisible = false;
        }

        private async void gridToContainOthers_Drop(object sender, DragEventArgs e)
        {
            try
            {
                DataPackageView dataView = e.DataView;

                if (dataView.Contains(StandardDataFormats.StorageItems))
                {
                    ProcessDataPackageViewForImages(dataView);
                }
                else
                {
                    await new MessageDialog("No items detected").ShowAsync();
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to drop item into app.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyDropError" + " " + ex.Message + " " + ex.StackTrace);
            }
        }

        async void LoadDataPackageViewImage(StorageFile droppedFile, BitmapImage bitmapFromDroppedFile)
        {
            Image image = new Image();
            image.Stretch = Stretch.Fill;
            image.Source = bitmapFromDroppedFile;

            double height = bitmapFromDroppedFile.PixelHeight;
            double width = bitmapFromDroppedFile.PixelWidth;
            double scale = 1;

            if (width > gridMain.ActualWidth || height > gridMain.ActualHeight)
            {
                scale = Math.Min(gridMain.ActualWidth / width, gridMain.ActualHeight / height);
                width = (width * scale) - 1;
                height = (height * scale) - 1;
            }

            // Save to local storage and generate unique name in case two of the same image are opened.
            StorageFile file2 = await droppedFile.CopyAsync(ApplicationData.Current.LocalFolder, droppedFile.Name, NameCollisionOption.GenerateUniqueName);

            string name = file2.Name;
            string path = "ms-appdata:///local/" + name;

            PaintObjectTemplatedControl paintObject = new PaintObjectTemplatedControl();
            paintObject.Width = width;
            paintObject.Height = height;
            paintObject.Content = image;
            paintObject.ImageFileName = name;
            paintObject.ImageFilePath = path;
            paintObject.ImageScale = scale;
            paintObject.OpacitySliderIsVisible = true;
            paintObject.Unloaded += PaintObject_Unloaded;
            paintObject.ManipulationStarting += GeneralPaintObj_ManipStarting;
            paintObject.ManipulationCompleted += GeneralPaintObj_ManipCompleted;
            paintObject.Closing += GeneralPaintObject_Closing;
            paintObject.Z_Order_Changed += GeneralPaintObject_Z_Order_Changed;

            gridMain.Children.Add(paintObject);

            _UndoRedo.InsertInUnDoRedoForAddRemoveElement(true, paintObject, gridMain);
            ManageUndoRedoButtons();

            imageCollection.Add(new StoredImage { FileName = name, FilePath = path });
        }

        async void ProcessDataPackageViewForImages(DataPackageView dataView)
        {
            var items = await dataView.GetStorageItemsAsync();
            if (items.Count > 0 && items.Count < 5)
            {
                var failCount = 0;
                var failString = "";

                try
                {
                    UnBindLast();

                    spanelBusy.Visibility = Visibility.Visible;
                    tblockBusy.Text = "Loading...";
                    pRing.IsActive = true;

                    foreach (StorageFile file in items)
                    {
                        try
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.SetSource(await file.OpenAsync(FileAccessMode.Read));

                            if (bitmap.PixelWidth < 42 || bitmap.PixelHeight < 42)
                            {
                                failCount++;
                                failString = failString + " One file too small;";
                            }
                            else
                            {
                                LoadDataPackageViewImage(file, bitmap);
                            }
                        }
                        catch
                        {
                            failCount++;
                            failString = failString + " One file of type " + file.FileType + " did not load;";
                        }
                    }
                }
                catch
                {
                    failCount++;
                    failString = failString + " The item is not supported in this app;";
                }
                finally
                {
                    pRing.IsActive = false;
                    tblockBusy.Text = "";
                    spanelBusy.Visibility = Visibility.Collapsed;
                }


                if (failCount > 0)
                {
                    await new MessageDialog(failCount.ToString() + " items could not be loaded. Additional info: " + failString).ShowAsync();
                }
            }
            else
            {
                await new MessageDialog("Too many items. A maximum of four items at a time are allowed.").ShowAsync();
            }
        }

#endregion

#region Save Selection

        private async void menuSaveSelection_Click(object sender, RoutedEventArgs e)
        {
            gridBranding.Visibility = Visibility.Visible;

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("SaveSelectionImage.png", CreationCollisionOption.ReplaceExisting);
            await ImageUtils.CaptureElementToFile(gridForOverall_0, file);

            gridCover.Visibility = Visibility.Visible;
            CropEnterStory.Begin();
            btnBack.Visibility = Visibility.Collapsed;
            btnCrop.Visibility = Visibility.Collapsed;
            btnSaveSelection.Visibility = Visibility.Visible;

            string name = file.Name;
            string path = "ms-appdata:///local/" + name;

            LoadImageIntoCropper(path);
        }

        private async void btnSaveSelection_Click(object sender, RoutedEventArgs e)
        {
            gridBranding.Visibility = Visibility.Collapsed;

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
                        CloseCropper();
                        gridCover.Visibility = Visibility.Visible;
                        gridforSaveSelection.Visibility = Visibility.Visible;
                        imageForSaveSelection.Source = bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to get the selected area.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyGetSelectedAreaError" + " " + ex.Message + " " + ex.StackTrace);

                CloseCropper();
                gridCover.Visibility = Visibility.Collapsed;
                gridforSaveSelection.Visibility = Visibility.Collapsed;
            }

            SaveSelection();
        }

        async void SaveSelection()
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
                    spanelBusy.Visibility = Visibility.Visible;
                    tblockBusy.Text = "Saving...";
                    pRing.IsActive = true;

                    await ImageUtils.CaptureElementToFile(imageForSaveSelection, file);
                    mightNeedToSave = false;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when trying to save the selection.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MySaveSelectionError" + " " + ex.Message + " " + ex.StackTrace);
            }
            finally
            {
                gridCover.Visibility = Visibility.Collapsed;
                gridforSaveSelection.Visibility = Visibility.Collapsed;

                pRing.IsActive = false;
                tblockBusy.Text = "";
                spanelBusy.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

#region SizeChanged events

        private void gridForOverall_0_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GeneralTransform gt = gridForOverall.TransformToVisual(gridForOverall_0);
            Point newP = gt.TransformPoint(new Point(0, 0));

            var oldP = workAreaStartPoint;

            if (newP.X > oldP.X)
            {
                gridForOverall.Width = gridForOverall_0.ActualWidth;
            }

            if (newP.Y > oldP.Y)
            {
                gridForOverall.Height = gridForOverall_0.ActualHeight;
            }
        }

        private void gridMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            gridCompressionOverlay.Height = gridMain.ActualHeight;
            gridExhOverlap.Height = gridMain.ActualHeight * 0.6;
            gridIntOverlap.Height = (gridMain.ActualHeight * 0.6) - 46;

            lineHrulerPres.X2 = gridMain.ActualWidth;
            lineHrulerZero.X2 = gridMain.ActualWidth;
            lineVrulerZero.Y2 = gridMain.ActualHeight;
            lineVruler720.Y2 = gridMain.ActualHeight;
            lineV2.Y2 = gridMain.ActualHeight;
            lineV1.Y2 = gridMain.ActualHeight;
            lineH2.X2 = gridMain.ActualWidth;
            lineH1.X2 = gridMain.ActualWidth;

            GeneralTransform gt = lineVruler720.TransformToVisual(gridToContainOthers);
            Point point720 = gt.TransformPoint(new Point(0, 0));

            GeneralTransform gt1 = gridCompressionOverlay.TransformToVisual(gridToContainOthers);
            Point point0 = gt1.TransformPoint(new Point(0, 0));

            gridCompressionOverlay.Width = point720.X - point0.X + 1;

            SetUnitsPerX();
            SetUnitsPerY();

            if (gridDelta.Visibility == Visibility.Visible)
            {
                SetTextofPink(false);
                SetTextofPurple(false);
            }
        }

        private void gridCropping_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!(String.IsNullOrEmpty(tblockFileName.Text)) && tblockFileName.Text != "Please choose an image") // then there is an image in the gridImageContainer
            {
                string path = "ms-appdata:///local/" + tblockFileName.Text;
                LoadImageIntoCropper(path);
            }

        }
        #endregion

#region User Settings

        void RestoreUserSettings()
        {
            RestoreColorSelections();
            RestoreSizeSelection();
            RestoreColorBoxSelection();
        }

        void RestoreColorBoxSelection()
        {
            Object _lastColorBox = localSettings.Values[lastColorBox];

            if (_lastColorBox != null)
            {
                var colorBoxName = _lastColorBox.ToString();

                switch (colorBoxName)
                {
                    //case "strokeColorRB":
                    //    strokeColorRB.IsChecked = true;
                    //    break;
                    case "fillColorRB":
                        fillColorRB.IsChecked = true;
                        break;
                    case "textColorRB":
                        textColorRB.IsChecked = true;
                        break;
                    case "pageColorRB":
                        pageColorRB.IsChecked = true;
                        break;
                    //case "compLinesColorRB":
                    //    strokeColorRB.IsChecked = true;
                    //    break;
                }
            }
        }

        void RestoreSizeSelection()
        {
            Object _lastSizeSelected = localSettings.Values[lastSizeSelected];

            if (_lastSizeSelected != null)
            {
                var size = (double)_lastSizeSelected;

                switch (size)
                {
                    case 1:
                        rbSize1.IsChecked = true;
                        break;
                    case 2:
                        rbSize2.IsChecked = true;
                        break;
                    case 6:
                        rbSize6.IsChecked = true;
                        break;
                    case 10:
                        rbSize10.IsChecked = true;
                        break;
                }
            }
        }

        void RestoreColorSelections()
        {
            var strokeBrush = GetBrushFromSettings(localSettings.Values[lastStrokeColor]);
            var fillBrush = GetBrushFromSettings(localSettings.Values[lastFillColor]);
            var textBrush = GetBrushFromSettings(localSettings.Values[lastTextColor]);
            var pageBrush = GetBrushFromSettings(localSettings.Values[lastPageColor]);
            var compBrush = GetBrushFromSettings(localSettings.Values[lastCompLinesColor]);

            var transparentBrush = new SolidColorBrush(Colors.Transparent);

            // Set stroke color
            if (strokeBrush != null)
            {
                InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                drawingAttributes.Color = strokeBrush.Color;
                inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
                borderForStrokeColor.Background = strokeBrush;
                if (strokeBrush.Color == transparentBrush.Color)
                {
                    strokeX.Visibility = Visibility.Visible;
                }
                else
                {
                    strokeX.Visibility = Visibility.Collapsed;
                }
            }

            // Set fill color
            if (fillBrush != null)
            {
                borderForFillColor.Background = fillBrush;
                if (fillBrush.Color == transparentBrush.Color)
                {
                    fillX.Visibility = Visibility.Visible;
                }
                else
                {
                    fillX.Visibility = Visibility.Collapsed;
                }
            }

            // Set text color
            if (textBrush != null)
            {
                borderForTextColor.Background = textBrush;
                if (textBrush.Color == transparentBrush.Color)
                {
                    textX.Visibility = Visibility.Visible;
                }
                else
                {
                    textX.Visibility = Visibility.Collapsed;
                }
            }

            // Set page color
            if (pageBrush != null)
            {
                if (pageBrush.Color == transparentBrush.Color)
                {
                    borderForPageColor.Background = new SolidColorBrush(Colors.White);
                }
                else
                {
                    borderForPageColor.Background = pageBrush;
                }
            }

            // Set compLines color
            if (compBrush != null)
            {
                if (compBrush.Color == transparentBrush.Color)
                {
                    borderForCompLinesColor.Background = new SolidColorBrush(Colors.Gray);
                }
                else
                {
                    borderForCompLinesColor.Background = compBrush;
                }
            }
        }

        SolidColorBrush GetBrushFromSettings(Object settingsHexColor)
        {
            if (settingsHexColor != null)
            {
                return ColorConverters.GetColorFromHex(settingsHexColor.ToString());
            }
            else
            {
                return null;
            }
        }

        #endregion

#region Show/Hide ruler lines

        private void PolyRuler_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var fwElement = sender as FrameworkElement;
            var name = fwElement.Name;

            polyRulerLine =
                  name == "polygonHpres" ? lineHrulerPres
                : name == "polygonHzero" ? lineHrulerZero
                : name == "polygonVzero" ? lineVrulerZero
                : name == "polygonV720" ? lineVruler720
                : null;

            var lineBrush = polyRulerLine.Stroke as SolidColorBrush;
            var grayBrush = new SolidColorBrush(Colors.Gray);

            bool lineIsVisible = lineBrush.Color == grayBrush.Color ? true : false ;
            menuShow.IsEnabled = !(lineIsVisible);
            menuHide.IsEnabled = lineIsVisible;

            FlyoutBase flyout = FlyoutBase.GetAttachedFlyout(polygonHpres);
            flyout.Placement = FlyoutPlacementMode.Right;
            flyout.ShowAt(fwElement);

            e.Handled = true;
        }

        private void ShowLine_Click(object sender, RoutedEventArgs e)
        {
            polyRulerLine.Stroke = new SolidColorBrush(Colors.Gray);
        }

        private void HideLine_Click(object sender, RoutedEventArgs e)
        {
            polyRulerLine.Stroke = new SolidColorBrush(Colors.Transparent);
        }

        private void flyoutShowHide_Opening(object sender, object e)
        {
            IsMouseDeviceRightClick = false;
        }


        #endregion
    }
}
