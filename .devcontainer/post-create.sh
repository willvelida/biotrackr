#!/bin/bash
set -e

echo "Running postCreateCommand..."

# Trust Cosmos DB Emulator certificate (vNext with HTTPS)
echo "Waiting for Cosmos DB Emulator..."
until curl -f -s http://cosmos-emulator:8080/ready > /dev/null 2>&1; do
    sleep 2
done
echo "Cosmos DB Emulator is ready."

# Download and trust the emulator certificate
curl -k https://cosmos-emulator:8081/_explorer/emulator.pem > /tmp/cosmos-emulator.crt 2>/dev/null || true
if [ -f /tmp/cosmos-emulator.crt ] && [ -s /tmp/cosmos-emulator.crt ]; then
    sudo cp /tmp/cosmos-emulator.crt /usr/local/share/ca-certificates/cosmos-emulator.crt
    sudo update-ca-certificates
    echo "Cosmos DB Emulator certificate trusted."
else
    echo "WARNING: Could not download Cosmos DB Emulator certificate."
fi

# Install Gitleaks pre-commit hook for secret scanning
echo "Installing Gitleaks pre-commit hook..."
mkdir -p .git/hooks
cat > .git/hooks/pre-commit << 'HOOK'
#!/bin/sh
gitleaks git --pre-commit --staged --verbose
HOOK
chmod +x .git/hooks/pre-commit
echo "Gitleaks pre-commit hook installed."

# Restore NuGet packages for in-scope services
echo "Restoring NuGet packages for in-scope services..."
RESTORE_FAILURES=0
SERVICES=(
    "src/Biotrackr.Activity.Api"
    "src/Biotrackr.Food.Api"
    "src/Biotrackr.Sleep.Api"
    "src/Biotrackr.Vitals.Api"
    "src/Biotrackr.Chat.Api"
    "src/Biotrackr.Mcp.Server"
    "src/Biotrackr.Reporting.Api"
    "src/Biotrackr.UI"
)
for svc in "${SERVICES[@]}"; do
    for sln in "$svc"/*.sln "$svc"/*.slnx; do
        if [ -f "$sln" ]; then
            echo "  Restoring $sln..."
            if ! dotnet restore "$sln"; then
                echo "  WARNING: Restore failed for $sln"
                RESTORE_FAILURES=$((RESTORE_FAILURES + 1))
            fi
        fi
    done
done
echo "NuGet restore complete."

# Build all in-scope services
echo "Building all in-scope services..."
BUILD_FAILURES=0
for svc in "${SERVICES[@]}"; do
    for sln in "$svc"/*.sln "$svc"/*.slnx; do
        if [ -f "$sln" ]; then
            echo "  Building $sln..."
            if ! dotnet build "$sln" --no-restore -v:q; then
                echo "  WARNING: Build failed for $sln"
                BUILD_FAILURES=$((BUILD_FAILURES + 1))
            fi
        fi
    done
done
if [ $RESTORE_FAILURES -gt 0 ] || [ $BUILD_FAILURES -gt 0 ]; then
    echo "WARNING: $RESTORE_FAILURES restore(s) and $BUILD_FAILURES build(s) failed. Some services may not start correctly."
else
    echo "Build complete."
fi

# Initialize Cosmos DB schema and seed data
echo "Initializing Cosmos DB with schema and sample data..."
bash .devcontainer/cosmos-init.sh

# Validate optional secrets
echo ""
echo "=== Secret Configuration Status ==="
[ -z "$Biotrackr__AnthropicApiKey" ] && echo "INFO: ANTHROPIC_API_KEY not set. Chat.Api AI features will be unavailable." || echo "  Anthropic API key configured"
[ -z "$Biotrackr__McpServerApiKey" ] && echo "INFO: MCP_SERVER_API_KEY not set." || echo "  MCP Server API key configured"
[ -z "$GITHUB_TOKEN" ] && echo "INFO: COPILOT_GITHUB_TOKEN not set. Reporting.Api sidecar will be unavailable." || echo "  GitHub Copilot token configured"
echo ""
echo "postCreateCommand complete! Dev container is ready."
