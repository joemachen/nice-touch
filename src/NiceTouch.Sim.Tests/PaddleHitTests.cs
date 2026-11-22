using System.Numerics;

namespace NiceTouch.Sim.Tests;

// Hit or miss, and spec rules 1, 2 and 6: the swipe makes spin, the swipe
// pushes, and spin bites the paddle.
public class PaddleHitTests
{
    private const float Dt = Tuning.TickSeconds;
    private const float Pace = 24;

    // No paddle smoothing, no fading and no bend, so a test can say exactly
    // how fast the paddle was moving and what the ball did.
    private static Tuning Exact() => TestRally.Tuning() with
    {
        PaddleHalfLifeSeconds = 0,
        SpinHalfLifeSeconds = 1e9f,
        SpinCurve = 0,
    };

    // The same, with the bend back on, to watch a shot curve after the hit.
    private static Tuning ExactWithBend() => Exact() with { SpinCurve = 0.001f };

    // A ball half a tick away from the player's end, coming in.
    private static Match ArrivingAtPlayer(
        Vector2 at = default, Vector2 sideways = default, Vector2 spin = default, Tuning? tuning = null, float pace = Pace)
    {
        tuning ??= Exact();
        float z = tuning.BallRadius + pace * Dt / 2;
        return TestRally.InFlight(new Vector3(at, z), new Vector3(sideways, -pace), spin, tuning);
    }

    // A ball half a tick away from the opponent's end, going away.
    private static Match ArrivingAtOpponent(Vector2 at = default, Vector2 spin = default, Tuning? tuning = null)
    {
        tuning ??= Exact();
        float z = tuning.TunnelDepth - tuning.BallRadius - Pace * Dt / 2;
        return TestRally.InFlight(new Vector3(at, z), new Vector3(0, 0, Pace), spin, tuning);
    }

    // Steps one tick with the player's paddle arriving at a spot while
    // moving at the given speed.
    private static void SwipeThrough(Match match, Vector2 paddleAt, Vector2 swipe)
    {
        match.PlayerPaddle = paddleAt - swipe * Dt;
        match.Step(new TickInput(paddleAt, Serve: false));
    }

    private static Vector2 Sideways(Match match) => new(match.BallVelocity.X, match.BallVelocity.Y);

    // Which way a spin bends the flight, found by flying it.
    private static Vector2 CurveDirection(Vector2 spin, int travel)
    {
        Match match = TestRally.InFlight(new Vector3(0, 0, 15), new Vector3(0, 0, Pace * travel), spin, ExactWithBend());
        match.Step();
        return Vector2.Normalize(Sideways(match));
    }

    // ---- Hit or miss ----

    [Fact]
    public void A_still_paddle_sends_a_ball_with_no_spin_straight_back_at_base_pace()
    {
        Match match = ArrivingAtPlayer();

        match.Step();

        Assert.True(match.Events.HasFlag(TickEvents.PaddleHit));
        Assert.Equal(new Vector3(0, 0, match.Tuning.BasePace), match.BallVelocity);
        Assert.Equal(Vector2.Zero, match.BallSpin);
        Assert.Null(match.LastMiss);
    }

    [Fact]
    public void The_opponents_paddle_sends_the_ball_back_towards_the_player()
    {
        Match match = ArrivingAtOpponent();

        match.Step();

        Assert.True(match.Events.HasFlag(TickEvents.PaddleHit));
        Assert.Equal(new Vector3(0, 0, -match.Tuning.BasePace), match.BallVelocity);
    }

    [Theory]
    [InlineData(31)]
    [InlineData(15)]
    public void A_paddle_hit_puts_the_pace_back_to_base(float paceOffAWall)
    {
        Match match = ArrivingAtPlayer(pace: paceOffAWall);

        match.Step();

        Assert.Equal(match.Tuning.BasePace, match.BallVelocity.Z);
    }

    // Paddle 3 by 2 and ball radius 0.5: the ball's centre can be 2 out
    // sideways or 1.5 up or down and still touch.
    [Theory]
    [InlineData(1.95f, 0, true)]
    [InlineData(-1.95f, 0, true)]
    [InlineData(2.05f, 0, false)]
    [InlineData(-2.05f, 0, false)]
    [InlineData(0, 1.45f, true)]
    [InlineData(0, -1.45f, true)]
    [InlineData(0, 1.55f, false)]
    [InlineData(0, -1.55f, false)]
    public void It_is_a_hit_if_any_part_of_the_ball_overlaps_the_paddle(float ballX, float ballY, bool hit)
    {
        Match atPlayer = ArrivingAtPlayer(at: new Vector2(ballX, ballY));
        // The opponent would chase the ball, so pin it for this test.
        Match atOpponent = ArrivingAtOpponent(at: new Vector2(ballX, ballY), tuning: Exact() with { OpponentSpeed = 0 });

        atPlayer.Step();
        atOpponent.Step();

        Assert.Equal(hit, atPlayer.Events.HasFlag(TickEvents.PaddleHit));
        Assert.Equal(!hit, atPlayer.Events.HasFlag(TickEvents.Miss));
        Assert.Equal(hit, atOpponent.Events.HasFlag(TickEvents.PaddleHit));
        Assert.Equal(!hit, atOpponent.Events.HasFlag(TickEvents.Miss));
    }

    [Fact]
    public void A_miss_stops_the_ball_at_the_end_of_the_tunnel_and_records_who_missed()
    {
        Match atPlayer = ArrivingAtPlayer(at: new Vector2(4, 0));
        Match atOpponent = ArrivingAtOpponent(at: new Vector2(4, 0), tuning: Exact() with { OpponentSpeed = 0 });

        atPlayer.Step();
        atOpponent.Step();

        Assert.Equal(Side.Player, atPlayer.LastMiss);
        Assert.Equal(Vector3.Zero, atPlayer.BallVelocity);
        Assert.Equal(atPlayer.Tuning.BallRadius, atPlayer.BallPosition.Z);
        Assert.False(atPlayer.Events.HasFlag(TickEvents.PaddleHit));

        Assert.Equal(Side.Opponent, atOpponent.LastMiss);
        Assert.Equal(atOpponent.Tuning.TunnelDepth - atOpponent.Tuning.BallRadius, atOpponent.BallPosition.Z);
    }

    [Fact]
    public void A_fast_ball_cannot_pass_through_a_paddle()
    {
        // 5000 a second is 83 units a tick, more than the tunnel is long.
        Match match = TestRally.InFlight(new Vector3(0, 0, 15), new Vector3(0, 0, -5000), tuning: Exact());

        match.Step();

        Assert.True(match.Events.HasFlag(TickEvents.PaddleHit));
        Assert.Equal(match.Tuning.BasePace, match.BallVelocity.Z);
    }

    [Fact]
    public void The_hit_is_judged_where_the_ball_crossed_not_where_the_tick_began_or_ended()
    {
        // Moving sideways at 8 units a tick, crossing half way through it.
        var sideways = new Vector2(480, 0);

        // Starts and ends the tick out of the paddle's reach, crosses over it.
        Match crossesOverPaddle = ArrivingAtPlayer(at: new Vector2(-5, 0), sideways: sideways);
        // Starts the tick in front of the paddle, crosses wide of it.
        Match crossesWide = ArrivingAtPlayer(at: new Vector2(0, 0), sideways: sideways);

        crossesOverPaddle.Step();
        crossesWide.Step();

        Assert.True(crossesOverPaddle.Events.HasFlag(TickEvents.PaddleHit));
        Assert.True(crossesWide.Events.HasFlag(TickEvents.Miss));
    }

    [Fact]
    public void A_hit_keeps_a_set_share_of_the_balls_sideways_speed()
    {
        var sideways = new Vector2(4, -2);
        Match match = ArrivingAtPlayer(sideways: sideways);

        match.Step();

        Assert.Equal(sideways * match.Tuning.LateralKeep, Sideways(match));
    }

    // ---- Rule 1: the swipe makes spin ----

    [Fact]
    public void A_faster_swipe_makes_more_spin()
    {
        Match slow = ArrivingAtPlayer();
        Match fast = ArrivingAtPlayer();

        SwipeThrough(slow, Vector2.Zero, new Vector2(6, 0));
        SwipeThrough(fast, Vector2.Zero, new Vector2(12, 0));

        Assert.Equal(6 * slow.Tuning.SwipeSpin, slow.BallSpin.Length(), 3);
        Assert.Equal(12 * fast.Tuning.SwipeSpin, fast.BallSpin.Length(), 3);
    }

    [Theory]
    [InlineData(500, 0)]
    [InlineData(0, -500)]
    [InlineData(300, 300)]
    public void Spin_from_a_swipe_stops_at_the_cap(float swipeX, float swipeY)
    {
        // A wide tunnel, so the paddle really can move this fast in a tick.
        Tuning tuning = Exact() with { TunnelWidth = 60, TunnelHeight = 60 };
        Match match = ArrivingAtPlayer(tuning: tuning);

        SwipeThrough(match, Vector2.Zero, new Vector2(swipeX, swipeY));

        Assert.Equal(match.Tuning.MaxSpin, match.BallSpin.Length(), 3);
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(-10, 0)]
    public void Swiping_left_or_right_makes_side_spin_only(float swipeX, float swipeY)
    {
        Match match = ArrivingAtPlayer();

        SwipeThrough(match, Vector2.Zero, new Vector2(swipeX, swipeY));

        Assert.Equal(0, match.BallSpin.X);
        Assert.NotEqual(0, match.BallSpin.Y);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(0, -10)]
    public void Swiping_up_or_down_makes_top_or_back_spin_only(float swipeX, float swipeY)
    {
        Match match = ArrivingAtPlayer();

        SwipeThrough(match, Vector2.Zero, new Vector2(swipeX, swipeY));

        Assert.NotEqual(0, match.BallSpin.X);
        Assert.Equal(0, match.BallSpin.Y);
    }

    [Fact]
    public void A_diagonal_swipe_makes_both_kinds_of_spin()
    {
        Match match = ArrivingAtPlayer();

        SwipeThrough(match, Vector2.Zero, new Vector2(8, 8));

        Assert.NotEqual(0, match.BallSpin.X);
        Assert.NotEqual(0, match.BallSpin.Y);
    }

    [Fact]
    public void Opposite_swipes_make_opposite_spin()
    {
        Match right = ArrivingAtPlayer();
        Match left = ArrivingAtPlayer();

        SwipeThrough(right, Vector2.Zero, new Vector2(10, 4));
        SwipeThrough(left, Vector2.Zero, new Vector2(-10, -4));

        Assert.Equal(-right.BallSpin, left.BallSpin);
    }

    // ---- Rule 2: the swipe also pushes, which makes the banana shot ----

    [Fact]
    public void The_ball_leaves_a_little_in_the_direction_of_the_swipe()
    {
        var swipe = new Vector2(10, -6);
        Match match = ArrivingAtPlayer();

        SwipeThrough(match, Vector2.Zero, swipe);

        Assert.Equal(swipe * match.Tuning.SwipePush, Sideways(match));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    [InlineData(0, -1)]
    public void The_players_swipe_pushes_the_ball_out_and_the_spin_bends_it_back(float x, float y)
    {
        var direction = new Vector2(x, y);
        Match match = ArrivingAtPlayer(tuning: ExactWithBend());

        SwipeThrough(match, Vector2.Zero, direction * 10);
        float atTheHit = Vector2.Dot(Sideways(match), direction);
        match.Run(40);
        float later = Vector2.Dot(Sideways(match), direction);

        Assert.True(atTheHit > 1, "pushed out along the swipe");
        Assert.True(later < 0, "bent back the other way");
        Assert.Equal(match.Tuning.BasePace, match.BallVelocity.Z);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    [InlineData(0, -1)]
    public void The_opponents_swipe_makes_the_same_banana_shot(float x, float y)
    {
        var direction = new Vector2(x, y);
        // The ball arrives one unit to one side of the opponent's paddle,
        // so the paddle is moving that way, at its one speed, when it hits.
        Match match = ArrivingAtOpponent(at: direction, tuning: ExactWithBend());

        match.Step();
        Assert.True(match.Events.HasFlag(TickEvents.PaddleHit));
        float atTheHit = Vector2.Dot(Sideways(match), direction);
        match.Run(40);
        float later = Vector2.Dot(Sideways(match), direction);

        Assert.True(atTheHit > 1, "pushed out along the swipe");
        Assert.True(later < 0, "bent back the other way");
        Assert.Equal(-match.Tuning.BasePace, match.BallVelocity.Z);
    }

    // ---- Rule 6: spin bites the paddle too ----

    public static TheoryData<float, float> FourSpins() => new()
    {
        { 12, 0 }, { -12, 0 }, { 0, 12 }, { 0, -12 },
    };

    [Theory]
    [MemberData(nameof(FourSpins))]
    public void A_spinning_ball_jumps_sideways_off_the_players_still_paddle(float spinX, float spinY)
    {
        var spin = new Vector2(spinX, spinY);
        Match match = ArrivingAtPlayer(spin: spin);

        match.Step();

        // It kicks against the way it was curving as it came in.
        Tuning t = match.Tuning;
        Assert.Equal(t.PaddleGrip * t.BallRadius * spin.Length(), Sideways(match).Length(), 3);
        Assert.True(Vector2.Dot(Sideways(match), CurveDirection(spin, travel: -1)) < 0);
    }

    [Theory]
    [MemberData(nameof(FourSpins))]
    public void A_spinning_ball_jumps_sideways_off_the_opponents_still_paddle(float spinX, float spinY)
    {
        var spin = new Vector2(spinX, spinY);
        // Dead in line with the opponent's paddle, so it has no need to move.
        Match match = ArrivingAtOpponent(spin: spin);

        match.Step();

        Tuning t = match.Tuning;
        Assert.Equal(Vector2.Zero, match.OpponentPaddleVelocity);
        Assert.Equal(t.PaddleGrip * t.BallRadius * spin.Length(), Sideways(match).Length(), 3);
        Assert.True(Vector2.Dot(Sideways(match), CurveDirection(spin, travel: +1)) < 0);
    }

    [Fact]
    public void The_ball_keeps_part_of_its_spin_off_a_still_paddle()
    {
        var spin = new Vector2(12, -8);
        Match match = ArrivingAtPlayer(spin: spin);

        match.Step();

        Assert.Equal(spin * match.Tuning.SpinKeep, match.BallSpin);
    }

    [Theory]
    [MemberData(nameof(FourSpins))]
    public void Swiping_against_the_jump_returns_a_spinning_ball_cleanly(float spinX, float spinY)
    {
        var spin = new Vector2(spinX, spinY);
        Match still = ArrivingAtPlayer(spin: spin);
        still.Step();
        Vector2 jump = Sideways(still);

        Match swiped = ArrivingAtPlayer(spin: spin);
        SwipeThrough(swiped, Vector2.Zero, -jump / swiped.Tuning.SwipePush);

        Assert.True(jump.Length() > 1);
        Assert.True(Sideways(swiped).Length() < 0.001f);
    }

    [Fact]
    public void With_paddle_grip_at_zero_a_spinning_ball_does_not_jump()
    {
        Match match = ArrivingAtPlayer(spin: new Vector2(12, -8), tuning: Exact() with { PaddleGrip = 0 });

        match.Step();

        Assert.Equal(Vector2.Zero, Sideways(match));
    }

    [Fact]
    public void Every_paddle_hit_adds_one_to_the_rally_length()
    {
        Match match = ArrivingAtPlayer(tuning: Exact() with { OpponentSpeed = 100 });

        // Long enough to go there and back twice.
        match.Run(5 * Tuning.TicksPerSecond);

        Assert.Null(match.LastMiss);
        Assert.True(match.RallyLength >= 4, $"rally length {match.RallyLength}");
    }
}
