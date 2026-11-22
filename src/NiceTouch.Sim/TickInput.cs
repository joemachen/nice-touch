using System.Numerics;

namespace NiceTouch.Sim;

// Everything the player does in one tick: where the mouse wants the paddle,
// in tunnel units, and whether the serve was clicked.
public readonly record struct TickInput(Vector2 PaddleTarget, bool Serve);
