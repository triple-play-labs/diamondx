using DiamondX.Core.Models;

namespace DiamondX.Core.Simulation;

public interface IPlateAppearanceResolver
{
    AtBatOutcome Resolve(Player batter);
}
