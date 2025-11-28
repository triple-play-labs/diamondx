namespace SimulationEngine.Orchestration;

using SimulationEngine.Core;
using SimulationEngine.Events;
using SimulationEngine.Random;
using SimulationEngine.State;
using SimulationEngine.Time;

// Suppress cognitive complexity warning - orchestration logic is inherently complex
#pragma warning disable S3776

/// <summary>
/// Coordinates multiple simulation models within a single run.
/// Ensures deterministic execution order based on dependencies and priority.
/// </summary>
public sealed class SimulationOrchestrator : ISimulationOrchestrator
{
    private readonly List<ModelRegistration> _registrations = new();
    private readonly Dictionary<string, ModelRegistration> _registrationById = new();
    private readonly SharedContext _sharedContext = new();
    private ISimulationContext? _context;
    private List<ModelRegistration>? _executionOrder;
    private long _stepCount;

    public Guid RunId { get; } = Guid.NewGuid();
    public OrchestratorState State { get; private set; } = OrchestratorState.Created;
    public IReadOnlyList<IModelRegistration> Models => _executionOrder?.AsReadOnly() ?? _registrations.AsReadOnly();
    public bool IsComplete => State == OrchestratorState.Completed;

    /// <summary>
    /// Shared context for cross-model communication.
    /// </summary>
    public ISharedContext SharedContext => _sharedContext;

    public event EventHandler<ModelStepEventArgs>? BeforeModelStep;
    public event EventHandler<ModelStepEventArgs>? AfterModelStep;
    public event EventHandler<BarrierEventArgs>? BarrierReached;

    public IModelRegistration Register(ISimulation model, ModelOptions? options = null)
    {
        return Register(model, options ?? new ModelOptions(), Array.Empty<string>());
    }

    public IModelRegistration Register(ISimulation model, ModelOptions options, params string[] dependsOn)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (State != OrchestratorState.Created && State != OrchestratorState.Ready)
            throw new InvalidOperationException($"Cannot register models in state {State}.");

        options ??= new ModelOptions();
        var id = options.Id ?? model.Name;

        if (_registrationById.ContainsKey(id))
            throw new ArgumentException($"Model with ID '{id}' is already registered.", nameof(model));

        var registration = new ModelRegistration(id, model, options, dependsOn);
        _registrations.Add(registration);
        _registrationById[id] = registration;

        State = OrchestratorState.Ready;
        return registration;
    }

    public void Initialize(ISimulationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (State != OrchestratorState.Ready)
            throw new InvalidOperationException($"Cannot initialize in state {State}. Must be in Ready state.");

        if (_registrations.Count == 0)
            throw new InvalidOperationException("No models registered. Register at least one model before initializing.");

        _context = context;

        // Resolve execution order via topological sort
        _executionOrder = ResolveExecutionOrder();

        // Initialize models in execution order
        foreach (var registration in _executionOrder)
        {
            registration.SetState(ModelState.Initializing);
            try
            {
                // Create model-specific context with merged parameters
                var modelContext = CreateModelContext(context, registration);
                registration.Model.Initialize(modelContext);
                registration.SetState(ModelState.Ready);
            }
            catch (Exception ex)
            {
                registration.SetState(ModelState.Error);
                registration.SetError(ex);

                if (!registration.Options.Optional)
                {
                    State = OrchestratorState.Error;
                    throw new OrchestratorException($"Failed to initialize model '{registration.Id}'.", ex);
                }
            }
        }

        State = OrchestratorState.Running;
    }

    public OrchestratorStepResult Step()
    {
        if (State != OrchestratorState.Running)
            throw new InvalidOperationException($"Cannot step in state {State}. Must be in Running state.");

        if (_executionOrder is null || _context is null)
            throw new InvalidOperationException("Orchestrator not initialized.");

        _stepCount++;
        var hasActiveModels = false;

        foreach (var registration in _executionOrder)
        {
            // Skip completed or errored models (unless ContinueAfterComplete)
            if (registration.State == ModelState.Error)
                continue;

            if (registration.State == ModelState.Completed && !registration.Options.ContinueAfterComplete)
                continue;

            if (registration.Model.IsComplete && !registration.Options.ContinueAfterComplete)
            {
                registration.SetState(ModelState.Completed);
                continue;
            }

            hasActiveModels = true;

            // Raise before event
            var beforeArgs = new ModelStepEventArgs
            {
                Model = registration,
                StepNumber = _stepCount
            };
            BeforeModelStep?.Invoke(this, beforeArgs);

            // Execute model step
            registration.SetState(ModelState.Stepping);
            SimulationStepResult? stepResult = null;
            Exception? error = null;

            try
            {
                stepResult = registration.Model.Step();
                registration.IncrementStepCount();

                if (stepResult == SimulationStepResult.Completed || registration.Model.IsComplete)
                {
                    registration.SetState(ModelState.Completed);
                }
                else
                {
                    registration.SetState(ModelState.Ready);
                }
            }
            catch (Exception ex)
            {
                error = ex;
                registration.SetState(ModelState.Error);
                registration.SetError(ex);

                if (!registration.Options.Optional)
                {
                    State = OrchestratorState.Error;

                    // Raise after event with error
                    AfterModelStep?.Invoke(this, new ModelStepEventArgs
                    {
                        Model = registration,
                        StepNumber = _stepCount,
                        Result = SimulationStepResult.Error,
                        Error = ex
                    });

                    throw new OrchestratorException($"Model '{registration.Id}' failed during step {_stepCount}.", ex);
                }
            }

            // Raise after event
            var afterArgs = new ModelStepEventArgs
            {
                Model = registration,
                StepNumber = _stepCount,
                Result = stepResult,
                Error = error
            };
            AfterModelStep?.Invoke(this, afterArgs);
        }

        // Raise barrier event
        BarrierReached?.Invoke(this, new BarrierEventArgs
        {
            StepNumber = _stepCount,
            Models = _executionOrder.AsReadOnly(),
            SimulatedTime = _context.Clock.CurrentTime
        });

        // Check if all models are complete
        if (!hasActiveModels)
        {
            State = OrchestratorState.Completed;
            return OrchestratorStepResult.Completed;
        }

        return OrchestratorStepResult.Continue;
    }

    /// <summary>
    /// Resolves execution order using topological sort based on dependencies.
    /// </summary>
    private List<ModelRegistration> ResolveExecutionOrder()
    {
        var result = new List<ModelRegistration>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        // Sort by priority first, then process dependencies
        var sortedByPriority = _registrations.OrderBy(r => r.Options.Priority).ToList();

        foreach (var registration in sortedByPriority)
        {
            Visit(registration, visited, visiting, result);
        }

        return result;
    }

    private void Visit(ModelRegistration registration, HashSet<string> visited, HashSet<string> visiting, List<ModelRegistration> result)
    {
        if (visited.Contains(registration.Id))
            return;

        if (visiting.Contains(registration.Id))
            throw new OrchestratorException($"Circular dependency detected involving model '{registration.Id}'.");

        visiting.Add(registration.Id);

        // Visit dependencies first
        foreach (var depId in registration.Dependencies)
        {
            if (!_registrationById.TryGetValue(depId, out var dependency))
                throw new OrchestratorException($"Model '{registration.Id}' depends on unknown model '{depId}'.");

            Visit(dependency, visited, visiting, result);
        }

        visiting.Remove(registration.Id);
        visited.Add(registration.Id);
        result.Add(registration);
    }

    private ISimulationContext CreateModelContext(ISimulationContext baseContext, ModelRegistration registration)
    {
        // Merge model-specific parameters with base parameters
        var mergedParams = new SimulationParameters();

        // Copy base parameters
        foreach (var key in baseContext.Parameters.GetKeys())
        {
            if (baseContext.Parameters.TryGet<object>(key, out var value))
            {
                mergedParams.Set(key, value);
            }
        }

        // Override with model-specific parameters
        if (registration.Options.Parameters is not null)
        {
            foreach (var key in registration.Options.Parameters.GetKeys())
            {
                if (registration.Options.Parameters.TryGet<object>(key, out var value))
                {
                    mergedParams.Set(key, value);
                }
            }
        }

        // Create a context that wraps the base context but with merged parameters
        return new OrchestratedModelContext(baseContext, mergedParams, _sharedContext);
    }

    public void Dispose()
    {
        foreach (var registration in _registrations)
        {
            try
            {
                registration.Model.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _registrations.Clear();
        _registrationById.Clear();
        _executionOrder?.Clear();
        _sharedContext.Clear();
    }
}

/// <summary>
/// Internal model registration tracking.
/// </summary>
internal sealed class ModelRegistration : IModelRegistration
{
    public string Id { get; }
    public ISimulation Model { get; }
    public ModelOptions Options { get; }
    public IReadOnlyList<string> Dependencies { get; }
    public ModelState State { get; private set; } = ModelState.Registered;
    public long StepCount { get; private set; }
    public Exception? LastError { get; private set; }

    public ModelRegistration(string id, ISimulation model, ModelOptions options, string[] dependencies)
    {
        Id = id;
        Model = model;
        Options = options;
        Dependencies = dependencies.ToList().AsReadOnly();
    }

    internal void SetState(ModelState state) => State = state;
    internal void IncrementStepCount() => StepCount++;
    internal void SetError(Exception ex) => LastError = ex;
}

/// <summary>
/// Context wrapper that provides model-specific parameters and shared context access.
/// </summary>
public sealed class OrchestratedModelContext : ISimulationContext
{
    private readonly ISimulationContext _baseContext;
    private readonly ISharedContext _sharedContext;
    private readonly ISimulationParameters _parameters;

    public Guid RunId => _baseContext.RunId;
    public IRandomSource Random => _baseContext.Random;
    public ISimulationClock Clock => _baseContext.Clock;
    public EventScheduler Events => _baseContext.Events;
    public IStateManager State => _baseContext.State;
    public ISimulationParameters Parameters => _parameters;

    public bool IsPauseRequested => _baseContext.IsPauseRequested;
    public bool IsStopRequested => _baseContext.IsStopRequested;

    /// <summary>
    /// Shared context for cross-model communication.
    /// </summary>
    public ISharedContext Shared => _sharedContext;

    public OrchestratedModelContext(ISimulationContext baseContext, ISimulationParameters parameters, ISharedContext sharedContext)
    {
        _baseContext = baseContext;
        _parameters = parameters;
        _sharedContext = sharedContext;
    }

    public void RequestPause() => _baseContext.RequestPause();
    public void RequestStop() => _baseContext.RequestStop();
}

/// <summary>
/// Exception thrown by the orchestrator for configuration or runtime errors.
/// </summary>
public class OrchestratorException : Exception
{
    public OrchestratorException(string message) : base(message) { }
    public OrchestratorException(string message, Exception innerException) : base(message, innerException) { }
}
