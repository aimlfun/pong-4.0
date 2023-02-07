using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pong.AI
{
    internal static class BallTrajectoriesPlotter
    {
        internal static bool Enabled = true;

        /// <summary>
        /// Shows all the paths of ball from right-hand-bat.
        /// </summary>
        internal static Bitmap GetImageWithAllThePathsPlotted(Bitmap baseImage)
        {
            Bitmap b = baseImage is null ? new(Config.WidthOfTennisCourtPX, Config.HeightOfTennisCourtPX) : new(baseImage);

            Graphics g = Graphics.FromImage(b);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

            if (!Enabled)
            {
                WriteTrajectoryIsDisabledInCentre(g);
            }
            else
            {
                float rightHandBadPositionX = Config.WidthOfTennisCourtPX - Config.DistanceOfBatFromEdgeXpx - Config.WidthOfBat / 2;
                float scale = Config.WidthOfTennisCourtPX * 1.5f;

                foreach (TrainingDataItem tdi in Trainer.TrainingData)
                {
                    using Pen p = new(Color.FromArgb(10, 255, 255, 255));
                    p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                    g.DrawLine(p,
                                new PointF(rightHandBadPositionX,
                                           (float)tdi.YPositionOfTheBallWhenItArrivesOnRightBatLine),
                                new PointF(rightHandBadPositionX + tdi.xVelocityOfBallInDirectionOfLeftBat * scale,
                                           (float)(tdi.YPositionOfTheBallWhenItArrivesOnRightBatLine + tdi.yVelocityOfBallInDirectionOfLeftBat * scale))); // width not height otherwise distorted angle
                }
            }

            g.Flush();

            return b;
        }

        /// <summary>
        /// Displays trajectory plot has been turned off.
        /// </summary>
        /// <param name="graphics"></param>
        private static void WriteTrajectoryIsDisabledInCentre(Graphics graphics)
        {
            using Font f = new("Arial", 10);
            string label = "Trajectory Plot: OFF";
            SizeF size = graphics.MeasureString(label, f);

            // remember this is Y vs. Y so height x height
            graphics.DrawString(label, f, Brushes.White, new PointF(Config.HeightOfTennisCourtPX / 2 - size.Width / 2, Config.HeightOfTennisCourtPX / 2 - size.Height / 2));
        }
    }
}