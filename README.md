# DiamondX: A Baseball Simulation Engine

DiamondX is a text-based baseball simulation engine in .NET 8. It models plate appearances, runner movement, inning flow, and scoring using player probabilities, with a testable core.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Quick Start

```zsh
git clone https://github.com/your-username/diamondx.git
cd diamondx
dotnet build
dotnet run
```

## Architecture

- `Core/Game.cs`: Orchestrates inning flow and uses services to resolve outcomes.
- `Core/State/GameState.cs`: Single source of truth for outs, bases, inning half, and scores.
- `Core/Simulation/PlateAppearanceResolver.cs`: Resolves `AtBatOutcome` using an injectable RNG.
- `Core/Random/IRandomSource.cs`: Abstraction for randomness to enable deterministic tests.
- `Core/Models/Player.cs`, `Core/Models/AtBatOutcome.cs`: Player data and outcome enum.

Notes:

- The app writes play-by-play to stdout; tests assert on `Game.State` instead of parsing console output.
- Legacy root-level `Game.cs`, `Player.cs`, `AtBatOutcome.cs` remain as stubs pointing to core types.

## Running Tests

```zsh
dotnet test DiamondX.Tests/DiamondX.Tests.csproj
```

## Deterministic Testing

Tests use `TestRandomSource` to feed known random values into `PlateAppearanceResolver`, enabling scripted sequences without flakiness. See:

- `DiamondX.Tests/GameStateTests.cs`: Base clearing and scoring updates.
- `DiamondX.Tests/WalkHandlingTests.cs`: Forced advances on walks, bases-loaded scoring.
- `DiamondX.Tests/HalfInningFlowTests.cs`: Deterministic runner movement and outs.

## Roadmap

- Pitcher model and batter-pitcher matchup modifiers.
- Expanded baserunning (steals, sac flies, double plays) and defensive events.
- Extra innings and walk-off logic.
- Box score/event log output (CSV/JSON).

## Deterministic Run Example

You can inject a seeded random source via `PlateAppearanceResolver` to make a run reproducible:

```csharp
using DiamondX.Core;
using DiamondX.Core.Models;
using DiamondX.Core.Random;
using DiamondX.Core.Simulation;

// Fixed sequence of rolls
var rng = new TestRandomSource(new[] { 0.12, 0.42, 0.97, 0.05 });
var resolver = new PlateAppearanceResolver(rng);

var home = new List<Player>
{
    new("H1", 0.1, 0.2, 0.05, 0.01, 0.02),
    new("H2", 0.08, 0.22, 0.04, 0.01, 0.03),
};
var away = new List<Player>
{
    new("A1", 0.09, 0.21, 0.05, 0.01, 0.02),
    new("A2", 0.07, 0.20, 0.06, 0.01, 0.03),
};

var game = new Game(home, away, resolver);
game.PlayGame();
```

For ad‑hoc scripts, reference the test `TestRandomSource` or implement your own `IRandomSource` with a fixed seed.
