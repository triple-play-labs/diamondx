#!/bin/bash
# docker-build.sh - Build Docker image with proper tagging

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Default values
IMAGE_NAME="diamondx"
DOCKERFILE="docker/Dockerfile"
VERSION=${VERSION:-"latest"}
BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
GIT_COMMIT=$(git rev-parse --short HEAD 2>/dev/null || echo "unknown")

echo -e "${GREEN}======================================${NC}"
echo -e "${GREEN}  DiamondX Docker Build${NC}"
echo -e "${GREEN}======================================${NC}"

# Navigate to the solution directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SOLUTION_DIR="$(dirname "$SCRIPT_DIR")"
cd "$SOLUTION_DIR"

# Check if Dockerfile exists
if [ ! -f "$DOCKERFILE" ]; then
    echo -e "${RED}Error: Dockerfile not found at $DOCKERFILE${NC}"
    echo -e "${YELLOW}Run this script after creating the Dockerfile in Phase 4${NC}"
    exit 1
fi

echo -e "\n${YELLOW}Building Docker image...${NC}"
echo -e "  Image: ${IMAGE_NAME}:${VERSION}"
echo -e "  Commit: ${GIT_COMMIT}"
echo -e "  Date: ${BUILD_DATE}"

docker build \
  --file "$DOCKERFILE" \
  --tag "${IMAGE_NAME}:${VERSION}" \
  --tag "${IMAGE_NAME}:${GIT_COMMIT}" \
  --tag "${IMAGE_NAME}:latest" \
  --build-arg VERSION="${VERSION}" \
  --build-arg BUILD_DATE="${BUILD_DATE}" \
  --build-arg GIT_COMMIT="${GIT_COMMIT}" \
  .

if [ $? -eq 0 ]; then
    echo -e "\n${GREEN}✓ Docker image built successfully!${NC}"
    echo -e "\nAvailable tags:"
    echo -e "  ${IMAGE_NAME}:${VERSION}"
    echo -e "  ${IMAGE_NAME}:${GIT_COMMIT}"
    echo -e "  ${IMAGE_NAME}:latest"
    echo -e "\nRun with: ${YELLOW}docker run --rm ${IMAGE_NAME}:latest${NC}"
else
    echo -e "\n${RED}✗ Docker build failed!${NC}"
    exit 1
fi
