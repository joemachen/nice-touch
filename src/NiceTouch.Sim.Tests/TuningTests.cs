namespace NiceTouch.Sim.Tests;

public class TuningTests
{
    [Fact]
    public void Two_untouched_tunings_are_equal_and_a_changed_one_is_not()
    {
        // This comparison is what puts "CUSTOM TUNING" on screen.
        Assert.Equal(new Tuning(), new Tuning());
        Assert.NotEqual(new Tuning(), new Tuning { TunnelDepth = 31 });
    }

    [Fact]
    public void A_match_keeps_its_own_copy_of_the_tuning()
    {
        var tuning = new Tuning();
        var match = new Match(tuning);

        tuning.TunnelDepth = 99;

        Assert.Equal(new Tuning().TunnelDepth, match.Tuning.TunnelDepth);
    }
}
