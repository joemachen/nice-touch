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
        PaddleWidth = 3,
        PaddleHeight = 2,
        PaddleHalfLifeSeconds = 0.01f,
        OpponentSpeed = 9,
        SwipeSpin = 1,
        MaxSpin = 40,
        SwipePush = 0.25f,
        LateralKeep = 0.5f,
        SpinKeep = 0.5f,
        PaddleGrip = 0.5f,
        MissPauseSeconds = 1,
    };

    // A ball already in flight.
    public static Match InFlight(Vector3 position, Vector3 velocity, Vector2 spin = default, Tuning? tuning = null) =>
        new(tuning ?? Tuning())
        {
            BallPosition = position,
            BallVelocity = velocity,
            BallSpin = spin,
            Phase = Phase.Rally,
        };

    // Steps with the mouse held still at one spot and no click.
    public static void Run(this Match match, int ticks, Vector2 paddleTarget = default)
    {
        for (int i = 0; i < ticks; i++)
            match.Step(new TickInput(paddleTarget, Serve: false));
    }

    public static void Step(this Match match) => match.Run(1);
}
