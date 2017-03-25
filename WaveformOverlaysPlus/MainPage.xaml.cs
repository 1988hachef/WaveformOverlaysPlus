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

namespace WaveformOverlaysPlus
{
    public sealed partial class MainPage : Page
    {
        WriteableBitmap originalWB;
        Image imageMain;
        CompositeTransform transformImage;

        static string locked = "\uE72E";
        static string unlocked = "\uE785";

        private PrintManager printMan;
        private PrintDocument printDoc;
        private IPrintDocumentSource printDocSource;

        DataTransferManager dataTransferManager;


        public MainPage()
        {
            this.InitializeComponent();
        }

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

        private void menuNew_Click(object sender, RoutedEventArgs e)
        {
            gridMain.Children.Clear();
        }

        private async void btnFile_Click(object sender, RoutedEventArgs e)
        {


            //StorageFile thumbnailFile = await ImageUtils.WriteableBitmapToStorageFile(wb, "thumbnail.png");
            //StorageFile shareFile = await ImageUtils.WriteableBitmapToStorageFile(wb, "shareFile.png");
        }

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
                originalWB = new WriteableBitmap(1, 1);
                await originalWB.LoadAsync(imgFile);

                if (originalWB.PixelWidth < 100 || originalWB.PixelHeight < 100)
                {
                    MessageDialog tooSmallMessage = new MessageDialog("Image too small. Please choose a larger image.");
                    await tooSmallMessage.ShowAsync();
                }
                else
                {
                    AddNewImage(originalWB);
                }

                StorageFile originalFile = await ImageUtils.WriteableBitmapToStorageFile(originalWB, "originalImage.png");
                StorageFile cropperFile = await ImageUtils.WriteableBitmapToStorageFile(originalWB, "cropImage.png");
            }
        }

        private void AddNewImage(WriteableBitmap wb)
        {
            if (imageMain != null)
            {
                gridMain.Children.Remove(imageMain);
                transformImage.TranslateX = 0;
                transformImage.TranslateY = 0;
            }

            // Initialize the transform for the image
            transformImage = new CompositeTransform();

            // Initialize the image
            imageMain = new Image
            {
                ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                RenderTransform = transformImage,
                Source = wb,
                Visibility = Visibility.Collapsed
            };

            // Create event handlers for image
            imageMain.ManipulationDelta += imageMain_ManipulationDelta;
            imageMain.PointerEntered += imageMain_PointerEntered;
            imageMain.PointerExited += imageMain_PointerExited;
            imageMain.SizeChanged += imageMain_SizeChanged;
            imageMain.Loaded += imageMain_Loaded;

            // Add the image
            gridMain.Children.Add(imageMain);
            imageMain.SetValue(Canvas.ZIndexProperty, -1);
        }
        #endregion

        #region Image events

        private void imageMain_Loaded(object sender, RoutedEventArgs e)
        {
            if (originalWB != null)
            {
                if (originalWB.PixelWidth < gridMain.ActualWidth && originalWB.PixelHeight < gridMain.ActualHeight)
                {
                    imageMain.Width = originalWB.PixelWidth;
                    imageMain.Height = originalWB.PixelHeight;
                }
                else
                {
                    imageMain.Width = gridMain.ActualWidth;
                    imageMain.Height = gridMain.ActualHeight;
                }
            }
            else
            {
                imageMain.Width = gridMain.ActualWidth;
                imageMain.Height = gridMain.ActualHeight;
            }

            if (imageMain.Visibility == Visibility.Collapsed)
            {
                imageMain.Visibility = Visibility.Visible;
            }
        }

        private void imageMain_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (tblockMove.Text == unlocked)
            {
                MoveAndRestrain(imageMain, gridMain, transformImage, e);
            }
        }

        private void imageMain_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (tblockMove.Text == unlocked)
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeAll, 1);
            }
        }

        private void imageMain_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.PointerCursor.Type == Windows.UI.Core.CoreCursorType.SizeAll)
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            }
        }

        private async void imageMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (imageMain != null)
            {
                if (imageMain.ActualWidth > gridMain.ActualWidth || imageMain.ActualHeight > gridMain.ActualHeight)
                {
                    double scale = Math.Min(gridMain.ActualWidth / imageMain.ActualWidth, gridMain.ActualHeight / imageMain.ActualHeight);
                    imageMain.Width = imageMain.ActualWidth * scale;
                    imageMain.Height = imageMain.ActualHeight * scale;

                    var dialog = new MessageDialog("Image at max size, but you can crop it then zoom in. (Image is restricted to staying within the border)");
                    await dialog.ShowAsync();
                }

                // Get the top left point of prisoner in relationship to jail
                GeneralTransform gt = imageMain.TransformToVisual(gridMain);
                Point prisonerTopLeftPoint = gt.TransformPoint(new Point(0, 0));

                // Set these variables to represent the edges of prisoner
                double left = prisonerTopLeftPoint.X;
                double top = prisonerTopLeftPoint.Y;
                double right = left + imageMain.ActualWidth;
                double bottom = top + imageMain.ActualHeight;

                // Reposition prisoner to keep in jail (jail's BorderThickness is subtracted for right and bottom because it affects postioning)
                if (left < 0)
                {
                    transformImage.TranslateX = 0;
                }
                else if ((right > gridMain.ActualWidth) && (left > 0))
                {
                    double updatedLeft = gridMain.ActualWidth - imageMain.ActualWidth;
                    transformImage.TranslateX = updatedLeft - gridMain.BorderThickness.Right;
                }

                if (top < 0)
                {
                    transformImage.TranslateY = 0;
                }
                else if ((bottom > gridMain.ActualHeight) && (top > 0))
                {
                    double updatedTop = gridMain.ActualHeight - imageMain.ActualHeight;
                    transformImage.TranslateY = updatedTop - gridMain.BorderThickness.Bottom;
                }
            }
        }

        #endregion

        #region MoveAndRestrain methods

        private void MoveAndRestrain(FrameworkElement prisoner, FrameworkElement jail, CompositeTransform move, ManipulationDeltaRoutedEventArgs eventData)
        {
            // Get the top left point of the prisoner in relationship to the jail
            GeneralTransform gt = prisoner.TransformToVisual(jail);
            Point prisonerTopLeftPoint = gt.TransformPoint(new Point(0, 0));

            // Set these variables to represent the edges of the prisoner
            double left = prisonerTopLeftPoint.X;
            double top = prisonerTopLeftPoint.Y;
            double right = left + prisoner.ActualWidth;
            double bottom = top + prisoner.ActualHeight;

            // Combine those edges with the movement value (When these are used in the next step, it keeps the prisoner from getting stuck at the jail boundary)
            double leftAdjust = left + eventData.Delta.Translation.X;
            double topAdjust = top + eventData.Delta.Translation.Y;
            double rightAdjust = right + eventData.Delta.Translation.X;
            double bottomAdjust = bottom + eventData.Delta.Translation.Y;

            // Allow prisoner movement if within jail boundary (Use two separate "if" statements here, so the movement isn't sticky at the boundary)
            if ((leftAdjust >= 0) && (rightAdjust <= jail.ActualWidth))
            {
                move.TranslateX += eventData.Delta.Translation.X;
            }

            if ((topAdjust >= 0) && (bottomAdjust <= jail.ActualHeight))
            {
                move.TranslateY += eventData.Delta.Translation.Y;
            }
        }

        private void MoveAndRestrainWithThumb(FrameworkElement prisoner, FrameworkElement jail, CompositeTransform movePrisoner, CompositeTransform moveThumb, ManipulationDeltaRoutedEventArgs eventData)
        {
            // Get the top left point of the prisoner in relationship to the jail
            GeneralTransform gt = prisoner.TransformToVisual(jail);
            Point prisonerTopLeftPoint = gt.TransformPoint(new Point(0, 0));

            // Set these variables to represent the edges of the prisoner
            double left = prisonerTopLeftPoint.X;
            double top = prisonerTopLeftPoint.Y;
            double right = left + prisoner.ActualWidth;
            double bottom = top + prisoner.ActualHeight;

            // Combine those edges with the movement value (When these are used in the next step, it keeps the prisoner from getting stuck at the jail boundary)
            double leftAdjust = left + eventData.Delta.Translation.X;
            double topAdjust = top + eventData.Delta.Translation.Y;
            double rightAdjust = right + eventData.Delta.Translation.X;
            double bottomAdjust = bottom + eventData.Delta.Translation.Y;

            // Allow prisoner movement if within jail boundary (Use two separate "if" statements here, so the movement isn't sticky at the boundary)
            if ((leftAdjust >= 0) && (rightAdjust <= jail.ActualWidth))
            {
                movePrisoner.TranslateX += eventData.Delta.Translation.X;
                moveThumb.TranslateX += eventData.Delta.Translation.X;
            }

            if ((topAdjust >= 0) && (bottomAdjust <= jail.ActualHeight))
            {
                movePrisoner.TranslateY += eventData.Delta.Translation.Y;
                moveThumb.TranslateY += eventData.Delta.Translation.Y;
            }
        }

        private void MoveAndRestrainThumb(FrameworkElement Alice,
                                          FrameworkElement Wonderland,
                                          CompositeTransform transform,
                                          int minSize,
                                          ManipulationDeltaRoutedEventArgs eventData)
        {
            GeneralTransform gt = Alice.TransformToVisual(Wonderland);
            Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

            // Set these variables to represent the edges of the thing we are resizing
            double right = TopLeftPoint.X + Alice.ActualWidth;
            double bottom = TopLeftPoint.Y + Alice.ActualHeight;

            // Combine the adjustable edges with the movement value.
            double rightAdjust = right + eventData.Delta.Translation.X;
            double bottomAdjust = bottom + eventData.Delta.Translation.Y;

            // Set these to use for restricting the minimum size
            double yadjust = Alice.Height + eventData.Delta.Translation.Y;
            double xadjust = Alice.Width + eventData.Delta.Translation.X;

            //// Get image's position inside the grid
            //GeneralTransform gt2 = imageMain.TransformToVisual(gridMain);
            //Point imageTopLeftPoint = gt2.TransformPoint(new Point(0, 0));

            // Restrict adjustments
            if ((rightAdjust <= Wonderland.ActualWidth) && (xadjust >= minSize))
            {
                Alice.Width = xadjust;
                transform.TranslateX += eventData.Delta.Translation.X;
            }

            if ((bottomAdjust <= Wonderland.ActualHeight) && (yadjust >= minSize))
            {
                Alice.Height = yadjust;
                transform.TranslateY += eventData.Delta.Translation.Y;
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
                savePicker.FileTypeChoices.Add(".bmp Bitmap", new List<string>() { ".bmp" });
                savePicker.FileTypeChoices.Add(".gif Graphical Interchange Format", new List<string>() { ".gif" });
                savePicker.FileTypeChoices.Add(".jpg Joint Photographic Experts Group", new List<string>() { ".jpg" });
                savePicker.FileTypeChoices.Add(".png Portable Network Graphics", new List<string>() { ".png" });
                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file != null)
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
                    await wb.SaveAsync(file);
                }
                else
                {

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
            gridContextDialog.Visibility = Visibility.Visible;

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
                        Content = "\nSorry, printing can' t proceed at this time.",
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
                gridContextDialog.Visibility = Visibility.Collapsed;
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
                });
            }

            if (args.Completion == PrintTaskCompletion.Abandoned ||
                args.Completion == PrintTaskCompletion.Canceled ||
                args.Completion == PrintTaskCompletion.Submitted)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    imageForPrint.ClearValue(Image.SourceProperty);
                    gridContextDialog.Visibility = Visibility.Collapsed;
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


    }
}
