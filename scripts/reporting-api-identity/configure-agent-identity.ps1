<#
.SYNOPSIS
    Configures the Agent Identity for Biotrackr Reporting Agent after infrastructure provisioning.

.DESCRIPTION
    Post-provision script that creates the Federated Identity Credential (FIC) and agent identity
    for the Reporting.Api. The Chat.Api uses this identity to acquire bearer tokens for
    inter-service A2A authentication (ASI03/ASI07).

    Requires outputs from setup-agent-identity.ps1 and values from provisioned infrastructure.

.PARAMETER TenantId
    The Entra ID tenant ID.

.PARAMETER AgentBlueprintAppId
    The appId of the Reporting.Api agent identity blueprint (output from setup-agent-identity.ps1).

.PARAMETER AgentBlueprintClientSecret
    The client secret for the blueprint (output from setup-agent-identity.ps1).

.PARAMETER SponsorUserId
    The sponsor user's object ID (output from setup-agent-identity.ps1).

.PARAMETER ManagedIdentityPrincipalId
    The principal ID of the provisioned UAI (uai-biotrackr-dev).

.EXAMPLE
    .\configure-agent-identity.ps1 `
        -TenantId "00000000-..." `
        -AgentBlueprintAppId "00000000-..." `
        -AgentBlueprintClientSecret "secret" `
        -SponsorUserId "00000000-..." `
        -ManagedIdentityPrincipalId "00000000-..."
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $TenantId,

    [Parameter(Mandatory = $true)]
    [string] $AgentBlueprintAppId,

    [Parameter(Mandatory = $true)]
    [string] $AgentBlueprintClientSecret,

    [Parameter(Mandatory = $true)]
    [string] $SponsorUserId,

    [Parameter(Mandatory = $true)]
    [string] $ManagedIdentityPrincipalId
)

$ErrorActionPreference = 'Stop'

Write-Host "=== Configure Reporting.Api Agent Identity ==="
Write-Host ""

# Install required modules if not already installed
if (-not (Get-Module -ListAvailable -Name Microsoft.Graph.Beta.Applications)) {
    Write-Host "Installing Microsoft.Graph.Beta.Applications module..."
    Install-Module Microsoft.Graph.Beta.Applications -Scope CurrentUser -Force
}
if (-not (Get-Module -ListAvailable -Name Microsoft.Graph.Beta.Identity.SignIns)) {
    Write-Host "Installing Microsoft.Graph.Beta.Identity.SignIns module..."
    Install-Module Microsoft.Graph.Beta.Identity.SignIns -Scope CurrentUser -Force
}

# Connect to Microsoft Graph with required scopes
Write-Host "Connecting to Microsoft Graph..."
Connect-MgGraph -Scopes @(
    "AgentIdentityBlueprint.AddRemoveCreds.All",
    "Application.ReadWrite.All",
    "DelegatedPermissionGrant.ReadWrite.All"
) -TenantId $TenantId -NoWelcome

$mgContext = Get-MgContext
if (-not $mgContext) {
    Write-Error "Failed to connect to Microsoft Graph."
    exit 1
}
Write-Host "Connected to Microsoft Graph as $($mgContext.Account)"
Write-Host ""

# ── Step 1: Federated Identity Credential ────────────────────────────────────
# Links the UAI (uai-biotrackr-dev) to the Reporting.Api blueprint so the
# Reporting.Api can authenticate as the blueprint using the managed identity's assertion.

Write-Host "── Step 1: Federated Identity Credential ──"
Write-Host "  Blueprint AppId: $AgentBlueprintAppId"
Write-Host "  UAI Principal ID (Subject): $ManagedIdentityPrincipalId"

$federatedCredential = @{
    Name      = "biotrackr-reporting-uai"
    Issuer    = "https://login.microsoftonline.com/$TenantId/v2.0"
    Subject   = $ManagedIdentityPrincipalId
    Audiences = @("api://AzureADTokenExchange")
}

$existing = Get-MgBetaApplicationFederatedIdentityCredential `
    -ApplicationId $AgentBlueprintAppId `
    -Filter "name eq 'biotrackr-reporting-uai'" `
    -ErrorAction SilentlyContinue

if ($existing) {
    Write-Host "  Federated credential already exists, updating..."
    Update-MgBetaApplicationFederatedIdentityCredential `
        -ApplicationId $AgentBlueprintAppId `
        -FederatedIdentityCredentialId $existing.Id `
        -BodyParameter $federatedCredential
} else {
    New-MgBetaApplicationFederatedIdentityCredential `
        -ApplicationId $AgentBlueprintAppId `
        -BodyParameter $federatedCredential
}

Write-Host "  Federated identity credential configured successfully."
Write-Host ""

# ── Step 2: Create Agent Identity ────────────────────────────────────────────
# Creates the agent identity using the blueprint's client credentials.
# This identity represents Reporting.Api as a service that Chat.Api authenticates against.

Write-Host "── Step 2: Agent Identity ──"

# Acquire an access token as the blueprint using client credentials grant
$tokenBody = @{
    client_id     = $AgentBlueprintAppId
    scope         = "https://graph.microsoft.com/.default"
    client_secret = $AgentBlueprintClientSecret
    grant_type    = "client_credentials"
}

$tokenResponse = Invoke-RestMethod -Method POST `
    -Uri "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token" `
    -ContentType "application/x-www-form-urlencoded" `
    -Body $tokenBody

if (-not $tokenResponse.access_token) {
    Write-Error "Failed to acquire blueprint access token."
    exit 1
}

$blueprintToken = $tokenResponse.access_token
Write-Host "  Acquired blueprint access token."

$agentBody = @{
    "@odata.type"             = "#Microsoft.Graph.AgentIdentity"
    "displayName"             = "biotrackr-reporting-agent"
    "agentIdentityBlueprintId" = $AgentBlueprintAppId
    "sponsors@odata.bind"     = @("https://graph.microsoft.com/v1.0/users/$SponsorUserId")
} | ConvertTo-Json -Depth 5

Write-Host "  Creating agent identity with sponsor: $SponsorUserId"

$agentResponse = Invoke-RestMethod -Method POST `
    -Uri "https://graph.microsoft.com/beta/serviceprincipals/Microsoft.Graph.AgentIdentity" `
    -Headers @{
        "Authorization" = "Bearer $blueprintToken"
        "OData-Version" = "4.0"
    } `
    -Body $agentBody `
    -ContentType "application/json"

$agentIdentityId = $agentResponse.appId
if (-not $agentIdentityId) {
    Write-Error "Failed to create agent identity - no appId returned."
    exit 1
}

Write-Host "  Agent identity created: $agentIdentityId"
Write-Host ""

Write-Host "=== Configuration complete ==="
Write-Host ""
Write-Host "Agent Identity ID: $agentIdentityId"
Write-Host ""
Write-Host "Update the following configuration values:"
Write-Host "  Reporting.Api Bicep param:  chatApiAgentIdentityId = <Chat.Api agent identity ID>"
Write-Host "  Chat.Api App Config:        Biotrackr:ReportingApiScope = api://$AgentBlueprintAppId/.default"
Write-Host ""
Write-Host "The Chat.Api agent identity token's 'azp' claim will be validated by Reporting.Api."
