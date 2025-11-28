namespace SimulationEngine.Core;

/// <summary>
/// Result of a simulation run.
/// </summary>
public sealed class SimulationResult
{
    /// <summary>
    /// Unique identifier for this run.
    /// </summary>
    public Guid RunId { get; }

    /// <summary>
    /// Final status of the simulation.
    /// </summary>
    public SimulationStatus Status { get; }

    /// <summary>
    /// Performance metrics from the run.
    /// </summary>
    public SimulationMetrics Metrics { get; }

    /// <summary>
    /// Random seed used (for reproducibility).
    /// </summary>
    public int Seed { get; }

    /// <summary>
    /// Exception if the simulation failed.
    /// </summary>
    public Exception? Error { get; }

    /// <summary>
    /// Whether the simulation completed successfully.
    /// </summary>
    public bool IsSuccess => Status == SimulationStatus.Completed;

    public SimulationResult(
        Guid runId,
        SimulationStatus status,
        SimulationMetrics metrics,
        int seed,
        Exception? error = null)
    {
        RunId = runId;
        Status = status;
        Metrics = metrics;
        Seed = seed;
        Error = error;
    }

    public override string ToString()
    {
        return $"SimulationResult: {Status} (Seed: {Seed})\n{Metrics}";
    }
}
