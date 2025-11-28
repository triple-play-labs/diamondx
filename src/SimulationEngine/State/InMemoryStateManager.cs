using System.Text.Json;

namespace SimulationEngine.State;

/// <summary>
/// In-memory implementation of state management.
/// Suitable for single-run simulations and testing.
/// </summary>
public sealed class InMemoryStateManager : IStateManager
{
    private readonly Dictionary<string, StoredSnapshot> _snapshots = new();
    private readonly object _lock = new();
    private readonly Func<(long TickCount, TimeSpan SimulationTime)> _clockProvider;

    public InMemoryStateManager(Func<(long TickCount, TimeSpan SimulationTime)>? clockProvider = null)
    {
        _clockProvider = clockProvider ?? (() => (0, TimeSpan.Zero));
    }

    public void CreateSnapshot(string name, object state)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(state);

        var (tickCount, simTime) = _clockProvider();
        var metadata = new SnapshotMetadata(
            name,
            DateTime.UtcNow,
            tickCount,
            simTime
        );

        // Deep copy via serialization
        var json = JsonSerializer.Serialize(state, state.GetType());

        lock (_lock)
        {
            _snapshots[name] = new StoredSnapshot(metadata, json, state.GetType());
        }
    }

    public string CreateSnapshot(object state)
    {
        var name = $"snapshot_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}";
        CreateSnapshot(name, state);
        return name;
    }

    public T? GetSnapshot<T>(string name) where T : class
    {
        lock (_lock)
        {
            if (!_snapshots.TryGetValue(name, out var stored))
                return null;

            return JsonSerializer.Deserialize<T>(stored.Json);
        }
    }

    public IEnumerable<string> ListSnapshots()
    {
        lock (_lock)
        {
            return _snapshots.Keys.ToList();
        }
    }

    public bool DeleteSnapshot(string name)
    {
        lock (_lock)
        {
            return _snapshots.Remove(name);
        }
    }

    public void ClearSnapshots()
    {
        lock (_lock)
        {
            _snapshots.Clear();
        }
    }

    public bool HasSnapshot(string name)
    {
        lock (_lock)
        {
            return _snapshots.ContainsKey(name);
        }
    }

    public SnapshotMetadata? GetMetadata(string name)
    {
        lock (_lock)
        {
            return _snapshots.TryGetValue(name, out var stored) ? stored.Metadata : null;
        }
    }

    public byte[] ExportSnapshots()
    {
        lock (_lock)
        {
            var exportData = _snapshots.Select(kvp => new ExportedSnapshot(
                kvp.Value.Metadata,
                kvp.Value.Json,
                kvp.Value.StateType.AssemblyQualifiedName!
            )).ToList();

            return JsonSerializer.SerializeToUtf8Bytes(exportData);
        }
    }

    public void ImportSnapshots(byte[] data)
    {
        var importedList = JsonSerializer.Deserialize<List<ExportedSnapshot>>(data);
        if (importedList == null) return;

        lock (_lock)
        {
            foreach (var imported in importedList)
            {
                var stateType = Type.GetType(imported.TypeName);
                if (stateType == null) continue;

                _snapshots[imported.Metadata.Name] = new StoredSnapshot(
                    imported.Metadata,
                    imported.Json,
                    stateType
                );
            }
        }
    }

    private sealed record StoredSnapshot(SnapshotMetadata Metadata, string Json, Type StateType);
    private sealed record ExportedSnapshot(SnapshotMetadata Metadata, string Json, string TypeName);
}
