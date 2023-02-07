using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pong.AI;

/// <summary>
/// Represents a training data item.
/// </summary>
internal class TrainingDataItem
{
    #region STATIC VARIABLES    
    /// <summary>
    /// Represents a snapshot image in double[] for a frame.
    /// </summary>
    static readonly double[] s_emptyFrame = new double[Config.HeightOfTennisCourtPX + Config.WidthOfEyes];

    /// <summary>
    /// Represents 3 snapshot image frames.
    /// </summary>
    static readonly double[] s_emptyVision = new double[Config.HeightOfTennisCourtPX * 3 + 3 * Config.WidthOfEyes];
    #endregion

    #region OBJECT PROPERTIES
    /// <summary>
    /// Epoch the training data item belongs to.
    /// </summary>
    internal int epoch = 0;

    /// <summary>
    /// Frame this training data is on. Starts on 1, increments then the 2nd image is snapshotted.
    /// </summary>
    internal int frame = 0;

    /// <summary>
    /// Increments if/when the ball was returned.
    /// </summary>
    internal int round = 0;

    /// <summary>
    /// Frame 1 ball position image.
    /// </summary>
    internal double[] doubleArrayOf1sAnd0sRepresentingFrame1ImageOfBallHittingBat = Array.Empty<double>();

    /// <summary>
    /// Frame 1 ball position image depth perception.
    /// </summary>
    internal double[] doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame1 = Array.Empty<double>();

    //-------------

    /// <summary>
    /// Frame 2 ball position image.
    /// </summary>
    internal double[] doubleArrayOf1sAnd0sRepresentingFrame2ImageOfBallAfterANumberOfFrames = Array.Empty<double>();

    /// <summary>
    /// Frame 2 ball position image depth perception.
    /// </summary>
    internal double[] doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame2 = Array.Empty<double>();

    //-------------

    /// <summary>
    /// Frame 3 ball position image.
    /// </summary>
    internal double[] doubleArrayOf1sAnd0sRepresentingFrame3ImageOfBallAfterANumberOfFrames = Array.Empty<double>();

    /// <summary>
    /// Frame 3 ball position image depth perception.
    /// </summary>
    internal double[] doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame3 = Array.Empty<double>();

    /// <summary>
    /// This is how far the ball moves horizontally per frame after hitting the bat.
    /// </summary>
    internal float xVelocityOfBallInDirectionOfLeftBat;

    /// <summary>
    /// This is how far the ball moves vertically per frame after hitting the bat.
    /// </summary>
    internal float yVelocityOfBallInDirectionOfLeftBat;

    /// <summary>
    /// Where the bat should have been positioned on the left.
    /// </summary>
    internal double lastBatExpectedYPosition;

    /// <summary>
    /// Where the bat was positioned on the left.
    /// </summary>
    internal double lastBatPositionYOutputByAI;

    internal double lastRightBatPos = -1;

    /// <summary>
    /// The "Y" position of the ball when it arrives on the right.
    /// </summary>
    internal double YPositionOfTheBallWhenItArrivesOnRightBatLine;

    /// <summary>
    /// The "Y" position of the ball when it arrives on the left.
    /// </summary>
    internal double YPositionOfTheBallWhenItArrivesOnLeftBatLine;
    #endregion

    /// <summary>
    /// Returns the training data item as a string.
    /// </summary>
    /// <returns></returns>
    public override string? ToString()
    {
        return $"{epoch},{round},{Math.Round(YPositionOfTheBallWhenItArrivesOnLeftBatLine, 2)},{Math.Round(YPositionOfTheBallWhenItArrivesOnRightBatLine, 2)}," +
               //    0       1                                  2                                                        3
               $"{Math.Round(xVelocityOfBallInDirectionOfLeftBat, 2)},{Math.Round(yVelocityOfBallInDirectionOfLeftBat, 2)}," +
               $"[{string.Join(",", doubleArrayOf1sAnd0sRepresentingFrame1ImageOfBallHittingBat)}],[{string.Join(",", doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame1)}]," +
               $"[{string.Join(",", doubleArrayOf1sAnd0sRepresentingFrame2ImageOfBallAfterANumberOfFrames)}],[{string.Join(",", doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame2)}]," +
               $"[{string.Join(",", doubleArrayOf1sAnd0sRepresentingFrame3ImageOfBallAfterANumberOfFrames)}],[{string.Join(",", doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame3)}]";
        //                         4                                           5             
    }

    /// <summary>
    /// Convert serialised training data back into an object.
    /// </summary>
    /// <returns></returns>
    internal static TrainingDataItem Deserialise(string line)
    {
        // split into {basic-vars} | horiz-frame 1 | vert frame 1 | ... 3
        //                 0                1             2          ...
        line = line.Replace(",[", "|").Replace("]", "");

        string[] parts = line.Split('|');

        // epoch,round,YPositionOfTheBallWhenItArrivesOnLeftBatLine,YPositionOfTheBallWhenItArrivesOnRightBatLine,xVelocityOfBallInDirectionOfLeftBat,yVelocityOfBallInDirectionOfLeftBat
        //   0     1                             2                                         3                                       4                                    5
        string[] tokens = parts[0].Split(",");

        TrainingDataItem item = new()
        {
            epoch = int.Parse(tokens[0]),
            round = int.Parse(tokens[1]),
            YPositionOfTheBallWhenItArrivesOnLeftBatLine = float.Parse(tokens[2]),
            YPositionOfTheBallWhenItArrivesOnRightBatLine = float.Parse(tokens[3]),
            xVelocityOfBallInDirectionOfLeftBat = float.Parse(tokens[4]),
            yVelocityOfBallInDirectionOfLeftBat = float.Parse(tokens[5]),
            doubleArrayOf1sAnd0sRepresentingFrame1ImageOfBallHittingBat = DeserializeFrame(parts[1]),
            doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame1 = DeserializeFrame(parts[2]),
            doubleArrayOf1sAnd0sRepresentingFrame2ImageOfBallAfterANumberOfFrames = DeserializeFrame(parts[3]),
            doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame2 = DeserializeFrame(parts[4]),
            doubleArrayOf1sAnd0sRepresentingFrame3ImageOfBallAfterANumberOfFrames = DeserializeFrame(parts[5]),
            doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame3 = DeserializeFrame(parts[6])
        };

        return item;
    }

    /// <summary>
    /// Pixels are serialised as binary digits e.g. 0,0,0,1,1,0,0,0. We need to return a double[] array (as that's the input to the neural network) 
    /// contain 1d/0d.
    /// </summary>
    /// <param name="lineOfPixels"></param>
    /// <returns></returns>
    private static double[] DeserializeFrame(string lineOfPixels)
    {
        string[] pixelArrayOf1and0Chars = lineOfPixels.Split(",");
        double[] pixels = new double[pixelArrayOf1and0Chars.Length];

        for(int pixel=0;pixel<pixelArrayOf1and0Chars.Length;pixel++)
        {
            if (pixelArrayOf1and0Chars[pixel] == "1") pixels[pixel] = 1; // 0 is default for array initialiser, we only need to set 1's
        }

        return pixels;
    }

    /// <summary>
    /// Returns the training data item as a string.
    /// </summary>
    /// <returns></returns>
    public string? ToString2()
    {
        //string.Join(",",EyesHit)
        return $"{Math.Round(YPositionOfTheBallWhenItArrivesOnLeftBatLine, 2)},{Math.Round(YPositionOfTheBallWhenItArrivesOnRightBatLine, 2)},{Math.Round(xVelocityOfBallInDirectionOfLeftBat, 2)},{Math.Round(yVelocityOfBallInDirectionOfLeftBat, 2)}";
        //                                   0                                                                 1                                      2                       3                                             4                     5             
    }

    /// <summary>
    /// Creates an array of the training data, used for training.
    /// </summary>
    /// <returns></returns>
    internal double[] ToArray()
    {
        // no image taken yet, returns double[] containing zeros
        if (doubleArrayOf1sAnd0sRepresentingFrame1ImageOfBallHittingBat is null) return s_emptyVision;

        // 1st frame image added to neural network input
        List<double> inputs = new(doubleArrayOf1sAnd0sRepresentingFrame1ImageOfBallHittingBat);
        inputs.AddRange(doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame1);

        // 2nd frame image added to neural network input
        if (doubleArrayOf1sAnd0sRepresentingFrame2ImageOfBallAfterANumberOfFrames is null)
        {
            inputs.AddRange(s_emptyFrame);
        }
        else
        {
            inputs.AddRange(doubleArrayOf1sAnd0sRepresentingFrame2ImageOfBallAfterANumberOfFrames);
            inputs.AddRange(doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame2);
        }

        // 3rd frame image added to neural network input
        if (doubleArrayOf1sAnd0sRepresentingFrame3ImageOfBallAfterANumberOfFrames is null)
        {
            inputs.AddRange(s_emptyFrame);
        }
        else
        {
            inputs.AddRange(doubleArrayOf1sAnd0sRepresentingFrame3ImageOfBallAfterANumberOfFrames);
            inputs.AddRange(doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame3);
        }

        return inputs.ToArray();
    }
}