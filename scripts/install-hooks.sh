#!/bin/bash
# Install Git hooks for DiamondX development

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
HOOKS_DIR="$REPO_ROOT/.git/hooks"

echo "Installing Git hooks..."

# Create pre-commit hook
cat > "$HOOKS_DIR/pre-commit" << 'EOF'
#!/bin/bash
# Pre-commit hook for DiamondX
# Checks: formatting, build, secrets detection, optional tests

set -e

SOLUTION="src/diamondx.sln"
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo -e "${YELLOW}Warning: dotnet not found, skipping checks${NC}"
    exit 0
fi

if [ ! -f "$SOLUTION" ]; then
    echo -e "${YELLOW}Warning: Solution file not found at $SOLUTION${NC}"
    exit 0
fi

echo "Running pre-commit checks..."
echo ""

# 1. Secrets Detection
echo -n "🔐 Checking for secrets... "

# Get staged diff, excluding the hook script itself (it contains pattern strings)
STAGED_DIFF=$(git diff --cached --diff-filter=ACM -- . ':!scripts/install-hooks.sh' 2>/dev/null || true)

SECRETS_FOUND=""
# Check for password/secret assignments
if echo "$STAGED_DIFF" | grep -iqE '(password|secret|api_key|apikey|token)[[:space:]]*[=:][[:space:]]*["\047][^"\047]{8,}'; then
    SECRETS_FOUND="password/secret pattern"
fi
# Check for AWS keys
if echo "$STAGED_DIFF" | grep -qE 'AKIA[0-9A-Z]{16}'; then
    SECRETS_FOUND="${SECRETS_FOUND} AWS_KEY"
fi
# Check for private keys (using base64 marker to avoid false positives)
if echo "$STAGED_DIFF" | grep -q 'BEGIN.*PRIVATE KEY'; then
    SECRETS_FOUND="${SECRETS_FOUND} PRIVATE_KEY"
fi
# Check for connection strings with passwords (SQL Server style)
if echo "$STAGED_DIFF" | grep -iqE 'Server=.{1,50}Password=[^;]{4,}'; then
    SECRETS_FOUND="${SECRETS_FOUND} CONNECTION_STRING"
fi

if [ -n "$SECRETS_FOUND" ]; then
    echo -e "${RED}FAILED${NC}"
    echo ""
    echo -e "${RED}❌ Potential secrets detected in staged changes!${NC}"
    echo "Detected:$SECRETS_FOUND"
    echo ""
    echo "If these are false positives, use: git commit --no-verify"
    exit 1
fi
echo -e "${GREEN}OK${NC}"

# 2. Format Check
echo -n "📝 Checking code formatting... "
if ! dotnet format "$SOLUTION" --verify-no-changes --verbosity quiet 2>&1; then
    echo -e "${RED}FAILED${NC}"
    echo ""
    echo -e "${RED}❌ Code formatting issues detected!${NC}"
    echo "Run 'dotnet format src/diamondx.sln' to fix them."
    exit 1
fi
echo -e "${GREEN}OK${NC}"

# 3. Build Check
echo -n "🔨 Building solution... "
if ! dotnet build "$SOLUTION" --configuration Debug --verbosity quiet --nologo 2>&1; then
    echo -e "${RED}FAILED${NC}"
    echo ""
    echo -e "${RED}❌ Build failed! Fix compilation errors before committing.${NC}"
    exit 1
fi
echo -e "${GREEN}OK${NC}"

# 4. Optional Tests (set RUN_TESTS=1 or use --run-tests alias)
if [ "$RUN_TESTS" = "1" ]; then
    echo -n "🧪 Running tests... "
    if ! dotnet test "$SOLUTION" --configuration Debug --no-build --verbosity quiet --nologo 2>&1; then
        echo -e "${RED}FAILED${NC}"
        echo ""
        echo -e "${RED}❌ Tests failed! Fix failing tests before committing.${NC}"
        exit 1
    fi
    echo -e "${GREEN}OK${NC}"
fi

echo ""
echo -e "${GREEN}✅ All pre-commit checks passed!${NC}"
EOF

chmod +x "$HOOKS_DIR/pre-commit"

echo "✅ Pre-commit hook installed successfully!"
echo ""
echo "The hook will check:"
echo "  🔐 Secrets detection (API keys, passwords, etc.)"
echo "  📝 Code formatting (dotnet format)"
echo "  🔨 Build verification (dotnet build)"
echo ""
echo "Optional:"
echo "  🧪 Run tests: RUN_TESTS=1 git commit -m 'message'"
echo ""
echo "To skip hooks temporarily: git commit --no-verify"
