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

- [x] **ISimulation / ISimulationContext** - Core contracts defining how simulation models interact with the engine
- [x] **SimulationRunner** - Executes single simulations or parallel Monte Carlo runs with seed management
- [x] **SimulationClock** - Time management with discrete-event, fixed-step, and real-time modes
- [x] **IStateManager / InMemoryStateManager** - Snapshot persistence for state capture and replay
- [x] **SeedableRandomSource** - Reproducible RNG with seed tracking for deterministic runs
- [x] **SimulationMetrics** - Engine-level performance tracking (steps/sec, time ratio, event count)

### Phase 2: Multi-Model Orchestration

- [ ] **ISimulationOrchestrator** - Coordinates multiple simulation models within a single run, controlling time advancement and ensuring deterministic execution order
- [ ] **Model Registration** - Register multiple `ISimulation` instances with execution priority, allowing models to run in defined order each time step
- [ ] **Dependency Management** - Declare dependencies between models (e.g., Weather model runs before Baseball model) with automatic topological ordering
- [ ] **Barrier Synchronization** - Ensure all models complete their step before advancing to the next time step, preventing race conditions
- [ ] **Shared Context** - Allow models to share state through a common context without direct coupling
- [ ] **Model Lifecycle Hooks** - BeforeStep/AfterStep hooks for cross-cutting concerns like logging or validation

### Phase 3: Enhanced Event System

- [ ] **Event Correlation** - Track causal chains between events by assigning correlation IDs, enabling root-cause analysis
- [ ] **Event Filtering** - Subscribe handlers to specific event types or patterns, reducing unnecessary processing
- [ ] **Event Replay** - Replay event streams from persisted logs to reproduce exact simulation behavior
- [ ] **Async Event Handlers** - Support `Task`-based handlers for I/O-bound operations without blocking the simulation
- [ ] **Event Batching** - Process multiple events per step to improve throughput for high-frequency event simulations

### Phase 4: Advanced State Management

- [ ] **Persistent Storage** - File-based and database snapshot backends for long-running simulations and disaster recovery
- [ ] **Incremental Snapshots** - Delta-based state capture for large simulations, storing only what changed since last snapshot
- [ ] **State Branching** - Fork simulation from any snapshot to explore "what-if" scenarios without losing the original timeline
- [ ] **State Comparison** - Diff two snapshots to identify exactly what changed, useful for debugging and validation
- [ ] **Compression** - Reduce snapshot storage size using compression algorithms for efficient disk usage

### Phase 5: Execution Modes

- [ ] **Checkpointing** - Automatic periodic snapshots at configurable intervals for crash recovery
- [ ] **Pause/Resume** - Suspend simulation mid-run and continue later, preserving all state
- [ ] **Step Debugging** - Step through simulation one tick at a time with full state inspection
- [ ] **Conditional Breakpoints** - Pause simulation when specific conditions are met (e.g., score tied in 9th inning)
- [ ] **Time Travel** - Jump backward or forward to any snapshot and continue simulation from that point

### Phase 6: Observability

- [ ] **Structured Logging** - Integration with `ILogger` for consistent, queryable log output
- [ ] **Tracing** - OpenTelemetry support for distributed tracing across simulation components
- [ ] **Custom Metrics** - Allow domain models to register their own metrics (e.g., batting average, ERA) alongside engine metrics
- [ ] **Real-time Monitoring** - Expose metrics via HTTP endpoints for dashboards and alerting
- [ ] **Profiling Hooks** - Identify performance bottlenecks by measuring time spent in each model/component

### Phase 7: Distribution & Scale

- [ ] **Distributed Execution** - Run simulation across multiple machines for large-scale Monte Carlo
- [ ] **Work Partitioning** - Split large parameter sweeps across workers with automatic load balancing
- [ ] **Result Aggregation** - Combine results from parallel workers into unified statistics
- [ ] **Cloud Integration** - Azure Batch / AWS Lambda execution backends for serverless scaling
- [ ] **Container Support** - Docker-based simulation workers for consistent execution environments

### Phase 8: Analysis & Visualization

- [ ] **Statistical Analysis** - Built-in aggregation (mean, std dev, percentiles) across Monte Carlo runs
- [ ] **Sensitivity Analysis** - Measure how parameter changes impact outcomes to identify key drivers
- [ ] **Confidence Intervals** - Detect Monte Carlo convergence and estimate required sample size
- [ ] **Export Formats** - Export results to CSV, JSON, Parquet for external analysis tools
- [ ] **Visualization Hooks** - Integration points for UI frameworks to render simulation state in real-time

## Design Principles

1. **Domain Agnostic** - No knowledge of what's being simulated
2. **Reproducible** - Same seed = same results, always
3. **Testable** - All components have interface abstractions
4. **Thread-Safe** - Safe for parallel execution by default
5. **Minimal Dependencies** - Core has no external dependencies
6. **Composable** - Mix and match components as needed

## Integration with DiamondX

DiamondX.Core implements `ISimulation` through `BaseballGameSimulation`, allowing baseball games to be executed by the engine:

```csharp
// Create game configuration
var config = new GameConfig
{
    HomeTeam = giantsLineup,
    AwayTeam = dodgersLineup,
    HomeTeamName = "Giants",
    AwayTeamName = "Dodgers",
    HomePitcher = webbPitcher,
    AwayPitcher = kershawPitcher
};

// Run Monte Carlo simulations
var parameters = new SimulationParameters();
parameters.Set("verbose", false);  // Silent mode for bulk runs

var seeds = Enumerable.Range(0, 10000).Select(_ => rng.Next()).ToArray();

Parallel.For(0, 10000, i =>
{
    var simulation = new BaseballGameSimulation(config);
    var context = CreateContext(seeds[i], parameters);
    
    simulation.Initialize(context);
    while (!simulation.IsComplete)
        simulation.Step();
    
    // Capture results
    var game = simulation.Game!;
    var homeWon = game.HomeScore > game.AwayScore;
});
```

**Command-line Monte Carlo:**

```zsh
# Run 10,000 simulations
dotnet run --project DiamondX.Console -- -mc

# Run 162 games (one season)  
dotnet run --project DiamondX.Console -- -mc --season

# Run single simulation
dotnet run --project DiamondX.Console -- -mc -1
```

## Contributing

When adding new features:

1. Define interfaces first for testability
2. Keep the engine domain-agnostic
3. Add comprehensive unit tests
4. Update this README with new capabilities
