<#
.SYNOPSIS
    Configures a new Agent Identity for Biotrackr Reporting.Svc under the existing Reporting.Api blueprint.

.DESCRIPTION
    Post-provision script that creates a Federated Identity Credential (FIC) and agent identity
    for Reporting.Svc under the SAME blueprint used by Reporting.Api. Both Chat.Api and
    Reporting.Svc share the same blueprint (same audience/scope) but each gets its own
    agent identity with a distinct 'azp' claim for auditability (ASI03/ASI07).

    Requires the existing Reporting.Api blueprint appId (from setup-agent-identity.ps1 in
    scripts/reporting-api-identity/) and values from provisioned infrastructure.

.PARAMETER TenantId
    The Entra ID tenant ID.

.PARAMETER AgentBlueprintAppId
    The appId of the EXISTING Reporting.Api agent identity blueprint.

.PARAMETER AgentBlueprintClientSecret
    The client secret for the existing blueprint.

.PARAMETER SponsorUserId
    The sponsor user's object ID.

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

Write-Host "=== Configure Reporting.Svc Agent Identity (Shared Blueprint) ==="
Write-Host "  Using existing Reporting.Api blueprint: $AgentBlueprintAppId"
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
# Links the UAI (uai-biotrackr-dev) to the existing Reporting.Api blueprint so
# Reporting.Svc can authenticate as the blueprint using the managed identity's assertion.
# This is a SECOND FIC on the same blueprint (Chat.Api already has one).

Write-Host "── Step 1: Federated Identity Credential ──"
Write-Host "  Blueprint AppId: $AgentBlueprintAppId"
Write-Host "  UAI Principal ID (Subject): $ManagedIdentityPrincipalId"

$federatedCredential = @{
    Name      = "biotrackr-reporting-svc-uai"
    Issuer    = "https://login.microsoftonline.com/$TenantId/v2.0"
    Subject   = $ManagedIdentityPrincipalId
    Audiences = @("api://AzureADTokenExchange")
}

$existing = Get-MgBetaApplicationFederatedIdentityCredential `
    -ApplicationId $AgentBlueprintAppId `
    -Filter "name eq 'biotrackr-reporting-svc-uai'" `
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
# Creates a new agent identity under the existing blueprint.
# This gives Reporting.Svc its own 'azp' claim, distinct from Chat.Api's identity.
# Reporting.Api's auth policy accepts both azp values.

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
    "displayName"             = "biotrackr-reporting-svc-agent"
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
Write-Host "Reporting.Svc Agent Identity ID: $agentIdentityId"
Write-Host ""
Write-Host "Update the following configuration values:"
Write-Host "  Reporting.Api App Config:    Biotrackr:ReportingSvcAgentIdentityId = $agentIdentityId"
Write-Host "  Reporting.Svc App Config:    Biotrackr:AgentIdentityId = $agentIdentityId"
Write-Host "  Reporting.Svc App Config:    Biotrackr:ReportingApiScope = api://$AgentBlueprintAppId/.default"
Write-Host ""
Write-Host "The Reporting.Svc agent identity token's 'azp' claim will be validated by Reporting.Api."
