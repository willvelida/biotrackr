<#
.SYNOPSIS
    Configures the Agent Identity for Biotrackr Chat Agent after infrastructure provisioning.

.DESCRIPTION
    Post-provision script that creates the agent identity, configures the Federated Identity
    Credential (FIC) for the autonomous app flow, and assigns Cosmos DB RBAC.
    Run this interactively after deploying infrastructure.

    The FIC subject uses the compound fmi_path format required by the autonomous agent flow:
      /eid1/c/pub/t/{base64url(tenantId)}/a/{base64url(audienceAppId)}/{agentIdentityId}
    This matches the assertion subject that Microsoft.Identity.Web sends when
    WithAgentIdentity() is called with RequestAppToken = true.

    Requires outputs from setup-agent-identity.ps1 and values from provisioned infrastructure.

.PARAMETER TenantId
    The Entra ID tenant ID.

.PARAMETER AgentBlueprintAppId
    The appId of the agent identity blueprint (output from setup-agent-identity.ps1).

.PARAMETER AgentBlueprintClientSecret
    The client secret for the blueprint (output from setup-agent-identity.ps1).

.PARAMETER SponsorUserId
    The sponsor user's object ID (output from setup-agent-identity.ps1).

.PARAMETER ReportingAgentBlueprintAppId
    The appId of the Reporting API agent identity blueprint. Used as the audience
    in the compound FIC subject for the autonomous agent-to-agent auth flow.

.PARAMETER CosmosDbAccountName
    The name of the provisioned Cosmos DB account.

.PARAMETER ResourceGroupName
    The Azure resource group containing Cosmos DB.

.PARAMETER SubscriptionId
    The Azure subscription ID.

.PARAMETER ExistingAgentIdentityId
    Optional. If the agent identity already exists (from a previous run), provide
    its appId here to skip creation and only update the FIC and RBAC.

.EXAMPLE
    .\configure-agent-identity.ps1 `
        -TenantId "00000000-..." `
        -AgentBlueprintAppId "00000000-..." `
        -AgentBlueprintClientSecret "secret" `
        -SponsorUserId "00000000-..." `
        -ReportingAgentBlueprintAppId "00000000-..." `
        -CosmosDbAccountName "cosmos-biotrackr-dev" `
        -ResourceGroupName "rg-biotrackr-dev" `
        -SubscriptionId "00000000-..."

.EXAMPLE
    # Re-run to update FIC only (agent identity already exists)
    .\configure-agent-identity.ps1 `
        -TenantId "00000000-..." `
        -AgentBlueprintAppId "00000000-..." `
        -AgentBlueprintClientSecret "secret" `
        -SponsorUserId "00000000-..." `
        -ReportingAgentBlueprintAppId "00000000-..." `
        -CosmosDbAccountName "cosmos-biotrackr-dev" `
        -ResourceGroupName "rg-biotrackr-dev" `
        -SubscriptionId "00000000-..." `
        -ExistingAgentIdentityId "707307f7-ffc4-4744-a66b-19fa942c1c10"
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $TenantId,

    [Parameter(Mandatory = $true)]
    [string] $AgentBlueprintAppId,

    [Parameter(Mandatory = $false)]
    [string] $AgentBlueprintClientSecret,

    [Parameter(Mandatory = $false)]
    [string] $SponsorUserId,

    [Parameter(Mandatory = $true)]
    [string] $ReportingAgentBlueprintAppId,

    [Parameter(Mandatory = $true)]
    [string] $CosmosDbAccountName,

    [Parameter(Mandatory = $true)]
    [string] $ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string] $SubscriptionId,

    [Parameter(Mandatory = $false)]
    [string] $ExistingAgentIdentityId
)

$ErrorActionPreference = 'Stop'

Write-Host "=== Configure Agent Identity ==="
Write-Host ""

# ── Helper: base64url-encode a GUID (no padding) ────────────────────────────
# Converts a GUID string to its .NET byte representation and base64url-encodes it.
# This matches the encoding used by the fmi_path mechanism in the autonomous agent flow.
function ConvertTo-Base64UrlGuid {
    param ([string]$GuidString)
    $guid = [Guid]::Parse($GuidString)
    $bytes = $guid.ToByteArray()
    $base64 = [Convert]::ToBase64String($bytes)
    return $base64.TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

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

# ── Step 1: Create Agent Identity ────────────────────────────────────────────
# Creates the agent identity using the blueprint's client credentials.
# This identity will be used by the Chat Agent API to call Cosmos DB and Reporting.Api.
# Runs before FIC creation because the compound FIC subject requires the agent identity ID.

$agentIdentityId = $ExistingAgentIdentityId

if ([string]::IsNullOrWhiteSpace($agentIdentityId)) {
    # Validate parameters required for agent identity creation
    if ([string]::IsNullOrWhiteSpace($AgentBlueprintClientSecret)) {
        Write-Error "AgentBlueprintClientSecret is required when creating a new agent identity."
        exit 1
    }
    if ([string]::IsNullOrWhiteSpace($SponsorUserId)) {
        Write-Error "SponsorUserId is required when creating a new agent identity."
        exit 1
    }

    Write-Host "── Step 1: Create Agent Identity ──"

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
        "displayName"             = "biotrackr-chat-agent"
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
} else {
    Write-Host "── Step 1: Using existing Agent Identity ──"
    Write-Host "  Agent Identity ID: $agentIdentityId"
}
Write-Host ""

# ── Step 2: Federated Identity Credential ────────────────────────────────────
# Creates/updates the FIC on the blueprint so the Chat Agent API can authenticate
# using the autonomous agent flow: managed identity → FIC → agent identity → resource token.
#
# The autonomous app flow (WithAgentIdentity + fmi_path) presents a compound assertion
# subject that includes the tenant, target audience, and agent identity ID.
# The FIC subject must match this compound format.
# See: https://learn.microsoft.com/en-us/entra/agent-id/identity-platform/agent-autonomous-app-oauth-flow

Write-Host "── Step 2: Federated Identity Credential ──"
Write-Host "  Blueprint AppId: $AgentBlueprintAppId"
Write-Host "  Reporting API Blueprint AppId (audience): $ReportingAgentBlueprintAppId"
Write-Host "  Agent Identity ID: $agentIdentityId"

# Compute the compound FIC subject for the autonomous agent flow.
# Format: /eid1/c/pub/t/{base64url(tenantId)}/a/{base64url(audienceAppId)}/{agentIdentityId}
$tenantBase64Url = ConvertTo-Base64UrlGuid -GuidString $TenantId
$audienceBase64Url = ConvertTo-Base64UrlGuid -GuidString $ReportingAgentBlueprintAppId
$compoundSubject = "/eid1/c/pub/t/$tenantBase64Url/a/$audienceBase64Url/$agentIdentityId"

Write-Host "  Compound FIC subject: $compoundSubject"

$federatedCredential = @{
    Name      = "biotrackr-uai"
    Issuer    = "https://login.microsoftonline.com/$TenantId/v2.0"
    Subject   = $compoundSubject
    Audiences = @("api://AzureADTokenExchange")
}

$existing = Get-MgBetaApplicationFederatedIdentityCredential `
    -ApplicationId $AgentBlueprintAppId `
    -Filter "name eq 'biotrackr-uai'" `
    -ErrorAction SilentlyContinue

if ($existing) {
    Write-Host "  Federated credential already exists, updating subject..."
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

# ── Step 3: Cosmos DB RBAC Role Assignment ───────────────────────────────────
# Assigns the Cosmos DB Built-in Data Contributor role to the agent identity
# at account scope (matching the existing sqlRoleAssignment pattern).

Write-Host "── Step 3: Cosmos DB Role Assignment ──"

# Wait for agent identity SP to propagate in Entra ID
$agentSpObjectId = $null
for ($i = 1; $i -le 6; $i++) {
    $agentSpObjectId = (az ad sp show --id $agentIdentityId --query id -o tsv 2>$null)
    if ($agentSpObjectId) { break }
    Write-Host "  Waiting for agent identity SP to propagate (attempt $i/6)..."
    Start-Sleep -Seconds 10
}
if (-not $agentSpObjectId) {
    Write-Error "Could not find service principal for agent identity $agentIdentityId."
    exit 1
}

$cosmosScope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.DocumentDB/databaseAccounts/$CosmosDbAccountName"

$existingAssignment = az cosmosdb sql role assignment list `
    --account-name $CosmosDbAccountName `
    --resource-group $ResourceGroupName `
    --query "[?principalId=='$agentSpObjectId']" -o json 2>$null | ConvertFrom-Json

if ($existingAssignment -and $existingAssignment.Count -gt 0) {
    Write-Host "  Cosmos DB role already assigned to agent identity. Skipping."
} else {
    Write-Host "  Assigning Cosmos DB Built-in Data Contributor role to agent identity ($agentSpObjectId)..."
    az cosmosdb sql role assignment create `
        --account-name $CosmosDbAccountName `
        --resource-group $ResourceGroupName `
        --role-definition-id "00000000-0000-0000-0000-000000000002" `
        --principal-id $agentSpObjectId `
        --scope $cosmosScope 2>&1 | Out-Null
    Write-Host "  Cosmos DB role assigned successfully."
}

Write-Host ""
Write-Host "=== Configuration complete ==="
Write-Host ""
Write-Host "Agent Identity ID: $agentIdentityId"
Write-Host ""
Write-Host "Save this value - it will be needed when configuring the Chat Agent API."
