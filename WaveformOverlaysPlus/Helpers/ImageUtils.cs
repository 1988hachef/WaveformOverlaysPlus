using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace WaveformOverlaysPlus.Helpers
{
    class ImageUtils
    {
        public static async Task<StorageFile> WriteableBitmapToStorageFile(WriteableBitmap WB, string fileName)
        {
            Guid endcoderID = GetBitmapEncoderId(fileName);
            DisplayInformation dispInfo = DisplayInformation.GetForCurrentView();
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(endcoderID, stream);
                Stream pixelStream = WB.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                     BitmapAlphaMode.Straight,
                                     (uint)WB.PixelWidth,
                                     (uint)WB.PixelHeight,
                                     dispInfo.LogicalDpi,
                                     dispInfo.LogicalDpi,
                                     pixels);
                await encoder.FlushAsync();
            }
            return file;
        }

        public static async Task CaptureElementToFile(UIElement uiElement, StorageFile outputFile)
        {
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(uiElement);
            IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

            DisplayInformation dispInfo = DisplayInformation.GetForCurrentView();
            Guid encoderId = GetBitmapEncoderId(outputFile.Name);

            using (var stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(encoderId, stream);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, 
                                     BitmapAlphaMode.Straight,
                                     (uint)renderTargetBitmap.PixelWidth,
                                     (uint)renderTargetBitmap.PixelHeight,
                                     dispInfo.LogicalDpi,
                                     dispInfo.LogicalDpi,
                                     pixelBuffer.ToArray());

                await encoder.FlushAsync();
            }
        }

        private static Guid GetBitmapEncoderId(string fileName)
        {
            Guid encoderId;

            var ext = Path.GetExtension(fileName);

            if (new[] { ".bmp", ".dib" }.Contains(ext))
            {
                encoderId = BitmapEncoder.BmpEncoderId;
            }
            else if (new[] { ".tiff", ".tif" }.Contains(ext))
            {
                encoderId = BitmapEncoder.TiffEncoderId;
            }
            else if (new[] { ".gif" }.Contains(ext))
            {
                encoderId = BitmapEncoder.GifEncoderId;
            }
            else if (new[] { ".jpg", ".jpeg", ".jpe", ".jfif", ".jif" }.Contains(ext))
            {
                encoderId = BitmapEncoder.JpegEncoderId;
            }
            else if (new[] { ".hdp", ".jxr", ".wdp" }.Contains(ext))
            {
                encoderId = BitmapEncoder.JpegXREncoderId;
            }
            else //if (new [] {".png"}.Contains(ext))
            {
                encoderId = BitmapEncoder.PngEncoderId;
            }

            return encoderId;
        }
    }
}
