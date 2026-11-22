using System.Numerics;

namespace NiceTouch.Sim.Tests;

// The serve from the paddle, and what happens after a miss.
public class ServeAndMissTests
{
    private const float Dt = NiceTouch.Sim.Tuning.TickSeconds;
    private static readonly TickInput Click = new(Vector2.Zero, Serve: true);

    private static Tuning Tuning() => TestRally.Tuning() with { PaddleHalfLifeSeconds = 0, MissPauseSeconds = 1 };

    // A rally one tick after a miss at the given end.
    private static Match JustMissed(Side side)
    {
        Tuning tuning = Tuning() with { OpponentSpeed = 0 };
        float z = side == Side.Player ? 0.6f : tuning.TunnelDepth - 0.6f;
        float pace = side == Side.Player ? -24 : 24;
        Match match = TestRally.InFlight(new Vector3(4, 3, z), new Vector3(0, 0, pace), new Vector2(5, 5), tuning);
        match.Step();
        Assert.Equal(side, match.LastMiss);
        return match;
    }

    [Fact]
    public void A_new_match_waits_for_the_serve_with_the_ball_on_the_players_paddle()
    {
        var match = new Match(Tuning());

        Assert.Equal(Phase.Serving, match.Phase);
        Assert.Equal(new Vector3(0, 0, match.Tuning.BallRadius), match.BallPosition);
        Assert.Null(match.LastMiss);
    }

    [Fact]
    public void Before_the_serve_the_ball_rides_on_the_paddle()
    {
        var match = new Match(Tuning());

        match.Run(30, new Vector2(3, -1));

        Assert.Equal(Phase.Serving, match.Phase);
        Assert.Equal(new Vector3(3, -1, match.Tuning.BallRadius), match.BallPosition);
        Assert.Equal(Vector3.Zero, match.BallVelocity);
    }

    [Fact]
    public void The_click_serves_the_ball_towards_the_opponent_at_base_pace()
    {
        var match = new Match(Tuning());

        match.Step(Click);

        Assert.Equal(Phase.Rally, match.Phase);
        Assert.Equal(new Vector3(0, 0, match.Tuning.BasePace), match.BallVelocity);
        Assert.Equal(Vector2.Zero, match.BallSpin);
        Assert.True(match.Events.HasFlag(TickEvents.PaddleHit));
        Assert.Equal(1, match.RallyLength);
    }

    [Fact]
    public void The_serve_can_be_made_from_anywhere_the_paddle_can_go()
    {
        var match = new Match(Tuning());
        match.Run(1, new Vector2(4, 2));

        match.Step(new TickInput(new Vector2(4, 2), Serve: true));
        match.Step();

        Assert.Equal(4, match.BallPosition.X);
        Assert.Equal(2, match.BallPosition.Y);
        Assert.True(match.BallPosition.Z > match.Tuning.BallRadius);
    }

    [Fact]
    public void A_swipe_at_the_moment_of_the_serve_works_like_any_other_hit()
    {
        var swipe = new Vector2(12, -6);
        var match = new Match(Tuning());
        match.PlayerPaddle = -swipe * Dt;

        match.Step(Click);

        Tuning t = match.Tuning;
        Assert.Equal(swipe.Length() * t.SwipeSpin, match.BallSpin.Length(), 3);
        Assert.Equal(swipe.X * t.SwipePush, match.BallVelocity.X, 3);
        Assert.Equal(swipe.Y * t.SwipePush, match.BallVelocity.Y, 3);
    }

    [Fact]
    public void A_click_during_the_rally_does_nothing()
    {
        Match clicked = TestRally.InFlight(new Vector3(1, 1, 15), new Vector3(2, 0, 24), new Vector2(5, 5), Tuning());
        Match left = TestRally.InFlight(new Vector3(1, 1, 15), new Vector3(2, 0, 24), new Vector2(5, 5), Tuning());

        clicked.Step(Click);
        left.Step();

        Assert.Equal(left.BallPosition, clicked.BallPosition);
        Assert.Equal(left.BallVelocity, clicked.BallVelocity);
        Assert.Equal(left.RallyLength, clicked.RallyLength);
    }

    [Theory]
    [InlineData(Side.Player)]
    [InlineData(Side.Opponent)]
    public void After_a_miss_play_stops_for_the_pause_and_the_ball_comes_back_to_the_player(Side whoMissed)
    {
        Match match = JustMissed(whoMissed);
        Vector3 whereItStopped = match.BallPosition;
        Assert.Equal(Phase.PointOver, match.Phase);

        // One second of pause is 60 ticks.
        match.Run(59);
        Assert.Equal(Phase.PointOver, match.Phase);
        Assert.Equal(whereItStopped, match.BallPosition);

        match.Run(1);
        Assert.Equal(Phase.Serving, match.Phase);
        Assert.Equal(new Vector3(0, 0, match.Tuning.BallRadius), match.BallPosition);
        Assert.Equal(Vector2.Zero, match.BallSpin);
        // Still on record, so the screen can go on saying who missed.
        Assert.Equal(whoMissed, match.LastMiss);
    }

    [Fact]
    public void A_click_during_the_pause_does_not_serve()
    {
        Match match = JustMissed(Side.Player);

        match.Step(Click);

        Assert.Equal(Phase.PointOver, match.Phase);
        Assert.Equal(Vector3.Zero, match.BallVelocity);
    }

    [Fact]
    public void The_paddle_still_follows_the_mouse_during_the_pause()
    {
        Match match = JustMissed(Side.Player);

        match.Run(1, new Vector2(-2, 1));

        Assert.Equal(new Vector2(-2, 1), match.PlayerPaddle);
    }

    [Fact]
    public void The_rally_length_starts_again_at_the_next_serve()
    {
        // Serve straight, let the opponent return it, then stand in the
        // corner so the ball comes back past the paddle.
        var match = new Match(Tuning());
        match.Step(Click);
        match.Run(4 * NiceTouch.Sim.Tuning.TicksPerSecond, new Vector2(4.5f, 3));
        Assert.Equal(Side.Player, match.LastMiss);
        Assert.Equal(Phase.Serving, match.Phase);
        // The finished rally's length stays on show until the next serve.
        Assert.Equal(2, match.RallyLength);

        match.Step(Click);

        Assert.Equal(1, match.RallyLength);
    }

    [Fact]
    public void The_opponent_does_not_chase_a_ball_that_is_waiting_to_be_served()
    {
        var match = new Match(Tuning());

        match.Run(30, new Vector2(4, 2));

        Assert.Equal(Vector2.Zero, match.OpponentPaddle);
    }
}
