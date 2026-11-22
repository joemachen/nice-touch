using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiceTouch.Sim;

// A game as it is saved to a file: the tuning in use, every input since the
// game began, and how it stood at the end. The sim is deterministic, so
// replaying the inputs with the same tuning must arrive at the same score,
// lives, level and ball. A recording that does not has caught a bug.
//
// Recordings saved before there was a game (M1a) hold a rally session with
// no score, lives or level. They still load, and Replay plays them.
public sealed class Recording
{
    private static readonly JsonSerializerOptions Json = new()
    {
        // Vector2 and Vector3 keep their numbers in fields.
        IncludeFields = true,
        // So a rally that has gone wrong can still be saved and looked at.
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public required Tuning Tuning { get; init; }

    // The paddle target for every tick in order: x, y, x, y and so on.
    public required float[] Targets { get; init; }

    // The ticks, counted from zero, on which serve was clicked.
    public required int[] ServeTicks { get; init; }

    public required Vector3 FinalBallPosition { get; init; }
    public required Vector3 FinalBallVelocity { get; init; }
    public required Vector2 FinalBallSpin { get; init; }

    // How the game stood. A rally recording from M1a has none of these.
    public int? FinalScore { get; init; }
    public int? FinalLives { get; init; }
    public int? FinalLevel { get; init; }

    // The text of a recording file for a game and the inputs that made it.
    public static string Save(Game game, IReadOnlyList<TickInput> inputs)
    {
        var recording = new Recording
        {
            Tuning = game.Tuning,
            Targets = inputs.SelectMany(i => new[] { i.PaddleTarget.X, i.PaddleTarget.Y }).ToArray(),
            ServeTicks = Enumerable.Range(0, inputs.Count).Where(i => inputs[i].Serve).ToArray(),
            FinalBallPosition = game.Match.BallPosition,
            FinalBallVelocity = game.Match.BallVelocity,
            FinalBallSpin = game.Match.BallSpin,
            FinalScore = game.Score,
            FinalLives = game.PlayerLives,
            FinalLevel = game.Level,
        };
        return JsonSerializer.Serialize(recording, Json);
    }

    public static Recording Load(string text) =>
        JsonSerializer.Deserialize<Recording>(text, Json) ?? throw new JsonException("The recording is empty.");

    public List<TickInput> Inputs()
    {
        var serves = new HashSet<int>(ServeTicks);
        var inputs = new List<TickInput>(Targets.Length / 2);
        for (int tick = 0; tick < Targets.Length / 2; tick++)
            inputs.Add(new TickInput(new Vector2(Targets[2 * tick], Targets[2 * tick + 1]), serves.Contains(tick)));
        return inputs;
    }

    // Plays the recorded inputs through a fresh game.
    public Game ReplayGame()
    {
        var game = new Game(Tuning);
        foreach (TickInput input in Inputs())
            game.Step(input);
        return game;
    }

    // Plays the inputs of an M1a rally recording through a fresh rally with
    // no game around it, which is how it was recorded.
    public Match Replay()
    {
        var match = new Match(Tuning);
        foreach (TickInput input in Inputs())
            match.Step(input);
        return match;
    }
}
