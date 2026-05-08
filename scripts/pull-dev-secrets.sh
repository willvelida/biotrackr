#!/bin/bash
# Opt-in: Pull secrets from Azure Key Vault for local development
# Requires: az login (run first), Key Vault Secrets User role on kv-biotrackr-dev

VAULT_NAME="kv-biotrackr-dev"

if ! az account show &>/dev/null; then
    echo "Not logged into Azure CLI. Run 'az login' first."
    exit 1
fi

echo "Pulling secrets from $VAULT_NAME..."

export Biotrackr__AnthropicApiKey=$(az keyvault secret show --vault-name "$VAULT_NAME" --name AnthropicApiKey --query value -o tsv 2>/dev/null)
export Biotrackr__McpServerApiKey=$(az keyvault secret show --vault-name "$VAULT_NAME" --name mcpserverapikey --query value -o tsv 2>/dev/null)

[ -n "$Biotrackr__AnthropicApiKey" ] && echo "  AnthropicApiKey loaded" || echo "  Failed to load AnthropicApiKey (check RBAC)"
[ -n "$Biotrackr__McpServerApiKey" ] && echo "  McpServerApiKey loaded" || echo "  Failed to load McpServerApiKey (check RBAC)"
echo ""
echo "To export into your current shell, run: source scripts/pull-dev-secrets.sh"
