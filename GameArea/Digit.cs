using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Pong.GameArea
{
    /// <summary>
    /// Large digit rendering.
    /// </summary>
    internal static class Digit
    {
        /*  Renders square digits to Bitmap.
         * 
         *   ###   #   ###  ###  # #  ###  ###  ###  ###  ###
         *   # #   #     #    #  # #  #    #      #  # #  # #
         *   # #   #   ###  ###  ###  ###  ###    #  ###  ###
         *   # #   #   #      #    #    #  # #    #  # #    #
         *   ###   #   ###  ###    #  ###  ###    #  ###    #
         */

        readonly static string[] digit0 = new[] {
            "###",
            "# #",
            "# #",
            "# #",
            "###" };

        readonly static string[] digit1 = new[] {
            " # ",
            " # ",
            " # ",
            " # ",
            " # " };

        readonly static string[] digit2 = new[] {
            "###",
            "  #",
            "###",
            "#  ",
            "###" };

        readonly static string[] digit3 = new[] {
            "###",
            "  #",
            "###",
            "  #",
            "###" };

        readonly static string[] digit4 = new[] {
            "# #",
            "# #",
            "###",
            "  #",
            "  #" };

        readonly static string[] digit5 = new[] {
            "###",
            "#  ",
            "###",
            "  #",
            "###" };

        readonly static string[] digit6 = new[] {
            "###",
            "#  ",
            "###",
            "# #",
            "###" };

        readonly static string[] digit7 = new[] {
            "###",
            "  #",
            "  #",
            "  #",
            "  #" };

        readonly static string[] digit8 = new[] {
            "###",
            "# #",
            "###",
            "# #",
            "###" };

        readonly static string[] digit9 = new[] {
            "###",
            "# #",
            "###",
            "  #",
            "  #" };

        readonly static string[][] digits = new string[][] { digit0, digit1, digit2, digit3, digit4, digit5, digit6, digit7, digit8, digit9 };

        readonly static Bitmap[] cachedDigitImageIndexByDigit;

        /// <summary>
        /// Generate digits 0...9 once at start up.
        /// </summary>
        static Digit()
        {
            List<Bitmap> listOfBitmaps = new();

            for (int i = 0; i < 10; i++)
            {
                listOfBitmaps.Add(DigitAsBitmap(i));
            }

            cachedDigitImageIndexByDigit = listOfBitmaps.ToArray();
        }

        /// <summary>
        /// Returns a cached image.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal static Bitmap GetImage(int i)
        {
            if (i < 0 || i > 9) throw new ArgumentOutOfRangeException(nameof(i),"This fetches an image of a digit 0..9");

            return cachedDigitImageIndexByDigit[i];
        }

        /// <summary>
        /// Returns a bitmap containing the digit.
        /// </summary>
        /// <param name="digit"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal static Bitmap DigitAsBitmap(int digit)
        {
            if (digit is < 0 or > 9) throw new ArgumentOutOfRangeException(nameof(digit), "Supports digits 0..9 only");
           
            Bitmap bitmap = new(3 * Config.ScoreBoardPixelSize, 5 * Config.ScoreBoardPixelSize);

            // we have an array of "raster" lines from which we draw
            string[] rasterLines = digits[digit];

            // on the original, they are greyish
            using SolidBrush brushGrey = new(Color.FromArgb(200, 255, 255, 255));

            using Graphics graphics = Graphics.FromImage(bitmap);

            // draw the digit (3x5), colouring illuminate pixels
            for (int y = 0; y < 5; y++)
            {
                string thisRasterLine = rasterLines[y];

                for (int x = 0; x < 3; x++)
                {
                    if (thisRasterLine[x] == '#') // denotes draw pixel
                    {
                        graphics.FillRectangle(brushGrey, x * Config.ScoreBoardPixelSize, y * Config.ScoreBoardPixelSize, Config.ScoreBoardPixelSize, Config.ScoreBoardPixelSize);
                    }
                }
            }

            graphics.Flush();

            return bitmap;
        }

        /// <summary>
        /// Returns size of the digit for scaling purpose.
        /// </summary>
        internal static Size SizeOfDigit
        {
            get
            {
                return new(3 * Config.ScoreBoardPixelSize, 5 * Config.ScoreBoardPixelSize);
            }
        }
    }
}