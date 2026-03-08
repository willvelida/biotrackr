<#
.SYNOPSIS
    Creates the Agent Identity Blueprint in Entra ID for Biotrackr Chat Agent.

.DESCRIPTION
    Pre-provision script that creates an Agent Identity Blueprint via Microsoft Graph beta API.
    Run this interactively before deploying infrastructure. Copy the output values
    to pass as parameters to configure-agent-identity.ps1.

.PARAMETER TenantId
    The Entra ID tenant ID.

.PARAMETER AgentBlueprintPrincipalName
    Display name for the agent identity blueprint.

.EXAMPLE
    .\setup-agent-identity.ps1 -TenantId "00000000-0000-0000-0000-000000000000" -AgentBlueprintPrincipalName "Biotrackr Chat Agent"
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $TenantId,

    [Parameter(Mandatory = $true)]
    [string] $AgentBlueprintPrincipalName
)

$ErrorActionPreference = 'Stop'

# Install required modules if not already installed
if (-not (Get-Module -ListAvailable -Name Microsoft.Graph.Beta.Applications)) {
    Write-Host "Installing Microsoft.Graph.Beta.Applications module..."
    Install-Module Microsoft.Graph.Beta.Applications -Scope CurrentUser -Force
}
if (-not (Get-Module -ListAvailable -Name Microsoft.Graph.Applications)) {
    Write-Host "Installing Microsoft.Graph.Applications module..."
    Install-Module Microsoft.Graph.Applications -Scope CurrentUser -Force
}

# Connect to Microsoft Graph with required scopes
$allScopes = @(
    "AgentIdentityBlueprint.Create",
    "AgentIdentityBlueprint.AddRemoveCreds.All",
    "AgentIdentityBlueprint.ReadWrite.All",
    "AgentIdentityBlueprintPrincipal.Create",
    "User.Read"
)
Connect-MgGraph -Scopes $allScopes -TenantId $TenantId -NoWelcome

# Get current signed-in user (sponsor)
$user = Invoke-MgGraphRequest -Method GET -Uri "https://graph.microsoft.com/v1.0/me" -OutputType PSObject
Write-Host "Current user: $($user.displayName) ($($user.id))"

# Step 1: Create the agent identity blueprint
Write-Host "Creating agent identity blueprint..."
$body = @{
    "@odata.type"          = "Microsoft.Graph.AgentIdentityBlueprint"
    "displayName"          = $AgentBlueprintPrincipalName
    "sponsors@odata.bind"  = @("https://graph.microsoft.com/v1.0/users/$($user.id)")
    "owners@odata.bind"    = @("https://graph.microsoft.com/v1.0/users/$($user.id)")
} | ConvertTo-Json -Depth 5

$response = Invoke-MgGraphRequest `
    -Method POST `
    -Uri "https://graph.microsoft.com/beta/applications/graph.agentIdentityBlueprint" `
    -Body $body `
    -ContentType "application/json"

$applicationId = $response.appId
if (-not $applicationId) {
    Write-Error "Failed to create agent identity blueprint - no appId returned from Graph API."
    exit 1
}
Write-Host "Blueprint appId: $applicationId"

# Step 2: Configure identifier URI and OAuth2 scope (access_agent)
Write-Host "Configuring identifier URI and access_agent scope..."
$identifierUri = "api://$applicationId"
$scopeId = [guid]::NewGuid()

$scope = @{
    adminConsentDescription = "Allow the application to access the agent on behalf of the signed-in user."
    adminConsentDisplayName = "Access agent"
    id                      = $scopeId
    isEnabled               = $true
    type                    = "User"
    value                   = "access_agent"
}

Update-MgBetaApplication -ApplicationId $applicationId `
    -IdentifierUris @($identifierUri) `
    -Api @{ oauth2PermissionScopes = @($scope) }

Write-Host "Configured identifier URI: $identifierUri"
Write-Host "Created scope 'access_agent' with ID: $scopeId"

# Step 3: Create the agent blueprint principal (service principal)
Write-Host "Creating agent blueprint principal..."
$spBody = @{
    appId = $applicationId
}

$spResponse = Invoke-MgGraphRequest `
    -Method POST `
    -Uri "https://graph.microsoft.com/beta/serviceprincipals/graph.agentIdentityBlueprintPrincipal" `
    -Headers @{ "OData-Version" = "4.0" } `
    -Body ($spBody | ConvertTo-Json)

Write-Host "Agent blueprint principal created for appId: $applicationId"

# Step 4: Add a temporary client secret (6-month expiry) for post-provision use
Write-Host "Creating client secret..."
$secretResult = Add-MgApplicationPassword -ApplicationId $applicationId `
    -PasswordCredential @{
        displayName = "postprovision-setup"
        endDateTime = (Get-Date).AddMonths(6).ToString("o")
    }

if (-not $secretResult.SecretText) {
    Write-Error "Failed to create client secret for blueprint."
    exit 1
}

Write-Host "Client secret created (shown only once - copy it now)."

# Output values needed by configure-agent-identity.ps1
Write-Host ""
Write-Host "Setup complete. Save the following values for configure-agent-identity.ps1:"
Write-Host "  -AgentBlueprintAppId       $applicationId"
Write-Host "  -AgentBlueprintClientSecret $($secretResult.SecretText)"
Write-Host "  -SponsorUserId             $($user.id)"
Write-Host ""
Write-Host "Next: Deploy infrastructure, then run configure-agent-identity.ps1 with the values above."
