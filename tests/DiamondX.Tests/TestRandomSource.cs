using SimulationEngine.Random;

namespace DiamondX.Tests;

public sealed class TestRandomSource : IRandomSource
{
    private readonly Queue<double> _values;

    public TestRandomSource(IEnumerable<double> values)
    {
        _values = new Queue<double>(values);
    }

    public double NextDouble()
    {
        if (_values.Count == 0)
        {
            throw new InvalidOperationException("TestRandomSource exhausted.");
        }
        return _values.Dequeue();
    }
}
