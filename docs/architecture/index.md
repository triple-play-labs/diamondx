---
layout: default
title: Architecture
---

# Architecture Overview

DiamondX follows a modular, event-driven architecture designed for extensibility and testability.

## Solution Structure

```
diamondx/
├── src/
│   ├── DiamondX.Console/       # CLI entry point
│   ├── DiamondX.Core/          # Baseball domain
│   ├── DiamondX.Weather/       # Weather simulation
│   └── SimulationEngine/       # Generic framework
└── tests/
    └── DiamondX.Tests/         # NUnit tests
```

## Core Components

### DiamondX.Core - Baseball Domain

The heart of the simulation, containing:

| Component | Purpose |
|-----------|---------|
| `Game.cs` | Orchestrates inning flow and game state |
| `GameState.cs` | Single source of truth (outs, bases, scores) |
| `PlateAppearanceResolver.cs` | Log5 batter-pitcher matchups |
| `Player.cs` | Batter with per-PA statistics |
| `Pitcher.cs` | Pitcher with fatigue tracking |

### SimulationEngine - Generic Framework

A domain-agnostic engine that can run any simulation:

| Component | Purpose |
|-----------|---------|
| `ISimulation` | Contract for simulation models |
| `SimulationRunner` | Executes simulations (parallel support) |
| `SimulationContext` | Runtime services (RNG, clock, events) |
| `EventScheduler` | Central event queue |
| `SeedableRandomSource` | Reproducible RNG |

### DiamondX.Weather

Weather simulation that affects baseball gameplay:

- Temperature, humidity, wind speed/direction
- Home run distance modifiers based on conditions
- Integrates with baseball simulation via orchestration

## Key Design Decisions

### Event-Driven Architecture

All game events flow through the `EventScheduler`:

```csharp
// Subscribe to events
eventScheduler.Subscribe<RunScoredEvent>(handler);

// Publish events
eventScheduler.Publish(new RunScoredEvent(team, runner));
```

### Log5 Method

Plate appearance outcomes use the Log5 formula to combine batter and pitcher statistics, producing realistic matchup results.

### Deterministic Testing

Tests use `TestRandomSource` to feed known random values, enabling:
- Scripted sequences without flakiness
- Reproducible game scenarios
- Edge case testing

## Data Flow

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Console   │────▶│    Game     │────▶│  GameState  │
└─────────────┘     └─────────────┘     └─────────────┘
                           │
                           ▼
                    ┌─────────────┐
                    │  Resolver   │
                    └─────────────┘
                           │
                           ▼
                    ┌─────────────┐
                    │   Events    │
                    └─────────────┘
```

## Further Reading

- [SimulationEngine README](https://github.com/triple-play-labs/diamondx/blob/main/src/SimulationEngine/README.md)
- [ADRs](../adr/) - Architecture Decision Records
