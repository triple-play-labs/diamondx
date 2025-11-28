using SimulationEngine.Events;
using SimulationEngine.Random;
using SimulationEngine.State;
using SimulationEngine.Time;

namespace SimulationEngine.Core;

/// <summary>
/// Executes simulation models and manages their lifecycle.
/// Thread-safe for running multiple simulations in parallel.
/// </summary>
public sealed class SimulationRunner
{
    private readonly SimulationRunnerOptions _options;

    public SimulationRunner() : this(new SimulationRunnerOptions())
    {
    }

    public SimulationRunner(SimulationRunnerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Run a simulation to completion.
    /// </summary>
    public SimulationResult Run(ISimulation simulation, SimulationParameters? parameters = null)
    {
        return Run(simulation, null, parameters);
    }

    /// <summary>
    /// Run a simulation with a specific random seed.
    /// </summary>
    public SimulationResult Run(ISimulation simulation, int? seed, SimulationParameters? parameters = null)
    {
        var runId = Guid.NewGuid();
        var metrics = new SimulationMetrics(runId);

        // Create random source
        var random = seed.HasValue
            ? new SeedableRandomSource(seed.Value)
            : new SeedableRandomSource();

        // Create clock
        var clock = new SimulationClock(_options.ClockMode, _options.DefaultTimeStep);

        // Create event scheduler
        var events = new EventScheduler();

        // Create state manager with clock provider
        var state = new InMemoryStateManager(() => (clock.TickCount, clock.CurrentTime));

        // Create context
        var context = new SimulationContext(
            runId,
            random,
            clock,
            events,
            state,
            parameters ?? new SimulationParameters(),
            metrics
        );

        // Track events for metrics
        events.RegisterHandler(new MetricsEventHandler(metrics));

        try
        {
            // Initialize
            metrics.Start();
            simulation.Initialize(context);

            // Run loop
            var result = RunLoop(simulation, context, metrics);

            return result;
        }
        catch (Exception ex)
        {
            metrics.RecordError();
            metrics.Stop(SimulationStatus.Error, clock.CurrentTime);

            return new SimulationResult(
                runId,
                SimulationStatus.Error,
                metrics,
                random is SeedableRandomSource seedable ? seedable.Seed : 0,
                ex
            );
        }
        finally
        {
            simulation.Dispose();
        }
    }

    /// <summary>
    /// Run multiple simulations in parallel (for Monte Carlo scenarios).
    /// </summary>
    public IReadOnlyList<SimulationResult> RunParallel(
        Func<ISimulation> simulationFactory,
        int count,
        SimulationParameters? sharedParameters = null,
        int? baseSeed = null)
    {
        var results = new SimulationResult[count];
        var seeds = GenerateSeeds(count, baseSeed);

        Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = _options.MaxParallelism }, i =>
        {
            var simulation = simulationFactory();
            results[i] = Run(simulation, seeds[i], sharedParameters);
        });

        return results;
    }

    private SimulationResult RunLoop(ISimulation simulation, SimulationContext context, SimulationMetrics metrics)
    {
        var maxSteps = _options.MaxSteps;
        var stepCount = 0L;

        while (!simulation.IsComplete && !context.IsStopRequested)
        {
            // Check for pause request
            if (context.IsPauseRequested)
            {
                return CreateResult(context, metrics, SimulationStatus.Paused);
            }

            // Check step limit
            if (maxSteps.HasValue && stepCount >= maxSteps.Value)
            {
                return CreateResult(context, metrics, SimulationStatus.Stopped);
            }

            // Execute step and handle result
            var stepResult = simulation.Step();
            metrics.RecordStep();
            stepCount++;

            var result = HandleStepResult(stepResult, context, metrics);
            if (result is not null)
            {
                return result;
            }
        }

        // Normal completion
        var finalStatus = context.IsStopRequested ? SimulationStatus.Stopped : SimulationStatus.Completed;
        return CreateResult(context, metrics, finalStatus);
    }

    private static SimulationResult? HandleStepResult(SimulationStepResult stepResult, SimulationContext context, SimulationMetrics metrics)
    {
        return stepResult switch
        {
            SimulationStepResult.Continue => null,
            SimulationStepResult.Completed => CreateResult(context, metrics, SimulationStatus.Completed),
            SimulationStepResult.Paused => CreateResult(context, metrics, SimulationStatus.Paused),
            SimulationStepResult.Error => CreateErrorResult(context, metrics),
            _ => null
        };
    }

    private static SimulationResult CreateResult(SimulationContext context, SimulationMetrics metrics, SimulationStatus status)
    {
        metrics.Stop(status, context.Clock.CurrentTime);
        return new SimulationResult(
            context.RunId,
            status,
            metrics,
            GetSeed(context)
        );
    }

    private static SimulationResult CreateErrorResult(SimulationContext context, SimulationMetrics metrics)
    {
        metrics.RecordError();
        return CreateResult(context, metrics, SimulationStatus.Error);
    }

    private static int GetSeed(SimulationContext context)
    {
        return context.Random is SeedableRandomSource seedable ? seedable.Seed : 0;
    }

    private static int[] GenerateSeeds(int count, int? baseSeed)
    {
        var seeds = new int[count];
        var rng = baseSeed.HasValue ? new System.Random(baseSeed.Value) : System.Random.Shared;

        for (int i = 0; i < count; i++)
        {
            seeds[i] = rng.Next();
        }

        return seeds;
    }

    /// <summary>
    /// Internal handler to track events for metrics.
    /// </summary>
    private sealed class MetricsEventHandler : IEventHandler
    {
        private readonly SimulationMetrics _metrics;

        public MetricsEventHandler(SimulationMetrics metrics) => _metrics = metrics;
        public IEnumerable<string>? EventTypeFilter => null;
        public void Handle(ISimulationEvent simulationEvent) => _metrics.RecordEvent();
    }
}

/// <summary>
/// Configuration options for the simulation runner.
/// </summary>
public sealed class SimulationRunnerOptions
{
    /// <summary>
    /// Maximum steps before stopping (null for unlimited).
    /// </summary>
    public long? MaxSteps { get; set; }

    /// <summary>
    /// Clock mode for simulations.
    /// </summary>
    public ClockMode ClockMode { get; set; } = ClockMode.DiscreteEvent;

    /// <summary>
    /// Default time step for fixed-step simulations.
    /// </summary>
    public TimeSpan DefaultTimeStep { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum parallelism for RunParallel.
    /// </summary>
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;
}
