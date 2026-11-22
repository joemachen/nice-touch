namespace NiceTouch.Sim.Tests;

// How levels get harder: each one adds a step to the opponent's speed and
// to the ball's pace.
public class LevelTests
{
    [Fact]
    public void Level_1_is_the_tuning_as_written()
    {
        Tuning tuning = TestRally.Tuning();

        Assert.Equal(tuning, tuning.ForLevel(1));
    }

    [Theory]
    [InlineData(2, 6, 25)]
    [InlineData(3, 7, 26)]
    [InlineData(6, 10, 29)]
    public void Each_level_after_adds_one_step_of_speed_and_one_of_pace(int level, float speed, float pace)
    {
        Tuning tuning = TestRally.Tuning() with { OpponentSpeed = 5, BasePace = 24, OpponentSpeedStep = 1, PaceStep = 1 };

        Tuning forLevel = tuning.ForLevel(level);

        Assert.Equal(speed, forLevel.OpponentSpeed);
        Assert.Equal(pace, forLevel.BasePace);
        // Nothing else about the rally changes.
        Assert.Equal(tuning, forLevel with { OpponentSpeed = 5, BasePace = 24 });
    }

    [Fact]
    public void The_steps_are_tuning_values()
    {
        Tuning tuning = TestRally.Tuning() with { OpponentSpeed = 5, BasePace = 24, OpponentSpeedStep = 2, PaceStep = 0.5f };

        Tuning level4 = tuning.ForLevel(4);

        Assert.Equal(11, level4.OpponentSpeed);
        Assert.Equal(25.5f, level4.BasePace);
    }
}
