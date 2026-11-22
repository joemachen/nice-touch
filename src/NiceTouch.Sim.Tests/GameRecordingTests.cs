using System.Numerics;

namespace NiceTouch.Sim.Tests;

// A whole game is as repeatable as a rally, and a recording of one replays
// to the same score, lives, level and ball.
public class GameRecordingTests
{
    // Three minutes of play.
    private const int Ticks = 3 * 60 * NiceTouch.Sim.Tuning.TicksPerSecond;

    private static Tuning Tuning() => TestRally.Tuning() with { OpponentSpeed = 5, Gravity = 7 };

    // What a stand-in player did, and the game it left behind.
    private sealed record Played(Game Game, List<TickInput> Inputs, int Winners, int LivesLost, int LevelsCleared, int NewGames);

    // The stand-in keeps the paddle near the ball with a wobble, which
    // swipes some shots and misses others, and clicks three times a second.
    // Like the real recorder it keeps only the inputs of the game in hand.
    private static Played Play(Tuning tuning, int ticks = Ticks)
    {
        var game = new Game(tuning);
        var inputs = new List<TickInput>();
        int winners = 0, livesLost = 0, levelsCleared = 0, newGames = 0;

        for (int i = 0; i < ticks; i++)
        {
            float t = i * NiceTouch.Sim.Tuning.TickSeconds;
            Vector3 ball = game.Match.BallPosition;
            var target = new Vector2(ball.X + 2.2f * MathF.Sin(t * 13), ball.Y + 1.2f * MathF.Cos(t * 11));
            var input = new TickInput(target, Serve: i % 20 == 0);
            int level = game.Level;

            inputs.Add(input);
            game.Step(input);

            if (game.Events.HasFlag(TickEvents.Miss) && game.Match.LastMiss == Side.Opponent) winners++;
            if (game.Events.HasFlag(TickEvents.Miss) && game.Match.LastMiss == Side.Player) livesLost++;
            if (game.Level > level) levelsCleared++;
            if (game.Events.HasFlag(TickEvents.NewGame))
            {
                newGames++;
                inputs.Clear();
            }
        }

        return new Played(game, inputs, winners, livesLost, levelsCleared, newGames);
    }

    private static Game Replay(Tuning tuning, List<TickInput> inputs)
    {
        var game = new Game(tuning);
        foreach (TickInput input in inputs)
            game.Step(input);
        return game;
    }

    private static void AssertSameGame(Game expected, Game actual)
    {
        Assert.Equal(expected.Tick, actual.Tick);
        Assert.Equal(expected.Phase, actual.Phase);
        Assert.Equal(expected.Score, actual.Score);
        Assert.Equal(expected.Level, actual.Level);
        Assert.Equal(expected.PlayerLives, actual.PlayerLives);
        Assert.Equal(expected.OpponentLives, actual.OpponentLives);
        Assert.Equal(expected.Winner, actual.Winner);
        Assert.Equal(expected.Match.Tuning, actual.Match.Tuning);
        Assert.Equal(expected.Match.Phase, actual.Match.Phase);
        Assert.Equal(expected.Match.BallPosition, actual.Match.BallPosition);
        Assert.Equal(expected.Match.BallVelocity, actual.Match.BallVelocity);
        Assert.Equal(expected.Match.BallSpin, actual.Match.BallSpin);
        Assert.Equal(expected.Match.PlayerPaddle, actual.Match.PlayerPaddle);
        Assert.Equal(expected.Match.OpponentPaddle, actual.Match.OpponentPaddle);
        Assert.Equal(expected.Match.RallyLength, actual.Match.RallyLength);
    }

    [Fact]
    public void The_stand_in_player_really_plays_games()
    {
        // Guards the tests below: they would prove nothing if no point was
        // ever won or lost.
        Played played = Play(Tuning());

        Assert.True(played.Winners >= 10, $"{played.Winners} winners");
        Assert.True(played.LivesLost >= 5, $"{played.LivesLost} lives lost");
        Assert.True(played.LevelsCleared >= 1, $"{played.LevelsCleared} levels cleared");
        Assert.True(played.NewGames >= 1, $"{played.NewGames} new games");
        Assert.True(played.Game.Score > 0, "nothing scored in the game in hand");
        Assert.True(played.Inputs.Count > 600, $"only {played.Inputs.Count} ticks into the game in hand");
    }

    [Fact]
    public void The_same_inputs_played_twice_give_exactly_the_same_game_and_score()
    {
        Played played = Play(Tuning());

        Game first = Replay(Tuning(), played.Inputs);
        Game second = Replay(Tuning(), played.Inputs);

        AssertSameGame(first, second);
        AssertSameGame(played.Game, first);
    }

    [Fact]
    public void A_saved_game_recording_replays_to_the_same_final_score_lives_level_and_ball()
    {
        Played played = Play(Tuning());
        string file = Recording.Save(played.Game, played.Inputs);

        Recording recording = Recording.Load(file);
        Game replayed = recording.ReplayGame();

        Assert.Equal(played.Game.Score, recording.FinalScore);
        Assert.Equal(played.Game.PlayerLives, recording.FinalLives);
        Assert.Equal(played.Game.Level, recording.FinalLevel);
        Assert.Equal(played.Game.Match.BallPosition, recording.FinalBallPosition);
        Assert.Equal(played.Game.Match.BallVelocity, recording.FinalBallVelocity);
        Assert.Equal(played.Game.Match.BallSpin, recording.FinalBallSpin);
        Assert.Equal(recording.FinalScore, replayed.Score);
        Assert.Equal(recording.FinalLives, replayed.PlayerLives);
        Assert.Equal(recording.FinalLevel, replayed.Level);
        AssertSameGame(played.Game, replayed);
    }

    [Fact]
    public void A_recording_carries_the_tuning_the_game_was_played_with()
    {
        // Not the defaults, and it changes how the game goes. The tuning
        // kept is the game's, not what the level in hand has made of it.
        Tuning custom = Tuning() with { BasePace = 31, SpinCurve = 0.002f, OpponentLives = 2, KickPoints = 75 };
        Played played = Play(custom);

        Recording recording = Recording.Load(Recording.Save(played.Game, played.Inputs));

        Assert.Equal(custom, recording.Tuning);
        AssertSameGame(played.Game, recording.ReplayGame());
        Assert.NotEqual(Play(Tuning()).Game.Score, played.Game.Score);
    }

    [Fact]
    public void A_recording_holds_every_input_of_the_game()
    {
        Played played = Play(Tuning(), ticks: 300);

        Recording recording = Recording.Load(Recording.Save(played.Game, played.Inputs));

        Assert.Equal(300, played.Inputs.Count);
        Assert.Equal(played.Inputs, recording.Inputs());
    }
}
