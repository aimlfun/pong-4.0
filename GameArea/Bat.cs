using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pong.Utils;

namespace Pong;

/// <summary>
/// Which side the bat is positioned.
/// </summary>
enum Side { Left, Right };

/// <summary>
/// Represents a bat.
/// 
/// c_batDistanceFromEdgeX
///     X
/// +---+
/// 
///     ||  -+
///     ||   | c_halfTheBatLength
///     ||   |
///     ##  -+  Y
///     ||
///     ||
///     ||
/// 
/// </summary>
internal class Bat
{
    /// <summary>
    /// Tracks whether this is a left or right bat.
    /// </summary>
    private readonly Side sideBatAppearsOn;

    /// <summary>
    /// Y position of the bat center.
    /// </summary>
    internal int Y;

    /// <summary>
    /// X position of the bat.
    /// </summary>
    internal int X;

    /// <summary>
    /// Height of the court.
    /// </summary>
    private readonly int heightPX;

    /// <summary>
    /// Pen for drawing the bat (solid thick line).
    /// </summary>
    private readonly Pen penBat = new(Color.FromArgb(240, 255, 255, 255), Config.WidthOfBat / 2);
    private readonly Pen dottedPen = new (Color.FromArgb(100, 255, 255, 255));
        
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="side"></param>
    /// <param name="height"></param>
    internal Bat(Side side, int width, int height)
    {
        sideBatAppearsOn = side;
        heightPX = height;

        Y = height / 2;

        // depending on side, we position to the left or right
        X = (side == Side.Left) ?
                    (Config.DistanceOfBatFromEdgeXpx + (int)penBat.Width / 2) :
                    (width - Config.DistanceOfBatFromEdgeXpx - (int)penBat.Width / 2);
    
        dottedPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
    }

    /// <summary>
    /// Moves the "bat" smoothly upto a fixed amount
    /// </summary>
    /// <param name="yTarget"></param>
    internal void Move(int yTarget)
    {
        // clamp so it moves a maximum of +/- 3 pixels in any go.
        // also clamp so it cannot empty the dead zone.
        Y = yTarget.Clamp(Y - 3, Y + 3).Clamp(Config.DeadZone + Config.HalfTheBatLength, heightPX - (Config.DeadZone + Config.HalfTheBatLength));
    }

    /// <summary>
    /// Detects where on the bat it hit.
    /// </summary>
    /// <param name="ballY"></param>
    /// <returns></returns>
    internal int BallHitBat(int ballY)
    {
        float dist = (ballY - Y);

        // did not hit the bat
        if (Math.Abs(dist)-1 >= Config.HalfTheBatLength) return int.MaxValue;

        // bat is split into 8 zones
        dist /= (Config.HalfTheBatLength / 4f);

        return (int)Math.Round(dist);
    }

    /// <summary>
    /// Draws the bat.
    /// </summary>
    /// <param name="graphics"></param>
    internal void Draw(Graphics graphics)
    {
        graphics.DrawLine(penBat, new Point(X, Y - Config.HalfTheBatLength), new Point(X, Y + Config.HalfTheBatLength));
        
        if(sideBatAppearsOn == Side.Left)
        {
            // draw a small dotted horizontal line indicating centre of bat
            using Pen p = new(Color.FromArgb(150, 255, 255, 255));
            p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            graphics.DrawLine(p, X-Config.HalfTheBatLength/2, Y, X+ Config.HalfTheBatLength/2, Y);
        }
    }

    /// <summary>
    /// Draws a dotted outline of the bat's position.
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="Ypos"></param>
    internal void DrawDottedOutline(Graphics graphics, float Ypos)
    {
        if (Ypos == -1) return;
        
        graphics.DrawRectangle(dottedPen,
                               X - penBat.Width / 2,
                               Ypos - Config.HalfTheBatLength,
                               penBat.Width,
                               2 * Config.HalfTheBatLength);
    }
}