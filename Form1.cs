using Pong.AI;
using Pong.GameArea;
using Pong.NewFolder;
using Pong.Utils;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Pong;

/// <summary>
/// Pong form, using images to move the bat.
/// </summary>
public partial class FormPong : Form
{
    #region CONSTANTS
    /// <summary>
    /// true - it will draw lines showing the "dead" zone and the "goal" line.
    /// </summary>
    private const bool c_drawDebugLines = false;

    /// <summary>
    /// When items reach this, we ensure it has trained everything.
    /// </summary>
    private const int c_numberOfEpochsBeforeForcedTraining = 50;

    /// <summary>
    /// Where the .ai model is saved.
    /// </summary>
    private const string c_pongAIfile = @"d:\temp\pong4-0.ai";
    #endregion

    #region STATIC VARIABLES
    /// <summary>
    /// Pen for drawing the net (dashed line).
    /// </summary>
    private static readonly Pen s_penForNet = new(Color.FromArgb(90, 255, 255, 255), 1);

    /// <summary>
    /// Pen for drawing the cursor, which is represented as a red square.
    /// </summary>
    private static readonly Pen s_penCursor = new(Color.FromArgb(100, 255, 0, 0));
    #endregion

    /// <summary>
    /// We could snapshot the whole tennis court, and use that, but AI would need to learn to ignore the numbers and bats.
    /// So we are selective in when we snapshot (before numbers), and what area (excludes bat).
    /// This is used for both images (ball-hits-bat + ball "n" frames after hitting bat).
    /// </summary>
    Bitmap snapShotImageTakenOfTheTennisCourtContainingTheBall;

    /// <summary>
    /// Used to ensure that we ignore timer ticks if we are busy in an existing timer.
    /// </summary>
    private static bool inTimer = false;

    /// <summary>
    /// Contains approximate list of points the ball visited.
    /// We don't keep all the points, as it is *meant* to travel in a straight line when it is not
    /// bouncing off a wall.
    /// </summary>
    private readonly List<PointF> pathOfBall = new();

    /// <summary>
    /// Training generation.
    /// </summary>
    private int epoch = 0;

    /// <summary>
    /// >0 indicates how many times the ball has been returned. Each round it speeds up.
    /// </summary>
    private int round = 0;

    /// <summary>
    /// Left "bat".
    /// </summary>
    private readonly Bat batOnTheLeftControlledByAI;

    /// <summary>
    /// Right "bat".
    /// </summary>
    private readonly Bat batOnTheRightControlledByHumanOrTrainer;

    /// <summary>
    /// Implements a 2 player scoreboard.
    /// </summary>
    private readonly ScoreBoard scoreBoard = new();

    /// <summary>
    /// The target position for the right hand bat.
    /// </summary>
    private int yTarget;

    /// <summary>
    /// A "ball" for this game.
    /// </summary>
    private Ball ball;

    /// <summary>
    /// Where the cursor is (mouse move) or where the auto mode positions it.
    /// </summary>
    private Point cursorPosition = new();

    /// <summary>
    /// The neural network controlling the bat.
    /// </summary>
    private readonly NeuralNetwork neuralNetworkControllingLeftBat;

    /// <summary>
    /// In auto-mode, it's playing against itself. The bat on the right is a "ball-tracker" not AI.
    /// </summary>
    private bool inAutoMode = true;

    /// <summary>
    /// Perpendicular hits are a bit of a waste of time, so we mostly let it choose to hit with the outer edges.
    /// </summary>
    private int offsetChosenByTrainerToStopPerpendicularHits = 0;

    /// <summary>
    /// A debug / sanity check to ensure the ball doesn't ricochet within the bat region.
    /// </summary>
    private bool expectArrivalLeft = false;

    /// <summary>
    /// Primary display (tennis court)
    /// </summary>
    private PictureBox pictureBoxDisplay;

    /// <summary>
    /// The visualiser showing the error of predicted vs. actual.
    /// </summary>
    private PictureBox pictureBoxLearningError;

    /// <summary>
    /// Image of all frames overlaid.
    /// </summary>
    private PictureBox pictureBoxFrameAllFrames;

    /// <summary>
    /// Show lines per training data item to give a visual of coverage.
    /// </summary>
    private PictureBox pictureBoxCoverage;

    /// <summary>
    /// Used for the debug output.
    /// </summary>
    private PictureBox pictureBoxLog;

    /// <summary>
    /// Used to track which items need training.
    /// </summary>
    List<TrainingDataItem> itemsThatNeedTraining = new();

    /// <summary>
    /// Constructor.
    /// </summary>
#pragma warning disable CS8618 // SPURIOUS WARNING. NewBall() populates it.
    public FormPong()
#pragma warning restore CS8618 // SPURIOUS WARNING. NewBall() populates it.
    {
        InitializeComponent();

        Config.ApplyScaling();

        batOnTheLeftControlledByAI = new(Side.Left, Config.WidthOfTennisCourtPX, Config.HeightOfTennisCourtPX);
        batOnTheRightControlledByHumanOrTrainer = new(Side.Right, Config.WidthOfTennisCourtPX, Config.HeightOfTennisCourtPX);

        yTarget = batOnTheRightControlledByHumanOrTrainer.Y;

        CreateNewBall();

        s_penForNet.DashPattern = new[] { 5f, 5f };

        //neuralNetworkControllingLeftBat = new(new int[] { 3 * (Config.HeightOfTennisCourtPX + Config.WidthOfEyes), 32, 1 });
        neuralNetworkControllingLeftBat = new(new int[] { 3 * (Config.HeightOfTennisCourtPX + Config.WidthOfEyes), 262, 1 });

        if (neuralNetworkControllingLeftBat.Load(c_pongAIfile)) MessageBox.Show("Loaded"); // load AI model if available

        Trainer.Load();

        // start where we left off...
        if (Trainer.TrainingData.Count > 0) epoch = Trainer.TrainingData[^1].epoch + 1;
    }

    /// <summary>
    /// Adds the pictureboxes for tennis court, learning error and frames.
    /// </summary>
    private void AddFormImages()
    {
        pictureBoxDisplay = new()
        {
            Location = new Point(3, 5),
            BackColor = Color.Black,
            Name = "pictureBoxDisplay",
            Size = new Size(Config.WidthOfTennisCourtPX, Config.HeightOfTennisCourtPX),
            TabStop = false
        };
        pictureBoxDisplay.MouseMove += new System.Windows.Forms.MouseEventHandler(PictureBoxDisplay_MouseMove);

        pictureBoxLearningError = new()
        {
            BackColor = Color.Black,
            Name = "pictureBoxLearningError",
            Location = new Point(pictureBoxDisplay.ClientSize.Width + pictureBoxDisplay.Left + 5, 5),

            Size = new Size(Config.HeightOfTennisCourtPX, Config.HeightOfTennisCourtPX), // plotting Y vs. Y (not X).
            TabStop = false
        };

        pictureBoxFrameAllFrames = new()
        {
            BackColor = Color.Black,
            Name = "pictureBoxFrame1",
            Size = new Size(Config.WidthOfEyes, Config.HeightOfTennisCourtPX),
            Location = new Point(pictureBoxLearningError.ClientSize.Width + pictureBoxLearningError.Left + 5, 5),
            TabStop = false
        };

        pictureBoxCoverage = new()
        {
            Location = new Point(3, pictureBoxDisplay.ClientSize.Height + 10),
            BackColor = Color.Black,
            Name = "pictureBoxCoverage",
            Size = new Size(Config.WidthOfTennisCourtPX, Config.HeightOfTennisCourtPX),
            TabStop = false
        };

        pictureBoxLog = new()
        {
            Location = new Point(pictureBoxDisplay.ClientSize.Width + pictureBoxDisplay.Left + 5, pictureBoxDisplay.ClientSize.Height + 10),
            BackColor = Color.Black,
            Name = "pictureBoxLog",
            Size = new Size(Config.WidthOfTennisCourtPX - 5, Config.HeightOfTennisCourtPX),
            TabStop = false
        };

        Controls.Clear();
        Controls.Add(pictureBoxFrameAllFrames);
        Controls.Add(pictureBoxLearningError);
        Controls.Add(pictureBoxDisplay);
        Controls.Add(pictureBoxCoverage);
        Controls.Add(pictureBoxLog);

        // we are required to "size" the form based on the contents.
        Width = pictureBoxFrameAllFrames.ClientSize.Width + pictureBoxFrameAllFrames.Location.X + 10;
        Height = pictureBoxCoverage.Height + pictureBoxCoverage.Location.Y + 5;

        DebugLog.Initialise(pictureBoxLog); // so it knows where to "draw" debug.
    }

    /// <summary>
    /// Repeatedly back-propagates until it accurately returns the bat position for all known training images.
    /// </summary>
    private void TrainUntilTheBatIsAccurateForAllKnownTrainingImages()
    {
        if (Trainer.s_currentTrainingDataItem is null) return; // nothing to train

        if (Trainer.Count % c_numberOfEpochsBeforeForcedTraining != 0) return; // train every "block" of new training items

        timer1.Stop(); // stops it moving the ball during the training!

        Train();

        timer1.Start(); // ensures the ball moves afterwards
    }

    /// <summary>
    /// Trains the neural network.
    /// </summary>
    private void Train()
    {
        DebugLog.Log("TRAINING INITIATED");
        ShowTRAININGtext();

        bool trained;

        int iterations = -1;

        if (Trainer.s_currentTrainingDataItem is not null)
        {
            Trainer.s_currentTrainingDataItem.lastBatPositionYOutputByAI = neuralNetworkControllingLeftBat.FeedForward(Trainer.s_currentTrainingDataItem.ToArray())[0];
            Trainer.s_currentTrainingDataItem.lastBatExpectedYPosition = Trainer.s_currentTrainingDataItem.YPositionOfTheBallWhenItArrivesOnLeftBatLine / Config.HeightOfTennisCourtPX;
        }

        int batY = batOnTheLeftControlledByAI.Y;

        List<int> toTest = new();

        int numberOfBackPropagationsApplied = 0;

        bool shortcut = true;

        do
        {
            ++iterations;

            if (!shortcut || iterations == 0 || toTest.Count == 0)
            {
                DebugLog.Log($"{iterations}. Back propagate {Trainer.TrainingData.Count} items");

                // first time
                for (int i = 0; i < Trainer.TrainingData.Count; i++)
                {
                    TrainItem(i);
                }

                numberOfBackPropagationsApplied += Trainer.TrainingData.Count;
                toTest.Clear(); // shortcut requires
            }
            else
            {
                DebugLog.Log($"{iterations}. Back propagate {toTest.Count} items");

                foreach (int i in toTest)
                {
                    TrainItem(i);
                }

                numberOfBackPropagationsApplied += toTest.Count;
            }

            Trainer.DetermineWhichItemsRequireMoreTraining(neuralNetworkControllingLeftBat, toTest, out List<int> requireTraining);

            trained = (toTest is not null && toTest.Count == 0 && requireTraining.Count == 0);

            toTest = requireTraining; // ones that failed

            // we don't draw the bat, but we move it, so it's in the right place to return the ball.
            if (Trainer.s_currentTrainingDataItem is not null)
            {
                Trainer.s_currentTrainingDataItem.lastBatPositionYOutputByAI = neuralNetworkControllingLeftBat.FeedForward(Trainer.s_currentTrainingDataItem.ToArray())[0];
                batOnTheLeftControlledByAI.Move((int)Math.Round(Trainer.s_currentTrainingDataItem.lastBatPositionYOutputByAI * Config.HeightOfTennisCourtPX));

                VisuallyShowLearningError(); // speeds things up

                if (batY != batOnTheLeftControlledByAI.Y)
                {
                    UpdateVideoDisplay();
                    ShowTRAININGtext();
                    batY = batOnTheLeftControlledByAI.Y;
                }
            }
            else
            {
                VisuallyShowLearningError();
            }
        } while (!trained);

        VisuallyShowLearningError();
        itemsThatNeedTraining.Clear();

        if (Trainer.s_currentTrainingDataItem is not null)
            batOnTheLeftControlledByAI.Move((int)Math.Round((neuralNetworkControllingLeftBat.FeedForward(Trainer.s_currentTrainingDataItem.ToArray())[0]) * Config.HeightOfTennisCourtPX));

        DebugLog.Log($"{epoch} MISSED left bat: training took {iterations} epoch(s)");
        DebugLog.Log($"{epoch} MISSED left bat: Back propagations applied: {numberOfBackPropagationsApplied}");
    }

    /// <summary>
    /// Trains a specific data item.
    /// </summary>
    /// <param name="i"></param>
    private void TrainItem(int i)
    {
        TrainingDataItem tdi = Trainer.TrainingData[i];
        neuralNetworkControllingLeftBat.BackPropagate(tdi.ToArray(),
                                                      new double[] { tdi.YPositionOfTheBallWhenItArrivesOnLeftBatLine / Config.HeightOfTennisCourtPX });
    }

    /// <summary>
    /// Writes "TRAINING" on the tennis court, so user knows what it's doing.
    /// </summary>
    private void ShowTRAININGtext()
    {
        if (pictureBoxDisplay.Image is null) return;

        Bitmap b = new(pictureBoxDisplay.Image);
        using Graphics graphics = Graphics.FromImage(b);
        using Font f = new("Courier New", 12 * Config.c_scale);
        string label = "T R A I N I N G";
        SizeF size = graphics.MeasureString(label, f);

        graphics.DrawString(label, f, Brushes.Yellow, new PointF(Config.WidthOfTennisCourtPX / 2 - size.Width / 2, Config.HeightOfTennisCourtPX / 2 - size.Height));

        pictureBoxDisplay.Image?.Dispose();
        pictureBoxDisplay.Image = b;
    }

    /// <summary>
    /// Draw a diagram showing which training points it is inaccurate at.
    /// Doing this slows the training. It doesn't need to inform you of how good/bad it's learning.
    /// But this kind of stuff makes the app more fun, and it's part of my aimlfun.com, so you've come to expect it.
    /// </summary>
    private void VisuallyShowLearningError()
    {
        // TODO: OPTIMISE FLAG. DO THIS ONCE WHEN IT IS ENABLED=FALSE, AND DON'T DO IT UNTIL SET TRUE
        // OR CACHE THE IMAGE.
        pictureBoxLearningError.Image?.Dispose(); // discard before replacing

        //if (Trainer.s_currentTrainingDataItem is not null)
        pictureBoxLearningError.Image = LearningVisualiser.GetDiagram(Trainer.s_currentTrainingDataItem, Config.MaxAllowedDiffInAIVsExpected, out itemsThatNeedTraining);


        Application.DoEvents();
    }

    /// <summary>
    /// Creates a new ball.
    /// </summary>
    private void CreateNewBall()
    {
        ++epoch;

        round = 0;
        ball = new Ball(BallDirection.LeftToRight);

        // events for when the ball reaches either bat
        ball.BallReachedBatLeft += Ball_BallReachedBatLeft;
        ball.BallReachedBatRight += Ball_BallReachedBatRight;

        SetOffsetOfTrainer(); // picks a random offset so bat doesn't return perpendicular balls

        Text = $"Pong 4.0 - Epoch {epoch}";

        expectArrivalLeft = false;
    }

    /// <summary>
    /// For each returned or new ball, the trainer picks an offset.
    /// This is necessary because the "trainer" bat will always be centred on the ball. Without
    /// offset, it will not travel at angles just flat.
    /// </summary>
    private void SetOffsetOfTrainer()
    {
        int delta = RandomNumberGenerator.GetInt32(-Config.HalfTheBatLength, Config.HalfTheBatLength);

        offsetChosenByTrainerToStopPerpendicularHits = delta;
    }

    /// <summary>
    /// Frame by frame animation, using a timer.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Timer1_Tick(object sender, EventArgs e)
    {
        if (inTimer) return; // skip this tick, we're busy.

        inTimer = true; // block timer ticks.

        try
        {
            // a training item is created upon hitting right bat
            if (Trainer.s_currentTrainingDataItem != null) Trainer.s_currentTrainingDataItem.frame++;

            MoveBallAndBothBats();

            UpdateVideoDisplay();

            // we do this "after" update video display, because it provides the image to grab.
            if (Trainer.s_currentTrainingDataItem != null) GrabFrameImage();

            // train every frame!
            ImproveByTrainingAsTheBallMoves(); // as the training points increase, performance will suffer
        }
        finally
        {
            inTimer = false;
        }
    }

    /// <summary>
    /// Scrapes the "frame" from the tennis court image. It merges the 3 frames into a single image.
    /// </summary>
    private void GrabFrameImage()
    {
        // snapshot should be taken
        if (Trainer.s_currentTrainingDataItem is null || snapShotImageTakenOfTheTennisCourtContainingTheBall is null)
        {
            Debugger.Break();
            return;
        }

        if (Trainer.s_currentTrainingDataItem.frame != Config.FrameAtWhichWeTake1stImage &&
            Trainer.s_currentTrainingDataItem.frame != Config.FrameAtWhichWeTake2ndImage &&
            Trainer.s_currentTrainingDataItem.frame != Config.FrameAtWhichWeTake3rdImage) return;

        double[] horizPixelsFlattened = AIviewFromAboveAndDecideResponseAsReducedPixels(out double[] vertPix);

        if (Trainer.s_currentTrainingDataItem.frame == Config.FrameAtWhichWeTake1stImage)
        {
            GrabFrame1(horizPixelsFlattened, vertPix);
            return;
        }

        Bitmap b = CreateNewBitmapByMergingTheseBitmaps((Bitmap)pictureBoxFrameAllFrames.Image, new(snapShotImageTakenOfTheTennisCourtContainingTheBall));
        pictureBoxFrameAllFrames.Image?.Dispose();
        pictureBoxFrameAllFrames.Image = b;

        if (Trainer.s_currentTrainingDataItem.frame == Config.FrameAtWhichWeTake2ndImage)
        {
            GrabFrame2(horizPixelsFlattened, vertPix);
            return;
        }

        if (Trainer.s_currentTrainingDataItem.frame == Config.FrameAtWhichWeTake3rdImage)
        {
            GrabFrame3(horizPixelsFlattened, vertPix);
        }
    }

    /// <summary>
    /// Stores frame 3, and updates the visualiser.
    /// </summary>
    /// <param name="horizPixelsFlattened"></param>
    /// <param name="vertPix"></param>
    private void GrabFrame3(double[] horizPixelsFlattened, double[] vertPix)
    {
        Trainer.s_currentTrainingDataItem.doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame3 = vertPix;
        Trainer.s_currentTrainingDataItem.doubleArrayOf1sAnd0sRepresentingFrame3ImageOfBallAfterANumberOfFrames = horizPixelsFlattened;

        Trainer.s_currentTrainingDataItem.lastBatPositionYOutputByAI = neuralNetworkControllingLeftBat.FeedForward(Trainer.s_currentTrainingDataItem.ToArray())[0];
        Trainer.s_currentTrainingDataItem.lastBatExpectedYPosition = -1;

        VisuallyShowLearningError();
        snapShotImageTakenOfTheTennisCourtContainingTheBall = null;
    }

    /// <summary>
    /// Stores frame 2.
    /// </summary>
    /// <param name="horizPixelsFlattened"></param>
    /// <param name="vertPix"></param>
    private static void GrabFrame2(double[] horizPixelsFlattened, double[] vertPix)
    {
        Trainer.s_currentTrainingDataItem.doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame2 = vertPix;
        Trainer.s_currentTrainingDataItem.doubleArrayOf1sAnd0sRepresentingFrame2ImageOfBallAfterANumberOfFrames = horizPixelsFlattened;
    }

    /// <summary>
    /// Stores frame 1.
    /// </summary>
    /// <param name="horizPixelsFlattened"></param>
    /// <param name="vertPix"></param>
    private void GrabFrame1(double[] horizPixelsFlattened, double[] vertPix)
    {
        pictureBoxFrameAllFrames.Image?.Dispose();
        pictureBoxFrameAllFrames.Image = new Bitmap(snapShotImageTakenOfTheTennisCourtContainingTheBall);

        Trainer.s_currentTrainingDataItem.doubleArrayOf1sAnd0sRepresentingFrame1ImageOfBallHittingBat = horizPixelsFlattened;
        Trainer.s_currentTrainingDataItem.doubleArrayOf1sAnd0sRepresentingVerticallWherePixelsAreForFrame1 = vertPix;
    }

    /// <summary>
    /// Creates an image by overlay image 1 with image 2. It works for this app because
    /// the background is black on the images. We simply make the 2nd image transparent
    /// and copy on top.
    /// </summary>
    /// <param name="image1"></param>
    /// <param name="image2"></param>
    /// <returns></returns>
    private static Bitmap CreateNewBitmapByMergingTheseBitmaps(Bitmap image1, Bitmap image2)
    {
        using Bitmap imageToOverlayOnTop = new(image2);
        imageToOverlayOnTop.MakeTransparent(Color.Black);

        Bitmap newImage = new(image1);
        using Graphics g = Graphics.FromImage(newImage);
        g.DrawImageUnscaled(imageToOverlayOnTop, 0, 0);
        g.Flush();

        return newImage;
    }

    /// <summary>
    /// Makes an empty display, draws the net, 2xbats + ball + score
    /// </summary>
    private void UpdateVideoDisplay()
    {
        Bitmap b = new(Config.WidthOfTennisCourtPX, Config.HeightOfTennisCourtPX);

        using Graphics g = Graphics.FromImage(b);
        g.Clear(Color.Black);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

        // draw net (goes vertical)
        g.DrawLine(s_penForNet, Config.WidthOfTennisCourtPX / 2, 0, Config.WidthOfTennisCourtPX / 2, Config.HeightOfTennisCourtPX);

        // draw a square where the cursor is horizontally
        if (!inAutoMode) g.DrawRectangle(s_penCursor, batOnTheRightControlledByHumanOrTrainer.X - 3, cursorPosition.Y - 3, 6, 6);

        // draw both bats + ball
        batOnTheLeftControlledByAI.Draw(g);
        batOnTheRightControlledByHumanOrTrainer.Draw(g);

        StoreBallPath();

        // ball gets drawn, then moved. This leads to a more Pong like visual plus a faster ball.
        ball.Draw(g);
        g.Flush();

        if (ball.X > Config.WidthOfTennisCourtPX - Config.DistanceOfBatFromEdgeXpx - Config.WidthOfEyes - 5)
        {
            TakeSnapshotOfBallNearRightBat(b);
        }

        // draw where the bat "was" when the ball struck - because straight after it will move tracking the return ball.
        if (Trainer.s_currentTrainingDataItem != null)
        {
            batOnTheRightControlledByHumanOrTrainer.DrawDottedOutline(g, (float)Trainer.s_currentTrainingDataItem.lastRightBatPos);

            if (pathOfBall.Count > 1)
            {
                List<PointF> p = new(pathOfBall)
                {
                    new PointF(ball.X, ball.Y)
                };

                using Pen penPath = new(Color.FromArgb(80, 0, 255, 0));
                penPath.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawLines(penPath, p.ToArray());
            }
        }

        scoreBoard.Draw(g, Config.WidthOfTennisCourtPX);

        if (c_drawDebugLines)
        {
            DrawDebugLines(g);
        }

        pictureBoxDisplay.Image?.Dispose();
        pictureBoxDisplay.Image = b;
    }

    /// <summary>
    /// Paint the part of the image containing the ball onto a smaller image used for the AI to see the ball.
    /// </summary>
    /// <param name="b"></param>
    private void TakeSnapshotOfBallNearRightBat(Bitmap b)
    {
        snapShotImageTakenOfTheTennisCourtContainingTheBall = new Bitmap(Config.WidthOfEyes, Config.HeightOfTennisCourtPX);
        Graphics graphicsEyesRight = Graphics.FromImage(snapShotImageTakenOfTheTennisCourtContainingTheBall);
        graphicsEyesRight.DrawImageUnscaled(b,
                                            -(Config.WidthOfTennisCourtPX - Config.DistanceOfBatFromEdgeXpx - Config.WidthOfBat / 2) + Config.WidthOfEyes,
                                            0,
                                            Config.WidthOfEyes,
                                            snapShotImageTakenOfTheTennisCourtContainingTheBall.Height);
    }

    /// <summary>
    /// Stores the position of the ball after bouncing, or every so many horizontal pixels.
    /// We don't store it for every pixel the ball moves.
    /// </summary>
    private void StoreBallPath()
    {
        // ball travelling left to right?
        if (ball.dx > 0) return; // yes, we don't store the path for that.

        // start of path?
        if (pathOfBall.Count == 0)
        {
            pathOfBall.Add(new PointF(ball.X, ball.Y)); // this is where the path begins
        }
        else
        {
            // has ball gone into the top or bottom "bounce"/"reflection" regions OR moved at least 30 pixels left? If so, store the position
            // bottom/top is required because the bounce would go unmissed.
            if (ball.Y < 10f * Config.c_scale || ball.Y > 252f * Config.c_scale || Math.Abs(pathOfBall[^1].X - ball.X) > 30f * Config.c_scale) pathOfBall.Add(new PointF(ball.X, ball.Y));
        }
    }

    /// <summary>
    /// Draw debug lines.
    /// </summary>
    /// <param name="g"></param>
    private static void DrawDebugLines(Graphics g)
    {
        using Pen penDebugLine = new(Color.FromArgb(70, 255, 0, 0));
        penDebugLine.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

        // lines where bats are
        g.DrawLine(penDebugLine, Config.DistanceOfBatFromEdgeXpx, 0, Config.DistanceOfBatFromEdgeXpx, Config.HeightOfTennisCourtPX);
        g.DrawLine(penDebugLine, Config.WidthOfTennisCourtPX - Config.DistanceOfBatFromEdgeXpx, 0, Config.WidthOfTennisCourtPX - Config.DistanceOfBatFromEdgeXpx, Config.HeightOfTennisCourtPX);

        // lines showing "dead" zone that bat cannot enter
        g.DrawLine(penDebugLine, 0, Config.DeadZone, Config.WidthOfTennisCourtPX, Config.DeadZone);
        g.DrawLine(penDebugLine, 0, Config.HeightOfTennisCourtPX - Config.DeadZone, Config.WidthOfTennisCourtPX, Config.HeightOfTennisCourtPX - Config.DeadZone);
    }

    /// <summary>
    /// Moves the ball and bats.
    /// </summary>
    private void MoveBallAndBothBats()
    {
        // now move the ball.
        ball.Move();

        if (inAutoMode)
        {
            cursorPosition.Y = (int)ball.Y + offsetChosenByTrainerToStopPerpendicularHits;
            yTarget = cursorPosition.Y;
        }

        // either human controlled, or auto
        batOnTheRightControlledByHumanOrTrainer.Move(yTarget);
        batOnTheRightControlledByHumanOrTrainer.Move(yTarget);

        // neural network controls left bat.
        if (Trainer.s_currentTrainingDataItem is not null)
        {
            batOnTheLeftControlledByAI.Move((int)Math.Round((neuralNetworkControllingLeftBat.FeedForward(Trainer.s_currentTrainingDataItem.ToArray())[0]) * Config.HeightOfTennisCourtPX));
            batOnTheLeftControlledByAI.Move((int)Math.Round((neuralNetworkControllingLeftBat.FeedForward(Trainer.s_currentTrainingDataItem.ToArray())[0]) * Config.HeightOfTennisCourtPX));
        }
    }

    /// <summary>
    /// We pick a random training point, and backpropagate, once per time the ball moves. This
    /// in theory refines the training.
    /// </summary>
    private void ImproveByTrainingAsTheBallMoves()
    {
        int numOfItems = itemsThatNeedTraining.Count;

        TrainingDataItem tdi;

        if (Trainer.TrainingData.Count == 0) return;

        if (numOfItems <= 1)
            tdi = Trainer.TrainingData[^1];
        else
            tdi = itemsThatNeedTraining[RandomNumberGenerator.GetInt32(0, numOfItems)];

        neuralNetworkControllingLeftBat.BackPropagate(tdi.ToArray(),
                                                     new double[] { tdi.YPositionOfTheBallWhenItArrivesOnLeftBatLine / Config.HeightOfTennisCourtPX });
    }

    /// <summary>
    /// If automode, it tracks the ball automatically. 
    /// If !automode, user is expected to use the mouse to control the bat.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PictureBoxDisplay_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!inAutoMode)
        {
            Control? c = FindControlAtCursor(this);

            if (c == null || c != pictureBoxDisplay) return;

            if (this.Focused) yTarget = e.Y;
            cursorPosition = e.Location;
        }
        else
        {
            cursorPosition.Y = (int)(ball.Y + RandomNumberGenerator.GetInt32(-10, 10));
            yTarget = cursorPosition.Y;
        }
    }

    /// <summary>
    /// Handle special keys.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FormPong_KeyDown(object sender, KeyEventArgs e)
    {
        // "P" pauses game.
        if (e.KeyCode == Keys.P) timer1.Enabled = !timer1.Enabled;

        // "S" cycles speed | ctrl-S saves
        if (e.KeyCode == Keys.S)
        {
            if (e.Control)
            {
                neuralNetworkControllingLeftBat.Save(c_pongAIfile);
                Trainer.Save();
                MessageBox.Show("Saved");
            }
            else StepThroughSpeeds();
        }

        // "V" visualiser.
        if (e.KeyCode == Keys.V) LearningVisualiser.Enabled = !LearningVisualiser.Enabled;

        // "W" turns trajectory plot on / off
        if (e.KeyCode == Keys.W) BallTrajectoriesPlotter.Enabled = !BallTrajectoriesPlotter.Enabled;

        // "A" turns on auto-return mode.
        if (e.KeyCode == Keys.A)
        {
            inAutoMode = !inAutoMode;
            if (inAutoMode) Cursor.Hide(); else Cursor.Show();
        }
    }

    /// <summary>
    /// Steps thru the speeds by changing the timer interval.
    /// </summary>
    internal void StepThroughSpeeds()
    {
        var newInterval = timer1.Interval switch
        {
            5 => 20,
            20 => 100,
            100 => 1000,
            _ => 5,
        };

        timer1.Interval = newInterval;
    }

    /// <summary>
    /// Works out from the control hierarchy where the point is situated.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static Control? FindControlAtPoint(Control container, Point pos)
    {
        Control? child;

        foreach (Control c in container.Controls)
        {
            if (c.Visible && c.Bounds.Contains(pos))
            {
                child = FindControlAtPoint(c, new Point(pos.X - c.Left, pos.Y - c.Top));

                if (child == null)
                    return c;
                else
                    return child;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns a control at the current cursor position of "null" if not within form.
    /// </summary>
    /// <param name="form"></param>
    /// <returns></returns>
    public static Control? FindControlAtCursor(Form form)
    {
        Point pos = Cursor.Position;

        if (form.Bounds.Contains(pos)) return FindControlAtPoint(form, form.PointToClient(pos));

        return null;
    }

    /// <summary>
    /// Returns ALL the pixels as a "double" array. The element containing a 1 has part the ball.
    /// </summary>
    /// <param name="horizPixels"></param>
    /// <returns></returns>
    private double[] AIviewFromAboveAndDecideResponseAsReducedPixels(out double[] horizPixels)
    {
        ByteAccessibleBitmap accessibleBitmap = new(snapShotImageTakenOfTheTennisCourtContainingTheBall);

        int width = snapShotImageTakenOfTheTennisCourtContainingTheBall.Width;
        int height = snapShotImageTakenOfTheTennisCourtContainingTheBall.Height;

        double[] vertPixels = new double[Config.HeightOfTennisCourtPX];
        horizPixels = new double[width];

        int whitePixelCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // white is red+green+blue, we only need one of them to detect presence of pixel
                if (accessibleBitmap.GetRedChannelPixel(x, y) != 0)
                {
                    vertPixels[y] = 1; // 1 = ball (white pixel), 0 = no pixel.
                    horizPixels[x] = 1; // vertical depth

                    ++whitePixelCount;
                }
            }
        }

        // if no white pixels, where has the ball vanished to? Indicates ball moved outside snapshot.
        if (whitePixelCount == 0) Debugger.Break();

        return vertPixels;
    }

    /// <summary>
    /// Shows the form, ensures the neural network is trained for all prior data-points
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FormPong_Load(object sender, EventArgs e)
    {
        AddFormImages();
        Show();

        ShowAllThePaths();
        Train();

        timer1.Enabled = true;
    }

    #region BALL REACHED LEFT BAT

    // Pong form: code to handle ball reaching bat on left hand side.

    /// <summary>
    /// Ball reached bat on left-hand-side. Did it hit the bat? 
    /// </summary>
    private void Ball_BallReachedBatLeft()
    {
        expectArrivalLeft = false;

        int whereBallHitBat = batOnTheLeftControlledByAI.BallHitBat((int)Math.Round(ball.Y));

        // if "yposition" == -1, then we haven't stored the position, and need to do so then save the training data.
        if (Trainer.s_currentTrainingDataItem is not null)
        {
            if (Trainer.s_currentTrainingDataItem.YPositionOfTheBallWhenItArrivesOnLeftBatLine == -1)
            {
                DebugLog.Log("Ball missed");
                Trainer.s_currentTrainingDataItem.YPositionOfTheBallWhenItArrivesOnLeftBatLine = Math.Round(ball.Y);

                // don't save ones that end up in the dead-zone. No point in learning this accurately as ball cannot be returned
                if (ball.Y > Config.DeadZone && ball.Y < Config.HeightOfTennisCourtPX - Config.DeadZone)
                {
                    scoreBoard.RightPlayerScored();

                    Trainer.AddToTrainingData(Trainer.s_currentTrainingDataItem); // store training item
                    ShowAllThePaths();

                    TrainUntilTheBatIsAccurateForAllKnownTrainingImages();
                    neuralNetworkControllingLeftBat.Save(c_pongAIfile);

                    Trainer.Save();
                    // bat will now return the ball
                }
                else
                {
                    DebugLog.Log("Ball in dead-zone");
                }
            }
            else
                // train if not centre-ish of bat, otherwise it'll learn to "just" hit the ball anywhere
                if (Math.Abs(whereBallHitBat) > 1) Trainer.AddToTrainingData(Trainer.s_currentTrainingDataItem);

            // these are unless within deadzone, added to the the training data,
            Trainer.s_currentTrainingDataItem.lastBatPositionYOutputByAI = neuralNetworkControllingLeftBat.FeedForward(Trainer.s_currentTrainingDataItem.ToArray())[0];
            Trainer.s_currentTrainingDataItem.lastBatExpectedYPosition = Trainer.s_currentTrainingDataItem.YPositionOfTheBallWhenItArrivesOnLeftBatLine / Config.HeightOfTennisCourtPX;
        }

        VisuallyShowLearningError();

        // this kills the current tracked item, but doesn't detract from the one last added to the training data.
        Trainer.s_currentTrainingDataItem = null;

        pictureBoxFrameAllFrames.Image?.Dispose();
        pictureBoxFrameAllFrames.Image = null;

        pathOfBall.Clear();

        // maxValue = missed or  ball is behind bat
        if (whereBallHitBat == int.MaxValue)
        {
            // left player missed, so right player gets a point.
            scoreBoard.RightPlayerScored();

            // lob a new ball
            CreateNewBall();
            return;
        }

        // ball hit the bat, ensure ball starts to right of left bat
        ball.X = Config.DistanceOfBatFromEdgeXpx + Config.WidthOfBat / 2 + 1;

        // inverts the direction and makes it go off in a hit centric direction
        ball.BounceBallOffBat(whereBallHitBat);
        ++round;
        Text = $"Pong 4.0 - Epoch {epoch} round {round}";

        SetOffsetOfTrainer();
    }

    /// <summary>
    /// Plots the balll paths.
    /// </summary>
    private void ShowAllThePaths()
    {
        Bitmap b = BallTrajectoriesPlotter.GetImageWithAllThePathsPlotted((Bitmap)pictureBoxDisplay.Image);

        pictureBoxCoverage.Image?.Dispose();
        pictureBoxCoverage.Image = b;
        Application.DoEvents();
    }
    #endregion

    #region BALL REACHED RIGHT BAT
    // Pong form: code to handle ball reaching bat on right hand side.

    /// <summary>
    /// Ball reached bat on right-hand-side. Did it hit the bat? 
    /// </summary>
    private void Ball_BallReachedBatRight()
    {
        if (expectArrivalLeft) Debugger.Break(); // bounced within itself

        // determine where the ball hit the bat, MaxValue means "missed" bat
        // 
        // 4 ||       O
        // 3 ||    ... 
        // 2 ||   O
        // 1 ||O..
        // 0 ||O  ---- O
        // 1 ||
        // 2 ||
        // 3 ||O
        // 4 ||  . 
        //        .
        //         O
        //

        int whereBallHitBat = batOnTheRightControlledByHumanOrTrainer.BallHitBat((int)Math.Round(ball.Y));

        // missed or is to right of the bat.
        if (whereBallHitBat == int.MaxValue || ball.X > Config.WidthOfTennisCourtPX - Config.DistanceOfBatFromEdgeXpx + 3)
        {
            scoreBoard.LeftPlayerScored();

            CreateNewBall();
            return;
        }

        expectArrivalLeft = true; // used to detect ball bouncing within the bat due to logic error

        // stop the ball going thru the bat. A bit rudimentary, but works for this example
        ball.X = Config.WidthOfTennisCourtPX - (Config.DistanceOfBatFromEdgeXpx + 7);
        double Ypos = ball.Y;

        ball.BounceBallOffBat(whereBallHitBat);
        ++round;
        Text = $"Pong 4.0 - Epoch {epoch} round {round}";

        // create a training item
        Trainer.s_currentTrainingDataItem = new TrainingDataItem
        {
            frame = 0,
            round = round,
            epoch = epoch,
            xVelocityOfBallInDirectionOfLeftBat = ball.dx,
            yVelocityOfBallInDirectionOfLeftBat = ball.dy,
            // the one below is stored when it reaches the "left" bat / goal line.
            YPositionOfTheBallWhenItArrivesOnLeftBatLine = -1,
            YPositionOfTheBallWhenItArrivesOnRightBatLine = Math.Round(Ypos),
            lastRightBatPos = batOnTheRightControlledByHumanOrTrainer.Y
        };

        StoreBallPath();
    }
    #endregion
}