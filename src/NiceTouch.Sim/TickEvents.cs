namespace NiceTouch.Sim;

// Things that happened in one tick. More than one can happen at once.
[Flags]
public enum TickEvents
{
    None = 0,
    WallBounce = 1,
    PaddleHit = 2,
    Miss = 4,
}
