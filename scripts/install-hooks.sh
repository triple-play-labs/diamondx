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
# Runs dotnet format to ensure code style consistency

set -e

echo "Running dotnet format check..."

# Find the solution file
SOLUTION="src/diamondx.sln"

if [ ! -f "$SOLUTION" ]; then
    echo "Warning: Solution file not found at $SOLUTION"
    exit 0
fi

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "Warning: dotnet not found, skipping format check"
    exit 0
fi

# Run format check
if ! dotnet format "$SOLUTION" --verify-no-changes --verbosity minimal 2>&1; then
    echo ""
    echo "❌ Code formatting issues detected!"
    echo ""
    echo "Run 'dotnet format src/diamondx.sln' to fix them."
    echo ""
    exit 1
fi

echo "✅ Code formatting OK"
EOF

chmod +x "$HOOKS_DIR/pre-commit"

echo "✅ Pre-commit hook installed successfully!"
echo ""
echo "The hook will run 'dotnet format --verify-no-changes' before each commit."
echo "To skip the hook temporarily, use: git commit --no-verify"
