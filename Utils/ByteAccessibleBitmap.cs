using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Pong.Utils
{
    /// <summary>
    /// GetPixel() on a Bitmap isn't quick. To do it, it has to lock/unlock. 
    /// This class does the lock/unlock once, copying the data to a byte array.
    /// </summary>
    internal class ByteAccessibleBitmap
    {
        /// <summary>
        /// This is the bitmap. If null, the track image needs to be locked and copied to it.
        /// </summary>
        private readonly Bitmap? srcDisplayBitMap = null;

        /// <summary>
        /// This is the attributes of the bitmap.
        /// </summary>
        private readonly BitmapData? srcDisplayMapData;

        /// <summary>
        /// This is a pointer to the bitmap's data.
        /// </summary>
        private readonly IntPtr srcDisplayMapDataPtr;

        /// <summary>
        /// Bytes per row of pixels.
        /// </summary>
        private readonly int strideDisplay;

        /// <summary>
        /// This is how many bytes the bitmap is.
        /// </summary>
        private readonly int totalLengthDisplay;

        /// <summary>
        /// This is the pixels in the bitmap.
        /// </summary>
        internal readonly byte[] Pixels;

        /// <summary>
        /// This is how many bytes each pixel occupies in the bitmap.
        /// </summary>
        private readonly int bytesPerPixelDisplay;

        /// <summary>
        /// This is how many bytes per row of the bitmap image (used to multiply "y" by to get to the correct data).
        /// </summary>
        private readonly int offsetDisplay;

        /// <summary>
        /// Turns a bitmap into a byte array.
        /// </summary>
        internal ByteAccessibleBitmap(Bitmap img)
        {
            if (img is null) throw new ArgumentNullException(nameof(img), "image should be populated before calling this."); // can't cache what has been drawn!

            srcDisplayBitMap = img;
            srcDisplayMapData = srcDisplayBitMap.LockBits(new Rectangle(0, 0, srcDisplayBitMap.Width, srcDisplayBitMap.Height), ImageLockMode.ReadOnly, img.PixelFormat);
            srcDisplayMapDataPtr = srcDisplayMapData.Scan0;
            strideDisplay = srcDisplayMapData.Stride;

            totalLengthDisplay = Math.Abs(strideDisplay) * srcDisplayBitMap.Height;
            Pixels = new byte[totalLengthDisplay];

            bytesPerPixelDisplay = Image.GetPixelFormatSize(srcDisplayMapData.PixelFormat) / 8;
            offsetDisplay = strideDisplay;
            System.Runtime.InteropServices.Marshal.Copy(srcDisplayMapDataPtr, Pixels, 0, totalLengthDisplay);

            srcDisplayBitMap.UnlockBits(srcDisplayMapData);
        }

        /// <summary>
        /// Returns a red channel pixel from the cached bytes representing the image.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        internal byte GetRedChannelPixel(int x, int y)
        {
            // 4 bytes per pixel to 1. (ARGB). Pixels are 255 R, 255 G, 255 B, 255 Alpha. ]
            return Pixels[y * offsetDisplay + x * bytesPerPixelDisplay];
        }

        /// <summary>
        /// Returns a green channel pixel from the cached bytes representing the image.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        internal byte GetGreenChannelPixel(int x, int y)
        {
            // 4 bytes per pixel to 1. (ARGB). Pixels are 255 R, 255 G, 255 B, 255 Alpha.
            return Pixels[y * offsetDisplay + x * bytesPerPixelDisplay + 1];
        }

        /// <summary>
        /// Returns a blue channel pixel from the cached bytes representing the image.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        internal byte GetBlueChannelPixel(int x, int y)
        {
            // 4 bytes per pixel to 1. (ARGB). Pixels are 255 R, 255 G, 255 B, 255 Alpha.
            return Pixels[y * offsetDisplay + x * bytesPerPixelDisplay + 2];
        }

        /// <summary>
        /// Returns complete pixel from the cached bytes representing the image.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        internal Color GetARGBPixel(int x, int y)
        {
            return Color.FromArgb(
                Pixels[y * offsetDisplay + x * bytesPerPixelDisplay + 3],
                Pixels[y * offsetDisplay + x * bytesPerPixelDisplay ],
                Pixels[y * offsetDisplay + x * bytesPerPixelDisplay + 1],
                Pixels[y * offsetDisplay + x * bytesPerPixelDisplay + 2]);
        }
    }
}