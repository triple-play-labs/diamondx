---
layout: default
title: "ADR-001: Event-Driven Architecture"
---

# ADR-001: Event-Driven Architecture

## Status

Accepted

## Context

DiamondX needs to model complex game events (plate appearances, runs scored, innings changing) while allowing for:
- Multiple consumers of events (logging, statistics, UI)
- Easy testing of game flow
- Extensibility for new features

## Decision

Implement an event-driven architecture using:
- Central `EventScheduler` for publishing and subscribing to events
- Strongly-typed event classes (e.g., `RunScoredEvent`, `AtBatEvent`)
- Decoupled handlers that react to events

```csharp
eventScheduler.Subscribe<RunScoredEvent>(e => {
    Console.WriteLine($"{e.Runner} scored!");
});

eventScheduler.Publish(new RunScoredEvent(team, runner));
```

## Consequences

### Positive

- **Loose coupling**: Game logic doesn't know about consumers
- **Testability**: Easy to mock event handlers
- **Extensibility**: Add new handlers without modifying core logic
- **Replay capability**: Events can be logged and replayed

### Negative

- **Complexity**: More classes and indirection
- **Debugging**: Event flow can be harder to trace
- **Performance**: Small overhead for event dispatch

## Notes

The event-driven pattern aligns well with the simulation engine's goal of being domain-agnostic and reusable.
