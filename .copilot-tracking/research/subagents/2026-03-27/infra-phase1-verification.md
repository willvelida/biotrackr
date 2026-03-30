# Infrastructure Phase 1 Verification — Reporting API

## Research Questions

1. Does `infra/modules/storage/storage-account.bicep` exist with Standard_LRS, TLS 1.2, no public access, `reports` container, and Storage Blob Data Contributor role assignment?
2. Does `infra/modules/host/container-app-http-sidecar.bicep` exist extending container-app-http.bicep with sidecar container support, volumes, and secrets parameters?
3. Does `infra/apps/reporting-api/main.bicep` exist deploying Reporting.Api with copilot-python sidecar, shared EmptyDir volume at /tmp/reports, github-copilot-token secret, and internal ingress?
4. Does `infra/core/main.bicep` contain an APIM route for `/reporting/*` (Step 1.4)?

---

## Findings

### 1. `infra/modules/storage/storage-account.bicep` — EXISTS

**Status:** Present and matches plan requirements.

**Key Contents:**

- **Resource types:** `Microsoft.Storage/storageAccounts@2024-01-01`, `Microsoft.Storage/storageAccounts/blobServices@2024-01-01`, `Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01`, `Microsoft.Authorization/roleAssignments@2022-04-01`
- **Parameters:** `storageAccountName`, `location`, `tags`, `uaiPrincipalId`
- **Outputs:** `endpoint` (blob primary endpoint URL)
- **SKU:** `Standard_LRS` ✅
- **TLS:** `minimumTlsVersion: 'TLS1_2'` ✅
- **No public access:** `allowBlobPublicAccess: false` ✅, container `publicAccess: 'None'` ✅
- **Container name:** `reports` ✅
- **Role assignment:** Storage Blob Data Contributor (`ba92f5b4-2d11-453d-a403-e96b0029c9fe`) scoped to the Storage Account for the UAI principal ✅

**Deviations:** None.

---

### 2. `infra/modules/host/container-app-http-sidecar.bicep` — EXISTS

**Status:** Present and matches plan requirements.

**Key Contents:**

- **Resource types:** `Microsoft.App/containerApps@2024-03-01` (plus existing references to `managedEnvironments`, `registries`, `userAssignedIdentities`)
- **Parameters:** `name`, `location`, `tags`, `containerAppEnvironmentName`, `containerRegistryName`, `uaiName`, `imageName`, `targetPort`, `envVariables`, `healthProbes`, `minReplicas`, `externalIngress`, `sidecarContainers` ✅, `volumes` ✅, `secrets` ✅, `cpu`, `memory`
- **Outputs:** `fqdn` (Container App FQDN)
- **Sidecar support:** `sidecarContainers` array parameter concatenated into `allContainers` via `concat(mainContainer, sidecarContainers)` ✅
- **Volumes:** `volumes` array mapped to `EmptyDir` storage type volume definitions; main container gets volume mounts from the same array ✅
- **Secrets:** `secrets` array passed to configuration secrets (with empty-check) ✅
- **Ingress:** Configurable via `externalIngress` bool (default `true`) ✅

**Deviation note:** This is a standalone module, not a Bicep "extension" of `container-app-http.bicep`. It is a separate file with its own full container app resource definition that includes sidecar support. This is a valid design approach — the plan likely intended "extend" conceptually, not via Bicep inheritance.

---

### 3. `infra/apps/reporting-api/main.bicep` — EXISTS

**Status:** Present and matches plan requirements.

**Key Contents:**

- **Module reference:** Uses `../../modules/host/container-app-http-sidecar.bicep` ✅
- **Parameters:** `name`, `imageName`, `sidecarImageName`, `location`, `tags`, `containerAppEnvironmentName`, `containerRegistryName`, `uaiName`, `appConfigName`, `appInsightsName`, `apimName`, `keyVaultName`, `storageAccountName`, `enableManagedIdentityAuth`, `tenantId`, `jwtAudience`
- **Internal ingress:** `externalIngress: false` ✅
- **Copilot Python sidecar:**
  - Container name: `copilot-python` ✅
  - Env var: `GITHUB_TOKEN` referencing `github-copilot-token` secret ✅
  - Resources: 0.5 CPU, 1Gi memory
  - Readiness probe on TCP port 4321 ✅
  - Volume mount at `/tmp/reports` ✅
- **Shared volume:** `shared-reports` EmptyDir at `/tmp/reports` ✅
- **Secret:** `github-copilot-token` from Key Vault via UAI identity ✅
- **APIM API:** Full API definition with path `reporting`, operations for agent card, A2A messaging, report generation, list, get, and health check ✅
- **APIM Product:** `Reporting` product linked to the API ✅
- **JWT auth policy:** Conditional on `enableManagedIdentityAuth` ✅
- **App Configuration entries:** ReportingApiUrl, ReportingBlobStorageEndpoint, CopilotCliUrl, ChatApiUaiPrincipalId, ReportGeneratorSystemPrompt (Key Vault reference) ✅
- **Key Vault placeholders:** GitHubCopilotToken secret (empty, populated via pipeline) ✅

**Deviations:** None material. The file is comprehensive and goes beyond the minimum plan requirements.

---

### 4. `infra/core/main.bicep` — APIM `/reporting/*` route — NOT FOUND

**Status:** No reference to "reporting" exists in `infra/core/main.bicep`.

The core infrastructure file deploys the APIM instance via `apim-consumption.bicep` module but does not include any routing configuration for the Reporting API. The APIM API definition and routing is instead handled entirely within `infra/apps/reporting-api/main.bicep` (which defines the `reporting` API path, operations, product, and policy directly).

**Assessment:** Step 1.4 ("Add APIM route for `/reporting/*`") from the plan is effectively satisfied by the APIM resources in `infra/apps/reporting-api/main.bicep` rather than in `infra/core/main.bicep`. Whether this counts as "done" depends on the plan's intent — if the plan expected the route to be defined in `core/main.bicep`, this is a deviation. However, the existing pattern follows a per-app APIM configuration approach which is architecturally consistent.

---

## Clarifying Questions

- None — all questions were answerable through file inspection.
