namespace SimulationEngine.Random;

/// <summary>
/// Seedable random source for reproducible simulation runs.
/// Thread-safe for use in parallel execution scenarios.
/// </summary>
public sealed class SeedableRandomSource : IRandomSource
{
    private readonly System.Random _random;
    private readonly object _lock = new();

    /// <summary>
    /// The seed used to initialize this random source.
    /// </summary>
    public int Seed { get; }

    /// <summary>
    /// Number of values generated from this source.
    /// </summary>
    public long GenerationCount { get; private set; }

    /// <summary>
    /// Create a random source with a specific seed for reproducibility.
    /// </summary>
    public SeedableRandomSource(int seed)
    {
        Seed = seed;
        _random = new System.Random(seed);
        GenerationCount = 0;
    }

    /// <summary>
    /// Create a random source with a random seed.
    /// </summary>
    public SeedableRandomSource() : this(System.Random.Shared.Next())
    {
    }

    public double NextDouble()
    {
        lock (_lock)
        {
            GenerationCount++;
            return _random.NextDouble();
        }
    }

    /// <summary>
    /// Generate a random integer in the range [0, maxValue).
    /// </summary>
    public int NextInt(int maxValue)
    {
        lock (_lock)
        {
            GenerationCount++;
            return _random.Next(maxValue);
        }
    }

    /// <summary>
    /// Generate a random integer in the range [minValue, maxValue).
    /// </summary>
    public int NextInt(int minValue, int maxValue)
    {
        lock (_lock)
        {
            GenerationCount++;
            return _random.Next(minValue, maxValue);
        }
    }

    /// <summary>
    /// Generate a random boolean with the given probability of being true.
    /// </summary>
    public bool NextBool(double probability = 0.5)
    {
        return NextDouble() < probability;
    }

    /// <summary>
    /// Select a random element from a collection.
    /// </summary>
    public T Choose<T>(IReadOnlyList<T> items)
    {
        if (items.Count == 0)
            throw new ArgumentException("Cannot choose from empty collection", nameof(items));

        return items[NextInt(items.Count)];
    }

    /// <summary>
    /// Create a snapshot of the current random state.
    /// Note: Cannot fully restore Random state, so this captures generation count.
    /// </summary>
    public RandomSnapshot CreateSnapshot()
    {
        lock (_lock)
        {
            return new RandomSnapshot(Seed, GenerationCount);
        }
    }
}

/// <summary>
/// Snapshot of random source state.
/// </summary>
public sealed record RandomSnapshot(int Seed, long GenerationCount);
