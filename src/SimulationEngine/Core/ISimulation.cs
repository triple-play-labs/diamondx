namespace SimulationEngine.Core;

/// <summary>
/// Contract for a runnable simulation model.
/// External projects implement this interface to define their domain-specific simulation.
/// </summary>
public interface ISimulation : IDisposable
{
    /// <summary>
    /// Unique identifier for this simulation type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Version of the simulation model (for compatibility tracking).
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Initialize the simulation with the provided context.
    /// Called once before the first step.
    /// </summary>
    void Initialize(ISimulationContext context);

    /// <summary>
    /// Execute one step of the simulation.
    /// For discrete-event simulations, this processes the next event.
    /// For time-stepped simulations, this advances by one time increment.
    /// </summary>
    /// <returns>Status indicating whether simulation should continue.</returns>
    SimulationStepResult Step();

    /// <summary>
    /// Whether the simulation has reached a terminal state.
    /// </summary>
    bool IsComplete { get; }
}

/// <summary>
/// Result of a single simulation step.
/// </summary>
public enum SimulationStepResult
{
    /// <summary>
    /// Step completed successfully, simulation should continue.
    /// </summary>
    Continue,

    /// <summary>
    /// Simulation has completed normally.
    /// </summary>
    Completed,

    /// <summary>
    /// Simulation was paused (e.g., for user interaction or checkpoint).
    /// </summary>
    Paused,

    /// <summary>
    /// Simulation encountered an error and cannot continue.
    /// </summary>
    Error
}
