using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiceTouch.Sim;

// A rally session as it is saved to a file: the tuning in use, every input
// since the session began, and where the ball ended up. The sim is
// deterministic, so replaying the inputs with the same tuning must arrive at
// the same final ball state. A recording that does not has caught a bug.
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

    // The text of a recording file for a match and the inputs that made it.
    public static string Save(Match match, IReadOnlyList<TickInput> inputs)
    {
        var recording = new Recording
        {
            Tuning = match.Tuning,
            Targets = inputs.SelectMany(i => new[] { i.PaddleTarget.X, i.PaddleTarget.Y }).ToArray(),
            ServeTicks = Enumerable.Range(0, inputs.Count).Where(i => inputs[i].Serve).ToArray(),
            FinalBallPosition = match.BallPosition,
            FinalBallVelocity = match.BallVelocity,
            FinalBallSpin = match.BallSpin,
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

    // Plays the recorded inputs through a fresh match.
    public Match Replay()
    {
        var match = new Match(Tuning);
        foreach (TickInput input in Inputs())
            match.Step(input);
        return match;
    }
}
