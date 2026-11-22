namespace NiceTouch.Sim;

// Where a game is. The rally inside it has its own Phase.
public enum GamePhase
{
    // A level is being played: serves, rallies and the pauses after misses.
    Playing,

    // The opponent's last life is gone. The next level starts when the
    // pause after that miss is over.
    LevelClear,

    // The player's last life is gone.
    GameOver,

    // The last level is cleared.
    Won,
}
