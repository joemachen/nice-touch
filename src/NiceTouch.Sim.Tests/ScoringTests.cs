namespace NiceTouch.Sim.Tests;

// What a winner pays. The numbers are TestRally's: 100 for the winner, 10 a
// rally hit, 10 a unit of bend, 50 a kick, and NICE TOUCH at 100 of style.
public class ScoringTests
{
    private static Winner Pay(int level = 1, int rallyHits = 0, float bend = 0, int kicks = 0) =>
        Winner.Pay(TestRally.Tuning(), level, rallyHits, bend, kicks);

    [Fact]
    public void A_plain_winner_scores_the_base_points()
    {
        Winner winner = Pay();

        Assert.Equal(100, winner.Total);
        Assert.False(winner.NiceTouch);
    }

    [Fact]
    public void Each_paddle_hit_in_the_rally_adds_its_points()
    {
        Winner winner = Pay(rallyHits: 4);

        Assert.Equal(40, winner.RallyPoints);
        Assert.Equal(140, winner.Total);
    }

    [Fact]
    public void Each_unit_of_bend_adds_its_points()
    {
        Winner winner = Pay(bend: 3);

        Assert.Equal(30, winner.BendPoints);
        Assert.Equal(130, winner.Total);
    }

    [Fact]
    public void Part_of_a_unit_of_bend_pays_its_share_rounded_down_to_a_whole_point()
    {
        Assert.Equal(25, Pay(bend: 2.57f).BendPoints);
    }

    [Fact]
    public void Each_kick_adds_its_points()
    {
        Winner winner = Pay(kicks: 1);

        Assert.Equal(50, winner.KickPoints);
        Assert.Equal(150, winner.Total);
    }

    [Fact]
    public void The_level_multiplies_the_whole_winner()
    {
        Winner winner = Pay(level: 3, rallyHits: 4, bend: 3, kicks: 1);

        Assert.Equal((100 + 40 + 30 + 50) * 3, winner.Total);
    }

    [Fact]
    public void Style_at_the_mark_is_a_nice_touch_and_counts_double()
    {
        // The spec's example: bent 5 units and kicked once is 100 of style.
        Winner winner = Pay(bend: 5, kicks: 1);

        Assert.True(winner.NiceTouch);
        Assert.Equal(100, winner.NiceTouchPoints);
        Assert.Equal(100 + 2 * 100, winner.Total);
    }

    [Fact]
    public void Style_just_under_the_mark_is_not_a_nice_touch()
    {
        Winner winner = Pay(bend: 4.9f, kicks: 1);

        Assert.False(winner.NiceTouch);
        Assert.Equal(0, winner.NiceTouchPoints);
        Assert.Equal(100 + 99, winner.Total);
    }

    [Fact]
    public void Bend_alone_or_kicks_alone_can_reach_the_mark()
    {
        Assert.True(Pay(bend: 10).NiceTouch);
        Assert.True(Pay(kicks: 2).NiceTouch);
    }

    [Fact]
    public void A_long_rally_is_not_style()
    {
        Winner winner = Pay(rallyHits: 30);

        Assert.False(winner.NiceTouch);
        Assert.Equal(100 + 300, winner.Total);
    }

    [Fact]
    public void The_level_multiplies_a_nice_touch_too()
    {
        Assert.Equal((100 + 2 * 100) * 4, Pay(level: 4, bend: 5, kicks: 1).Total);
    }

    [Theory]
    [InlineData(1, 0, 0f, 0)]
    [InlineData(2, 7, 3.3f, 1)]
    [InlineData(6, 12, 21.6f, 3)]
    public void The_lines_shown_on_screen_add_up_to_the_total(int level, int rallyHits, float bend, int kicks)
    {
        Winner winner = Pay(level, rallyHits, bend, kicks);

        int lines = winner.BasePoints + winner.RallyPoints + winner.BendPoints + winner.KickPoints + winner.NiceTouchPoints;
        Assert.Equal(lines * winner.Level, winner.Total);
        Assert.Equal(rallyHits, winner.RallyHits);
        Assert.Equal(bend, winner.Bend);
        Assert.Equal(kicks, winner.Kicks);
    }

    [Fact]
    public void Every_amount_is_a_tuning_value()
    {
        Tuning tuning = TestRally.Tuning() with
        {
            WinnerPoints = 7,
            RallyHitPoints = 3,
            BendPoints = 2,
            KickPoints = 11,
            NiceTouchMark = 15,
        };

        Winner winner = Winner.Pay(tuning, level: 1, rallyHits: 2, bend: 2, kicks: 1);

        Assert.True(winner.NiceTouch);
        Assert.Equal(7 + 2 * 3 + 2 * (2 * 2 + 11), winner.Total);
    }
}
