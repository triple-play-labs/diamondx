---
layout: default
title: Operations
---

# Operations Guide

Deployment, monitoring, and operational procedures for DiamondX.

## Docker

### Running with Docker

```bash
# Pull latest image
docker pull ghcr.io/triple-play-labs/diamondx:latest

# Run a single game
docker run --rm ghcr.io/triple-play-labs/diamondx

# Run Monte Carlo simulations
docker run --rm ghcr.io/triple-play-labs/diamondx -mc -n=10000

# Run orchestrated demo
docker run --rm ghcr.io/triple-play-labs/diamondx -o
```

### Building Locally

```bash
docker build -f docker/Dockerfile -t diamondx .
docker run --rm diamondx
```

### Image Tags

| Tag | Description |
|-----|-------------|
| `latest` | Latest build from main branch |
| `x.y.z` | Specific release version |
| `sha-abc123` | Specific commit |

## CI/CD Pipeline

### Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `ci.yml` | Push/PR to main | Build, test, lint, Docker |
| `release.yml` | Version tags | Create release artifacts |

### Branch Protection

- Required reviews: 1
- Required status checks: `build-and-test`, `lint`
- No force pushes to main

## Monitoring

### Health Checks

For containerized deployments, the application exits with:
- `0` - Success
- Non-zero - Failure

### Logs

Console output includes:
- Game events (innings, at-bats, scores)
- Simulation metrics (games/second)
- Error messages

## Runbooks

- Runbooks (coming soon) - Operational procedures

## Configuration

### Environment Variables

Currently, DiamondX uses command-line arguments only. Future versions may support:

| Variable | Description | Default |
|----------|-------------|---------|
| `DIAMONDX_LOG_LEVEL` | Logging verbosity | `Information` |
| `DIAMONDX_SEED` | Random seed for reproducibility | Random |

## Performance Tuning

### Monte Carlo Simulations

For optimal performance:
- Use `-n` flag to specify simulation count
- Container has access to all CPU cores by default
- Typical throughput: 50,000-70,000 simulations/second

## Security

### Container Image

- Based on `mcr.microsoft.com/dotnet/runtime:8.0`
- Non-root user (future enhancement)
- No exposed ports (CLI application)
- Scanned for vulnerabilities in CI

### Dependencies

- Dependabot monitors for vulnerable packages
- Security updates merged within 48 hours
