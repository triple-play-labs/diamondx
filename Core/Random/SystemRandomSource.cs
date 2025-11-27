using System;

namespace DiamondX.Core.Random;

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

    public double NextDouble() => _random.NextDouble();
}
