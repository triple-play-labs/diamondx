#!/bin/bash
# local-setup.sh - Set up local development environment

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}======================================${NC}"
echo -e "${GREEN}  DiamondX Local Setup${NC}"
echo -e "${GREEN}======================================${NC}"

# Navigate to the solution directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SOLUTION_DIR="$(dirname "$SCRIPT_DIR")"
cd "$SOLUTION_DIR"

# Check for required tools
echo -e "\n${YELLOW}Checking prerequisites...${NC}"

# Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}✗ .NET SDK not found${NC}"
    echo -e "  Install from: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
else
    DOTNET_VERSION=$(dotnet --version)
    echo -e "${GREEN}✓ .NET SDK${NC} (version $DOTNET_VERSION)"
fi

# Check Git
if ! command -v git &> /dev/null; then
    echo -e "${RED}✗ Git not found${NC}"
    exit 1
else
    GIT_VERSION=$(git --version)
    echo -e "${GREEN}✓ Git${NC} ($GIT_VERSION)"
fi

# Check Docker (optional)
if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version)
    echo -e "${GREEN}✓ Docker${NC} ($DOCKER_VERSION)"
else
    echo -e "${YELLOW}! Docker not found (optional)${NC}"
fi

# Restore NuGet packages
echo -e "\n${YELLOW}Restoring NuGet packages...${NC}"
cd "$SOLUTION_DIR/src"
dotnet restore --nologo

# Build solution
echo -e "\n${YELLOW}Building solution...${NC}"
dotnet build --configuration Debug --no-restore --nologo

# Run tests to verify setup
echo -e "\n${YELLOW}Running tests to verify setup...${NC}"
dotnet test --configuration Debug --no-build --nologo --verbosity quiet

# Success message
echo -e "\n${GREEN}======================================${NC}"
echo -e "${GREEN}  Setup Complete!${NC}"
echo -e "${GREEN}======================================${NC}"
echo -e "\n${BLUE}Next steps:${NC}"
echo -e "  1. Run a simulation: ${YELLOW}dotnet run --project src/DiamondX.Console${NC}"
echo -e "  2. Run tests: ${YELLOW}./scripts/run-tests.sh${NC}"
echo -e "  3. Open in IDE: ${YELLOW}code .${NC} or ${YELLOW}open src/diamondx.sln${NC}"
echo -e "\n${BLUE}Useful commands:${NC}"
echo -e "  Monte Carlo: ${YELLOW}dotnet run --project src/DiamondX.Console -- -mc${NC}"
echo -e "  Orchestration: ${YELLOW}dotnet run --project src/DiamondX.Console -- -o${NC}"
echo ""
