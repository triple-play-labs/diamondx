---
layout: default
title: "ADR-002: Log5 Method for Matchups"
---

# ADR-002: Log5 Method for Batter-Pitcher Matchups

## Status

Accepted

## Context

Baseball outcomes depend on both batter and pitcher abilities. We need a method to combine their statistics that:
- Produces realistic probability distributions
- Handles edge cases (elite vs. elite)
- Is well-established in sabermetrics

## Decision

Use the **Log5 method** developed by Bill James to calculate expected outcomes when a batter faces a pitcher.

The formula for a rate stat (e.g., walk rate):

```
Expected = (Batter × Pitcher / League) / 
           (Batter × Pitcher / League + (1 - Batter) × (1 - Pitcher) / (1 - League))
```

This is implemented in `PlateAppearanceResolver.cs`.

## Consequences

### Positive

- **Accuracy**: Matches real-world outcome distributions
- **Credibility**: Well-known sabermetric method
- **Composability**: Works for all rate stats (BB, 1B, 2B, 3B, HR)

### Negative

- **Complexity**: Formula is non-trivial to understand
- **League averages**: Requires baseline league statistics
- **Park factors**: Doesn't account for stadium effects (future enhancement)

## References

- [Log5 Method - Wikipedia](https://en.wikipedia.org/wiki/Log5)
- [Bill James on Log5](https://www.billjamesonline.com/article802/)
