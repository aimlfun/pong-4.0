using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pong.AI;

namespace Pong.NewFolder
{
    /// <summary>
    /// Visualiser for training current data item.
    /// 
    ///              ypos right bat
    /// +-------------|------+
    /// |             |      |
    /// |             |      |
    /// |    x        |      | <- expected AI response
    /// |    x        |      |
    /// -----x--------+------- ypos left bat
    /// |    x        |      | <- actual AI response
    /// |             |      |
    /// +-------------|------+
    ///      ^ means for that position of "right bat", the AI was inaccurate.
    /// 
    /// 
    /// </summary>
    internal static class LearningVisualiser
    {
        /// <summary>
        /// Indicates whether to expend effort painting all the deviations or not.
        /// </summary>
        internal static bool Enabled = true;

        /// <summary>
        /// Draw a diagram showing which training points it is inaccurate at.
        /// </summary>
        /// <param name="currentTrainingDataItem"></param>
        /// <param name="maxAllowedDiffInAIVsExpected"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static Bitmap GetDiagram(TrainingDataItem currentTrainingDataItem, float maxAllowedDiffInAIVsExpected, out List<TrainingDataItem> itemsThatNeedTraining)
        {
            itemsThatNeedTraining = new();
            
            // 262 Y positions ball can be vs. 262 Y positions of bat.
            Bitmap image = new(Config.HeightOfTennisCourtPX, Config.HeightOfTennisCourtPX);

            using Graphics graphics = Graphics.FromImage(image);

            graphics.Clear(Color.Black);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

            if (!Enabled)
            {
                WriteVisualiserIsDisabledInCentre(graphics);
                return image;
            }

            LabelXAxis(graphics);

            LabelYAxis(graphics);

            DrawGreyLineIndicatingWhereBallHitRightBat(currentTrainingDataItem, graphics);

            // for the item that missed, draw a horizontal line indicating where it is currently at (neural-network output)
            DrawGreenAndRedLinesShowingActualAndExpectedForCurrentItemBeingTrained(graphics, currentTrainingDataItem);
            
            for (int i = 0; i < Trainer.TrainingData.Count; i++)
            {
                TrainingDataItem tdi = Trainer.TrainingData[i];
          
                double errorDifferenceBetweenAIresultAndExpected = tdi.lastBatExpectedYPosition - tdi.lastBatPositionYOutputByAI;

                // if error is within acceptable ranges, we draw nothing (speeding up the training)
                if (Math.Abs(errorDifferenceBetweenAIresultAndExpected) > maxAllowedDiffInAIVsExpected)
                {
                    DrawErrorDeviationBar(graphics, tdi, errorDifferenceBetweenAIresultAndExpected);
                    itemsThatNeedTraining.Add(tdi);
                }
            }

            graphics.Flush();

            return image;
        }

        /// <summary>
        /// Displays visualiser has been turned off.
        /// </summary>
        /// <param name="graphics"></param>
        private static void WriteVisualiserIsDisabledInCentre(Graphics graphics)
        {
            using Font f = new("Arial", 10);
            string label = "Visualiser: OFF";
            SizeF size = graphics.MeasureString(label, f);

            // remember this is Y vs. Y so height x height
            graphics.DrawString(label, f, Brushes.White, new PointF(Config.HeightOfTennisCourtPX / 2 - size.Width / 2, Config.HeightOfTennisCourtPX / 2 - size.Height/2));
        }

        /// <summary>
        /// Draws a fat bar showing where returned AI value differs from expected (above an acceptable threshold).
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="tdi"></param>
        /// <param name="errorDifferenceBetweenAIresultAndExpected"></param>
        private static void DrawErrorDeviationBar(Graphics graphics, TrainingDataItem tdi, double errorDifferenceBetweenAIresultAndExpected)
        {
            Color colour = Color.FromArgb(180,
                                errorDifferenceBetweenAIresultAndExpected < 0 ? 255 : 100, // -ve in red
                                100, // brightens the red/blue [adding green]
                                errorDifferenceBetweenAIresultAndExpected > 0 ? 255 : 100 // +ve in blue
                                );

            using Pen pen = new(colour, 1);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

            graphics.DrawLine(pen,
                              (int)Math.Round(tdi.YPositionOfTheBallWhenItArrivesOnRightBatLine),
                              (int)Math.Round(tdi.lastBatExpectedYPosition * Config.HeightOfTennisCourtPX),
                              (int)Math.Round(tdi.YPositionOfTheBallWhenItArrivesOnRightBatLine),
                              (int)Math.Round(tdi.lastBatPositionYOutputByAI * Config.HeightOfTennisCourtPX));

            graphics.DrawLine(Pens.Crimson,
                              (int)Math.Round(tdi.YPositionOfTheBallWhenItArrivesOnRightBatLine - 1),
                              (int)Math.Round(tdi.lastBatPositionYOutputByAI * Config.HeightOfTennisCourtPX),
                              (int)Math.Round(tdi.YPositionOfTheBallWhenItArrivesOnRightBatLine + 1),
                              (int)Math.Round(tdi.lastBatPositionYOutputByAI * Config.HeightOfTennisCourtPX));

            graphics.DrawLine(Pens.White,
                              (int)Math.Round(tdi.YPositionOfTheBallWhenItArrivesOnRightBatLine-1),
                              (int)Math.Round(tdi.lastBatExpectedYPosition * Config.HeightOfTennisCourtPX ),
                              (int)Math.Round(tdi.YPositionOfTheBallWhenItArrivesOnRightBatLine+1),
                              (int)Math.Round(tdi.lastBatExpectedYPosition * Config.HeightOfTennisCourtPX) );
        }
        
        /// <summary>
        /// Draw two horizontal lines (the one the AI returns, and the one we want the AI should return).
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="tdi"></param>
        private static void DrawGreenAndRedLinesShowingActualAndExpectedForCurrentItemBeingTrained(Graphics graphics, TrainingDataItem tdi)
        {
            if (tdi is null) return;

            // draw a horizontal red line indicating where the AI thinks the bat should be

            using Pen dottedRedLineIndicatingAIpositionOfBat = new(Color.FromArgb(60, 255, 0, 0));
            dottedRedLineIndicatingAIpositionOfBat.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

            graphics.DrawLine(dottedRedLineIndicatingAIpositionOfBat,
                        0,
                        (int)Math.Round(tdi.lastBatPositionYOutputByAI * Config.HeightOfTennisCourtPX),
                        Config.HeightOfTennisCourtPX,
                        (int)Math.Round(tdi.lastBatPositionYOutputByAI * Config.HeightOfTennisCourtPX));

            // draw a horizontal green line indicating where the AI should position the bat (expected position).

            if (tdi.lastBatExpectedYPosition != -1)
            {
                using Pen dottedGreenLineIndicatingPositionBatShouldBe = new(Color.FromArgb(60, 0, 255, 0));
                dottedGreenLineIndicatingPositionBatShouldBe.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                graphics.DrawLine(dottedGreenLineIndicatingPositionBatShouldBe,
                            0,
                            (int)Math.Round(tdi.lastBatExpectedYPosition * Config.HeightOfTennisCourtPX),
                            Config.HeightOfTennisCourtPX,
                            (int)Math.Round(tdi.lastBatExpectedYPosition * Config.HeightOfTennisCourtPX));
            }
        }

        /// <summary>
        /// Draw a vertical line of where it hit the right bat, indicating the one we are fixing.
        /// </summary>
        /// <param name="currentTrainingDataItem"></param>
        /// <param name="graphics"></param>
        private static void DrawGreyLineIndicatingWhereBallHitRightBat(TrainingDataItem currentTrainingDataItem, Graphics graphics)
        {
            if (currentTrainingDataItem is null) return;

            using Pen dottedGreyLineIndicationOfWhereBallHitRightBat = new(Color.FromArgb(40, 255, 255, 255));
            dottedGreyLineIndicationOfWhereBallHitRightBat.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

            graphics.DrawLine(dottedGreyLineIndicationOfWhereBallHitRightBat,
                        (int)Math.Round(currentTrainingDataItem.YPositionOfTheBallWhenItArrivesOnRightBatLine), 0,
                        (int)Math.Round(currentTrainingDataItem.YPositionOfTheBallWhenItArrivesOnRightBatLine), 262f);
        }

        /// <summary>
        /// Horizontal label at top of diagram
        /// </summary>
        /// <param name="graphics"></param>
        private static void LabelXAxis(Graphics graphics)
        {
            using Font f = new("Arial", 7 * Config.c_scale);
            string label = "Y Position - Right Bat";
            SizeF size = graphics.MeasureString(label, f);

            graphics.DrawString(label, f, Brushes.White, new PointF(Config.HeightOfTennisCourtPX / 2 - size.Width / 2, 5));
        }

        /// <summary>
        /// Vertical text on lhs of diagram.
        /// </summary>
        /// <param name="graphics"></param>
        private static void LabelYAxis(Graphics graphics)
        {
            using Font f = new("Arial", 7 * Config.c_scale);

            StringFormat stringFormat = new()
            {
                FormatFlags = StringFormatFlags.DirectionVertical
            };

            string label = "Y Position - Left Bat";
            SizeF size = graphics.MeasureString(label, f);
            graphics.DrawString(label, f, Brushes.White, new PointF(5, Config.HeightOfTennisCourtPX / 2 - size.Width / 2), stringFormat);

        }
    }
}