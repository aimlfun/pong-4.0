using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pong.AI
{
    internal static class DebugLog
    {
        /// <summary>
        /// 
        /// </summary>
        private const int c_maxItems = 10;

        /// <summary>
        /// 
        /// </summary>
        private readonly static List<string> debug = new();

        /// <summary>
        /// 
        /// </summary>
        private static PictureBox canvas;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="canvasPB"></param>
        internal static void Initialise(PictureBox canvasPB)
        {
            canvas = canvasPB;
        }

        internal static void Log(string log)
        {
            debug.Add(log);

            if (debug.Count > c_maxItems) debug.RemoveAt(0); // remove top item to keep the max items

            if (canvas is null) return;

            Bitmap bitmap = new(canvas.Width, canvas.Height); // do not use "using", we are assigning it
            
            using Graphics g = Graphics.FromImage(bitmap);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            
            g.Clear(Color.Black);
            g.DrawString(string.Join("\n",debug), new Font("Arial", 6 ), Brushes.White, 2, 2);
            g.Flush();

            canvas.Image?.Dispose();
            canvas.Image = bitmap;

            Application.DoEvents();
        }
    }
}
