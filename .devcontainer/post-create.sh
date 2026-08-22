#!/bin/bash
# Container-specific setup only. Everything shared with the Windows host —
# prerequisite checks, git hooks, the harness manifest, and NuGet restore —
# lives in scripts/init.sh so there is one code path and one place to fix.
set -e

echo "Running postCreateCommand..."

# Shared bootstrap. --emulator performs a bounded wait on the sibling compose
# service; COSMOS_EMULATOR_READY_URL tells init.sh not to start its own.
COSMOS_EMULATOR_READY_URL="http://cosmos-emulator:8080/ready" \
    bash scripts/init.sh --emulator

# Download and trust the emulator certificate (container-only; the host
# equivalent needs an elevated PowerShell and init.sh prints it as a manual step)
curl -k https://cosmos-emulator:8081/_explorer/emulator.pem > /tmp/cosmos-emulator.crt 2>/dev/null || true
if [ -f /tmp/cosmos-emulator.crt ] && [ -s /tmp/cosmos-emulator.crt ]; then
    sudo cp /tmp/cosmos-emulator.crt /usr/local/share/ca-certificates/cosmos-emulator.crt
    sudo update-ca-certificates
    echo "Cosmos DB Emulator certificate trusted."
else
    echo "WARNING: Could not download Cosmos DB Emulator certificate."
fi

# Secret scanning and NuGet restore are handled by scripts/init.sh — the
# Gitleaks scan now lives in the version-controlled .githooks/pre-commit
# instead of being written imperatively into untracked .git/hooks/, and
# init.sh restores all 14 services rather than an in-scope subset of 8.

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
