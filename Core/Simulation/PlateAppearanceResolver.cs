using DiamondX.Core.Models;
using DiamondX.Core.Random;

namespace DiamondX.Core.Simulation;

/// <summary>
/// Default implementation of IPlateAppearanceResolver.
/// Uses the Log5 method for batter-pitcher matchups.
/// </summary>
public class PlateAppearanceResolver : IPlateAppearanceResolver
{
    private readonly IRandomSource _random;

    // League average rates (approximated for modern MLB)
    private const double LeagueWalkRate = 0.085;
    private const double LeagueSingleRate = 0.155;
    private const double LeagueDoubleRate = 0.045;
    private const double LeagueTripleRate = 0.005;
    private const double LeagueHomeRunRate = 0.035;

    public PlateAppearanceResolver(IRandomSource random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public AtBatOutcome Resolve(Player batter)
    {
        double roll = _random.NextDouble();

        if (roll < batter.WalkRate) return AtBatOutcome.Walk;
        roll -= batter.WalkRate;

        if (roll < batter.SingleRate) return AtBatOutcome.Single;
        roll -= batter.SingleRate;

        if (roll < batter.DoubleRate) return AtBatOutcome.Double;
        roll -= batter.DoubleRate;

        if (roll < batter.TripleRate) return AtBatOutcome.Triple;
        roll -= batter.TripleRate;

        if (roll < batter.HomeRunRate) return AtBatOutcome.HomeRun;

        return AtBatOutcome.Out;
    }

    public AtBatOutcome Resolve(Player batter, Pitcher pitcher)
    {
        // Use Log5 to blend batter and pitcher rates
        double walkRate = Log5(batter.WalkRate, pitcher.WalkRate, LeagueWalkRate);
        double singleRate = Log5(batter.SingleRate, pitcher.SinglesAllowedRate, LeagueSingleRate);
        double doubleRate = Log5(batter.DoubleRate, pitcher.DoublesAllowedRate, LeagueDoubleRate);
        double tripleRate = Log5(batter.TripleRate, pitcher.TriplesAllowedRate, LeagueTripleRate);
        double homeRunRate = Log5(batter.HomeRunRate, pitcher.HomeRunsAllowedRate, LeagueHomeRunRate);

        double roll = _random.NextDouble();

        if (roll < walkRate) return AtBatOutcome.Walk;
        roll -= walkRate;

        if (roll < singleRate) return AtBatOutcome.Single;
        roll -= singleRate;

        if (roll < doubleRate) return AtBatOutcome.Double;
        roll -= doubleRate;

        if (roll < tripleRate) return AtBatOutcome.Triple;
        roll -= tripleRate;

        if (roll < homeRunRate) return AtBatOutcome.HomeRun;

        return AtBatOutcome.Out;
    }

    /// <summary>
    /// Log5 method: combines batter rate (B), pitcher rate (P), and league rate (L).
    /// Formula: (B * P) / L, clamped to [0, 1].
    /// </summary>
    private static double Log5(double batterRate, double pitcherRate, double leagueRate)
    {
        if (leagueRate <= 0) return batterRate;

        double result = (batterRate * pitcherRate) / leagueRate;
        return Math.Clamp(result, 0.0, 1.0);
    }
}
