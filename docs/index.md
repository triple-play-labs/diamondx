---
layout: default
title: Home
---

# DiamondX Documentation

DiamondX is a text-based baseball simulation engine built with .NET 8. It models plate appearances, runner movement, inning flow, scoring, and pitcher fatigue using real player/pitcher statistics with a testable, event-driven core.

## Quick Links

- [Getting Started](getting-started) - Installation and first simulation
- [Architecture](architecture/) - System design and components
- [API Reference](api/) - Core classes and interfaces
- [Operations](operations/) - Deployment and monitoring
- [ADRs](adr/) - Architecture Decision Records

## Features

- **Realistic Plate Appearances**: Uses the Log5 method to combine batter and pitcher statistics
- **Pitcher Fatigue**: Tracks pitch count with configurable fatigue thresholds
- **Monte Carlo Simulations**: Run thousands of games to calculate win probabilities
- **Event-Driven Architecture**: Extensible event system for custom handlers
- **Multi-Model Orchestration**: Coordinate multiple simulations (weather + baseball)

## Quick Start

```bash
# Clone and build
git clone https://github.com/triple-play-labs/diamondx.git
cd diamondx/src
dotnet build

# Run a single game
dotnet run --project DiamondX.Console

# Run Monte Carlo simulations
dotnet run --project DiamondX.Console -- -mc

# Run with Docker
docker run --rm ghcr.io/triple-play-labs/diamondx
```

## Project Status

[![CI](https://github.com/triple-play-labs/diamondx/actions/workflows/ci.yml/badge.svg)](https://github.com/triple-play-labs/diamondx/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/triple-play-labs/diamondx/branch/main/graph/badge.svg)](https://codecov.io/gh/triple-play-labs/diamondx)

## Links

- [GitHub Repository](https://github.com/triple-play-labs/diamondx)
- [Container Registry](https://github.com/triple-play-labs/diamondx/pkgs/container/diamondx)
- [Releases](https://github.com/triple-play-labs/diamondx/releases)
