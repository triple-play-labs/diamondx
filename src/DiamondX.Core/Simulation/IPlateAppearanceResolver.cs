using DiamondX.Core.Models;

namespace DiamondX.Core.Simulation;

public interface IPlateAppearanceResolver
{
    /// <summary>
    /// Resolves a plate appearance outcome using batter stats only (legacy/testing).
    /// </summary>
    AtBatOutcome Resolve(Player batter);

    /// <summary>
    /// Resolves a plate appearance outcome combining batter and pitcher stats
    /// using the Log5 method, with fatigue adjustments applied to pitcher.
    /// </summary>
    AtBatOutcome Resolve(Player batter, Pitcher pitcher);
}
