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

namespace WaveformOverlaysPlus
{
    public sealed partial class MainPage : Page
    {
        private PrintManager printMan;
        private PrintDocument printDoc;
        private IPrintDocumentSource printDocSource;

        DataTransferManager dataTransferManager;


        public MainPage()
        {
            this.InitializeComponent();

            // Set visibility of Feedback, if device supports Feedback
            if (StoreServicesFeedbackLauncher.IsSupported())
            {
                menuFeedback.Visibility = Visibility.Visible;
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

        private void tool_Checked(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
