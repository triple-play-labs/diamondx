namespace DiamondX.Core.Models;

/// <summary>
/// Statistical rates for a pitcher, used for Log5 calculations.
/// </summary>
/// <param name="WalkRate">Rate at which pitcher allows walks (BB/PA).</param>
/// <param name="SinglesAllowedRate">Rate at which pitcher allows singles.</param>
/// <param name="DoublesAllowedRate">Rate at which pitcher allows doubles.</param>
/// <param name="TriplesAllowedRate">Rate at which pitcher allows triples.</param>
/// <param name="HomeRunsAllowedRate">Rate at which pitcher allows home runs (HR/PA).</param>
/// <param name="StrikeoutRate">Rate at which pitcher records strikeouts (K/PA).</param>
public readonly record struct PitcherStats(
    double WalkRate,
    double SinglesAllowedRate,
    double DoublesAllowedRate,
    double TriplesAllowedRate,
    double HomeRunsAllowedRate,
    double StrikeoutRate
)
{
    /// <summary>
    /// Creates league average pitcher stats.
    /// </summary>
    public static PitcherStats LeagueAverage => new(
        WalkRate: 0.08,
        SinglesAllowedRate: 0.15,
        DoublesAllowedRate: 0.045,
        TriplesAllowedRate: 0.005,
        HomeRunsAllowedRate: 0.03,
        StrikeoutRate: 0.22
    );
}

/// <summary>
/// Represents a pitcher with per-plate-appearance rates.
/// These rates are used in combination with batter rates via the Log5 method.
/// </summary>
public class Pitcher
{
    public string Name { get; }

    /// <summary>Rate at which pitcher allows walks (BB/PA).</summary>
    public double WalkRate { get; }

    /// <summary>Rate at which pitcher allows singles.</summary>
    public double SinglesAllowedRate { get; }

    /// <summary>Rate at which pitcher allows doubles.</summary>
    public double DoublesAllowedRate { get; }

    /// <summary>Rate at which pitcher allows triples.</summary>
    public double TriplesAllowedRate { get; }

    /// <summary>Rate at which pitcher allows home runs (HR/PA).</summary>
    public double HomeRunsAllowedRate { get; }

    /// <summary>Rate at which pitcher records strikeouts (K/PA).</summary>
    public double StrikeoutRate { get; }

    /// <summary>Number of pitches thrown in this game.</summary>
    public int PitchCount { get; private set; }

    /// <summary>Pitch count threshold where fatigue begins to affect performance.</summary>
    public int FatigueThreshold { get; }

    /// <summary>Maximum pitch count before pitcher is completely gassed.</summary>
    public int MaxPitchCount { get; }

    /// <summary>
    /// Current fatigue level from 0.0 (fresh) to 1.0 (exhausted).
    /// Fatigue increases negative outcome rates for the pitcher.
    /// </summary>
    public double FatigueLevel
    {
        get
        {
            if (PitchCount <= FatigueThreshold)
                return 0.0;

            double fatigueRange = MaxPitchCount - FatigueThreshold;
            double pitchesOverThreshold = PitchCount - FatigueThreshold;
            return Math.Min(1.0, pitchesOverThreshold / fatigueRange);
        }
    }

    /// <summary>
    /// Returns true if pitcher has exceeded their max pitch count.
    /// </summary>
    public bool IsExhausted => PitchCount >= MaxPitchCount;

    /// <summary>
    /// Creates a pitcher with specified stats and fatigue settings.
    /// </summary>
    public Pitcher(
        string name,
        PitcherStats stats,
        int fatigueThreshold = 75,
        int maxPitchCount = 110)
    {
        Name = name;
        WalkRate = stats.WalkRate;
        SinglesAllowedRate = stats.SinglesAllowedRate;
        DoublesAllowedRate = stats.DoublesAllowedRate;
        TriplesAllowedRate = stats.TriplesAllowedRate;
        HomeRunsAllowedRate = stats.HomeRunsAllowedRate;
        StrikeoutRate = stats.StrikeoutRate;
        FatigueThreshold = fatigueThreshold;
        MaxPitchCount = maxPitchCount;
        PitchCount = 0;
    }

    /// <summary>
    /// Creates a pitcher with individual stat parameters (legacy compatibility).
    /// </summary>
    public Pitcher(
        string name,
        double walkRate,
        double singlesAllowedRate,
        double doublesAllowedRate,
        double triplesAllowedRate,
        double homeRunsAllowedRate,
        double strikeoutRate,
        int fatigueThreshold = 75,
        int maxPitchCount = 110)
        : this(
            name,
            new PitcherStats(walkRate, singlesAllowedRate, doublesAllowedRate, triplesAllowedRate, homeRunsAllowedRate, strikeoutRate),
            fatigueThreshold,
            maxPitchCount)
    {
    }

    /// <summary>
    /// Records pitches thrown during a plate appearance.
    /// Average MLB plate appearance is ~4 pitches.
    /// </summary>
    public void RecordPitches(int count)
    {
        PitchCount += count;
    }

    /// <summary>
    /// Resets pitch count (for new game or relief appearance tracking).
    /// </summary>
    public void ResetPitchCount()
    {
        PitchCount = 0;
    }

    /// <summary>
    /// Gets the fatigue-adjusted rate for a given base rate.
    /// Negative outcomes (walks, hits) increase with fatigue.
    /// </summary>
    public double GetFatigueAdjustedRate(double baseRate, double maxIncrease = 0.5)
    {
        // As fatigue increases, negative outcomes become more likely
        // At max fatigue, rates can increase by up to maxIncrease (50% by default)
        return baseRate * (1.0 + (FatigueLevel * maxIncrease));
    }

    /// <summary>
    /// Gets the fatigue-adjusted strikeout rate.
    /// Strikeout rate decreases with fatigue.
    /// </summary>
    public double GetFatigueAdjustedStrikeoutRate(double maxDecrease = 0.4)
    {
        // As fatigue increases, strikeout rate decreases
        // At max fatigue, strikeout rate can decrease by up to maxDecrease (40% by default)
        return StrikeoutRate * (1.0 - (FatigueLevel * maxDecrease));
    }

    public override string ToString() => $"Pitcher({Name}, Pitches: {PitchCount}, Fatigue: {FatigueLevel:P0})";
}
