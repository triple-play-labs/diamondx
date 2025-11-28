namespace SimulationEngine.State;

/// <summary>
/// Manages simulation state snapshots for persistence and replay.
/// Enables mid-simulation parameter changes and branching scenarios.
/// </summary>
public interface IStateManager
{
    /// <summary>
    /// Create a named snapshot of current simulation state.
    /// </summary>
    /// <param name="name">Unique name for this snapshot.</param>
    /// <param name="state">The state object to snapshot (must be serializable).</param>
    void CreateSnapshot(string name, object state);

    /// <summary>
    /// Create an auto-named snapshot with timestamp.
    /// </summary>
    string CreateSnapshot(object state);

    /// <summary>
    /// Retrieve a snapshot by name.
    /// </summary>
    T? GetSnapshot<T>(string name) where T : class;

    /// <summary>
    /// List all available snapshot names.
    /// </summary>
    IEnumerable<string> ListSnapshots();

    /// <summary>
    /// Delete a snapshot by name.
    /// </summary>
    bool DeleteSnapshot(string name);

    /// <summary>
    /// Clear all snapshots.
    /// </summary>
    void ClearSnapshots();

    /// <summary>
    /// Check if a snapshot exists.
    /// </summary>
    bool HasSnapshot(string name);

    /// <summary>
    /// Get metadata about a snapshot.
    /// </summary>
    SnapshotMetadata? GetMetadata(string name);

    /// <summary>
    /// Export all snapshots to a persistence format.
    /// </summary>
    byte[] ExportSnapshots();

    /// <summary>
    /// Import snapshots from a persistence format.
    /// </summary>
    void ImportSnapshots(byte[] data);
}

/// <summary>
/// Metadata about a state snapshot.
/// </summary>
public sealed record SnapshotMetadata(
    string Name,
    DateTime CreatedAt,
    long TickCount,
    TimeSpan SimulationTime,
    string? Description = null
);

/// <summary>
/// Interface for simulation states that support snapshotting.
/// Implement this in your domain state classes for automatic serialization.
/// </summary>
public interface ISnapshotable<T> where T : class
{
    /// <summary>
    /// Create a deep copy of the current state.
    /// </summary>
    T CreateSnapshot();

    /// <summary>
    /// Restore state from a snapshot.
    /// </summary>
    void RestoreFrom(T snapshot);
}
