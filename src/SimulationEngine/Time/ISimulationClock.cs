namespace SimulationEngine.Time;

/// <summary>
/// Abstraction for simulation time management.
/// Supports both discrete-event and time-stepped simulation styles.
/// </summary>
public interface ISimulationClock
{
    /// <summary>
    /// Current simulation time.
    /// </summary>
    TimeSpan CurrentTime { get; }

    /// <summary>
    /// Number of ticks/steps that have occurred.
    /// </summary>
    long TickCount { get; }

    /// <summary>
    /// Advance time by a fixed delta (for time-stepped simulations).
    /// </summary>
    void Advance(TimeSpan delta);

    /// <summary>
    /// Set time to a specific value (for discrete-event simulations).
    /// </summary>
    void SetTime(TimeSpan time);

    /// <summary>
    /// Increment the tick counter.
    /// </summary>
    void Tick();

    /// <summary>
    /// Reset clock to initial state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Create a snapshot of current clock state.
    /// </summary>
    ClockSnapshot CreateSnapshot();

    /// <summary>
    /// Restore clock state from a snapshot.
    /// </summary>
    void RestoreSnapshot(ClockSnapshot snapshot);
}

/// <summary>
/// Immutable snapshot of clock state for persistence/replay.
/// </summary>
public sealed record ClockSnapshot(TimeSpan Time, long TickCount);

/// <summary>
/// Clock mode determines how time advances.
/// </summary>
public enum ClockMode
{
    /// <summary>
    /// Time advances by fixed increments (time-stepped).
    /// </summary>
    FixedStep,

    /// <summary>
    /// Time jumps to the next scheduled event (discrete-event).
    /// </summary>
    DiscreteEvent,

    /// <summary>
    /// Time advances in real-time (for interactive simulations).
    /// </summary>
    RealTime
}
