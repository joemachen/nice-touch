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

    // Tunnel size in world units.
    public float TunnelWidth { get; set; } = 12;
    public float TunnelHeight { get; set; } = 8;
    public float TunnelDepth { get; set; } = 30;

    // How sharply spin bends the path. The curvature of the path is this
    // times the spin, whatever the pace, so a faster ball follows the same
    // curve. Zero switches the bend off.
    public float SpinCurve { get; set; } = 0.001f;

    // Spin loses half its strength in this many seconds.
    public float SpinHalfLifeSeconds { get; set; } = 3;
}
