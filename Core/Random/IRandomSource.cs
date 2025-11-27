namespace DiamondX.Core.Random;

/// <summary>
/// Abstraction for random number generation to enable deterministic testing.
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// Returns a random floating-point number between 0.0 and 1.0.
    /// </summary>
    double NextDouble();
}

/// <summary>
/// Default implementation using System.Random.
/// </summary>
public class SystemRandomSource : IRandomSource
{
    private readonly System.Random _random = new();

    public double NextDouble() => _random.NextDouble();
}
