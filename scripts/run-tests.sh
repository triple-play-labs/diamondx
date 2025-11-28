#!/bin/bash
# run-tests.sh - Run all tests with code coverage

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}======================================${NC}"
echo -e "${GREEN}  DiamondX Test Suite${NC}"
echo -e "${GREEN}======================================${NC}"

# Navigate to the solution directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SOLUTION_DIR="$(dirname "$SCRIPT_DIR")"
cd "$SOLUTION_DIR/src"

# Clean previous build artifacts
echo -e "\n${YELLOW}Cleaning previous build...${NC}"
dotnet clean --configuration Debug --nologo --verbosity quiet

# Restore dependencies
echo -e "\n${YELLOW}Restoring dependencies...${NC}"
dotnet restore --nologo

# Build solution
echo -e "\n${YELLOW}Building solution...${NC}"
dotnet build --configuration Debug --no-restore --nologo

# Run tests with coverage
echo -e "\n${YELLOW}Running tests with code coverage...${NC}"
dotnet test \
  --configuration Debug \
  --no-build \
  --nologo \
  --verbosity normal \
  --collect:"XPlat Code Coverage" \
  --results-directory:"$SOLUTION_DIR/TestResults" \
  --logger:"console;verbosity=normal"

# Check if tests passed
if [ $? -eq 0 ]; then
    echo -e "\n${GREEN}✓ All tests passed!${NC}"
    
    # Display coverage summary if available
    if command -v reportgenerator &> /dev/null; then
        echo -e "\n${YELLOW}Generating coverage report...${NC}"
        reportgenerator \
          -reports:"$SOLUTION_DIR/TestResults/**/coverage.cobertura.xml" \
          -targetdir:"$SOLUTION_DIR/TestResults/CoverageReport" \
          -reporttypes:Html
        echo -e "${GREEN}Coverage report generated at: TestResults/CoverageReport/index.html${NC}"
    fi
else
    echo -e "\n${RED}✗ Tests failed!${NC}"
    exit 1
fi
