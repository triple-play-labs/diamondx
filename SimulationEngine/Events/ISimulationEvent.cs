namespace SimulationEngine.Events;

/// <summary>
/// Base interface for all simulation events.
/// Events are immutable records that capture what happened in the simulation.
/// </summary>
public interface ISimulationEvent
{
    /// <summary>
    /// Sequence number assigned by the EventScheduler.
    /// </summary>
    long Sequence { get; }

    /// <summary>
    /// Simulation time when the event occurred.
    /// </summary>
    TimeSpan Timestamp { get; }

    /// <summary>
    /// Type identifier for the event (e.g., "baseball.atbat.completed").
    /// </summary>
    string EventType { get; }
}

/// <summary>
/// Abstract base record for simulation events.
/// Provides default implementation of sequence and timestamp.
/// </summary>
public abstract record SimulationEventBase : ISimulationEvent
{
    public long Sequence { get; init; }
    public TimeSpan Timestamp { get; init; }
    public abstract string EventType { get; }
}
