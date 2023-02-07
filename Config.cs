using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pong
{
    internal static class Config
    {
        /// <summary>
        /// Determines how big everything is
        /// </summary>
        internal const float c_scale = 0.5f;

        /// <summary>
        /// Define width of the court.
        /// </summary>
        internal static int WidthOfTennisCourtPX = 430;

        /// <summary>
        /// Define height of the court
        /// </summary>
        internal static int HeightOfTennisCourtPX = 262;

        /// <summary>
        /// Define the size of the ball relative to court.
        /// </summary>
        internal static int BallRadiusPX = 2;

        /// <summary>
        /// How far the bat is from the edge.
        /// </summary>
        internal static int DistanceOfBatFromEdgeXpx = 30;

        /// <summary>
        /// Length of bat is double this. We draw up this much, plus down this much.
        /// </summary>
        internal static int HalfTheBatLength = 16;

        /// <summary>
        /// Define the width of the bat.
        /// </summary>
        internal static int WidthOfBat = 6;

        // Each digit is drawn using squares of this size, one per "#".
        // Overall image size = (width) 3 x pixelSize by (height) 5 x pixelSize
        internal static int ScoreBoardPixelSize = 3;

        /// <summary>
        /// How many pixels do we want to examine to take an image of the ball.
        /// </summary>
        internal static int WidthOfEyes = 150;

        /// <summary>
        /// As we change, we are going to expect more and more images to map to a left-bat pos.
        /// There is going to be some error in that. To be 100% accurate = 0.00382 (1/262).
        /// We generally go for approximate for faster learning.
        /// </summary>
        internal static float MaxAllowedDiffInAIVsExpected = 1 / (HeightOfTennisCourtPX * c_scale * 2f); // 0.5 rounded

        /// <summary>
        /// We take 3 images. The first after hitting the bat and ricocheting.
        /// From these images one can learn to infer/associate the destination of the ball.
        /// </summary>
        internal static int FrameAtWhichWeTake1stImage = 1;

        /// <summary>
        /// We take 3 images. One when it hits the bat, the 2nd after "n" frames. This is "n".
        /// From these images one can learn to infer/associate the destination of the ball.
        /// </summary>
        internal static int FrameAtWhichWeTake2ndImage = FrameAtWhichWeTake1stImage + 10;

        /// <summary>
        /// We take 3 images. One when it hits the bat, the 3nd after "n" more frames. This is "n".
        /// From these images one can learn to infer/associate the destination of the ball.
        /// </summary>
        internal static int FrameAtWhichWeTake3rdImage = FrameAtWhichWeTake2ndImage + 10;

        /// <summary>
        /// How much zone the bat cannot reach (ball placed here wins game).
        /// </summary>
        internal static int DeadZone = 8;

        /// <summary>
        /// Apply scaling so everything is relative in size.
        /// </summary>
        internal static void ApplyScaling()
        {
            WidthOfTennisCourtPX = (int)Math.Round(WidthOfTennisCourtPX * c_scale);
            HeightOfTennisCourtPX = (int)Math.Round(HeightOfTennisCourtPX * c_scale);
            BallRadiusPX = (int)Math.Round(BallRadiusPX * c_scale);
            DistanceOfBatFromEdgeXpx = (int)Math.Round(DistanceOfBatFromEdgeXpx * c_scale);
            HalfTheBatLength = (int)Math.Round(HalfTheBatLength * c_scale);
            WidthOfBat = (int)Math.Round(WidthOfBat * c_scale);
            ScoreBoardPixelSize = (int)Math.Round(ScoreBoardPixelSize * c_scale);
            WidthOfEyes = (int)Math.Round(WidthOfEyes * c_scale);
            FrameAtWhichWeTake2ndImage = (int)Math.Round(FrameAtWhichWeTake2ndImage * c_scale);
            FrameAtWhichWeTake3rdImage = (int)Math.Round(FrameAtWhichWeTake3rdImage * c_scale);
            DeadZone = (int)Math.Round(DeadZone * c_scale);

            MaxAllowedDiffInAIVsExpected = 0.03f;
        }
    }
}