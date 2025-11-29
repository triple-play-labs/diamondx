---
layout: default
title: API Reference
---

# API Reference

Core classes and interfaces in DiamondX.

## DiamondX.Core

### Game

The main game orchestrator.

```csharp
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

### Player

Represents a batter with per-plate-appearance statistics.

```csharp
var player = new Player(
    name: "Mookie Betts",
    walkRate: 0.137,
    singlesRate: 0.163,
    doublesRate: 0.060,
    triplesRate: 0.001,
    homeRunRate: 0.057
);
```

### Pitcher

Represents a pitcher with fatigue tracking.

```csharp
var pitcher = new Pitcher(
    name: "Clayton Kershaw",
    walkRate: 0.050,
    singlesAllowedRate: 0.138,
    doublesAllowedRate: 0.038,
    triplesAllowedRate: 0.002,
    homeRunsAllowedRate: 0.028,
    strikeoutRate: 0.245,
    fatigueThreshold: 85,    // Optional
    maxPitchCount: 115       // Optional
);
```

### AtBatOutcome

Enum representing possible plate appearance results:

- `Walk`
- `Single`
- `Double`
- `Triple`
- `HomeRun`
- `Out`

## SimulationEngine

### ISimulation

Interface for runnable simulations.

```csharp
public interface ISimulation
{
    string Name { get; }
    void Initialize(ISimulationContext context);
    SimulationResult Run();
}
```

### SimulationRunner

Executes simulations with optional parallelism.

```csharp
var runner = new SimulationRunner();
var results = runner.RunMultiple(simulation, count: 10000, parallel: true);
```

### IRandomSource

Interface for random number generation.

```csharp
public interface IRandomSource
{
    double NextDouble();
    int Next(int maxValue);
}
```

### EventScheduler

Central event queue with handler registration.

```csharp
var scheduler = new EventScheduler();

// Subscribe
scheduler.Subscribe<GameStartedEvent>(e => Console.WriteLine("Game started!"));

// Publish
scheduler.Publish(new GameStartedEvent());
```

## Events

### Baseball Events

| Event | Description |
|-------|-------------|
| `GameStartedEvent` | Game has begun |
| `InningStartedEvent` | New inning starting |
| `AtBatEvent` | Plate appearance occurred |
| `RunScoredEvent` | Run scored |
| `GameEndedEvent` | Game complete |

## Further Reading

- [Full source code](https://github.com/triple-play-labs/diamondx/tree/main/src)
- [SimulationEngine README](https://github.com/triple-play-labs/diamondx/blob/main/src/SimulationEngine/README.md)
