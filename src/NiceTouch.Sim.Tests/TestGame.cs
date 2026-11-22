using System.Numerics;

namespace NiceTouch.Sim.Tests;

// Shared set-up for the tests of the game around the rally.
internal static class TestGame
{
    public const int PauseTicks = NiceTouch.Sim.Tuning.TicksPerSecond;
    public static readonly TickInput Click = new(Vector2.Zero, Serve: true);

    // The paddle goes exactly where it is told, and the pause after a miss
    // is one second.
    public static Tuning Tuning() => TestRally.Tuning() with { PaddleHalfLifeSeconds = 0, MissPauseSeconds = 1 };

    // Steps with the mouse held still at one spot and no click.
    public static void Run(this Game game, int ticks, Vector2 paddleTarget = default)
    {
        for (int i = 0; i < ticks; i++)
            game.Step(new TickInput(paddleTarget, Serve: false));
    }

    // Makes one side miss on the next tick, whatever the rally was doing:
    // the ball is put one tick short of that end, with no spin, as far
    // from both paddles as it can be.
    public static void Miss(this Game game, Side side)
    {
        Match match = game.Match;
        Tuning tuning = match.Tuning;
        var corner = new Vector2(match.OpponentPaddle.X > 0 ? -4 : 4, match.OpponentPaddle.Y > 0 ? -3 : 3);
        float z = side == Side.Player ? 0.6f : tuning.TunnelDepth - 0.6f;
        float pace = side == Side.Player ? -tuning.BasePace : tuning.BasePace;

        match.Phase = Phase.Rally;
        match.BallPosition = new Vector3(corner, z);
        match.BallVelocity = new Vector3(0, 0, pace);
        match.BallSpin = Vector2.Zero;
        game.Step(new TickInput(-corner, Serve: false));

        Assert.True(game.Events.HasFlag(TickEvents.Miss));
        Assert.Equal(side, match.LastMiss);
    }

    // Takes every life the opponent has left, which clears the level.
    public static void ClearLevel(this Game game)
    {
        while (game.OpponentLives > 0)
            game.Miss(Side.Opponent);
    }

    // Loses every life the player has left, which ends the game.
    public static void LoseEveryLife(this Game game)
    {
        while (game.PlayerLives > 0)
            game.Miss(Side.Player);
    }
}
