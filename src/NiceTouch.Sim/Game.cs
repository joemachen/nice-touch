using RallyPhase = NiceTouch.Sim.Phase;

namespace NiceTouch.Sim;

// The game around the rally: lives, a ladder of levels, the score and the
// two endings. It owns the rally, one Match for each level, and steps it.
// How the ball and the paddles behave is the Match's business and is not
// touched here.
public sealed class Game
{
    public Game(Tuning tuning)
    {
        // A copy, so nothing outside can change the rules of a running game.
        Tuning = tuning with { };
        Match = StartGame();
    }

    // The game's tuning. Each level's rally has its own, made from this.
    public Tuning Tuning { get; }

    // The rally of the level being played.
    public Match Match { get; private set; }

    // Ticks since this game began.
    public long Tick { get; private set; }

    public GamePhase Phase { get; private set; }
    public int Level { get; private set; }
    public int Score { get; private set; }
    public int PlayerLives { get; private set; }
    public int OpponentLives { get; private set; }

    // What the latest point paid, if the player won it. It stays on show
    // from the opponent's miss until the next serve.
    public Winner? Winner { get; private set; }

    // What happened during the latest tick, in the rally and in the game.
    public TickEvents Events { get; private set; }

    public void Step(TickInput input)
    {
        // Only a level in play can be served in. The paddles still follow
        // the mouse whatever the game is doing.
        Match.Step(Phase == GamePhase.Playing ? input : input with { Serve = false });
        Events = Match.Events;

        switch (Phase)
        {
            case GamePhase.Playing:
                if (Events.HasFlag(TickEvents.Miss))
                    SettlePoint();
                else if (Match.Phase == RallyPhase.Rally)
                    Winner = null;
                break;

            case GamePhase.LevelClear:
                // The pause after the miss that cleared the level is over.
                if (Match.Phase == RallyPhase.Serving)
                    StartLevel(Level + 1);
                break;

            default:
                // The game has ended. Once the pause after the last miss is
                // over, a click starts a new game, exactly as a fresh one
                // would start. It takes another click to serve.
                if (Match.Phase == RallyPhase.Serving && input.Serve)
                {
                    Match = StartGame();
                    Events = TickEvents.NewGame;
                    return;
                }
                break;
        }

        Tick++;
    }

    private Match StartGame()
    {
        Tick = 0;
        Phase = GamePhase.Playing;
        Level = 1;
        Score = 0;
        PlayerLives = Tuning.PlayerLives;
        OpponentLives = Tuning.OpponentLives;
        Winner = null;
        return new Match(Tuning.ForLevel(1));
    }

    private void StartLevel(int level)
    {
        Phase = GamePhase.Playing;
        Level = level;
        OpponentLives = Tuning.OpponentLives;
        Match = new Match(Tuning.ForLevel(level), Match);
    }

    // A miss costs whoever missed one life. The player's miss pays nothing.
    // The opponent's is a winner, and its last life clears the level.
    private void SettlePoint()
    {
        if (Match.LastMiss == Side.Player)
        {
            if (--PlayerLives <= 0)
                Phase = GamePhase.GameOver;
            return;
        }

        Winner winner = Winner.Pay(Tuning, Level, Match.RallyLength, Match.ShotBend, Match.ShotKicks);
        if (--OpponentLives <= 0)
        {
            winner = winner with { LevelBonus = Tuning.LevelBonus * Level };
            PlayerLives = Math.Min(PlayerLives + Tuning.LivesBack, Tuning.PlayerLives);
            Phase = Level >= Tuning.Levels ? GamePhase.Won : GamePhase.LevelClear;
        }

        Score += winner.Total + winner.LevelBonus;
        Winner = winner;
        if (winner.NiceTouch)
            Events |= TickEvents.NiceTouch;
    }
}
