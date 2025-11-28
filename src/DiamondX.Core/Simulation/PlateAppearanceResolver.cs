using System;
using DiamondX.Core.Models;
using SimulationEngine.Random;

namespace DiamondX.Core.Simulation;

public sealed class PlateAppearanceResolver : IPlateAppearanceResolver
{
    private readonly IRandomSource _randomSource;

    // League average rates (approximate MLB averages)
    private const double LeagueWalkRate = 0.085;
    private const double LeagueSingleRate = 0.155;
    private const double LeagueDoubleRate = 0.045;
    private const double LeagueTripleRate = 0.005;
    private const double LeagueHomeRunRate = 0.035;
    private const double LeagueStrikeoutRate = 0.22;

    // Average pitches per plate appearance outcome
    private const int PitchesPerOut = 4;
    private const int PitchesPerWalk = 6;
    private const int PitchesPerStrikeout = 5;
    private const int PitchesPerHit = 3;

    public PlateAppearanceResolver(IRandomSource randomSource)
    {
        _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
    }

    /// <summary>
    /// Legacy resolve using batter stats only.
    /// </summary>
    public AtBatOutcome Resolve(Player batter)
    {
        if (batter is null)
        {
            throw new ArgumentNullException(nameof(batter));
        }

        double roll = _randomSource.NextDouble();

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

    /// <summary>
    /// Resolves a plate appearance using Log5 method combining batter and pitcher stats,
    /// with fatigue adjustments applied to the pitcher.
    /// </summary>
    public AtBatOutcome Resolve(Player batter, Pitcher pitcher)
    {
        if (batter is null)
            throw new ArgumentNullException(nameof(batter));
        if (pitcher is null)
            throw new ArgumentNullException(nameof(pitcher));

        var probabilities = CalculateProbabilities(batter, pitcher);
        var outcome = DetermineOutcome(probabilities);
        RecordPitches(outcome, batter, pitcher, probabilities);

        return outcome;
    }

    private static OutcomeProbabilities CalculateProbabilities(Player batter, Pitcher pitcher)
    {
        // Get fatigue-adjusted pitcher rates
        double pitcherWalkRate = pitcher.GetFatigueAdjustedRate(pitcher.WalkRate);
        double pitcherSingleRate = pitcher.GetFatigueAdjustedRate(pitcher.SinglesAllowedRate);
        double pitcherDoubleRate = pitcher.GetFatigueAdjustedRate(pitcher.DoublesAllowedRate);
        double pitcherTripleRate = pitcher.GetFatigueAdjustedRate(pitcher.TriplesAllowedRate);
        double pitcherHRRate = pitcher.GetFatigueAdjustedRate(pitcher.HomeRunsAllowedRate);
        double pitcherKRate = pitcher.GetFatigueAdjustedStrikeoutRate();

        return new OutcomeProbabilities(
            Walk: Log5(batter.WalkRate, pitcherWalkRate, LeagueWalkRate),
            Single: Log5(batter.SingleRate, pitcherSingleRate, LeagueSingleRate),
            Double: Log5(batter.DoubleRate, pitcherDoubleRate, LeagueDoubleRate),
            Triple: Log5(batter.TripleRate, pitcherTripleRate, LeagueTripleRate),
            HomeRun: Log5(batter.HomeRunRate, pitcherHRRate, LeagueHomeRunRate),
            Strikeout: pitcherKRate
        );
    }

    private AtBatOutcome DetermineOutcome(OutcomeProbabilities probs)
    {
        double roll = _randomSource.NextDouble();

        if (roll < probs.Walk) return AtBatOutcome.Walk;
        roll -= probs.Walk;

        if (roll < probs.Single) return AtBatOutcome.Single;
        roll -= probs.Single;

        if (roll < probs.Double) return AtBatOutcome.Double;
        roll -= probs.Double;

        if (roll < probs.Triple) return AtBatOutcome.Triple;
        roll -= probs.Triple;

        if (roll < probs.HomeRun) return AtBatOutcome.HomeRun;

        return AtBatOutcome.Out;
    }

    private void RecordPitches(AtBatOutcome outcome, Player batter, Pitcher pitcher, OutcomeProbabilities probs)
    {
        int pitches = outcome switch
        {
            AtBatOutcome.Walk => PitchesPerWalk,
            AtBatOutcome.Single or AtBatOutcome.Double or AtBatOutcome.Triple or AtBatOutcome.HomeRun => PitchesPerHit,
            AtBatOutcome.Out => DetermineOutPitches(batter, probs),
            _ => PitchesPerOut
        };

        pitcher.RecordPitches(pitches);
    }

    private int DetermineOutPitches(Player batter, OutcomeProbabilities probs)
    {
        // Calculate strikeout probability for outs
        double batterOutRate = 1.0 - batter.WalkRate - batter.SingleRate - batter.DoubleRate - batter.TripleRate - batter.HomeRunRate;
        double kProb = Log5(batterOutRate, probs.Strikeout, LeagueStrikeoutRate);

        return _randomSource.NextDouble() < kProb ? PitchesPerStrikeout : PitchesPerOut;
    }

    private readonly record struct OutcomeProbabilities(
        double Walk,
        double Single,
        double Double,
        double Triple,
        double HomeRun,
        double Strikeout
    );

    /// <summary>
    /// Log5 formula for combining batter and pitcher probabilities.
    /// Returns the probability of an event given batter rate, pitcher rate, and league average.
    /// </summary>
    private static double Log5(double batterRate, double pitcherRate, double leagueRate)
    {
        // Avoid division by zero
        if (leagueRate <= 0 || leagueRate >= 1)
            return (batterRate + pitcherRate) / 2;

        double numerator = batterRate * pitcherRate / leagueRate;
        double denominator = numerator + ((1 - batterRate) * (1 - pitcherRate) / (1 - leagueRate));

        if (denominator <= 0)
            return 0;

        return Math.Max(0, Math.Min(1, numerator / denominator));
    }
}
