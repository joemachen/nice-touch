using System.Numerics;

namespace NiceTouch.Sim;

public sealed class Match
{
    public long Tick { get; private set; }
    public Vector3 BallPosition { get; set; }
    public Vector3 BallVelocity { get; set; }

    public void Step()
    {
        BallPosition += BallVelocity / Tuning.TicksPerSecond;
        Tick++;
    }
}
