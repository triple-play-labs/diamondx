using DiamondX.Core.Models;

namespace DiamondX.Core.Simulation;

/// <summary>
/// Resolves plate appearance outcomes for batters.
/// </summary>
public interface IPlateAppearanceResolver
{
    /// <summary>
    /// Resolve a plate appearance using only batter stats.
    /// </summary>
    AtBatOutcome Resolve(Player batter);

    /// <summary>
    /// Resolve a plate appearance using batter vs pitcher matchup (Log5 method).
    /// </summary>
    AtBatOutcome Resolve(Player batter, Pitcher pitcher);
}
