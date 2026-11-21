namespace NiceTouch.Sim;

// The one place tuning values live until the run arrives (DESIGN.md,
// "One place for tuning"). Speeds are per second, never per tick.
public static class Tuning
{
    public const int TicksPerSecond = 60;

    // Tunnel size in world units. Placeholder numbers until M1a tunes them.
    public const float TunnelWidth = 12;
    public const float TunnelHeight = 8;
    public const float TunnelDepth = 30;
}
