using DiamondX.Core.Random;

namespace DiamondX.Tests;

/// <summary>
/// Deterministic random source for testing.
/// Returns values from a predefined queue.
/// </summary>
public class TestRandomSource : IRandomSource
{
    private readonly Queue<double> _values;

    public TestRandomSource(Queue<double> values)
    {
        _values = values ?? throw new ArgumentNullException(nameof(values));
    }

    public double NextDouble()
    {
        if (_values.Count == 0)
        {
            throw new InvalidOperationException("No more random values available in test queue.");
        }
        return _values.Dequeue();
    }
}
