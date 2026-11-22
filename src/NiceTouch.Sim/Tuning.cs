using System.Text.Json;

namespace NiceTouch.Sim;

// The one place tuning values live until the run arrives (DESIGN.md,
// "One place for tuning"). The defaults written here are the game's tuning.
// The tuning panel edits a copy, and a record compares by value, so "is
// anything different from the defaults" is one comparison.
// Speeds are per second, never per tick. Values that fade are half-lives.
public sealed record Tuning
{
    // The tick rate is fixed, not tuned: recordings depend on it.
    public const int TicksPerSecond = 60;
    public const float TickSeconds = 1f / TicksPerSecond;

    private static readonly JsonSerializerOptions Readable = new() { WriteIndented = true };

    // As text, for copying out of the tuning panel and for remembering
    // values between runs. A value the text does not name keeps its default.
    public string ToJson() => JsonSerializer.Serialize(this, Readable);

    public static Tuning FromJson(string text) => JsonSerializer.Deserialize<Tuning>(text) ?? new Tuning();

    // Tunnel size in world units.
    public float TunnelWidth { get; set; } = 12;
    public float TunnelHeight { get; set; } = 8;
    public float TunnelDepth { get; set; } = 30;

    public float BallRadius { get; set; } = 0.5f;

    // Both paddles are the same size.
    public float PaddleWidth { get; set; } = 3;
    public float PaddleHeight { get; set; } = 2;

    // The player's paddle closes half the gap to the mouse in this many
    // seconds. Zero means no smoothing at all.
    public float PaddleHalfLifeSeconds { get; set; } = 0.01f;

    // How fast the opponent's paddle can move. This is how quickly it reacts.
    public float OpponentSpeed { get; set; } = 5;

    // Speed down the tunnel off a paddle. Only a wall bite changes it.
    public float BasePace { get; set; } = 24;

    // How long play stops after a miss before the ball is back on the
    // player's paddle.
    public float MissPauseSeconds { get; set; } = 1;

    // Spin a paddle rubs onto the ball, in radians per second for each
    // unit per second of sideways paddle speed, and the most spin the ball
    // can carry.
    public float SwipeSpin { get; set; } = 1;
    public float MaxSpin { get; set; } = 40;

    // The share of the paddle's sideways speed that the ball leaves with.
    public float SwipePush { get; set; } = 0.35f;

    // The shares of its sideways speed and of its spin that the ball keeps
    // through a paddle hit.
    public float LateralKeep { get; set; } = 0.5f;
    public float SpinKeep { get; set; } = 0.5f;

    // How hard a spinning ball jumps sideways off a paddle. Zero switches
    // the jump off.
    public float PaddleGrip { get; set; } = 0.5f;

    // How sharply spin bends the path. The curvature of the path is this
    // times the spin, whatever the pace, so a faster ball follows the same
    // curve. Zero switches the bend off.
    public float SpinCurve { get; set; } = 0.0012f;

    // Spin loses half its strength in this many seconds.
    public float SpinHalfLifeSeconds { get; set; } = 3;

    // How hard spin kicks the ball's pace when it bites a wall. Zero
    // switches the kick off.
    public float WallGrip { get; set; } = 0.4f;

    // The share of the biting spin a wall bounce uses up, from 0 to 1.
    public float WallSpinLoss { get; set; } = 0.4f;

    // Limits on pace after a wall bite, as multiples of BasePace.
    public float PaceMin { get; set; } = 0.6f;
    public float PaceMax { get; set; } = 1.6f;
}
