<#
.SYNOPSIS
    Uploads the chat system prompt and reviewer prompt to Azure Key Vault.
.DESCRIPTION
    Reads system-prompt.txt and reviewer-prompt.txt and uploads them as
    Key Vault secrets named 'ChatSystemPrompt' and 'ReviewerSystemPrompt'.
.PARAMETER VaultName
    The name of the Azure Key Vault.
#>
param(
    [Parameter(Mandatory)]
    [string]$VaultName
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Upload chat system prompt
$systemPromptFile = Join-Path $scriptDir 'system-prompt.txt'

if (-not (Test-Path $systemPromptFile)) {
    Write-Error "System prompt file not found: $systemPromptFile"
    exit 1
}

az keyvault secret set `
    --vault-name $VaultName `
    --name 'ChatSystemPrompt' `
    --file $systemPromptFile `
    --encoding utf-8 `
    --content-type 'text/plain'

if ($LASTEXITCODE -eq 0) {
    Write-Host "System prompt uploaded successfully to Key Vault '$VaultName'" -ForegroundColor Green
} else {
    Write-Error "Failed to upload system prompt to Key Vault"
    exit 1
}

# Upload reviewer prompt
$reviewerPromptFile = Join-Path $scriptDir 'reviewer-prompt.txt'

if (-not (Test-Path $reviewerPromptFile)) {
    Write-Error "Reviewer prompt file not found: $reviewerPromptFile"
    exit 1
}

az keyvault secret set `
    --vault-name $VaultName `
    --name 'ReviewerSystemPrompt' `
    --file $reviewerPromptFile `
    --encoding utf-8 `
    --content-type 'text/plain'

if ($LASTEXITCODE -eq 0) {
    Write-Host "Reviewer prompt uploaded successfully to Key Vault '$VaultName'" -ForegroundColor Green
} else {
    Write-Error "Failed to upload reviewer prompt to Key Vault"
    exit 1
}
