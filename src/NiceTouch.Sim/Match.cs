using System.Numerics;

namespace NiceTouch.Sim;

// Coordinates: x right, y up, z depth from the player's end (0) to the
// opponent's end (TunnelDepth).
public sealed class Match
{
    private const float Dt = Tuning.TickSeconds;

    public Match(Tuning tuning)
    {
        // A copy, so nothing outside can change the rules of a running match.
        Tuning = tuning with { };
    }

    public Tuning Tuning { get; }
    public long Tick { get; private set; }
    public Vector3 BallPosition { get; internal set; }
    public Vector3 BallVelocity { get; internal set; }

    // Spin as angular velocity in radians per second. X is top spin (back
    // spin when negative, for a ball going away), Y is side spin. There is
    // no spin about the depth axis.
    public Vector2 BallSpin { get; internal set; }

    // What happened during the latest tick, for sound and effects.
    public TickEvents Events { get; private set; }

    // Paddle centres, as x and y on each paddle's end of the tunnel, and
    // their speeds in units per second measured over the latest tick.
    public Vector2 PlayerPaddle { get; internal set; }
    public Vector2 PlayerPaddleVelocity { get; private set; }
    public Vector2 OpponentPaddle { get; internal set; }
    public Vector2 OpponentPaddleVelocity { get; private set; }

    public void Step(TickInput input)
    {
        Events = TickEvents.None;
        MovePaddles(input.PaddleTarget);
        Fly();
        Tick++;
    }

    private void MovePaddles(Vector2 playerTarget)
    {
        // The player's paddle follows the mouse with a touch of smoothing.
        float closed = 1 - Remaining(Tuning.PaddleHalfLifeSeconds);
        Vector2 player = Vector2.Lerp(PlayerPaddle, InsideTunnel(playerTarget), closed);
        PlayerPaddleVelocity = (player - PlayerPaddle) / Dt;
        PlayerPaddle = player;

        // The opponent chases the ball while it is coming, and drifts back
        // to the centre otherwise, at one speed.
        bool ballIsComing = BallVelocity.Z > 0;
        Vector2 goal = InsideTunnel(ballIsComing ? new Vector2(BallPosition.X, BallPosition.Y) : Vector2.Zero);
        Vector2 gap = goal - OpponentPaddle;
        float reach = Tuning.OpponentSpeed * Dt;
        Vector2 opponent = gap.Length() <= reach ? goal : OpponentPaddle + Vector2.Normalize(gap) * reach;
        OpponentPaddleVelocity = (opponent - OpponentPaddle) / Dt;
        OpponentPaddle = opponent;
    }

    // The nearest place a paddle's centre can be with its edges inside the
    // walls.
    private Vector2 InsideTunnel(Vector2 centre)
    {
        var limit = Vector2.Max(Vector2.Zero, new Vector2(
            (Tuning.TunnelWidth - Tuning.PaddleWidth) / 2,
            (Tuning.TunnelHeight - Tuning.PaddleHeight) / 2));
        return Vector2.Clamp(centre, -limit, limit);
    }

    private void Fly()
    {
        BallSpin *= Remaining(Tuning.SpinHalfLifeSeconds);

        // The bend is spin crossed with velocity, sideways parts only, so
        // it reverses when the ball's direction of travel reverses. The
        // extra factor of pace makes the curve the same shape at any pace.
        Vector3 v = BallVelocity;
        float bend = Tuning.SpinCurve * v.Z * MathF.Abs(v.Z) * Dt;
        v.X += BallSpin.Y * bend;
        v.Y -= BallSpin.X * bend;
        BallVelocity = v;

        Advance(Dt);
    }

    // Moves the ball in a straight line for part of a tick and bounces it
    // off the walls.
    private void Advance(float seconds)
    {
        Vector3 p = BallPosition + BallVelocity * seconds;
        Vector3 v = BallVelocity;
        int fromSide = Reflect(ref p.X, ref v.X, Tuning.TunnelWidth / 2 - Tuning.BallRadius);
        int fromFloor = Reflect(ref p.Y, ref v.Y, Tuning.TunnelHeight / 2 - Tuning.BallRadius);
        BallPosition = p;
        BallVelocity = v;

        if (fromSide != 0)
            BiteWall(new Vector3(fromSide, 0, 0));
        if (fromFloor != 0)
            BiteWall(new Vector3(0, fromFloor, 0));
    }

    // Keeps one sideways coordinate between its two walls by reflecting it.
    // Returns which way the wall it bounced off faces along this axis, or
    // zero if there was no bounce.
    private static int Reflect(ref float position, ref float velocity, float limit)
    {
        limit = MathF.Max(limit, 0);
        int facing = position > limit ? -1 : position < -limit ? 1 : 0;
        if (facing != 0)
        {
            position = Math.Clamp(-2 * facing * limit - position, -limit, limit);
            velocity = -velocity;
        }
        return facing;
    }

    // Spin bites a wall: the spinning surface grips it and kicks the ball
    // along the tunnel. Spin that bent the ball into the wall speeds it up,
    // the opposite spin slows it. A ball with no spin is left alone.
    private void BiteWall(Vector3 wallFacing)
    {
        Events |= TickEvents.WallBounce;

        Vector3 v = BallVelocity;
        float travel = MathF.Sign(v.Z);
        float pace = travel * (v.Z - Tuning.WallGrip * ContactSpeed(wallFacing).Z);
        float slowest = Tuning.BasePace * Tuning.PaceMin;
        float fastest = Tuning.BasePace * Tuning.PaceMax;
        v.Z = travel * MathF.Max(slowest, MathF.Min(pace, fastest));
        BallVelocity = v;

        // Side walls are bitten by side spin, floor and ceiling by top spin.
        float kept = 1 - Tuning.WallSpinLoss;
        BallSpin *= wallFacing.X != 0 ? new Vector2(1, kept) : new Vector2(kept, 1);
    }

    // How fast the ball's spinning surface is moving, relative to its
    // centre, at the point where it touches a surface facing this way.
    private Vector3 ContactSpeed(Vector3 surfaceFacing) =>
        Vector3.Cross(new Vector3(BallSpin, 0), -Tuning.BallRadius * surfaceFacing);

    // The fraction of a fading value left after one tick.
    private static float Remaining(float halfLifeSeconds) =>
        halfLifeSeconds > 0 ? MathF.Pow(0.5f, Dt / halfLifeSeconds) : 0;
}
