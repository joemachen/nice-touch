using System.Numerics;

namespace NiceTouch.Sim.Tests;

// Gravity, added after the first QA of M1a because the ball felt floaty.
// It is a tuning value, so zero means the game plays as it did without it.
public class GravityTests
{
    private const float Pace = 24;

    private static Tuning WithGravity(float gravity) => TestRally.Tuning() with { Gravity = gravity };

    [Theory]
    [InlineData(+1)]
    [InlineData(-1)]
    public void Gravity_pulls_the_ball_down_whichever_way_it_is_travelling(int travel)
    {
        Match match = TestRally.InFlight(new Vector3(1, 2, 15), new Vector3(0.5f, 0, Pace * travel), tuning: WithGravity(6));

        match.Run(30);

        // Half a second at 6 units per second, per second.
        Assert.Equal(-3, match.BallVelocity.Y, 3);
        Assert.True(match.BallPosition.Y < 2);
        // Sideways and down the tunnel it is untouched.
        Assert.Equal(0.5f, match.BallVelocity.X);
        Assert.Equal(Pace * travel, match.BallVelocity.Z);
    }

    [Fact]
    public void More_gravity_pulls_harder()
    {
        Match light = TestRally.InFlight(new Vector3(0, 2, 15), new Vector3(0, 0, Pace), tuning: WithGravity(4));
        Match heavy = TestRally.InFlight(new Vector3(0, 2, 15), new Vector3(0, 0, Pace), tuning: WithGravity(8));

        light.Run(20);
        heavy.Run(20);

        Assert.Equal(2 * light.BallVelocity.Y, heavy.BallVelocity.Y, 3);
        Assert.True(heavy.BallPosition.Y < light.BallPosition.Y);
    }

    [Fact]
    public void With_gravity_at_zero_the_ball_does_not_drop()
    {
        Match match = TestRally.InFlight(new Vector3(0, 2, 15), new Vector3(0, 0, Pace), tuning: WithGravity(0));

        match.Run(20);

        Assert.Equal(2, match.BallPosition.Y);
        Assert.Equal(0, match.BallVelocity.Y);
    }

    [Fact]
    public void A_dropped_ball_bounces_off_the_floor_back_to_the_height_it_fell_from()
    {
        // Hanging still down the tunnel, so it only falls and bounces.
        Match match = TestRally.InFlight(new Vector3(0, 1, 15), Vector3.Zero, tuning: WithGravity(20));
        int bounces = 0;
        float highestAfterBounce = float.MinValue;

        // Long enough to fall, bounce and come all the way back up once.
        for (int i = 0; i < 80 && bounces < 2; i++)
        {
            match.Step();
            if (match.Events.HasFlag(TickEvents.WallBounce))
                bounces++;
            else if (bounces == 1)
                highestAfterBounce = MathF.Max(highestAfterBounce, match.BallPosition.Y);
        }

        Assert.True(bounces >= 1, "it reached the floor");
        // Within a few hundredths of the 4.5 units it fell: one bounce can
        // land a touch high or low depending on where in a tick it happens.
        // The next test checks that this never adds up.
        Assert.InRange(highestAfterBounce, 0.85f, 1.15f);
    }

    [Fact]
    public void A_bouncing_ball_keeps_its_height_bounce_after_bounce()
    {
        // Thirty seconds of bouncing on the spot, from one unit above the
        // floor's reach. It must neither die away nor climb.
        Match match = TestRally.InFlight(new Vector3(0, -2.5f, 15), Vector3.Zero, tuning: WithGravity(20));
        int bounces = 0;
        float highestLately = float.MinValue;

        for (int i = 0; i < 30 * Tuning.TicksPerSecond; i++)
        {
            match.Step();
            if (match.Events.HasFlag(TickEvents.WallBounce))
                bounces++;
            if (i >= 29 * Tuning.TicksPerSecond)
                highestLately = MathF.Max(highestLately, match.BallPosition.Y);
        }

        Assert.True(bounces > 20, $"{bounces} bounces");
        Assert.Equal(-2.5f, highestLately, 1);
    }

    [Fact]
    public void A_ball_with_no_spin_bounces_off_the_floor_under_gravity_without_changing_pace()
    {
        Match match = TestRally.InFlight(new Vector3(0, -3, 5), new Vector3(0, 0, Pace), tuning: WithGravity(20));
        bool bounced = false;

        for (int i = 0; i < 30; i++)
        {
            match.Step();
            bounced |= match.Events.HasFlag(TickEvents.WallBounce);
        }

        Assert.True(bounced);
        Assert.Equal(Pace, match.BallVelocity.Z);
    }

    // ---- A ball with too little bounce left rolls along the floor ----

    // Just above the floor and sinking slowly, with top spin on it.
    private static Match AboutToRoll(float gravity = 20) => TestRally.InFlight(
        new Vector3(0, -3.45f, 5), new Vector3(0, -1, Pace), new Vector2(10, 0),
        WithGravity(gravity) with { SpinHalfLifeSeconds = 1e9f });

    [Fact]
    public void A_ball_too_slow_to_bounce_rolls_along_the_floor_in_silence()
    {
        Match match = AboutToRoll();
        float floor = -(match.Tuning.TunnelHeight / 2 - match.Tuning.BallRadius);

        for (int i = 0; i < 40; i++)
        {
            match.Step();
            Assert.False(match.Events.HasFlag(TickEvents.WallBounce), $"bounce reported on tick {i}");
        }

        Assert.Equal(floor, match.BallPosition.Y, 3);
        // Still on its way down the tunnel, and the floor has not bitten:
        // pace and spin are as they were.
        Assert.Equal(Pace, match.BallVelocity.Z);
        Assert.Equal(new Vector2(10, 0), match.BallSpin);
    }

    [Fact]
    public void A_ball_that_still_has_bounce_in_it_does_not_roll()
    {
        Match match = TestRally.InFlight(new Vector3(0, -3.4f, 5), new Vector3(0, -6, Pace), tuning: WithGravity(20));

        match.Run(3);

        Assert.True(match.BallVelocity.Y > 4);
    }

    [Fact]
    public void Without_gravity_even_a_slow_touch_on_the_floor_is_a_bounce()
    {
        Match match = AboutToRoll(gravity: 0);
        bool bounced = false;

        for (int i = 0; i < 10; i++)
        {
            match.Step();
            bounced |= match.Events.HasFlag(TickEvents.WallBounce);
        }

        Assert.True(bounced);
        Assert.True(match.BallVelocity.Y > 0);
    }

    [Fact]
    public void An_upward_swipe_lifts_a_rolling_ball_off_the_floor()
    {
        // Rolling back towards the player, who swipes up through it.
        Tuning tuning = WithGravity(20) with { PaddleHalfLifeSeconds = 0 };
        Match match = TestRally.InFlight(new Vector3(0, -3.5f, 0.7f), new Vector3(0, 0, -Pace), tuning: tuning);
        match.PlayerPaddle = new Vector2(0, -3 - 12 * Tuning.TickSeconds);

        match.Step(new TickInput(new Vector2(0, -3), Serve: false));
        Assert.True(match.Events.HasFlag(TickEvents.PaddleHit));
        match.Run(10, new Vector2(0, -3));

        Assert.True(match.BallPosition.Y > -3.4f, $"ball at {match.BallPosition.Y}");
    }

    [Fact]
    public void Gravity_does_not_act_on_a_ball_waiting_on_the_paddle()
    {
        var match = new Match(WithGravity(20));

        match.Run(30);

        Assert.Equal(new Vector3(0, 0, match.Tuning.BallRadius), match.BallPosition);
        Assert.Equal(Vector3.Zero, match.BallVelocity);
    }
}
