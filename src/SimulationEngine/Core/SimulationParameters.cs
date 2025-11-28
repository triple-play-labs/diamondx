namespace SimulationEngine.Core;

/// <summary>
/// Mutable parameter collection for simulation configuration.
/// </summary>
public sealed class SimulationParameters : ISimulationParameters
{
    private readonly Dictionary<string, object> _parameters = new();
    private readonly object _lock = new();

    public SimulationParameters()
    {
    }

    public SimulationParameters(IDictionary<string, object> initialValues)
    {
        foreach (var kvp in initialValues)
        {
            _parameters[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Set a parameter value.
    /// </summary>
    public SimulationParameters Set<T>(string key, T value)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (_lock)
        {
            _parameters[key] = value!;
        }
        return this;
    }

    /// <summary>
    /// Remove a parameter.
    /// </summary>
    public bool Remove(string key)
    {
        lock (_lock)
        {
            return _parameters.Remove(key);
        }
    }

    /// <summary>
    /// Clear all parameters.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _parameters.Clear();
        }
    }

    public T Get<T>(string key)
    {
        lock (_lock)
        {
            if (!_parameters.TryGetValue(key, out var value))
                throw new KeyNotFoundException($"Parameter '{key}' not found");

            return (T)value;
        }
    }

    public bool TryGet<T>(string key, out T value)
    {
        lock (_lock)
        {
            if (_parameters.TryGetValue(key, out var obj) && obj is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default!;
            return false;
        }
    }

    public T GetOrDefault<T>(string key, T defaultValue)
    {
        return TryGet<T>(key, out var value) ? value : defaultValue;
    }

    public bool Contains(string key)
    {
        lock (_lock)
        {
            return _parameters.ContainsKey(key);
        }
    }

    public IEnumerable<string> Keys
    {
        get
        {
            lock (_lock)
            {
                return _parameters.Keys.ToList();
            }
        }
    }

    /// <summary>
    /// Create a read-only copy of current parameters.
    /// </summary>
    public ISimulationParameters AsReadOnly()
    {
        lock (_lock)
        {
            return new SimulationParameters(new Dictionary<string, object>(_parameters));
        }
    }
}
