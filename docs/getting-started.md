---
layout: default
title: Getting Started
---

# Getting Started

This guide will help you get DiamondX up and running.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/) (optional)

## Installation

### From Source

```bash
git clone https://github.com/triple-play-labs/diamondx.git
cd diamondx/src
dotnet build
```

### Using Docker

```bash
docker pull ghcr.io/triple-play-labs/diamondx:latest
```

## Running Simulations

### Single Game

```bash
# From source
dotnet run --project DiamondX.Console

# With Docker
docker run --rm ghcr.io/triple-play-labs/diamondx
```

### Monte Carlo Win Probability

Calculate win probabilities by running thousands of simulations:

```bash
# Default 10,000 simulations
dotnet run --project DiamondX.Console -- -mc

# Custom count
dotnet run --project DiamondX.Console -- -mc -n=50000

# Single simulation (debugging)
dotnet run --project DiamondX.Console -- -mc -n=1

# Full season (162 games)
dotnet run --project DiamondX.Console -- -mc --season
```

### Multi-Model Orchestration

Run weather and baseball simulations together:

```bash
dotnet run --project DiamondX.Console -- -o
```

## Running Tests

```bash
cd src
dotnet test
```

## Development Setup

For contributors, run the local setup script:

```bash
./scripts/local-setup.sh
```

This will:
- Install pre-commit hooks
- Restore NuGet packages
- Build the solution
- Run tests

## Next Steps

- [Architecture Overview](architecture/) - Understand the system design
- [API Reference](api/) - Explore the core classes
- [Contributing](https://github.com/triple-play-labs/diamondx/blob/main/CONTRIBUTING.md) - How to contribute
