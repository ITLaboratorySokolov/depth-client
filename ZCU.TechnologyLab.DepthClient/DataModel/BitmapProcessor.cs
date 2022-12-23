using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    internal class BitmapProcessor
    {
        /// <summary>
        /// Helper method for encoding BitmapSource into Bitmap image
        /// </summary>
        /// <param name="bitmapSource"> Bitmap source </param>
        /// <returns> Bitmap image </returns>
        public static BitmapImage Bitmap2Image(BitmapSource bitmapSource)
        {
            // before encoding/decoding, check if bitmapSource is already a BitmapImage

            if (!(bitmapSource is BitmapImage bitmapImage))
            {
                bitmapImage = new BitmapImage();

                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    encoder.Save(memoryStream);
                    memoryStream.Position = 0;

                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();
                }
            }

            return bitmapImage;
        }

        /// <summary>
        /// Build bitmap from byte array
        /// </summary>
        /// <param name="data"> Data </param>
        /// <param name="w"> Width </param>
        /// <param name="h"> Height </param>
        /// <param name="ch"> Pixel format - 1 = Gray8, 3 = RGB24, 4 = BGR+alpha </param>
        /// <returns></returns>
        public static BitmapSource BuildBitmap(byte[] data, int w, int h, int ch)
        {
            PixelFormat format = PixelFormats.Default;

            if (ch == 1) format = PixelFormats.Gray8; //grey scale image 0-255
            if (ch == 3) format = PixelFormats.Rgb24; //RGB
            if (ch == 4) format = PixelFormats.Bgr32; //BGR + alpha


            WriteableBitmap wbm = new WriteableBitmap(w, h, 96, 96, format, null);
            wbm.WritePixels(new Int32Rect(0, 0, w, h), data, ch * w, 0);

            return wbm;
        }

    }
}
