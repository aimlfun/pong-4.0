using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pong.AI
{
    internal static class Trainer
    {
        #region CONSTANTS
        /// <summary>
        /// Where we write / expect the training data.
        /// </summary>
        private const string c_trainingDataFilePath = @"d:\temp\pong\pong-4.txt";
        
        /// <summary>
        /// This is written to the training data file as the "header".
        /// </summary>
        private const string c_trainingDataHeader = "epoch,round,YPositionOfTheBallWhenItArrivesOnLeftBatLine,YPositionOfTheBallWhenItArrivesOnRightBatLine,xVelocityOfBallInDirectionOfLeftBat,yVelocityOfBallInDirectionOfLeftBat,frame1h,frame1v,frame2h,frame2v,frame3h,frame3v";
        #endregion

        #region STATIC VARIABLES
        /// <summary>
        /// List of training items recorded (or loaded and added to).
        /// </summary>
        private static readonly List<TrainingDataItem> s_trainingData = new();

        /// <summary>
        /// Stores a mapping of training data items by the "image" of it.
        /// </summary>
        private static readonly Dictionary<string, TrainingDataItem> s_uniqueTrainingDataCapture = new();
        #endregion

        /// <summary>
        /// The current training item (assigned when the ball reaches rhs).
        /// </summary>
        internal static TrainingDataItem? s_currentTrainingDataItem = null;

        /// <summary>
        /// Returns the number of items.
        /// </summary>
        internal static int Count
        {
            get { return s_trainingData.Count; }
        }

        /// <summary>
        /// Returns the training data.
        /// </summary>
        internal static List<TrainingDataItem> TrainingData
        {
            get
            {
                return s_trainingData;
            }
        }

        /// <summary>
        /// Stores the training data into a list, and then saves to file.
        /// </summary>
        internal static void AddToTrainingData(TrainingDataItem? item, bool save = true)
        {
            if (item is null) return;

            string? itemText = item.ToString2();
            if (string.IsNullOrEmpty(itemText)) return;

            string hash = GetHashFromCapturedFrameImages(item);

            DebugLog.Log($"THIS: {item}");

            if (s_uniqueTrainingDataCapture.ContainsKey(hash))
            {
                DebugLog.Log($"PREVIOUS: {s_uniqueTrainingDataCapture[hash]}");
           
                // nothing to learn: we've encountered a position we learnt previously, and have training data for
                if (Math.Round(s_uniqueTrainingDataCapture[hash].YPositionOfTheBallWhenItArrivesOnLeftBatLine, 2) == Math.Round(item.YPositionOfTheBallWhenItArrivesOnLeftBatLine, 2) &&
                    Math.Round(s_uniqueTrainingDataCapture[hash].YPositionOfTheBallWhenItArrivesOnRightBatLine, 2) == Math.Round(item.YPositionOfTheBallWhenItArrivesOnRightBatLine, 2))
                {
                    DebugLog.Log("ALREADY IN TRAINING DATA");
                    return;
                }

                DebugLog.Log("TWO ITEMS HAVE SAME HASH");

                // if (1) xvel=-2.53 and (2) xvel=-2.49, then after 10 frames=> 2.53 / 24.9; both rounded = 25.
                // we'd need a lot of pixels in frame.
                // If they nearly arrive at same left pos, simply average. I know if a 3rd arrives, average will be (((#1+#2)/2)+#3)/2. Not great averaging an average.
                if (Math.Abs(item.YPositionOfTheBallWhenItArrivesOnLeftBatLine - s_uniqueTrainingDataCapture[hash].YPositionOfTheBallWhenItArrivesOnLeftBatLine) < 4)
                {
                    s_uniqueTrainingDataCapture[hash].YPositionOfTheBallWhenItArrivesOnLeftBatLine = (s_uniqueTrainingDataCapture[hash].YPositionOfTheBallWhenItArrivesOnLeftBatLine +
                                                                                                      item.YPositionOfTheBallWhenItArrivesOnLeftBatLine) / 2;
                    DebugLog.Log("ALREADY IN TRAINING DATA: SIMILAR YPOS");
                    return;
                }

                DebugLog.Log("WARNING: IN TRAINING DATA - TOO DIFFERENT");
                Debugger.Break();

                // if we go past this point, the hash will cause an error (already in dictionary).
                return;
            }

            s_uniqueTrainingDataCapture.Add(hash, item);

            // expand training data 
            s_trainingData.Add(item);
        }

        /// <summary>
        /// Saves the training data.
        /// </summary>
        internal static void Save()
        { 
            File.WriteAllText(c_trainingDataFilePath, c_trainingDataHeader + "\n" +
                                                       string.Join("\n", s_trainingData));
        }

        /// <summary>
        /// Comparing all the pixels 3x(height+width) is inefficient. If we turn to a string and sha2, we can detect
        /// identical but with a small-ish string.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static string GetHashFromCapturedFrameImages(TrainingDataItem? item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item), "item not initialised, it should contain the frame1/2 images");

            string text = string.Join("", item.doubleArrayOf1sAnd0sRepresentingFrame1ImageOfBallHittingBat) +
                          string.Join("", item.doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame1) +
                          string.Join("", item.doubleArrayOf1sAnd0sRepresentingFrame2ImageOfBallAfterANumberOfFrames) +
                          string.Join("", item.doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame2) +
                          string.Join("", item.doubleArrayOf1sAnd0sRepresentingFrame3ImageOfBallAfterANumberOfFrames) +
                          string.Join("", item.doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame3);

            return StringSha256Hash(text);
        }


        /// <summary>
        /// Computes the SHA2 hash of the string.
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        static string StringSha256Hash(string rawData)
        {
            using SHA256 sha256Hash = SHA256.Create();

            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        /// <summary>
        /// Loads the training file.
        /// </summary>
        internal static bool Load()
        {
            // the training data is optional. If not present there is nothing to load.
            if (!File.Exists(c_trainingDataFilePath)) return false;

            string[] lines = File.ReadAllLines(c_trainingDataFilePath);

            foreach (string line in lines)
            {
                if (line.Contains(c_trainingDataHeader)) continue;

                AddToTrainingData(TrainingDataItem.Deserialise(line), false);
            }

            return true;
        }

        /// <summary>
        /// Pushes all training data to confirm that the AI NN output matches expected output for everyone of them.
        /// </summary>
        /// <returns></returns>
        internal static void DetermineWhichItemsRequireMoreTraining(NeuralNetwork neuralNetworkControllingLeftBat, List<int> toTest, out List<int> requireTraining)
        {
            requireTraining = new ();

            // Initially we have to test ALL the data points and compile a list of ones that are inaccurate.
            // all the ones that are inaccurate we "retrain".
            // Now you might think "we're done", after all you fixed the inaccurate ones. But nope.
            // In training, the inaccurate ones become inaccurate but it has an affect on others, so
            // once "trained" we repeat the first step. If all are accurate in the first step, we're done.

            if (toTest is null || toTest.Count == 0)
            {
                for (int i = 0; i < Trainer.TrainingData.Count; i++)
                {
                    TrainingDataItem tdi = Trainer.TrainingData[i];

                    // these are stored for the sake of the visualiser, not learning. It plots them.
                    tdi.lastBatPositionYOutputByAI = neuralNetworkControllingLeftBat.FeedForward(tdi.ToArray())[0];
                    tdi.lastBatExpectedYPosition = tdi.YPositionOfTheBallWhenItArrivesOnLeftBatLine / Config.HeightOfTennisCourtPX;

                    if (Math.Abs(tdi.lastBatPositionYOutputByAI - tdi.lastBatExpectedYPosition) > Config.MaxAllowedDiffInAIVsExpected)
                    {
                        requireTraining.Add(i);
                    }
                }
            }
            else
            {
                foreach (int i in toTest)
                {
                    TrainingDataItem tdi = Trainer.TrainingData[i];

                    // these are stored for the sake of the visualiser, not learning. It plots them.
                    tdi.lastBatPositionYOutputByAI = neuralNetworkControllingLeftBat.FeedForward(tdi.ToArray())[0];
                    tdi.lastBatExpectedYPosition = tdi.YPositionOfTheBallWhenItArrivesOnLeftBatLine / Config.HeightOfTennisCourtPX;

                    if (Math.Abs(tdi.lastBatPositionYOutputByAI - tdi.lastBatExpectedYPosition) > Config.MaxAllowedDiffInAIVsExpected)
                    {
                        requireTraining.Add(i);
                    }
                }
            }
        }

    }
}