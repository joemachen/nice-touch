using System.Numerics;

namespace NiceTouch.Sim.Tests;

// Spec rule 5: spin bites the walls. A ball with no spin bounces cleanly.
public class WallTests
{
    public enum Wall { Left, Right, Floor, Ceiling }

    private const float Pace = 24;

    // Fading is switched off so the tests can state spin exactly.
    private static Tuning Tuning() => TestRally.Tuning() with { SpinHalfLifeSeconds = 1e9f };

    // The direction from the middle of the tunnel towards a wall.
    private static Vector3 Towards(Wall wall) => wall switch
    {
        Wall.Left => -Vector3.UnitX,
        Wall.Right => Vector3.UnitX,
        Wall.Floor => -Vector3.UnitY,
        _ => Vector3.UnitY,
    };

    private static bool IsSideWall(Wall wall) => wall is Wall.Left or Wall.Right;

    // A ball one tick away from touching the wall, heading into it.
    private static Match AboutToBounce(Wall wall, int travel, Vector2 spin, Tuning? tuning = null)
    {
        tuning ??= Tuning();
        float half = (IsSideWall(wall) ? tuning.TunnelWidth : tuning.TunnelHeight) / 2;
        Vector3 position = Towards(wall) * (half - tuning.BallRadius - 0.05f) + new Vector3(0, 0, 15);
        Vector3 velocity = Towards(wall) * 6 + new Vector3(0, 0, Pace * travel);
        return TestRally.InFlight(position, velocity, spin, tuning);
    }

    // Finds the spin that bends the flight towards the wall by trying each
    // kind in open flight, so these tests do not repeat the flight formula.
    private static Vector2 SpinThatCurvesInto(Wall wall, int travel)
    {
        Vector2[] candidates = [new(10, 0), new(-10, 0), new(0, 10), new(0, -10)];
        foreach (Vector2 spin in candidates)
        {
            Match match = TestRally.InFlight(new Vector3(0, 0, 15), new Vector3(0, 0, Pace * travel), spin, Tuning());
            match.Step();
            if (Vector3.Dot(match.BallVelocity, Towards(wall)) > 0)
                return spin;
        }
        throw new InvalidOperationException("no spin curves that way");
    }

    public static TheoryData<Wall, int> EveryWallBothWays()
    {
        var data = new TheoryData<Wall, int>();
        foreach (Wall wall in Enum.GetValues<Wall>())
        {
            data.Add(wall, +1);
            data.Add(wall, -1);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryWallBothWays))]
    public void A_ball_with_no_spin_bounces_cleanly(Wall wall, int travel)
    {
        Match match = AboutToBounce(wall, travel, Vector2.Zero);
        Vector3 before = match.BallVelocity;

        match.Step();

        Assert.True(match.Events.HasFlag(TickEvents.WallBounce));
        // The speed into the wall is turned round and nothing else changes.
        Assert.Equal(Vector3.Reflect(before, Towards(wall)), match.BallVelocity);
        Assert.Equal(Vector2.Zero, match.BallSpin);
    }

    [Theory]
    [MemberData(nameof(EveryWallBothWays))]
    public void The_ball_stays_inside_the_tunnel(Wall wall, int travel)
    {
        Match match = AboutToBounce(wall, travel, Vector2.Zero);

        match.Step();

        Tuning t = match.Tuning;
        Assert.True(MathF.Abs(match.BallPosition.X) <= t.TunnelWidth / 2 - t.BallRadius);
        Assert.True(MathF.Abs(match.BallPosition.Y) <= t.TunnelHeight / 2 - t.BallRadius);
    }

    [Fact]
    public void No_bounce_is_reported_in_open_flight()
    {
        Match match = TestRally.InFlight(new Vector3(0, 0, 15), new Vector3(1, 1, Pace));

        match.Step();

        Assert.False(match.Events.HasFlag(TickEvents.WallBounce));
    }

    [Theory]
    [MemberData(nameof(EveryWallBothWays))]
    public void A_ball_that_curved_into_the_wall_comes_off_faster(Wall wall, int travel)
    {
        Match match = AboutToBounce(wall, travel, SpinThatCurvesInto(wall, travel));

        match.Step();

        // Faster in the direction it was already travelling.
        Assert.Equal(travel, Math.Sign(match.BallVelocity.Z));
        Assert.True(MathF.Abs(match.BallVelocity.Z) > Pace + 1);
    }

    [Theory]
    [MemberData(nameof(EveryWallBothWays))]
    public void A_ball_spinning_the_other_way_checks_up_and_comes_off_slower(Wall wall, int travel)
    {
        Match match = AboutToBounce(wall, travel, -SpinThatCurvesInto(wall, travel));

        match.Step();

        Assert.Equal(travel, Math.Sign(match.BallVelocity.Z));
        Assert.True(MathF.Abs(match.BallVelocity.Z) < Pace - 1);
    }

    [Theory]
    [MemberData(nameof(EveryWallBothWays))]
    public void The_bite_never_takes_the_pace_past_its_limits(Wall wall, int travel)
    {
        // The bend is switched off so that spin this heavy cannot steer the
        // ball away from the wall before it gets there.
        Tuning noBend = Tuning() with { SpinCurve = 0 };
        Vector2 hugeSpin = SpinThatCurvesInto(wall, travel) * 1000;
        Match fast = AboutToBounce(wall, travel, hugeSpin, noBend);
        Match slow = AboutToBounce(wall, travel, -hugeSpin, noBend);

        fast.Step();
        slow.Step();

        Tuning t = fast.Tuning;
        Assert.Equal(travel * t.BasePace * t.PaceMax, fast.BallVelocity.Z, 3);
        Assert.Equal(travel * t.BasePace * t.PaceMin, slow.BallVelocity.Z, 3);
    }

    [Theory]
    [MemberData(nameof(EveryWallBothWays))]
    public void The_bite_uses_up_some_of_the_spin_that_bit(Wall wall, int travel)
    {
        var spin = new Vector2(8, 10);
        Match match = AboutToBounce(wall, travel, spin);

        match.Step();

        float kept = 1 - match.Tuning.WallSpinLoss;
        Vector2 expected = IsSideWall(wall) ? new Vector2(spin.X, spin.Y * kept) : new Vector2(spin.X * kept, spin.Y);
        Assert.Equal(expected.X, match.BallSpin.X, 4);
        Assert.Equal(expected.Y, match.BallSpin.Y, 4);
    }

    [Theory]
    [InlineData(Wall.Left)]
    [InlineData(Wall.Right)]
    [InlineData(Wall.Floor)]
    [InlineData(Wall.Ceiling)]
    public void Side_spin_bites_only_side_walls_and_top_spin_only_floor_and_ceiling(Wall wall)
    {
        // The kind of spin that does not belong to this wall.
        Vector2 spin = IsSideWall(wall) ? new Vector2(10, 0) : new Vector2(0, 10);
        Match match = AboutToBounce(wall, +1, spin);

        match.Step();

        Assert.True(match.Events.HasFlag(TickEvents.WallBounce));
        Assert.Equal(Pace, match.BallVelocity.Z);
        Assert.Equal(spin, match.BallSpin);
    }

    [Fact]
    public void The_change_of_pace_lasts_after_the_bounce()
    {
        Match match = AboutToBounce(Wall.Right, +1, SpinThatCurvesInto(Wall.Right, +1));
        match.Step();
        float paceOffTheWall = match.BallVelocity.Z;

        match.Run(5);

        Assert.Equal(paceOffTheWall, match.BallVelocity.Z);
    }

    [Fact]
    public void With_wall_grip_at_zero_a_spinning_ball_keeps_its_pace()
    {
        Tuning tuning = Tuning() with { WallGrip = 0 };
        Match match = AboutToBounce(Wall.Right, +1, new Vector2(0, 30), tuning);

        match.Step();

        Assert.Equal(Pace, match.BallVelocity.Z);
    }
}
