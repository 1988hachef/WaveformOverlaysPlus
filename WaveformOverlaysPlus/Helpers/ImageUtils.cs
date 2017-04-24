using System;
using System.IO;
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
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                Stream pixelStream = WB.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                     BitmapAlphaMode.Straight,
                                     (uint)WB.PixelWidth,
                                     (uint)WB.PixelHeight,
                                     96.0,
                                     96.0,
                                     pixels);
                await encoder.FlushAsync();
            }
            return file;
        }

        public static async Task CaptureElementToFile(UIElement uiElement, StorageFile file)
        {
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(uiElement);
            IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

            DisplayInformation dispInfo = DisplayInformation.GetForCurrentView();

            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                    (uint)renderTargetBitmap.PixelWidth,
                    (uint)renderTargetBitmap.PixelHeight,
                    dispInfo.LogicalDpi, dispInfo.LogicalDpi,
                    pixelBuffer.ToArray());

                await encoder.FlushAsync();
            }
        }
    }
}
