using System.Numerics;

namespace NiceTouch.Sim.Tests;

public class MatchTests
{
    [Fact]
    public void Step_moves_the_ball_by_its_speed_for_one_tick()
    {
        // Speeds are per second, so one tick moves the ball 1/60 of them.
        var match = new Match(TestRally.Tuning())
        {
            BallPosition = new Vector3(1, 2, 3),
            BallVelocity = new Vector3(60, -120, 30),
        };

        match.Step();

        Assert.Equal(new Vector3(2, 0, 3.5f), match.BallPosition);
        Assert.Equal(1, match.Tick);
    }
}
