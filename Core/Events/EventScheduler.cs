namespace DiamondX.Core.Events;

/// <summary>
/// Central event scheduler for the simulation engine.
/// Queues events, assigns sequence numbers, and dispatches to handlers.
/// Designed to be domain-agnostic - baseball logic lives in event types and handlers.
/// </summary>
public class EventScheduler
{
    private readonly List<IEventHandler> _handlers = new();
    private readonly List<ISimulationEvent> _eventLog = new();
    private readonly object _lock = new();

    private long _sequenceCounter;
    private TimeSpan _currentTime = TimeSpan.Zero;

    /// <summary>
    /// When true, prints debug information about event traffic to stderr.
    /// </summary>
    public bool DebugMode { get; set; }

    /// <summary>
    /// Read-only access to all dispatched events.
    /// </summary>
    public IReadOnlyList<ISimulationEvent> EventLog => _eventLog;

    /// <summary>
    /// Current simulation time.
    /// </summary>
    public TimeSpan CurrentTime => _currentTime;

    /// <summary>
    /// Register an event handler.
    /// </summary>
    public void RegisterHandler(IEventHandler handler)
    {
        lock (_lock)
        {
            _handlers.Add(handler);
        }
    }

    /// <summary>
    /// Unregister an event handler.
    /// </summary>
    public void UnregisterHandler(IEventHandler handler)
    {
        lock (_lock)
        {
            _handlers.Remove(handler);
        }
    }

    /// <summary>
    /// Advance simulation time.
    /// </summary>
    public void AdvanceTime(TimeSpan delta)
    {
        _currentTime += delta;
    }

    /// <summary>
    /// Set absolute simulation time.
    /// </summary>
    public void SetTime(TimeSpan time)
    {
        _currentTime = time;
    }

    /// <summary>
    /// Publish an event: assign sequence/timestamp, log it, and dispatch to handlers.
    /// </summary>
    public TEvent Publish<TEvent>(TEvent simulationEvent) where TEvent : SimulationEventBase
    {
        long sequence;
        TimeSpan timestamp;

        lock (_lock)
        {
            sequence = ++_sequenceCounter;
            timestamp = _currentTime;
        }

        // Create new instance with assigned sequence and timestamp
        var finalEvent = simulationEvent with
        {
            Sequence = sequence,
            Timestamp = timestamp
        };

        lock (_lock)
        {
            _eventLog.Add(finalEvent);
        }

        if (DebugMode)
        {
            Console.Error.WriteLine($"[DEBUG] #{finalEvent.Sequence:D4} @{finalEvent.Timestamp:mm':'ss'.'fff} | {finalEvent.EventType}");
        }

        DispatchToHandlers(finalEvent);

        return finalEvent;
    }

    /// <summary>
    /// Publish a pre-built event (for events that don't inherit SimulationEventBase).
    /// </summary>
    public void PublishRaw(ISimulationEvent simulationEvent)
    {
        lock (_lock)
        {
            _eventLog.Add(simulationEvent);
        }

        DispatchToHandlers(simulationEvent);
    }

    private void DispatchToHandlers(ISimulationEvent simulationEvent)
    {
        List<IEventHandler> handlers;
        lock (_lock)
        {
            handlers = _handlers.ToList();
        }

        foreach (var handler in handlers)
        {
            var filter = handler.EventTypeFilter;
            if (filter == null || !filter.Any() || filter.Contains(simulationEvent.EventType))
            {
                try
                {
                    if (DebugMode)
                    {
                        Console.Error.WriteLine($"[DEBUG]   → Dispatching to {handler.GetType().Name}");
                    }
                    handler.Handle(simulationEvent);
                }
                catch (Exception ex)
                {
                    // Log but don't throw - one handler failure shouldn't stop others
                    Console.Error.WriteLine($"Event handler error: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Clear event log (useful for testing or memory management).
    /// </summary>
    public void ClearLog()
    {
        lock (_lock)
        {
            _eventLog.Clear();
        }
    }

    /// <summary>
    /// Reset scheduler state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _eventLog.Clear();
            _sequenceCounter = 0;
            _currentTime = TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Get events of a specific type from the log.
    /// </summary>
    public IEnumerable<TEvent> GetEvents<TEvent>() where TEvent : ISimulationEvent
    {
        lock (_lock)
        {
            return _eventLog.OfType<TEvent>().ToList();
        }
    }

    /// <summary>
    /// Get events matching a predicate.
    /// </summary>
    public IEnumerable<ISimulationEvent> GetEvents(Func<ISimulationEvent, bool> predicate)
    {
        lock (_lock)
        {
            return _eventLog.Where(predicate).ToList();
        }
    }
}
