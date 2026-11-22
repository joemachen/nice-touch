using System.Numerics;

namespace NiceTouch.Sim.Tests;

// The two facts the rally reports about the shot in flight, which scoring
// pays for: how far spin has bent it, and how many wall or floor bites have
// sped it up. Both are counted from the latest paddle hit.
public class ShotReadingTests
{
    private const float Pace = 24;
    private static readonly TickInput Click = new(Vector2.Zero, Serve: true);

    // Fading is switched off so a flight keeps the spin it was given.
    private static Tuning Tuning() => TestRally.Tuning() with { SpinHalfLifeSeconds = 1e9f };

    // A ball in the middle of the tunnel, a little way from the player.
    private static Match OpenFlight(Vector2 spin, Tuning? tuning = null) =>
        TestRally.InFlight(new Vector3(0, 0, 2), new Vector3(1, 0.5f, Pace), spin, tuning ?? Tuning());

    // A ball one tick away from a wall, heading into it. Which spin a wall
    // bite rewards is not repeated here: the tests try both and compare.
    private static Match AboutToBounce(Vector3 towardsWall, Vector2 spin, Tuning? tuning = null)
    {
        tuning ??= Tuning();
        float half = (towardsWall.X != 0 ? tuning.TunnelWidth : tuning.TunnelHeight) / 2;
        Vector3 position = towardsWall * (half - tuning.BallRadius - 0.05f) + new Vector3(0, 0, 15);
        return TestRally.InFlight(position, towardsWall * 6 + new Vector3(0, 0, Pace), spin, tuning);
    }

    public static TheoryData<float, float, float, float> WallsAndTheSpinThatBitesThem() => new()
    {
        // Towards the wall (x, y), then the spin that bites it (top, side).
        { 1, 0, 0, 10 },
        { -1, 0, 0, 10 },
        { 0, -1, 10, 0 },
        { 0, 1, 10, 0 },
    };

    [Fact]
    public void A_ball_with_no_spin_has_no_bend()
    {
        Match match = OpenFlight(Vector2.Zero);

        match.Run(40);

        Assert.Equal(0, match.ShotBend);
    }

    [Fact]
    public void Bend_is_how_far_spin_has_carried_the_ball_off_a_straight_line()
    {
        Match match = OpenFlight(new Vector2(4, 6));
        Vector3 start = match.BallPosition;
        Vector3 velocity = match.BallVelocity;

        match.Run(40);

        Vector3 straight = start + velocity * (40 * NiceTouch.Sim.Tuning.TickSeconds);
        float offLine = (match.BallPosition - straight).Length();
        Assert.True(offLine > 0.5f, $"only {offLine} off the line");
        Assert.Equal(offLine, match.ShotBend, 3);
    }

    [Fact]
    public void More_spin_is_more_bend()
    {
        Match gentle = OpenFlight(new Vector2(0, 3));
        Match heavy = OpenFlight(new Vector2(0, 6));

        gentle.Run(40);
        heavy.Run(40);

        Assert.Equal(2 * gentle.ShotBend, heavy.ShotBend, 3);
    }

    [Fact]
    public void Gravitys_drop_is_not_bend()
    {
        // Long enough to fall to the floor and bounce.
        Match match = OpenFlight(Vector2.Zero, Tuning() with { Gravity = 20 });

        match.Run(60);

        Assert.NotEqual(0.5f, match.BallVelocity.Y);
        Assert.Equal(0, match.ShotBend);
    }

    [Fact]
    public void Gravity_adds_nothing_to_the_bend_of_a_spinning_ball()
    {
        Match weightless = OpenFlight(new Vector2(0, 6));
        Match heavy = OpenFlight(new Vector2(0, 6), Tuning() with { Gravity = 7 });

        weightless.Run(40);
        heavy.Run(40);

        Assert.True(heavy.BallPosition.Y < weightless.BallPosition.Y - 1);
        Assert.True(weightless.ShotBend > 0.5f);
        Assert.Equal(weightless.ShotBend, heavy.ShotBend);
    }

    [Fact]
    public void A_wall_bounce_does_not_undo_the_bend()
    {
        // Spin goes on pushing the same way after the bounce, so the
        // reading goes on growing.
        Match match = AboutToBounce(Vector3.UnitX, new Vector2(0, 10));
        match.Run(5);
        float before = match.ShotBend;

        match.Run(5);

        Assert.True(match.ShotBend > before);
    }

    [Theory]
    [MemberData(nameof(WallsAndTheSpinThatBitesThem))]
    public void A_bite_that_speeds_the_ball_up_is_a_kick_and_one_that_slows_it_is_not(float x, float y, float top, float side)
    {
        var towardsWall = new Vector3(x, y, 0);
        Match one = AboutToBounce(towardsWall, new Vector2(top, side));
        Match other = AboutToBounce(towardsWall, -new Vector2(top, side));

        one.Step();
        other.Step();

        Assert.True(one.Events.HasFlag(TickEvents.WallBounce));
        Assert.True(other.Events.HasFlag(TickEvents.WallBounce));
        Match faster = one.BallVelocity.Z > other.BallVelocity.Z ? one : other;
        Match slower = faster == one ? other : one;
        Assert.True(faster.BallVelocity.Z > Pace);
        Assert.True(slower.BallVelocity.Z < Pace);
        Assert.Equal(1, faster.ShotKicks);
        Assert.Equal(0, slower.ShotKicks);
    }

    [Fact]
    public void A_clean_bounce_is_not_a_kick()
    {
        Match match = AboutToBounce(Vector3.UnitX, Vector2.Zero);

        match.Step();

        Assert.True(match.Events.HasFlag(TickEvents.WallBounce));
        Assert.Equal(0, match.ShotKicks);
    }

    [Fact]
    public void A_bite_that_cannot_make_the_ball_any_faster_is_not_a_kick()
    {
        // The ball is already at the fastest pace allowed.
        Tuning capped = Tuning() with { PaceMax = 1 };
        Match one = AboutToBounce(Vector3.UnitX, new Vector2(0, 10), capped);
        Match other = AboutToBounce(Vector3.UnitX, new Vector2(0, -10), capped);

        one.Step();
        other.Step();

        Assert.Equal(0, one.ShotKicks);
        Assert.Equal(0, other.ShotKicks);
    }

    [Fact]
    public void Every_kick_on_the_way_is_counted()
    {
        // Heavy side spin with nothing to stop it: it curves into a side
        // wall and keeps coming back to it.
        Tuning tuning = Tuning() with { TunnelDepth = 200, OpponentSpeed = 0 };
        Match match = TestRally.InFlight(new Vector3(0, 0, 2), new Vector3(0, 0, Pace), new Vector2(0, 30), tuning);
        int kicks = 0;

        for (int i = 0; i < 240 && match.Phase == Phase.Rally; i++)
        {
            float before = match.BallVelocity.Z;
            match.Step();
            if (match.Events.HasFlag(TickEvents.WallBounce) && match.BallVelocity.Z > before)
                kicks++;
        }

        Assert.True(kicks >= 2, $"{kicks} kicks");
        Assert.Equal(kicks, match.ShotKicks);
    }

    [Fact]
    public void A_paddle_hit_starts_both_readings_again()
    {
        // Kicks off the right wall, then reaches the opponent, who has not
        // had to move far.
        Tuning tuning = Tuning() with { OpponentSpeed = 50 };
        Match match = AboutToBounce(Vector3.UnitX, new Vector2(0, 10), tuning);
        match.Step();
        Assert.Equal(1, match.ShotKicks);
        Assert.True(match.ShotBend > 0);

        while (!match.Events.HasFlag(TickEvents.PaddleHit))
        {
            Assert.Equal(Phase.Rally, match.Phase);
            match.Step();
        }

        Assert.Equal(0, match.ShotKicks);
        Assert.Equal(0, match.ShotBend);
    }

    [Fact]
    public void The_readings_stay_on_record_after_a_miss_until_the_next_serve()
    {
        // The opponent stays in the middle and the ball goes by wide.
        Tuning tuning = Tuning() with { OpponentSpeed = 0, PaddleHalfLifeSeconds = 0 };
        Match match = AboutToBounce(Vector3.UnitX, new Vector2(0, 10), tuning);
        while (match.Phase == Phase.Rally)
            match.Step();
        Assert.Equal(Side.Opponent, match.LastMiss);
        float bend = match.ShotBend;
        Assert.True(bend > 0);
        Assert.Equal(1, match.ShotKicks);

        // Through the pause and on to the serve.
        match.Run(NiceTouch.Sim.Tuning.TicksPerSecond);
        Assert.Equal(Phase.Serving, match.Phase);
        Assert.Equal(bend, match.ShotBend);
        Assert.Equal(1, match.ShotKicks);

        match.Step(Click);
        Assert.Equal(0, match.ShotBend);
        Assert.Equal(0, match.ShotKicks);
    }
}
