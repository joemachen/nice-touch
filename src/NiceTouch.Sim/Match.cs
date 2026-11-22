using System.Numerics;

namespace NiceTouch.Sim;

// Coordinates: x right, y up, z depth from the player's end (0) to the
// opponent's end (TunnelDepth).
public sealed class Match
{
    private const float Dt = Tuning.TickSeconds;

    private int _pauseTicksLeft;

    // The sideways speed spin has given the ball since the latest paddle
    // hit, and how far that speed has carried it.
    private Vector2 _spinDrift;
    private Vector2 _spinCarry;

    public Match(Tuning tuning)
    {
        // A copy, so nothing outside can change the rules of a running match.
        Tuning = tuning with { };
        PutBallOnPlayersPaddle();
    }

    // The rally for the next level of a game. The paddles stay where they
    // were, so nothing jumps and the level's first swipe is measured from
    // where the paddle really was.
    internal Match(Tuning tuning, Match previous) : this(tuning)
    {
        PlayerPaddle = previous.PlayerPaddle;
        OpponentPaddle = previous.OpponentPaddle;
        PutBallOnPlayersPaddle();
    }

    public Tuning Tuning { get; }
    public long Tick { get; private set; }
    public Phase Phase { get; internal set; }
    public Vector3 BallPosition { get; internal set; }
    public Vector3 BallVelocity { get; internal set; }

    // Spin as angular velocity in radians per second. X is top spin (back
    // spin when negative, for a ball going away), Y is side spin. There is
    // no spin about the depth axis.
    public Vector2 BallSpin { get; internal set; }

    // Paddle hits since the serve.
    public int RallyLength { get; private set; }

    // Two readings on the shot in flight, counted from the latest paddle
    // hit. Scoring pays for them. They report on the rally and change
    // nothing in it.
    //
    // Bend is how far spin has carried the ball off a straight line. It is
    // measured by keeping a separate account that only spin pays into: each
    // tick the push spin gives the ball is added to a drift speed, and the
    // drift speed is added to a drift distance. Gravity, the swipe and the
    // walls never enter that account, so a ball with no spin reads zero
    // however far it drops, and a bounce does not undo bend already made.
    public float ShotBend => _spinCarry.Length();

    // Wall and floor bites that left the ball faster down the tunnel.
    public int ShotKicks { get; private set; }

    // Who missed most recently, if anyone has yet.
    public Side? LastMiss { get; private set; }

    // What happened during the latest tick, for sound and effects.
    public TickEvents Events { get; private set; }

    // Paddle centres, as x and y on each paddle's end of the tunnel, and
    // their speeds in units per second measured over the latest tick.
    public Vector2 PlayerPaddle { get; internal set; }
    public Vector2 PlayerPaddleVelocity { get; private set; }
    public Vector2 OpponentPaddle { get; internal set; }
    public Vector2 OpponentPaddleVelocity { get; private set; }

    public void Step(TickInput input)
    {
        Events = TickEvents.None;
        MovePaddles(input.PaddleTarget);

        switch (Phase)
        {
            case Phase.Serving:
                // The ball rides on the paddle until the click, and the
                // serve is a paddle hit like any other.
                PutBallOnPlayersPaddle();
                if (input.Serve)
                {
                    RallyLength = 0;
                    HitWithPaddle(Side.Player);
                    Phase = Phase.Rally;
                }
                break;

            case Phase.Rally:
                Fly();
                break;

            case Phase.PointOver:
                if (--_pauseTicksLeft <= 0)
                {
                    PutBallOnPlayersPaddle();
                    Phase = Phase.Serving;
                }
                break;
        }

        Tick++;
    }

    private void PutBallOnPlayersPaddle()
    {
        BallPosition = new Vector3(PlayerPaddle, Tuning.BallRadius);
        BallVelocity = Vector3.Zero;
        BallSpin = Vector2.Zero;
    }

    private void MovePaddles(Vector2 playerTarget)
    {
        // The player's paddle follows the mouse with a touch of smoothing.
        float closed = 1 - Remaining(Tuning.PaddleHalfLifeSeconds);
        Vector2 player = Vector2.Lerp(PlayerPaddle, InsideTunnel(playerTarget), closed);
        PlayerPaddleVelocity = (player - PlayerPaddle) / Dt;
        PlayerPaddle = player;

        // The opponent chases the ball while it is coming, and drifts back
        // to the centre otherwise, at one speed.
        bool ballIsComing = BallVelocity.Z > 0;
        Vector2 goal = InsideTunnel(ballIsComing ? new Vector2(BallPosition.X, BallPosition.Y) : Vector2.Zero);
        Vector2 gap = goal - OpponentPaddle;
        float reach = Tuning.OpponentSpeed * Dt;
        Vector2 opponent = gap.Length() <= reach ? goal : OpponentPaddle + Vector2.Normalize(gap) * reach;
        OpponentPaddleVelocity = (opponent - OpponentPaddle) / Dt;
        OpponentPaddle = opponent;
    }

    // The nearest place a paddle's centre can be with its edges inside the
    // walls.
    private Vector2 InsideTunnel(Vector2 centre)
    {
        var limit = Vector2.Max(Vector2.Zero, new Vector2(
            (Tuning.TunnelWidth - Tuning.PaddleWidth) / 2,
            (Tuning.TunnelHeight - Tuning.PaddleHeight) / 2));
        return Vector2.Clamp(centre, -limit, limit);
    }

    private void Fly()
    {
        BallSpin *= Remaining(Tuning.SpinHalfLifeSeconds);

        // The bend is spin crossed with velocity, sideways parts only, so
        // it reverses when the ball's direction of travel reverses. The
        // extra factor of pace makes the curve the same shape at any pace.
        Vector3 v = BallVelocity;
        float bend = Tuning.SpinCurve * v.Z * MathF.Abs(v.Z) * Dt;
        v.X += BallSpin.Y * bend;
        v.Y -= BallSpin.X * bend;
        _spinDrift += new Vector2(BallSpin.Y, -BallSpin.X) * bend;

        // Gravity works by time, not by distance, so unlike the bend a
        // faster ball flies flatter. Half is applied before the ball moves
        // and half after, which keeps a bounce off the floor from losing
        // or gaining height tick by tick.
        float halfPull = Tuning.Gravity * Dt / 2;
        v.Y -= halfPull;
        BallVelocity = v;

        Move();

        if (Phase == Phase.Rally)
            BallVelocity -= new Vector3(0, halfPull, 0);
    }

    // Moves the ball for one tick, stopping on the way to deal with a
    // paddle or a miss if it reaches an end of the tunnel.
    private void Move()
    {
        Vector3 v = BallVelocity;

        // Work out exactly when the ball reaches the end of the tunnel it
        // is heading for, so a fast ball cannot slip through a paddle.
        if (v.Z != 0)
        {
            Side end = v.Z > 0 ? Side.Opponent : Side.Player;
            float endZ = end == Side.Player ? Tuning.BallRadius : Tuning.TunnelDepth - Tuning.BallRadius;
            float seconds = MathF.Max(0, (endZ - BallPosition.Z) / v.Z);
            if (seconds <= Dt)
            {
                Advance(seconds);
                BallPosition = BallPosition with { Z = endZ };
                if (Touches(end == Side.Player ? PlayerPaddle : OpponentPaddle))
                {
                    HitWithPaddle(end);
                    Advance(Dt - seconds);
                }
                else
                {
                    // Play stops where the ball got past, for a moment.
                    BallVelocity = Vector3.Zero;
                    LastMiss = end;
                    Events |= TickEvents.Miss;
                    Phase = Phase.PointOver;
                    _pauseTicksLeft = (int)MathF.Round(Tuning.MissPauseSeconds * Tuning.TicksPerSecond);
                }
                return;
            }
        }

        Advance(Dt);
    }

    // A hit if any part of the ball overlaps the paddle.
    private bool Touches(Vector2 paddle)
    {
        Vector2 gap = Vector2.Abs(new Vector2(BallPosition.X, BallPosition.Y) - paddle);
        return gap.X <= Tuning.PaddleWidth / 2 + Tuning.BallRadius
            && gap.Y <= Tuning.PaddleHeight / 2 + Tuning.BallRadius;
    }

    // The same rules for both paddles. The swipe pushes the ball and rubs
    // spin onto it, and spin the ball arrived with grips the paddle and
    // kicks the ball sideways.
    private void HitWithPaddle(Side side)
    {
        Vector2 swipe = side == Side.Player ? PlayerPaddleVelocity : OpponentPaddleVelocity;
        var facing = new Vector3(0, 0, side == Side.Player ? 1 : -1);

        Vector3 contact = ContactSpeed(facing);
        Vector2 sideways = new Vector2(BallVelocity.X, BallVelocity.Y) * Tuning.LateralKeep
            + swipe * Tuning.SwipePush
            - new Vector2(contact.X, contact.Y) * Tuning.PaddleGrip;
        BallVelocity = new Vector3(sideways, 0) + facing * Tuning.BasePace;

        // Rubbed on so that the spin bends the ball back against the push:
        // swipe right and the ball goes out right, then curves left.
        Vector3 rubbed = Vector3.Cross(new Vector3(swipe, 0), facing) * Tuning.SwipeSpin;
        Vector2 spin = BallSpin * Tuning.SpinKeep + new Vector2(rubbed.X, rubbed.Y);
        float size = spin.Length();
        BallSpin = size > Tuning.MaxSpin ? spin * (Tuning.MaxSpin / size) : spin;

        _spinDrift = Vector2.Zero;
        _spinCarry = Vector2.Zero;
        ShotKicks = 0;
        RallyLength++;
        Events |= TickEvents.PaddleHit;
    }

    // Moves the ball in a straight line for part of a tick and bounces it
    // off the walls.
    private void Advance(float seconds)
    {
        _spinCarry += _spinDrift * seconds;
        Vector3 p = BallPosition + BallVelocity * seconds;
        Vector3 v = BallVelocity;
        int fromSide = Reflect(ref p.X, ref v.X, Tuning.TunnelWidth / 2 - Tuning.BallRadius);
        float floorReach = MathF.Max(Tuning.TunnelHeight / 2 - Tuning.BallRadius, 0);
        int fromFloor = Reflect(ref p.Y, ref v.Y, floorReach);

        // Under gravity a ball with too little bounce left rolls along the
        // floor instead: no bounce, no bite, and nothing to hear.
        if (fromFloor == 1 && v.Y < Tuning.Gravity * Tuning.ShortestBounceSeconds / 2)
        {
            p.Y = -floorReach;
            v.Y = 0;
            fromFloor = 0;
        }

        BallPosition = p;
        BallVelocity = v;

        if (fromSide != 0)
            BiteWall(new Vector3(fromSide, 0, 0));
        if (fromFloor != 0)
            BiteWall(new Vector3(0, fromFloor, 0));
    }

    // Keeps one sideways coordinate between its two walls by reflecting it.
    // Returns which way the wall it bounced off faces along this axis, or
    // zero if there was no bounce.
    private static int Reflect(ref float position, ref float velocity, float limit)
    {
        limit = MathF.Max(limit, 0);
        int facing = position > limit ? -1 : position < -limit ? 1 : 0;
        if (facing != 0)
        {
            position = Math.Clamp(-2 * facing * limit - position, -limit, limit);
            velocity = -velocity;
        }
        return facing;
    }

    // Spin bites a wall: the spinning surface grips it and kicks the ball
    // along the tunnel. Spin that bent the ball into the wall speeds it up,
    // the opposite spin slows it. A ball with no spin is left alone.
    private void BiteWall(Vector3 wallFacing)
    {
        Events |= TickEvents.WallBounce;

        Vector3 v = BallVelocity;
        float travel = MathF.Sign(v.Z);
        float pace = travel * (v.Z - Tuning.WallGrip * ContactSpeed(wallFacing).Z);
        float slowest = Tuning.BasePace * Tuning.PaceMin;
        float fastest = Tuning.BasePace * Tuning.PaceMax;
        v.Z = travel * MathF.Max(slowest, MathF.Min(pace, fastest));
        if (MathF.Abs(v.Z) > MathF.Abs(BallVelocity.Z))
            ShotKicks++;
        BallVelocity = v;

        // Side walls are bitten by side spin, floor and ceiling by top spin.
        float kept = 1 - Tuning.WallSpinLoss;
        BallSpin *= wallFacing.X != 0 ? new Vector2(1, kept) : new Vector2(kept, 1);
    }

    // How fast the ball's spinning surface is moving, relative to its
    // centre, at the point where it touches a surface facing this way.
    private Vector3 ContactSpeed(Vector3 surfaceFacing) =>
        Vector3.Cross(new Vector3(BallSpin, 0), -Tuning.BallRadius * surfaceFacing);

    // The fraction of a fading value left after one tick.
    private static float Remaining(float halfLifeSeconds) =>
        halfLifeSeconds > 0 ? MathF.Pow(0.5f, Dt / halfLifeSeconds) : 0;
}
