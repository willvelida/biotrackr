<#
.SYNOPSIS
    Uploads the chat system prompt to Azure Key Vault.
.DESCRIPTION
    Reads the system prompt from system-prompt.txt and uploads it as a
    Key Vault secret named 'ChatSystemPrompt'.
.PARAMETER VaultName
    The name of the Azure Key Vault.
#>
param(
    [Parameter(Mandatory)]
    [string]$VaultName
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$promptFile = Join-Path $scriptDir 'system-prompt.txt'

if (-not (Test-Path $promptFile)) {
    Write-Error "System prompt file not found: $promptFile"
    exit 1
}

az keyvault secret set `
    --vault-name $VaultName `
    --name 'ChatSystemPrompt' `
    --file $promptFile `
    --encoding utf-8 `
    --content-type 'text/plain'

if ($LASTEXITCODE -eq 0) {
    Write-Host "System prompt uploaded successfully to Key Vault '$VaultName'" -ForegroundColor Green
} else {
    Write-Error "Failed to upload system prompt to Key Vault"
    exit 1
}
