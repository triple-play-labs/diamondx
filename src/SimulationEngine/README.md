# SimulationEngine

A domain-agnostic simulation framework for building and executing discrete-event and time-stepped simulations.

## Overview

SimulationEngine provides the foundational infrastructure for running simulations, managing time, handling events, persisting state, and collecting metrics. It is designed to accept simulation models from external projects (like DiamondX.Core's baseball simulation) and execute them with full control over reproducibility, parallelization, and state management.

## Architecture

```text
SimulationEngine/
├── Core/           # Simulation contracts and orchestration
├── Events/         # Event scheduling and handling
├── Random/         # Reproducible random number generation
├── State/          # State snapshots and persistence
└── Time/           # Simulation clock and time management
```

## Current Features

### Core (`SimulationEngine.Core`)

- **ISimulation** - Contract for runnable simulation models
- **SimulationRunner** - Orchestrates single and parallel simulation runs
- **SimulationContext** - Runtime services provided to simulations
- **SimulationParameters** - Type-safe, mutable parameter collection
- **SimulationMetrics** - Engine-level performance tracking
- **SimulationResult** - Run outcome with metrics and seed for reproducibility

### Events (`SimulationEngine.Events`)

- **ISimulationEvent** - Base event interface with timestamp
- **IEventHandler** - Handler contract for event processing
- **EventScheduler** - Priority queue for discrete-event scheduling

### Random (`SimulationEngine.Random`)

- **IRandomSource** - RNG abstraction for testability
- **SystemRandomSource** - Production RNG using System.Random
- **SeedableRandomSource** - Reproducible RNG with seed tracking

### State (`SimulationEngine.State`)

- **IStateManager** - Snapshot persistence interface
- **InMemoryStateManager** - JSON-based in-memory snapshots
- **ISnapshotable\<T\>** - Interface for domain states to implement

### Time (`SimulationEngine.Time`)

- **ISimulationClock** - Time management abstraction
- **SimulationClock** - Thread-safe implementation supporting:
  - `DiscreteEvent` - Time jumps to next event
  - `FixedStep` - Time advances by fixed increment
  - `RealTime` - Time tracks wall clock (scaled)

## Usage Example

```csharp
// Implement ISimulation for your domain
public class MySimulation : ISimulation
{
    public string Name => "My Simulation";
    public string Version => "1.0.0";
    public bool IsComplete { get; private set; }

    public void Initialize(ISimulationContext context)
    {
        // Set up initial state
    }

    public SimulationStepResult Step(ISimulationContext context)
    {
        // Advance simulation by one step
        context.Clock.Advance(TimeSpan.FromSeconds(1));

        if (/* done condition */)
        {
            IsComplete = true;
            return SimulationStepResult.Completed;
        }

        return SimulationStepResult.Continue;
    }

    public void Dispose() { }
}

// Run the simulation
var runner = new SimulationRunner();
var result = runner.Run(new MySimulation(), seed: 42);

Console.WriteLine($"Steps: {result.Metrics.StepCount}");
Console.WriteLine($"Time: {result.Metrics.SimulatedTimeElapsed}");

// Run Monte Carlo simulations in parallel
var results = runner.RunParallel(
    simulationFactory: () => new MySimulation(),
    runCount: 1000,
    baseSeed: 42
);
```

## Roadmap

### Phase 1: Foundation ✅

- [x] Core simulation abstractions (ISimulation, ISimulationContext)
- [x] Simulation runner with single and parallel execution
- [x] Clock with discrete-event, fixed-step, and real-time modes
- [x] State snapshots with JSON serialization
- [x] Seedable RNG for reproducibility
- [x] Engine-level metrics (steps/sec, time ratio, event count)

### Phase 2: Enhanced Event System

- [ ] **Event Correlation** - Track causal chains between events
- [ ] **Event Filtering** - Subscribe to specific event types
- [ ] **Event Replay** - Replay event streams from logs
- [ ] **Async Event Handlers** - Support for `Task`-based handlers
- [ ] **Event Batching** - Process multiple events per step for performance

### Phase 3: Advanced State Management

- [ ] **Persistent Storage** - File-based and database snapshot backends
- [ ] **Incremental Snapshots** - Delta-based state for large simulations
- [ ] **State Branching** - Fork simulation from any snapshot
- [ ] **State Comparison** - Diff snapshots for debugging
- [ ] **Compression** - Reduce snapshot storage size

### Phase 4: Execution Modes

- [ ] **Checkpointing** - Automatic periodic snapshots
- [ ] **Pause/Resume** - Suspend and continue simulations
- [ ] **Step Debugging** - Step through simulation with inspection
- [ ] **Conditional Breakpoints** - Pause on specific conditions
- [ ] **Time Travel** - Jump to any snapshot and continue

### Phase 5: Observability

- [ ] **Structured Logging** - Integration with ILogger
- [ ] **Tracing** - OpenTelemetry support for distributed simulations
- [ ] **Custom Metrics** - Domain-specific metric registration
- [ ] **Real-time Monitoring** - Expose metrics via endpoints
- [ ] **Profiling Hooks** - Identify performance bottlenecks

### Phase 6: Distribution & Scale

- [ ] **Distributed Execution** - Run across multiple machines
- [ ] **Work Partitioning** - Split large simulations
- [ ] **Result Aggregation** - Combine parallel run results
- [ ] **Cloud Integration** - Azure/AWS execution backends
- [ ] **Container Support** - Docker-based simulation workers

### Phase 7: Analysis & Visualization

- [ ] **Statistical Analysis** - Built-in result aggregation
- [ ] **Sensitivity Analysis** - Parameter impact assessment
- [ ] **Confidence Intervals** - Monte Carlo convergence detection
- [ ] **Export Formats** - CSV, JSON, Parquet output
- [ ] **Visualization Hooks** - Integration points for UI

## Design Principles

1. **Domain Agnostic** - No knowledge of what's being simulated
2. **Reproducible** - Same seed = same results, always
3. **Testable** - All components have interface abstractions
4. **Thread-Safe** - Safe for parallel execution by default
5. **Minimal Dependencies** - Core has no external dependencies
6. **Composable** - Mix and match components as needed

## Integration with DiamondX

DiamondX.Core implements `ISimulation` through its `Game` class (or a wrapper), allowing baseball games to be executed by the engine:

```csharp
// Baseball simulation using the engine
var runner = new SimulationRunner();
var results = runner.RunParallel(
    () => new BaseballGameSimulation(homeTeam, awayTeam),
    runCount: 10000,
    baseSeed: 42
);

// Analyze win probability
var homeWins = results.Count(r => r.FinalState.HomeScore > r.FinalState.AwayScore);
var winProbability = homeWins / (double)results.Count;
```

## Contributing

When adding new features:

1. Define interfaces first for testability
2. Keep the engine domain-agnostic
3. Add comprehensive unit tests
4. Update this README with new capabilities
