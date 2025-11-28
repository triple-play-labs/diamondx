using SimulationEngine.Events;
using SimulationEngine.Random;
using SimulationEngine.Time;
using SimulationEngine.State;

namespace SimulationEngine.Core;

/// <summary>
/// Runtime context provided to simulations during execution.
/// Provides access to engine services without tight coupling.
/// </summary>
public interface ISimulationContext
{
    /// <summary>
    /// Unique identifier for this simulation run.
    /// </summary>
    Guid RunId { get; }

    /// <summary>
    /// Random number generator for this run.
    /// Use this for all stochastic decisions to ensure reproducibility.
    /// </summary>
    IRandomSource Random { get; }

    /// <summary>
    /// Simulation clock for time management.
    /// </summary>
    ISimulationClock Clock { get; }

    /// <summary>
    /// Event scheduler for publishing and subscribing to events.
    /// </summary>
    EventScheduler Events { get; }

    /// <summary>
    /// State manager for snapshots and persistence.
    /// </summary>
    IStateManager State { get; }

    /// <summary>
    /// Configuration parameters for this run.
    /// </summary>
    ISimulationParameters Parameters { get; }

    /// <summary>
    /// Request the simulation to pause after the current step.
    /// </summary>
    void RequestPause();

    /// <summary>
    /// Check if a pause has been requested.
    /// </summary>
    bool IsPauseRequested { get; }

    /// <summary>
    /// Request the simulation to stop.
    /// </summary>
    void RequestStop();

    /// <summary>
    /// Check if a stop has been requested.
    /// </summary>
    bool IsStopRequested { get; }
}

/// <summary>
/// Read-only access to simulation parameters.
/// Parameters can be modified between runs or during pause.
/// </summary>
public interface ISimulationParameters
{
    /// <summary>
    /// Get a parameter value by key.
    /// </summary>
    T Get<T>(string key);

    /// <summary>
    /// Try to get a parameter value by key.
    /// </summary>
    bool TryGet<T>(string key, out T value);

    /// <summary>
    /// Get a parameter value with a default if not found.
    /// </summary>
    T GetOrDefault<T>(string key, T defaultValue);

    /// <summary>
    /// Check if a parameter exists.
    /// </summary>
    bool Contains(string key);

    /// <summary>
    /// Get all parameter keys.
    /// </summary>
    IEnumerable<string> GetKeys();
}
