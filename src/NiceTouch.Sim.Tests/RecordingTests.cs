using System.Numerics;

namespace NiceTouch.Sim.Tests;

// The rally is deterministic, and tuning values survive being written as
// text. Saving and replaying whole games is in GameRecordingTests, and the
// recording saved from the real game in M1a is in SavedRecordingTests.
public class RecordingTests
{
    // A few seconds of made-up play: the mouse wanders, sweeps hard now and
    // then, and clicks often enough to serve again after every miss.
    private static List<TickInput> ScriptedInputs(int ticks)
    {
        var inputs = new List<TickInput>();
        for (int i = 0; i < ticks; i++)
        {
            float t = i * Tuning.TickSeconds;
            var target = new Vector2(5 * MathF.Sin(t * 2.3f) + MathF.Sin(t * 17), 3 * MathF.Cos(t * 1.7f));
            inputs.Add(new TickInput(target, Serve: i % 45 == 10));
        }
        return inputs;
    }

    private static Match Play(Tuning tuning, List<TickInput> inputs)
    {
        var match = new Match(tuning);
        foreach (TickInput input in inputs)
            match.Step(input);
        return match;
    }

    private static void AssertSameState(Match expected, Match actual)
    {
        Assert.Equal(expected.Tick, actual.Tick);
        Assert.Equal(expected.Phase, actual.Phase);
        Assert.Equal(expected.BallPosition, actual.BallPosition);
        Assert.Equal(expected.BallVelocity, actual.BallVelocity);
        Assert.Equal(expected.BallSpin, actual.BallSpin);
        Assert.Equal(expected.PlayerPaddle, actual.PlayerPaddle);
        Assert.Equal(expected.OpponentPaddle, actual.OpponentPaddle);
        Assert.Equal(expected.RallyLength, actual.RallyLength);
        Assert.Equal(expected.LastMiss, actual.LastMiss);
    }

    [Fact]
    public void The_scripted_play_really_is_a_rally()
    {
        // Guards the tests below: they would prove nothing if the ball
        // never left the paddle.
        var match = new Match(TestRally.Tuning());
        int hits = 0, bounces = 0, misses = 0;
        foreach (TickInput input in ScriptedInputs(1800))
        {
            match.Step(input);
            if (match.Events.HasFlag(TickEvents.PaddleHit)) hits++;
            if (match.Events.HasFlag(TickEvents.WallBounce)) bounces++;
            if (match.Events.HasFlag(TickEvents.Miss)) misses++;
        }

        Assert.True(hits >= 5, $"{hits} paddle hits");
        Assert.True(bounces >= 1, $"{bounces} wall bounces");
        Assert.True(misses >= 1, $"{misses} misses");
    }

    [Fact]
    public void The_same_inputs_played_twice_give_exactly_the_same_rally()
    {
        List<TickInput> inputs = ScriptedInputs(1800);

        Match first = Play(TestRally.Tuning(), inputs);
        Match second = Play(TestRally.Tuning(), inputs);

        AssertSameState(first, second);
    }

    [Fact]
    public void Tuning_values_survive_being_copied_out_as_text_and_read_back()
    {
        Tuning custom = TestRally.Tuning() with { WallGrip = 0.123f, MaxSpin = 55.5f };

        Assert.Equal(custom, Tuning.FromJson(custom.ToJson()));
    }

    [Fact]
    public void Text_that_names_only_some_values_leaves_the_rest_at_their_defaults()
    {
        Tuning read = Tuning.FromJson("""{ "BasePace": 30 }""");

        Assert.Equal(new Tuning { BasePace = 30 }, read);
    }
}
