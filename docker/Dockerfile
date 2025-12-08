# DiamondX Baseball Simulation - Dockerfile
# Placeholder for future containerization

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy solution and project files
COPY src/*.sln .
COPY src/DiamondX.Console/*.csproj ./DiamondX.Console/
COPY src/DiamondX.Core/*.csproj ./DiamondX.Core/
COPY src/DiamondX.Weather/*.csproj ./DiamondX.Weather/
COPY src/SimulationEngine/*.csproj ./SimulationEngine/
COPY tests/DiamondX.Tests/*.csproj ./tests/DiamondX.Tests/

# Restore dependencies (only main projects, skip tests at runtime)
RUN dotnet restore DiamondX.Console/DiamondX.Console.csproj

# Copy source code
COPY src/. .

# Build release
RUN dotnet publish DiamondX.Console/DiamondX.Console.csproj \
    -c Release \
    -o /app \
    --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

COPY --from=build /app .

# Default entry point
ENTRYPOINT ["dotnet", "DiamondX.Console.dll"]

# Example usage:
# docker build -f docker/Dockerfile -t diamondx .
# docker run --rm diamondx
# docker run --rm diamondx -o  # orchestrated demo
