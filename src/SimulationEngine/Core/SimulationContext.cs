using SimulationEngine.Events;
using SimulationEngine.Random;
using SimulationEngine.State;
using SimulationEngine.Time;

namespace SimulationEngine.Core;

/// <summary>
/// Default implementation of simulation context.
/// </summary>
public sealed class SimulationContext : ISimulationContext
{
    private volatile bool _pauseRequested;
    private volatile bool _stopRequested;

    public Guid RunId { get; }
    public IRandomSource Random { get; }
    public ISimulationClock Clock { get; }
    public EventScheduler Events { get; }
    public IStateManager State { get; }
    public ISimulationParameters Parameters { get; }

    internal SimulationMetrics Metrics { get; }

    public SimulationContext(
        Guid runId,
        IRandomSource random,
        ISimulationClock clock,
        EventScheduler events,
        IStateManager state,
        ISimulationParameters parameters,
        SimulationMetrics metrics)
    {
        RunId = runId;
        Random = random;
        Clock = clock;
        Events = events;
        State = state;
        Parameters = parameters;
        Metrics = metrics;
    }

    public bool IsPauseRequested => _pauseRequested;
    public bool IsStopRequested => _stopRequested;

    public void RequestPause()
    {
        _pauseRequested = true;
    }

    public void RequestStop()
    {
        _stopRequested = true;
    }

    internal void ClearPauseRequest()
    {
        _pauseRequested = false;
    }

    internal void Reset()
    {
        _pauseRequested = false;
        _stopRequested = false;
    }
}
