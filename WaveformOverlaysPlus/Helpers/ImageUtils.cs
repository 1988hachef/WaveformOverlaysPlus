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

        public static async Task<StorageFile> WriteableBitmapToTemporaryFile(WriteableBitmap WB, string fileName)
        {
            Guid endcoderID = GetBitmapEncoderId(fileName);
            DisplayInformation dispInfo = DisplayInformation.GetForCurrentView();
            StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

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



        // <summary>
        /// Resizes and crops source file image so that resized image width/height are not larger than <param name="requestedMinSide"></param>
        /// </summary>
        /// <param name="sourceFile">Source StorageFile</param>
        /// <param name="requestedMinSide">Width/Height of the output image</param>
        /// <param name="resizedImageFile">Target StorageFile</param>
        /// <returns></returns>
        public static async Task<StorageFile> CreateThumbnailFromFile(StorageFile sourceFile, int requestedMinSide, StorageFile resizedImageFile)
        {
            var imageStream = await sourceFile.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(imageStream);
            var originalPixelWidth = decoder.PixelWidth;
            var originalPixelHeight = decoder.PixelHeight;

            using (imageStream)
            {
                //do resize only if needed
                if (originalPixelHeight > requestedMinSide && originalPixelWidth > requestedMinSide)
                {
                    using (var resizedStream = await resizedImageFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        //create encoder based on decoder of the source file
                        var encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                        double widthRatio = (double)requestedMinSide / originalPixelWidth;
                        double heightRatio = (double)requestedMinSide / originalPixelHeight;
                        uint aspectHeight = (uint)requestedMinSide;
                        uint aspectWidth = (uint)requestedMinSide;
                        uint cropX = 0, cropY = 0;
                        var scaledSize = (uint)requestedMinSide;
                        if (originalPixelWidth > originalPixelHeight)
                        {
                            aspectWidth = (uint)(heightRatio * originalPixelWidth);
                            cropX = (aspectWidth - aspectHeight) / 2;
                        }
                        else
                        {
                            aspectHeight = (uint)(widthRatio * originalPixelHeight);
                            cropY = (aspectHeight - aspectWidth) / 2;
                        }
                        //you can adjust interpolation and other options here, so far linear is fine for thumbnails
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
                        encoder.BitmapTransform.ScaledHeight = aspectHeight;
                        encoder.BitmapTransform.ScaledWidth = aspectWidth;
                        encoder.BitmapTransform.Bounds = new BitmapBounds()
                        {
                            Width = scaledSize,
                            Height = scaledSize,
                            X = cropX,
                            Y = cropY,
                        };
                        await encoder.FlushAsync();
                    }
                }
                else
                {
                    //otherwise just use source file as thumbnail
                    await sourceFile.CopyAndReplaceAsync(resizedImageFile);
                }
            }
            return resizedImageFile;
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
