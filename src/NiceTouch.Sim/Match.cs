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

    public void Step()
    {
        BallPosition += BallVelocity * Dt;
        Tick++;
    }
}
