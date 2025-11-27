# DiamondX: A Baseball Simulation Engine

DiamondX is a text-based baseball simulation engine built with .NET 8. It models plate appearances, runner movement, inning flow, scoring, and pitcher fatigue using real player/pitcher statistics with a testable, event-driven core.

## Features

- **Realistic Plate Appearances**: Uses the Log5 method to combine batter and pitcher statistics
- **Pitcher Fatigue**: Tracks pitch count with configurable fatigue thresholds that affect performance
- **Team Support**: Custom team names displayed throughout the game
- **Extra Innings**: Games continue until there's a winner (no ties!)
- **Walk-off Detection**: Games end immediately when the home team takes the lead in the bottom of the 9th or later
- **Event-Driven Architecture**: Extensible event system for custom handlers and logging

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Quick Start

```zsh
git clone https://github.com/JimCorrell/diamondx.git
cd diamondx
dotnet build
dotnet run
```

## Sample Output

```text
--- Game Start: Dodgers @ Giants ---

Top of Inning 1 | Score: Dodgers 0 - Giants 0
At bat: Mookie Betts vs Logan Webb (Pitch #1, Fatigue: 0%)
Result: OUT!
...
[Logan Webb: 87 pitches, 7% fatigue]
--------------------

--- Game Over ---
Final Score: Dodgers 3 - Giants 5 (11 innings)
Giants Win!
```

## Architecture

### Core Components

- `Core/Game.cs`: Orchestrates inning flow, manages pitchers, and coordinates game state
- `Core/State/GameState.cs`: Single source of truth for outs, bases, inning, and scores
- `Core/Simulation/PlateAppearanceResolver.cs`: Resolves outcomes using Log5 batter-pitcher matchups
- `Core/Random/IRandomSource.cs`: Abstraction for randomness to enable deterministic tests

### Models

- `Core/Models/Player.cs`: Batter with per-PA rates (walk, single, double, triple, HR)
- `Core/Models/Pitcher.cs`: Pitcher with allowed rates, pitch count, and fatigue tracking
- `Core/Models/AtBatOutcome.cs`: Outcome enum (Walk, Single, Double, Triple, HomeRun, Out)

### Events

- `Core/Events/EventScheduler.cs`: Central event queue with handler registration
- `Core/Events/Baseball/BaseballEvents.cs`: Domain events (GameStarted, AtBat, RunScored, etc.)
- `Core/Events/Handlers/ConsoleEventHandler.cs`: Human-readable game output

## Pitcher Fatigue System

Pitchers track their pitch count throughout the game. Fatigue affects performance:

- **Fatigue Threshold**: Pitch count where fatigue begins (default: 75)
- **Max Pitch Count**: When pitcher is fully exhausted (default: 110)
- **Fatigue Effects**:
  - Walk/hit rates increase by up to 50% at max fatigue
  - Strikeout rate decreases by up to 40% at max fatigue

```csharp
var pitcher = new Pitcher(
    name: "Logan Webb",
    walkRate: 0.055,
    singlesAllowedRate: 0.145,
    doublesAllowedRate: 0.040,
    triplesAllowedRate: 0.003,
    homeRunsAllowedRate: 0.022,
    strikeoutRate: 0.200,
    fatigueThreshold: 85,    // Starts tiring at 85 pitches
    maxPitchCount: 115       // Fully exhausted at 115 pitches
);
```

## Running Tests

```zsh
dotnet test
```

## Deterministic Testing

Tests use `TestRandomSource` to feed known random values into `PlateAppearanceResolver`, enabling scripted sequences without flakiness:

- `DiamondX.Tests/GameStateTests.cs`: Base clearing and scoring updates
- `DiamondX.Tests/WalkHandlingTests.cs`: Forced advances on walks, bases-loaded scoring
- `DiamondX.Tests/HalfInningFlowTests.cs`: Deterministic runner movement and outs
- `DiamondX.Tests/EventSchedulerTests.cs`: Event ordering, handler dispatch, filtering

## Deterministic Run Example

```csharp
using DiamondX.Core;
using DiamondX.Core.Models;
using DiamondX.Core.Random;
using DiamondX.Core.Simulation;

// Fixed sequence of rolls for reproducible results
var rng = new TestRandomSource(new[] { 0.12, 0.42, 0.97, 0.05, 0.33, 0.88 });
var resolver = new PlateAppearanceResolver(rng);

var homeLineup = new List<Player>
{
    new("Wade Jr.", 0.124, 0.153, 0.041, 0.004, 0.033),
    new("Estrada", 0.048, 0.201, 0.050, 0.002, 0.027),
};

var awayLineup = new List<Player>
{
    new("Betts", 0.137, 0.163, 0.060, 0.001, 0.057),
    new("Freeman", 0.131, 0.187, 0.082, 0.003, 0.041),
};

var homePitcher = new Pitcher("Webb", 0.055, 0.145, 0.040, 0.003, 0.022, 0.200);
var awayPitcher = new Pitcher("Kershaw", 0.050, 0.138, 0.038, 0.002, 0.028, 0.245);

var game = new Game(
    homeTeam: homeLineup,
    awayTeam: awayLineup,
    homeTeamName: "Giants",
    awayTeamName: "Dodgers",
    homePitcher: homePitcher,
    awayPitcher: awayPitcher,
    plateAppearanceResolver: resolver
);

game.PlayGame();
```

## Roadmap

- [x] ~~Pitcher model and batter-pitcher matchup modifiers~~
- [x] ~~Extra innings and walk-off logic~~
- [ ] Bullpen management and pitcher substitutions
- [ ] Expanded baserunning (steals, sac flies, double plays)
- [ ] Defensive events and fielding positions
- [ ] Box score and detailed statistics output (CSV/JSON)
- [ ] Game replay from event log
