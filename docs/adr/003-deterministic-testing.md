---
layout: default
title: "ADR-003: Deterministic Testing Strategy"
---

# ADR-003: Deterministic Testing Strategy

## Status

Accepted

## Context

Baseball simulation inherently involves randomness. Traditional random testing leads to:
- Flaky tests that sometimes pass, sometimes fail
- Difficulty reproducing bugs
- Inability to test specific scenarios

## Decision

Implement deterministic testing using:

1. **`IRandomSource` interface**: Abstract random number generation
2. **`TestRandomSource`**: Test implementation that returns predefined values
3. **`SeedableRandomSource`**: Production implementation with optional seed

```csharp
// Test with predetermined sequence
var rng = new TestRandomSource(new[] { 0.12, 0.42, 0.97, 0.05 });
var resolver = new PlateAppearanceResolver(rng);

// Each NextDouble() call returns the next value in sequence
var outcome = resolver.Resolve(batter, pitcher); // Uses 0.12
```

## Consequences

### Positive

- **Reproducibility**: Tests always produce same results
- **Scenario testing**: Can craft specific game situations
- **Debugging**: Production issues can be reproduced with seed
- **No flakiness**: 100% reliable test suite

### Negative

- **Setup complexity**: Tests need to calculate expected random values
- **Fragility**: Changes to random consumption order break tests
- **Coverage gaps**: May miss edge cases random testing would find

## Implementation

```csharp
public interface IRandomSource
{
    double NextDouble();
    int Next(int maxValue);
}

public class TestRandomSource : IRandomSource
{
    private readonly Queue<double> _values;
    
    public TestRandomSource(double[] values) 
        => _values = new Queue<double>(values);
    
    public double NextDouble() 
        => _values.Dequeue();
}
```

## Notes

Consider adding property-based testing (FsCheck) for broader coverage while keeping deterministic tests for critical paths.
