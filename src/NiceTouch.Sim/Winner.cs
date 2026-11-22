namespace NiceTouch.Sim;

// What the player was paid for making the opponent miss, line by line, so
// the screen can show the sum and not only the total.
public sealed record Winner
{
    public required int Level { get; init; }

    // The flat amount for the winner itself.
    public required int BasePoints { get; init; }

    // Paddle hits in the rally, the serve included, and what they paid.
    public required int RallyHits { get; init; }
    public required int RallyPoints { get; init; }

    // Style. How far spin bent the winning shot, and the wall and floor
    // bites that sped it up, with what each paid.
    public required float Bend { get; init; }
    public required int BendPoints { get; init; }
    public required int Kicks { get; init; }
    public required int KickPoints { get; init; }

    // Style that reached the mark counts double. These are the extra
    // points that gave, or zero.
    public required bool NiceTouch { get; init; }
    public required int NiceTouchPoints { get; init; }

    // Every line above added up, times the level.
    public required int Total { get; init; }

    // The bonus for clearing the level, if this winner cleared it. It is
    // paid on top of the total.
    public int LevelBonus { get; init; }

    // Prices a winner from the facts of the rally and the winning shot.
    public static Winner Pay(Tuning tuning, int level, int rallyHits, float bend, int kicks)
    {
        int rallyPoints = rallyHits * tuning.RallyHitPoints;
        // Part of a unit of bend pays its share, down to a whole point.
        int bendPoints = (int)(bend * tuning.BendPoints);
        int kickPoints = kicks * tuning.KickPoints;
        int style = bendPoints + kickPoints;
        bool niceTouch = style >= tuning.NiceTouchMark;
        int niceTouchPoints = niceTouch ? style : 0;

        return new Winner
        {
            Level = level,
            BasePoints = tuning.WinnerPoints,
            RallyHits = rallyHits,
            RallyPoints = rallyPoints,
            Bend = bend,
            BendPoints = bendPoints,
            Kicks = kicks,
            KickPoints = kickPoints,
            NiceTouch = niceTouch,
            NiceTouchPoints = niceTouchPoints,
            Total = (tuning.WinnerPoints + rallyPoints + style + niceTouchPoints) * level,
        };
    }
}
