namespace DiamondX.Core.Events;

/// <summary>
/// Interface for handling simulation events.
/// </summary>
public interface IEventHandler
{
    /// <summary>
    /// Event types this handler is interested in. Null or empty means all events.
    /// </summary>
    IEnumerable<string>? EventTypeFilter { get; }

    /// <summary>
    /// Handle an event.
    /// </summary>
    void Handle(ISimulationEvent simulationEvent);
}

/// <summary>
/// Strongly-typed event handler for a specific event type.
/// </summary>
public interface IEventHandler<in TEvent> : IEventHandler where TEvent : ISimulationEvent
{
    /// <summary>
    /// Handle a typed event.
    /// </summary>
    void Handle(TEvent simulationEvent);

    void IEventHandler.Handle(ISimulationEvent simulationEvent)
    {
        if (simulationEvent is TEvent typedEvent)
        {
            Handle(typedEvent);
        }
    }
}
