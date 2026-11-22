using System.Numerics;

namespace NiceTouch.Sim.Tests;

// The game around the rally: lives, levels, the score and the two endings.
// The numbers are TestRally's: 5 lives against 3, one life back for a
// cleared level, 6 levels, 100 for a plain winner and a level bonus of 500.
public class GameTests
{
    private static readonly TickInput Click = TestGame.Click;

    private static Game NewGame(Tuning? tuning = null) => new(tuning ?? TestGame.Tuning());

    // A game that has just been lost or won, as asked.
    private static Game Ended(GamePhase ending)
    {
        Game game = NewGame(TestGame.Tuning() with { Levels = 1 });
        game.Miss(Side.Opponent);
        if (ending == GamePhase.Won)
            game.ClearLevel();
        else
            game.LoseEveryLife();
        Assert.Equal(ending, game.Phase);
        return game;
    }

    [Fact]
    public void A_new_game_starts_on_level_1_with_full_lives_no_score_and_the_player_to_serve()
    {
        Game game = NewGame();

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(1, game.Level);
        Assert.Equal(5, game.PlayerLives);
        Assert.Equal(3, game.OpponentLives);
        Assert.Equal(0, game.Score);
        Assert.Null(game.Winner);
        Assert.Equal(Phase.Serving, game.Match.Phase);
    }

    [Fact]
    public void Level_1_is_played_with_the_tuning_as_written()
    {
        Tuning tuning = TestGame.Tuning();

        Assert.Equal(tuning, new Game(tuning).Match.Tuning);
    }

    [Fact]
    public void A_game_keeps_its_own_copy_of_the_tuning()
    {
        Tuning tuning = TestGame.Tuning();
        var game = new Game(tuning);

        tuning.PlayerLives = 99;

        Assert.Equal(5, game.Tuning.PlayerLives);
    }

    [Fact]
    public void The_click_serves_and_the_rally_is_the_rally()
    {
        Game game = NewGame();

        game.Step(Click);

        Assert.Equal(Phase.Rally, game.Match.Phase);
        Assert.True(game.Events.HasFlag(TickEvents.PaddleHit));
        Assert.Equal(1, game.Tick);
    }

    [Fact]
    public void The_players_miss_costs_the_player_one_life_and_nothing_else()
    {
        Game game = NewGame();

        game.Miss(Side.Player);

        Assert.Equal(4, game.PlayerLives);
        Assert.Equal(3, game.OpponentLives);
        Assert.Equal(0, game.Score);
        Assert.Equal(1, game.Level);
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Null(game.Winner);
    }

    [Fact]
    public void The_opponents_miss_costs_the_opponent_one_life_and_the_player_none()
    {
        Game game = NewGame();

        game.Miss(Side.Opponent);

        Assert.Equal(2, game.OpponentLives);
        Assert.Equal(5, game.PlayerLives);
        Assert.Equal(1, game.Level);
        Assert.Equal(GamePhase.Playing, game.Phase);
    }

    [Fact]
    public void A_miss_costs_its_life_once_however_long_the_pause_lasts()
    {
        Game game = NewGame();
        game.Miss(Side.Player);
        game.Miss(Side.Opponent);

        game.Run(3 * TestGame.PauseTicks);

        Assert.Equal(4, game.PlayerLives);
        Assert.Equal(2, game.OpponentLives);
    }

    [Fact]
    public void After_a_miss_and_its_pause_the_player_serves_again()
    {
        Game game = NewGame();
        game.Miss(Side.Player);

        game.Step(Click);
        Assert.Equal(Phase.PointOver, game.Match.Phase);
        game.Run(TestGame.PauseTicks);
        Assert.Equal(Phase.Serving, game.Match.Phase);
        game.Step(Click);

        Assert.Equal(Phase.Rally, game.Match.Phase);
    }

    [Fact]
    public void A_plain_winner_scores_the_base_points()
    {
        Game game = NewGame();

        game.Miss(Side.Opponent);

        Assert.Equal(100, game.Score);
        Assert.NotNull(game.Winner);
        Assert.Equal(100, game.Winner.Total);
        Assert.Equal(0, game.Winner.LevelBonus);
    }

    [Fact]
    public void Winners_add_up()
    {
        Game game = NewGame();

        game.Miss(Side.Opponent);
        game.Miss(Side.Player);
        game.Miss(Side.Opponent);

        Assert.Equal(200, game.Score);
    }

    [Fact]
    public void A_winner_is_paid_for_the_rally_and_for_what_spin_did_to_the_winning_shot()
    {
        // Served, then sent on its way with heavy side spin from close to
        // the right wall. The opponent is stuck in the far corner, so the
        // ball goes by.
        Game game = NewGame(TestGame.Tuning() with { OpponentSpeed = 0, SpinHalfLifeSeconds = 1e9f });
        game.Step(Click);
        Match match = game.Match;
        match.BallPosition = new Vector3(4.9f, 0, 2);
        match.BallVelocity = new Vector3(6, 0, 24);
        match.BallSpin = new Vector2(0, 12);
        match.OpponentPaddle = new Vector2(-4.5f, -3);

        while (match.Phase == Phase.Rally)
            game.Run(1);

        Assert.Equal(Side.Opponent, match.LastMiss);
        Assert.True(match.ShotBend > 1, $"bend {match.ShotBend}");
        Assert.True(match.ShotKicks >= 1, $"{match.ShotKicks} kicks");
        Assert.Equal(Winner.Pay(game.Tuning, 1, match.RallyLength, match.ShotBend, match.ShotKicks), game.Winner);
        Assert.Equal(1, game.Winner!.RallyHits);
        Assert.True(game.Winner.BendPoints > 0);
        Assert.True(game.Winner.KickPoints > 0);
        Assert.Equal(game.Winner.Total, game.Score);
    }

    [Fact]
    public void A_nice_touch_is_called_on_the_tick_of_the_winner()
    {
        Game game = NewGame(TestGame.Tuning() with { NiceTouchMark = 0 });

        game.Miss(Side.Opponent);
        Assert.True(game.Winner!.NiceTouch);
        Assert.True(game.Events.HasFlag(TickEvents.NiceTouch));

        game.Run(1);
        Assert.False(game.Events.HasFlag(TickEvents.NiceTouch));
    }

    [Fact]
    public void An_ordinary_winner_is_not_called_a_nice_touch()
    {
        Game game = NewGame();

        game.Miss(Side.Opponent);

        Assert.False(game.Winner!.NiceTouch);
        Assert.False(game.Events.HasFlag(TickEvents.NiceTouch));
    }

    [Fact]
    public void The_winner_stays_on_show_until_the_next_serve()
    {
        Game game = NewGame();
        game.Miss(Side.Opponent);
        Winner winner = game.Winner!;

        game.Run(TestGame.PauseTicks + 30);
        Assert.Equal(Phase.Serving, game.Match.Phase);
        Assert.Same(winner, game.Winner);

        game.Step(Click);
        Assert.Null(game.Winner);
    }

    [Fact]
    public void Taking_the_opponents_last_life_clears_the_level_and_pays_the_bonus()
    {
        Game game = NewGame();

        game.ClearLevel();

        Assert.Equal(GamePhase.LevelClear, game.Phase);
        Assert.Equal(1, game.Level);
        Assert.Equal(0, game.OpponentLives);
        Assert.Equal(500, game.Winner!.LevelBonus);
        Assert.Equal(3 * 100 + 500, game.Score);
    }

    [Fact]
    public void Clearing_a_level_gives_one_life_back()
    {
        Game game = NewGame();
        game.Miss(Side.Player);
        game.Miss(Side.Player);

        game.ClearLevel();

        Assert.Equal(4, game.PlayerLives);
    }

    [Fact]
    public void A_life_back_never_takes_the_player_above_the_starting_lives()
    {
        Game game = NewGame();

        game.ClearLevel();

        Assert.Equal(5, game.PlayerLives);
    }

    [Fact]
    public void The_lives_given_back_are_a_tuning_value()
    {
        Game game = NewGame(TestGame.Tuning() with { LivesBack = 2 });
        game.Miss(Side.Player);
        game.Miss(Side.Player);
        game.Miss(Side.Player);

        game.ClearLevel();

        Assert.Equal(4, game.PlayerLives);
    }

    [Fact]
    public void A_click_while_a_cleared_level_waits_does_not_serve()
    {
        Game game = NewGame();
        game.ClearLevel();

        for (int i = 0; i < TestGame.PauseTicks - 1; i++)
        {
            game.Step(Click);
            Assert.Equal(GamePhase.LevelClear, game.Phase);
            Assert.NotEqual(Phase.Rally, game.Match.Phase);
        }
    }

    [Fact]
    public void After_the_pause_the_next_level_starts_quicker_and_faster_with_the_player_to_serve()
    {
        Tuning tuning = TestGame.Tuning();
        Game game = NewGame(tuning);
        game.ClearLevel();

        game.Run(TestGame.PauseTicks);

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(2, game.Level);
        Assert.Equal(3, game.OpponentLives);
        Assert.Equal(tuning.ForLevel(2), game.Match.Tuning);
        Assert.Equal(tuning.OpponentSpeed + 1, game.Match.Tuning.OpponentSpeed);
        Assert.Equal(Phase.Serving, game.Match.Phase);
        Assert.Null(game.Match.LastMiss);

        game.Step(Click);
        Assert.Equal(new Vector3(0, 0, tuning.BasePace + 1), game.Match.BallVelocity);
    }

    [Fact]
    public void The_score_and_the_players_lives_carry_into_the_next_level()
    {
        Game game = NewGame();
        game.Miss(Side.Player);
        game.Miss(Side.Player);
        game.ClearLevel();
        int score = game.Score;

        game.Run(TestGame.PauseTicks);

        Assert.Equal(score, game.Score);
        Assert.Equal(4, game.PlayerLives);
    }

    [Fact]
    public void The_winner_that_cleared_a_level_stays_on_show_until_the_next_levels_serve()
    {
        Game game = NewGame();
        game.ClearLevel();

        game.Run(TestGame.PauseTicks + 30);
        Assert.Equal(500, game.Winner!.LevelBonus);

        game.Step(Click);
        Assert.Null(game.Winner);
    }

    [Fact]
    public void The_paddles_do_not_jump_when_a_level_starts()
    {
        // An opponent that cannot move, left off centre.
        var mouse = new Vector2(3, -2);
        var opponent = new Vector2(2, 1);
        Game game = NewGame(TestGame.Tuning() with { OpponentSpeed = 0, OpponentSpeedStep = 0 });
        game.ClearLevel();
        game.Match.OpponentPaddle = opponent;
        game.Run(TestGame.PauseTicks - 1, mouse);
        Assert.Equal(1, game.Level);

        game.Run(1, mouse);

        Assert.Equal(2, game.Level);
        Assert.Equal(mouse, game.Match.PlayerPaddle);
        Assert.Equal(opponent, game.Match.OpponentPaddle);
        Assert.Equal(new Vector3(mouse, game.Match.Tuning.BallRadius), game.Match.BallPosition);

        // So a serve on the level's first tick is not a wild swipe.
        game.Step(new TickInput(mouse, Serve: true));
        Assert.Equal(Vector2.Zero, game.Match.BallSpin);
    }

    [Fact]
    public void A_winner_on_a_later_level_is_multiplied_by_the_level()
    {
        Game game = NewGame();
        game.ClearLevel();
        game.Run(TestGame.PauseTicks);
        int before = game.Score;

        game.Miss(Side.Opponent);

        Assert.Equal(before + 200, game.Score);
    }

    [Fact]
    public void The_level_bonus_is_multiplied_by_the_level()
    {
        Game game = NewGame();
        game.ClearLevel();
        game.Run(TestGame.PauseTicks);
        int before = game.Score;

        game.ClearLevel();

        Assert.Equal(1000, game.Winner!.LevelBonus);
        Assert.Equal(before + 3 * 200 + 1000, game.Score);
    }

    [Fact]
    public void Losing_the_last_life_ends_the_game_and_keeps_the_score_and_level()
    {
        Game game = NewGame();
        game.ClearLevel();
        game.Run(TestGame.PauseTicks);
        int score = game.Score;

        game.LoseEveryLife();

        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.Equal(0, game.PlayerLives);
        Assert.Equal(2, game.Level);
        Assert.Equal(score, game.Score);
    }

    [Fact]
    public void The_game_is_not_over_while_the_player_has_a_life_left()
    {
        Game game = NewGame();

        for (int i = 0; i < 4; i++)
            game.Miss(Side.Player);

        Assert.Equal(1, game.PlayerLives);
        Assert.Equal(GamePhase.Playing, game.Phase);
    }

    [Theory]
    [InlineData(GamePhase.GameOver)]
    [InlineData(GamePhase.Won)]
    public void After_the_game_has_ended_nothing_more_can_be_served(GamePhase ending)
    {
        Game game = Ended(ending);
        int score = game.Score;

        // Clicking all through the pause, as someone would who had not
        // noticed the game was over.
        for (int i = 0; i < TestGame.PauseTicks - 1; i++)
        {
            game.Step(Click);
            Assert.Equal(ending, game.Phase);
            Assert.NotEqual(Phase.Rally, game.Match.Phase);
        }

        Assert.Equal(score, game.Score);
    }

    [Theory]
    [InlineData(GamePhase.GameOver)]
    [InlineData(GamePhase.Won)]
    public void An_ended_game_waits_for_as_long_as_it_takes(GamePhase ending)
    {
        Game game = Ended(ending);
        int score = game.Score;

        game.Run(10 * TestGame.PauseTicks);

        Assert.Equal(ending, game.Phase);
        Assert.Equal(score, game.Score);
        Assert.Equal(1, game.Level);
    }

    [Theory]
    [InlineData(GamePhase.GameOver)]
    [InlineData(GamePhase.Won)]
    public void Once_the_pause_has_passed_one_click_starts_a_new_game_at_level_1(GamePhase ending)
    {
        Game game = Ended(ending);
        game.Run(TestGame.PauseTicks);

        game.Step(Click);

        Assert.True(game.Events.HasFlag(TickEvents.NewGame));
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(1, game.Level);
        Assert.Equal(5, game.PlayerLives);
        Assert.Equal(3, game.OpponentLives);
        Assert.Equal(0, game.Score);
        Assert.Null(game.Winner);
        Assert.Equal(0, game.Tick);
        // That click started the game. It takes another to serve.
        Assert.Equal(Phase.Serving, game.Match.Phase);

        game.Step(Click);
        Assert.Equal(Phase.Rally, game.Match.Phase);
        Assert.False(game.Events.HasFlag(TickEvents.NewGame));
    }

    [Fact]
    public void Clearing_the_last_level_wins_the_game()
    {
        Game game = NewGame(TestGame.Tuning() with { Levels = 2 });
        game.ClearLevel();
        game.Run(TestGame.PauseTicks);
        int before = game.Score;

        game.ClearLevel();

        Assert.Equal(GamePhase.Won, game.Phase);
        Assert.Equal(2, game.Level);
        Assert.Equal(before + 3 * 200 + 1000, game.Score);

        // There is no level after the last.
        game.Run(3 * TestGame.PauseTicks);
        Assert.Equal(GamePhase.Won, game.Phase);
        Assert.Equal(2, game.Level);
    }

    [Fact]
    public void A_whole_game_is_six_levels()
    {
        Game game = NewGame();

        for (int level = 1; level <= 6; level++)
        {
            Assert.Equal(GamePhase.Playing, game.Phase);
            Assert.Equal(level, game.Level);
            game.ClearLevel();
            game.Run(TestGame.PauseTicks);
        }

        Assert.Equal(GamePhase.Won, game.Phase);
        // Eighteen plain winners and six bonuses, each times its level.
        Assert.Equal((3 * 100 + 500) * (1 + 2 + 3 + 4 + 5 + 6), game.Score);
    }

    [Fact]
    public void A_game_started_by_the_click_plays_exactly_like_a_fresh_one()
    {
        // The recorder starts again with each new game, so the two must
        // not differ in anything.
        Game restarted = Ended(GamePhase.GameOver);
        restarted.Run(TestGame.PauseTicks, new Vector2(3, 2));
        restarted.Step(Click);
        Game fresh = NewGame(restarted.Tuning);

        foreach (Game game in new[] { restarted, fresh })
        {
            game.Step(new TickInput(new Vector2(1, 1), Serve: true));
            game.Run(40, new Vector2(-2, 0.5f));
        }

        Assert.Equal(fresh.Tick, restarted.Tick);
        Assert.Equal(fresh.Match.BallPosition, restarted.Match.BallPosition);
        Assert.Equal(fresh.Match.BallVelocity, restarted.Match.BallVelocity);
        Assert.Equal(fresh.Match.BallSpin, restarted.Match.BallSpin);
        Assert.Equal(fresh.Match.PlayerPaddle, restarted.Match.PlayerPaddle);
        Assert.Equal(fresh.Match.OpponentPaddle, restarted.Match.OpponentPaddle);
    }
}
