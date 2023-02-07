using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Pong.Utils;

namespace Pong;

/// <summary>
/// Direction ball is moving.
/// </summary>
internal enum BallDirection { LeftToRight, RightToLeft };

/// <summary>
/// Represents a "Ball" that can bounce off walls, that notifies owner if it reaches goal line.
/// </summary>
internal class Ball
{
    /// <summary>
    /// Brush used to draw ball.
    /// </summary>
    internal static SolidBrush s_brushUsedToDrawBall = new(Color.FromArgb(150, 255, 255, 255));

    /// <summary>
    /// Delegate for notifying when ball reaches either bat.
    /// </summary>
    /// <param name="zone"></param>
    internal delegate void GoalLineNotify();  // delegate

    /// <summary>
    /// Event fired when ball reaches left goal area.
    /// </summary>
    internal event GoalLineNotify? BallReachedBatLeft;

    /// <summary>
    /// Event fired when ball reaches right goal area.
    /// </summary>
    internal event GoalLineNotify? BallReachedBatRight;

    /// <summary>
    /// X position (horizontal) of ball.
    /// </summary>
    internal float X;

    /// <summary>
    /// Y position (vertical) of ball.
    /// </summary>
    internal float Y;

    /// <summary>
    /// DeltaX ball is moving each frame.
    /// </summary>
    internal float dx;

    /// <summary>
    /// DeltaY ball is moving each frame.
    /// </summary>
    internal float dy;

    /// <summary>
    /// Acceleration to apply each time the ball makes contact with a bat.
    /// </summary>
    internal float accel = 1.05f; //5% faster

    /// <summary>
    /// Constructor. For a new ball.
    /// </summary>
    /// <param name="direction"></param>
    internal Ball(BallDirection direction)
    {
        X = Config.WidthOfTennisCourtPX / 2;
        Y = Config.HeightOfTennisCourtPX / 2;

        // balls randomly fired to the right.
        dy = ((float)(RandomNumberGenerator.GetInt32(0, 3000) / 1000f) * (RandomNumberGenerator.GetInt32(1, 2) > 1 ? -1 : 1));
        dx = ((float)(RandomNumberGenerator.GetInt32(500, 2500) / 1000f) * (direction == BallDirection.LeftToRight ? 1 : -1));

        // improve training by rounding. The ball position is rounded by virtue it is drawn at
        // a pixel location.
        dx = (float)Math.Round(dx, 1);
        dy = (float)Math.Round(dy, 1);
    }

    /// <summary>
    /// Moves the ball.
    /// </summary>
    internal void Move()
    {
        // move the ball
        X += dx;
        Y += dy;

        // if the ball has reached the left goal-line, we fire the event
        if (X <= Config.DistanceOfBatFromEdgeXpx + Config.WidthOfBat/2)
        {
            X -= dx;
            Y -= dy;
            
            BallReachedBatLeft?.Invoke();
        }
        // if the ball has reached the right goal-line, we fire the event
        else if (X >= Config.WidthOfTennisCourtPX - Config.DistanceOfBatFromEdgeXpx - Config.WidthOfBat / 2)
        {
            X -= dx;
            Y -= dy;
            
            BallReachedBatRight?.Invoke();
        }

        // bounce of top
        if (Y < 0)
        {
            dy = -dy;
            Y = 0 + dy;
        }
        else
        // bounce of bottom
        if (Y > Config.HeightOfTennisCourtPX)
        {
            dy = -dy;
            Y = Config.HeightOfTennisCourtPX + dy;
        }
    }

    /// <summary>
    /// Draw the ball. Sure we could draw a round one, but that would not be "authentic".
    /// </summary>
    /// <param name="graphics"></param>
    internal void Draw(Graphics graphics)
    {
        graphics.FillRectangle(s_brushUsedToDrawBall, X - Config.BallRadiusPX, Y - Config.BallRadiusPX, 2 * Config.BallRadiusPX, 2 * Config.BallRadiusPX);
    }

    /// <summary>
    /// Ball bounces off bat.
    /// </summary>
    /// <param name="hit"></param>
    internal void BounceBallOffBat(int hit)
    {
        X -= dx; // move away from being inside the bat

        dx = -dx;  // switch direction (right->left, or left->right)
        dy = hit / 2f;

        // speed it up in x & y direction
        //dx *= accel;
        //dy *= accel;

        // prevent it moving too fast for the frame 2nd image to catch it.
        float maxDx = (float)Config.WidthOfEyes / (float)Config.FrameAtWhichWeTake3rdImage - 5;

        // without this, it could cause it to exponentially speed up.
        dx = dx.Clamp(-maxDx, maxDx);
        dy = dy.Clamp(-maxDx, maxDx);

        // improve training by rounding. The ball position is rounded by virtue it is drawn at
        // a pixel location.
        dx = (float)Math.Round(dx, 1);
        dy = (float)Math.Round(dy, 1);
    }
}