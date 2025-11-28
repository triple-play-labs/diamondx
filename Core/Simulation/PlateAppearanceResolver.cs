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

        // Get fatigue-adjusted pitcher rates
        double pitcherWalkRate = pitcher.GetFatigueAdjustedRate(pitcher.WalkRate);
        double pitcherSingleRate = pitcher.GetFatigueAdjustedRate(pitcher.SinglesAllowedRate);
        double pitcherDoubleRate = pitcher.GetFatigueAdjustedRate(pitcher.DoublesAllowedRate);
        double pitcherTripleRate = pitcher.GetFatigueAdjustedRate(pitcher.TriplesAllowedRate);
        double pitcherHRRate = pitcher.GetFatigueAdjustedRate(pitcher.HomeRunsAllowedRate);
        double pitcherKRate = pitcher.GetFatigueAdjustedStrikeoutRate();

        // Apply Log5 formula: P = (Pb * Pp) / Pl / ((Pb * Pp / Pl) + ((1-Pb) * (1-Pp) / (1-Pl)))
        // Where Pb = batter rate, Pp = pitcher rate, Pl = league average rate
        double walkProb = Log5(batter.WalkRate, pitcherWalkRate, LeagueWalkRate);
        double singleProb = Log5(batter.SingleRate, pitcherSingleRate, LeagueSingleRate);
        double doubleProb = Log5(batter.DoubleRate, pitcherDoubleRate, LeagueDoubleRate);
        double tripleProb = Log5(batter.TripleRate, pitcherTripleRate, LeagueTripleRate);
        double hrProb = Log5(batter.HomeRunRate, pitcherHRRate, LeagueHomeRunRate);

        double roll = _randomSource.NextDouble();
        AtBatOutcome outcome;

        if (roll < walkProb)
        {
            outcome = AtBatOutcome.Walk;
            pitcher.RecordPitches(PitchesPerWalk);
        }
        else
        {
            roll -= walkProb;
            if (roll < singleProb)
            {
                outcome = AtBatOutcome.Single;
                pitcher.RecordPitches(PitchesPerHit);
            }
            else
            {
                roll -= singleProb;
                if (roll < doubleProb)
                {
                    outcome = AtBatOutcome.Double;
                    pitcher.RecordPitches(PitchesPerHit);
                }
                else
                {
                    roll -= doubleProb;
                    if (roll < tripleProb)
                    {
                        outcome = AtBatOutcome.Triple;
                        pitcher.RecordPitches(PitchesPerHit);
                    }
                    else
                    {
                        roll -= tripleProb;
                        if (roll < hrProb)
                        {
                            outcome = AtBatOutcome.HomeRun;
                            pitcher.RecordPitches(PitchesPerHit);
                        }
                        else
                        {
                            // Out - check if it's a strikeout (for pitch count purposes)
                            outcome = AtBatOutcome.Out;
                            double kProb = Log5(1.0 - batter.WalkRate - batter.SingleRate - batter.DoubleRate - batter.TripleRate - batter.HomeRunRate,
                                                pitcherKRate, LeagueStrikeoutRate);
                            if (_randomSource.NextDouble() < kProb)
                            {
                                pitcher.RecordPitches(PitchesPerStrikeout);
                            }
                            else
                            {
                                pitcher.RecordPitches(PitchesPerOut);
                            }
                        }
                    }
                }
            }
        }

        return outcome;
    }

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
