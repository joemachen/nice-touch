namespace NiceTouch.Sim;

// Where a rally session is in its cycle.
public enum Phase
{
    // The ball is on the player's paddle, waiting for the click.
    Serving,

    // The ball is in play.
    Rally,

    // Someone missed. Play stops for a moment before the next serve.
    PointOver,
}
