using System.Diagnostics;

namespace SimulationEngine.Core;

/// <summary>
/// Metrics collected during simulation execution.
/// Tracks engine-level performance, not domain-specific data.
/// </summary>
public sealed class SimulationMetrics
{
    private readonly object _lock = new();
    private readonly Stopwatch _wallClockTime = new();
    private long _stepCount;
    private long _eventCount;
    private long _snapshotCount;
    private long _errorCount;

    /// <summary>
    /// Unique identifier for the simulation run.
    /// </summary>
    public Guid RunId { get; }

    /// <summary>
    /// When the simulation started.
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// When the simulation ended (null if still running).
    /// </summary>
    public DateTime? EndTime { get; private set; }

    /// <summary>
    /// Total wall-clock time elapsed.
    /// </summary>
    public TimeSpan WallClockElapsed => _wallClockTime.Elapsed;

    /// <summary>
    /// Total simulation time at end of run.
    /// </summary>
    public TimeSpan SimulationTime { get; private set; }

    /// <summary>
    /// Number of simulation steps executed.
    /// </summary>
    public long StepCount
    {
        get { lock (_lock) { return _stepCount; } }
    }

    /// <summary>
    /// Number of events published.
    /// </summary>
    public long EventCount
    {
        get { lock (_lock) { return _eventCount; } }
    }

    /// <summary>
    /// Number of state snapshots created.
    /// </summary>
    public long SnapshotCount
    {
        get { lock (_lock) { return _snapshotCount; } }
    }

    /// <summary>
    /// Number of errors encountered.
    /// </summary>
    public long ErrorCount
    {
        get { lock (_lock) { return _errorCount; } }
    }

    /// <summary>
    /// Steps per second (wall-clock).
    /// </summary>
    public double StepsPerSecond
    {
        get
        {
            var elapsed = WallClockElapsed.TotalSeconds;
            return elapsed > 0 ? StepCount / elapsed : 0;
        }
    }

    /// <summary>
    /// Simulation time ratio (simulation time / wall-clock time).
    /// Greater than 1 means simulation runs faster than real-time.
    /// </summary>
    public double TimeRatio
    {
        get
        {
            var wallClock = WallClockElapsed.TotalMilliseconds;
            return wallClock > 0 ? SimulationTime.TotalMilliseconds / wallClock : 0;
        }
    }

    /// <summary>
    /// Final status of the simulation.
    /// </summary>
    public SimulationStatus Status { get; private set; } = SimulationStatus.NotStarted;

    public SimulationMetrics(Guid runId)
    {
        RunId = runId;
    }

    internal void Start()
    {
        StartTime = DateTime.UtcNow;
        Status = SimulationStatus.Running;
        _wallClockTime.Start();
    }

    internal void Stop(SimulationStatus finalStatus, TimeSpan simulationTime)
    {
        _wallClockTime.Stop();
        EndTime = DateTime.UtcNow;
        SimulationTime = simulationTime;
        Status = finalStatus;
    }

    internal void RecordStep()
    {
        lock (_lock) { _stepCount++; }
    }

    internal void RecordEvent()
    {
        lock (_lock) { _eventCount++; }
    }

    internal void RecordSnapshot()
    {
        lock (_lock) { _snapshotCount++; }
    }

    internal void RecordError()
    {
        lock (_lock) { _errorCount++; }
    }

    /// <summary>
    /// Create a summary string for logging/display.
    /// </summary>
    public override string ToString()
    {
        return $"Run {RunId:N}\n" +
               $"  Status: {Status}\n" +
               $"  Steps: {StepCount:N0} ({StepsPerSecond:N0}/sec)\n" +
               $"  Events: {EventCount:N0}\n" +
               $"  Sim Time: {SimulationTime}\n" +
               $"  Wall Time: {WallClockElapsed}\n" +
               $"  Time Ratio: {TimeRatio:N2}x";
    }
}

/// <summary>
/// Overall status of a simulation run.
/// </summary>
public enum SimulationStatus
{
    NotStarted,
    Running,
    Paused,
    Completed,
    Stopped,
    Error
}
