<#
.SYNOPSIS
    Uploads the report generator prompt to Azure Key Vault.
.DESCRIPTION
    Reads the report generator prompt from report-generator-prompt.txt and uploads it as a
    Key Vault secret named 'ReportGeneratorPrompt'.
.PARAMETER VaultName
    The name of the Azure Key Vault.
#>
param(
    [Parameter(Mandatory)]
    [string]$VaultName
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$promptFile = Join-Path $scriptDir 'report-generator-prompt.txt'

if (-not (Test-Path $promptFile)) {
    Write-Error "Report generator prompt file not found: $promptFile"
    exit 1
}

az keyvault secret set `
    --vault-name $VaultName `
    --name 'ReportGeneratorPrompt' `
    --file $promptFile `
    --encoding utf-8 `
    --content-type 'text/plain'

if ($LASTEXITCODE -eq 0) {
    Write-Host "Report generator prompt uploaded successfully to Key Vault '$VaultName'" -ForegroundColor Green
} else {
    Write-Error "Failed to upload report generator prompt to Key Vault"
    exit 1
}
