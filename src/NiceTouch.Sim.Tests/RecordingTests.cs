using System.Numerics;

namespace NiceTouch.Sim.Tests;

// Determinism, and the recording that relies on it.
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
    public void A_saved_recording_replays_to_exactly_the_same_final_ball_position()
    {
        List<TickInput> inputs = ScriptedInputs(1800);
        Match played = Play(TestRally.Tuning(), inputs);
        string file = Recording.Save(played, inputs);

        Recording recording = Recording.Load(file);
        Match replayed = recording.Replay();

        Assert.Equal(played.BallPosition, recording.FinalBallPosition);
        Assert.Equal(recording.FinalBallPosition, replayed.BallPosition);
        AssertSameState(played, replayed);
    }

    [Fact]
    public void A_recording_carries_the_tuning_it_was_played_with()
    {
        // Not the defaults, and it changes how the rally goes.
        Tuning custom = TestRally.Tuning() with { BasePace = 31, SpinCurve = 0.002f, OpponentSpeed = 14 };
        List<TickInput> inputs = ScriptedInputs(900);
        Match played = Play(custom, inputs);

        Recording recording = Recording.Load(Recording.Save(played, inputs));

        Assert.Equal(custom, recording.Tuning);
        AssertSameState(played, recording.Replay());
        Assert.NotEqual(Play(TestRally.Tuning(), inputs).BallPosition, played.BallPosition);
    }

    [Fact]
    public void A_recording_holds_every_input()
    {
        List<TickInput> inputs = ScriptedInputs(300);
        Match played = Play(TestRally.Tuning(), inputs);

        Recording recording = Recording.Load(Recording.Save(played, inputs));

        Assert.Equal(inputs, recording.Inputs());
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
