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

    public void Step()
    {
        Fly();
        Tick++;
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

        BallPosition += BallVelocity * Dt;
    }

    // The fraction of a fading value left after one tick.
    private static float Remaining(float halfLifeSeconds) =>
        halfLifeSeconds > 0 ? MathF.Pow(0.5f, Dt / halfLifeSeconds) : 0;
}
