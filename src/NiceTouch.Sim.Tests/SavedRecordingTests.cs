namespace NiceTouch.Sim.Tests;

// Recordings saved from the real game with F9 and kept here, in the
// Recordings folder. Each must replay to exactly the ball state it ended
// with. To turn a bug report into a test, drop its recording in the folder.
//
// A recording holds the tuning it was played with but not the rules. If a
// rule is changed on purpose, an older recording stops being a true record:
// replace it with a fresh one from Joe. Do not loosen this test.
public class SavedRecordingTests
{
    private static string Folder => Path.Combine(AppContext.BaseDirectory, "Recordings");

    public static TheoryData<string> Files()
    {
        var files = new TheoryData<string>();
        foreach (string path in Directory.GetFiles(Folder, "*.json").Order())
            files.Add(Path.GetFileName(path));
        return files;
    }

    [Fact]
    public void There_is_at_least_one_saved_recording()
    {
        Assert.NotEmpty(Files());
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void A_recording_saved_from_the_game_replays_exactly(string file)
    {
        Recording recording = Recording.Load(File.ReadAllText(Path.Combine(Folder, file)));

        Match replayed = recording.Replay();

        Assert.Equal(recording.FinalBallPosition, replayed.BallPosition);
        Assert.Equal(recording.FinalBallVelocity, replayed.BallVelocity);
        Assert.Equal(recording.FinalBallSpin, replayed.BallSpin);
    }

    [Fact]
    public void Joes_first_recording_is_a_real_rally()
    {
        // 55 seconds from the M1a exit test: four serves, with rallies,
        // wall bounces and misses. Guards against the replay test passing
        // on a recording in which nothing happened.
        Recording recording = Recording.Load(File.ReadAllText(Path.Combine(Folder, "rally-20261121-201241.json")));
        var match = new Match(recording.Tuning);
        int hits = 0, bounces = 0, misses = 0;

        foreach (TickInput input in recording.Inputs())
        {
            match.Step(input);
            if (match.Events.HasFlag(TickEvents.PaddleHit)) hits++;
            if (match.Events.HasFlag(TickEvents.WallBounce)) bounces++;
            if (match.Events.HasFlag(TickEvents.Miss)) misses++;
        }

        Assert.Equal(3296, match.Tick);
        Assert.True(hits >= 15, $"{hits} paddle hits");
        Assert.True(bounces >= 5, $"{bounces} wall bounces");
        Assert.True(misses >= 3, $"{misses} misses");
    }
}
