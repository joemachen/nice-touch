using System.Numerics;

namespace NiceTouch.Sim.Tests;

// Spec rules 3 and 4: spin bends the flight, and spin fades.
public class FlightTests
{
    private static readonly Vector3 MidTunnel = new(0, 0, 10);

    [Fact]
    public void A_ball_with_no_spin_flies_straight()
    {
        var velocity = new Vector3(1, -0.5f, 24);
        Match match = TestRally.InFlight(MidTunnel, velocity);

        match.Run(30);

        Vector3 expected = MidTunnel + velocity * 0.5f;
        Assert.Equal(expected.X, match.BallPosition.X, 4);
        Assert.Equal(expected.Y, match.BallPosition.Y, 4);
        Assert.Equal(expected.Z, match.BallPosition.Z, 3);
        Assert.Equal(velocity, match.BallVelocity);
    }

    // Side spin bends the ball sideways and top spin bends it up or down.
    // Top spin (positive x) on a ball going away dips, as it does in life.
    // The same spin bends the other way once the ball is coming back.
    [Theory]
    [InlineData(0, 10, +1, +1, 0)]
    [InlineData(0, -10, +1, -1, 0)]
    [InlineData(10, 0, +1, 0, -1)]
    [InlineData(-10, 0, +1, 0, +1)]
    [InlineData(0, 10, -1, -1, 0)]
    [InlineData(0, -10, -1, +1, 0)]
    [InlineData(10, 0, -1, 0, +1)]
    [InlineData(-10, 0, -1, 0, -1)]
    public void Spin_curves_the_ball_the_right_way_out_and_back(
        float spinX, float spinY, int travel, int expectedX, int expectedY)
    {
        Match match = TestRally.InFlight(new Vector3(0, 0, 15), new Vector3(0, 0, 24 * travel), new Vector2(spinX, spinY));

        match.Run(20);

        Assert.Equal(expectedX, Math.Sign(match.BallPosition.X));
        Assert.Equal(expectedY, Math.Sign(match.BallPosition.Y));
    }

    [Fact]
    public void A_diagonal_spin_curves_both_ways_at_once()
    {
        Match match = TestRally.InFlight(MidTunnel, new Vector3(0, 0, 24), new Vector2(10, 10));

        match.Run(20);

        Assert.True(match.BallPosition.X > 0.1f);
        Assert.True(match.BallPosition.Y < -0.1f);
    }

    [Fact]
    public void More_spin_curves_more()
    {
        Match gentle = TestRally.InFlight(MidTunnel, new Vector3(0, 0, 24), new Vector2(0, 5));
        Match heavy = TestRally.InFlight(MidTunnel, new Vector3(0, 0, 24), new Vector2(0, 10));

        gentle.Run(20);
        heavy.Run(20);

        Assert.True(gentle.BallPosition.X > 0);
        Assert.Equal(2 * gentle.BallPosition.X, heavy.BallPosition.X, 4);
    }

    [Fact]
    public void A_faster_ball_curves_along_the_same_shaped_path()
    {
        // Fading is switched off here, because it depends on time and the
        // rule is about the shape of the bend.
        Tuning tuning = TestRally.Tuning() with { SpinHalfLifeSeconds = 1e9f };
        var spin = new Vector2(0, 10);
        Match slow = TestRally.InFlight(MidTunnel, new Vector3(0, 0, 12), spin, tuning);
        Match fast = TestRally.InFlight(MidTunnel, new Vector3(0, 0, 24), spin, tuning);

        // Both travel the same 12 units down the tunnel.
        slow.Run(60);
        fast.Run(30);

        Assert.Equal(slow.BallPosition.Z, fast.BallPosition.Z, 3);
        Assert.True(slow.BallPosition.X > 0.5f);
        float difference = MathF.Abs(slow.BallPosition.X - fast.BallPosition.X);
        Assert.True(difference < 0.03f * slow.BallPosition.X, $"slow {slow.BallPosition.X}, fast {fast.BallPosition.X}");
    }

    [Fact]
    public void Flight_does_not_change_the_pace()
    {
        Match match = TestRally.InFlight(MidTunnel, new Vector3(0, 0, 24), new Vector2(8, -8));

        match.Run(20);

        Assert.Equal(24, match.BallVelocity.Z);
    }

    [Fact]
    public void Spin_halves_in_the_stated_time()
    {
        Tuning tuning = TestRally.Tuning() with { SpinHalfLifeSeconds = 2 };
        // A ball hanging still, so it spins for two seconds without
        // reaching anything.
        Match match = TestRally.InFlight(MidTunnel, Vector3.Zero, new Vector2(12, -20), tuning);

        match.Run(2 * Tuning.TicksPerSecond);

        Assert.Equal(6, match.BallSpin.X, 3);
        Assert.Equal(-10, match.BallSpin.Y, 3);
    }
}
