using System.Numerics;

namespace NiceTouch.Sim.Tests;

// Shared set-up for the rule tests.
internal static class TestRally
{
    // Fixed numbers for the tests, so retuning the game's defaults cannot
    // break a rule test. Tests that depend on one value set it themselves.
    public static Tuning Tuning() => new()
    {
        TunnelWidth = 12,
        TunnelHeight = 8,
        TunnelDepth = 30,
        SpinCurve = 0.001f,
        SpinHalfLifeSeconds = 3,
        BallRadius = 0.5f,
        BasePace = 24,
        WallGrip = 0.4f,
        WallSpinLoss = 0.4f,
        PaceMin = 0.6f,
        PaceMax = 1.6f,
    };

    // A ball already in flight.
    public static Match InFlight(Vector3 position, Vector3 velocity, Vector2 spin = default, Tuning? tuning = null) =>
        new(tuning ?? Tuning())
        {
            BallPosition = position,
            BallVelocity = velocity,
            BallSpin = spin,
        };

    public static void Run(this Match match, int ticks)
    {
        for (int i = 0; i < ticks; i++)
            match.Step();
    }
}
