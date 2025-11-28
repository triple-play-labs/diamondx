namespace SimulationEngine.Time;

/// <summary>
/// Default implementation of the simulation clock.
/// Thread-safe for use in parallel simulation scenarios.
/// </summary>
public sealed class SimulationClock : ISimulationClock
{
    private readonly object _lock = new();
    private TimeSpan _currentTime;
    private long _tickCount;

    /// <summary>
    /// The clock mode (informational - doesn't change behavior).
    /// </summary>
    public ClockMode Mode { get; }

    /// <summary>
    /// For fixed-step mode, the default time increment per tick.
    /// </summary>
    public TimeSpan DefaultStepSize { get; }

    public SimulationClock(ClockMode mode = ClockMode.DiscreteEvent, TimeSpan? defaultStepSize = null)
    {
        Mode = mode;
        DefaultStepSize = defaultStepSize ?? TimeSpan.FromSeconds(1);
        _currentTime = TimeSpan.Zero;
        _tickCount = 0;
    }

    public TimeSpan CurrentTime
    {
        get
        {
            lock (_lock)
            {
                return _currentTime;
            }
        }
    }

    public long TickCount
    {
        get
        {
            lock (_lock)
            {
                return _tickCount;
            }
        }
    }

    public void Advance(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
            throw new ArgumentException("Cannot advance time backwards", nameof(delta));

        lock (_lock)
        {
            _currentTime += delta;
            _tickCount++;
        }
    }

    public void SetTime(TimeSpan time)
    {
        lock (_lock)
        {
            if (time < _currentTime)
                throw new InvalidOperationException(
                    $"Cannot set time backwards: current={_currentTime}, requested={time}");
            _currentTime = time;
        }
    }

    public void Tick()
    {
        lock (_lock)
        {
            _tickCount++;
            if (Mode == ClockMode.FixedStep)
            {
                _currentTime += DefaultStepSize;
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _currentTime = TimeSpan.Zero;
            _tickCount = 0;
        }
    }

    public ClockSnapshot CreateSnapshot()
    {
        lock (_lock)
        {
            return new ClockSnapshot(_currentTime, _tickCount);
        }
    }

    public void RestoreSnapshot(ClockSnapshot snapshot)
    {
        lock (_lock)
        {
            _currentTime = snapshot.Time;
            _tickCount = snapshot.TickCount;
        }
    }
}
