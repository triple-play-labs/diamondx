namespace SimulationEngine.Random;

/// <summary>
/// Default random source using System.Random.
/// </summary>
public sealed class SystemRandomSource : IRandomSource
{
    private readonly System.Random _random;

    public SystemRandomSource() : this(System.Random.Shared)
    {
    }

    public SystemRandomSource(System.Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public SystemRandomSource(int seed) : this(new System.Random(seed))
    {
    }

    public double NextDouble() => _random.NextDouble();
}
