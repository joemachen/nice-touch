namespace NiceTouch.Sim;

// Things that happened in one tick. More than one can happen at once.
[Flags]
public enum TickEvents
{
    None = 0,
    WallBounce = 1,
    PaddleHit = 2,
    Miss = 4,

    // Raised by the game around the rally, never by a rally on its own.
    // A winner with enough style to be called out.
    NiceTouch = 8,

    // A click after the game had ended started a new one.
    NewGame = 16,
}
