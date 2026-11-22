using System.Numerics;

namespace NiceTouch.Sim.Tests;

// How the two paddles move. What they do to the ball is in PaddleHitTests.
public class PaddleMovementTests
{
    [Fact]
    public void The_players_paddle_closes_half_the_gap_to_its_target_every_half_life()
    {
        Tuning tuning = TestRally.Tuning() with { PaddleHalfLifeSeconds = 0.1f };
        var match = new Match(tuning);
        var target = new Vector2(4, -2);

        match.Run(6, target);

        Assert.Equal(2, match.PlayerPaddle.X, 3);
        Assert.Equal(-1, match.PlayerPaddle.Y, 3);
    }

    [Fact]
    public void With_no_smoothing_the_players_paddle_is_on_its_target_at_once()
    {
        Tuning tuning = TestRally.Tuning() with { PaddleHalfLifeSeconds = 0 };
        var match = new Match(tuning);

        match.Run(1, new Vector2(4, -2));

        Assert.Equal(new Vector2(4, -2), match.PlayerPaddle);
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(-100, -100)]
    public void The_players_paddle_stays_inside_the_tunnel(float targetX, float targetY)
    {
        var match = new Match(TestRally.Tuning());

        match.Run(120, new Vector2(targetX, targetY));

        // Its edges touch the walls: tunnel 12 by 8, paddle 3 by 2.
        Assert.Equal(4.5f * MathF.Sign(targetX), match.PlayerPaddle.X, 3);
        Assert.Equal(3 * MathF.Sign(targetY), match.PlayerPaddle.Y, 3);
    }

    [Fact]
    public void Paddle_speed_is_measured_in_units_per_second()
    {
        Tuning tuning = TestRally.Tuning() with { PaddleHalfLifeSeconds = 0 };
        var match = new Match(tuning);

        // Half a unit right and a quarter down in one tick of a 60th of a second.
        match.Run(1, new Vector2(0.5f, -0.25f));

        Assert.Equal(30, match.PlayerPaddleVelocity.X, 3);
        Assert.Equal(-15, match.PlayerPaddleVelocity.Y, 3);
    }

    [Fact]
    public void A_paddle_held_against_a_wall_is_not_moving()
    {
        Tuning tuning = TestRally.Tuning() with { PaddleHalfLifeSeconds = 0 };
        var match = new Match(tuning);
        match.Run(1, new Vector2(50, 0));

        // The mouse keeps going but the paddle cannot.
        match.Run(1, new Vector2(60, 0));

        Assert.Equal(Vector2.Zero, match.PlayerPaddleVelocity);
    }

    [Fact]
    public void The_opponent_chases_a_ball_that_is_coming_towards_it()
    {
        Match match = TestRally.InFlight(new Vector3(3, -2, 5), new Vector3(0, 0, 24));

        match.Step();

        // Straight at the ball, at its one speed.
        float step = match.Tuning.OpponentSpeed * Tuning.TickSeconds;
        Vector2 expected = Vector2.Normalize(new Vector2(3, -2)) * step;
        Assert.Equal(expected.X, match.OpponentPaddle.X, 4);
        Assert.Equal(expected.Y, match.OpponentPaddle.Y, 4);
        Assert.Equal(match.Tuning.OpponentSpeed, match.OpponentPaddleVelocity.Length(), 3);
    }

    [Fact]
    public void The_opponent_stops_on_the_ball_and_does_not_overshoot()
    {
        Match match = TestRally.InFlight(new Vector3(0.05f, 0, 5), new Vector3(0, 0, 24));

        match.Step();

        Assert.Equal(0.05f, match.OpponentPaddle.X, 4);
        Assert.Equal(0, match.OpponentPaddle.Y, 4);
    }

    [Fact]
    public void The_opponent_drifts_back_to_the_centre_when_the_ball_is_going_away_from_it()
    {
        Match match = TestRally.InFlight(new Vector3(3, 2, 25), new Vector3(0, 0, -24));
        match.OpponentPaddle = new Vector2(2, 1);

        match.Step();
        float afterOneTick = match.OpponentPaddle.Length();
        match.Run(40);

        Assert.True(afterOneTick < new Vector2(2, 1).Length());
        Assert.Equal(Vector2.Zero, match.OpponentPaddle);
    }

    [Fact]
    public void The_opponent_stays_inside_the_tunnel()
    {
        // A ball hugging the corner, further out than a paddle's centre can go.
        Tuning tuning = TestRally.Tuning() with { OpponentSpeed = 50 };
        Match match = TestRally.InFlight(new Vector3(5.4f, 3.4f, 5), new Vector3(0, 0, 1), tuning: tuning);

        match.Run(60);

        Assert.Equal(4.5f, match.OpponentPaddle.X, 3);
        Assert.Equal(3, match.OpponentPaddle.Y, 3);
    }
}
