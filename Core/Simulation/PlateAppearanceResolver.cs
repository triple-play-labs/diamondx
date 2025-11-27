using System;
using DiamondX.Core.Models;
using DiamondX.Core.Random;

namespace DiamondX.Core.Simulation;

public sealed class PlateAppearanceResolver : IPlateAppearanceResolver
{
    private readonly IRandomSource _randomSource;

    public PlateAppearanceResolver(IRandomSource randomSource)
    {
        _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
    }

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
}
