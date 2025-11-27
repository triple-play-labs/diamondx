namespace DiamondX.Core.Models;

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

    public Pitcher(
        string name,
        double walkRate,
        double singlesAllowedRate,
        double doublesAllowedRate,
        double triplesAllowedRate,
        double homeRunsAllowedRate,
        double strikeoutRate)
    {
        Name = name;
        WalkRate = walkRate;
        SinglesAllowedRate = singlesAllowedRate;
        DoublesAllowedRate = doublesAllowedRate;
        TriplesAllowedRate = triplesAllowedRate;
        HomeRunsAllowedRate = homeRunsAllowedRate;
        StrikeoutRate = strikeoutRate;
    }

    public override string ToString() => $"Pitcher({Name})";
}
