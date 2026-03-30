# Phase 1 & Phase 2 File Verification Report

## Research Topics

Verify that planned infrastructure and Docker files exist and match their specifications from the Reporting API Copilot SDK plan.

---

## Step 1.1: storage-account.bicep

- **Path:** `infra/modules/storage/storage-account.bicep`
- **STATUS: MATCH**

All requirements verified:

| Requirement | Present | Details |
|---|---|---|
| Microsoft.Storage/storageAccounts resource | Yes | `Microsoft.Storage/storageAccounts@2024-01-01` |
| Standard_LRS | Yes | `sku.name: 'Standard_LRS'` |
| StorageV2 | Yes | `kind: 'StorageV2'` |
| Hot tier | Yes | `accessTier: 'Hot'` |
| allowBlobPublicAccess: false | Yes | Set in properties |
| minimumTlsVersion: 'TLS1_2' | Yes | Set in properties |
| supportsHttpsTrafficOnly: true | Yes | Set in properties |
| Blob service + 'reports' container | Yes | `blobServices/default` + `containers/reports` |
| publicAccess: 'None' on container | Yes | Set in container properties |
| Storage Blob Data Contributor role | Yes | Role ID `ba92f5b4-2d11-453d-a403-e96b0029c9fe` assigned to UAI principal |
| Output: endpoint | Yes | `output endpoint string = storageAccount.properties.primaryEndpoints.blob` |

---

## Step 1.2: container-app-http-sidecar.bicep

- **Path:** `infra/modules/host/container-app-http-sidecar.bicep`
- **STATUS: MATCH**

All requirements verified:

| Requirement | Present | Details |
|---|---|---|
| sidecarContainers parameter (array) | Yes | `param sidecarContainers array = []` |
| volumes parameter (array) | Yes | `param volumes array = []` |
| secrets parameter (array) | Yes | `param secrets array = []` |
| Merges sidecar containers alongside main | Yes | `var allContainers = concat(mainContainer, sidecarContainers)` |
| Volume mounts | Yes | `mainContainerVolumeMounts` variable maps volume definitions to mounts, `volumeDefinitions` creates EmptyDir volumes |

Additional features beyond plan: cpu/memory params, externalIngress param, minReplicas param — all beneficial.

---

## Step 1.3: reporting-api main.bicep

- **Path:** `infra/apps/reporting-api/main.bicep`
- **STATUS: MATCH**

All requirements verified:

| Requirement | Present | Details |
|---|---|---|
| Main container (biotrackr-reporting-api) | Yes | Uses `container-app-http-sidecar` module with `name` param, port 8080, cpu '0.5', memory '1Gi' |
| Sidecar (copilot-python) | Yes | `name: 'copilot-python'` in `sidecarContainers` array |
| Sidecar port 4321 | Yes | Readiness probe `tcpSocket.port: 4321` |
| Sidecar 0.5 vCPU / 1 GiB | Yes | `cpu: json('0.5')`, `memory: '1Gi'` |
| Sidecar readiness probe TCP on 4321 | Yes | `type: 'Readiness'`, `tcpSocket: { port: 4321 }` |
| Shared EmptyDir volume at /tmp/reports | Yes | `volumes: [{ name: 'shared-reports', mountPath: '/tmp/reports' }]`, sidecar also mounts it |
| github-copilot-token secret | Yes | Key Vault reference secret `GitHubCopilotToken` |
| Env: azureappconfigendpoint | Yes | From `appConfig.properties.endpoint` |
| Env: managedidentityclientid | Yes | From `uai.properties.clientId` |
| Env: BlobStorageEndpoint | Yes | From `storageAccount.properties.primaryEndpoints.blob` |
| Internal ingress | Yes | `externalIngress: false` |
| UAI identity | Yes | `uaiName: uai.name` passed to module |

Additional features beyond plan: APIM API + operations + product + named values, App Configuration entries, Key Vault secret placeholder, policy-jwt-auth.xml policy — all part of the broader infrastructure.

---

## Step 1.4: APIM Route

- **Expected:** `infra/core/main.bicep` modified to add reporting API route
- **STATUS: DEVIATION**

The APIM route for the reporting API is **NOT** in `infra/core/main.bicep`. Instead, the APIM API definition (resource `reportingApimApi`), operations, product, and policy are all defined within `infra/apps/reporting-api/main.bicep` itself.

This is **consistent with the project pattern** — other APIs (e.g., chat-api) also define their APIM routes within their own `infra/apps/<api>/main.bicep` rather than modifying `infra/core/main.bicep`.

**Conclusion:** The APIM route exists but in a different location than the plan specified. The actual implementation follows a better architectural pattern (self-contained per-API modules).

---

## Step 2.1: Dockerfile.sidecar

- **Path:** `src/Biotrackr.Reporting.Api/Dockerfile.sidecar`
- **STATUS: MATCH**

All requirements verified:

| Requirement | Present | Details |
|---|---|---|
| Multi-stage from copilot CLI image | Yes | `FROM ghcr.io/github/copilot-cli:latest AS cli`, binary copied into final image |
| python:3.12-slim | Yes | `FROM python:3.12-slim-bookworm` (bookworm variant) |
| pip install pandas | Yes | `pandas==2.2.3` |
| pip install matplotlib | Yes | `matplotlib==3.10.0` |
| pip install seaborn | Yes | `seaborn==0.13.2` |
| pip install reportlab | Yes | `reportlab==4.3.1` |
| pip install numpy | Yes | `numpy==2.2.3` |
| mkdir /tmp/reports | Yes | `RUN mkdir -p /tmp/reports` |
| EXPOSE 4321 | Yes | Present |
| Copilot entrypoint --headless --port 4321 | Yes | `ENTRYPOINT ["copilot", "--headless", "--port", "4321", ...]` |

Additional security measures: non-root user (`copilot`), `--bind 0.0.0.0`, `--auth-token-env GITHUB_TOKEN`, `--no-auto-update`, ca-certificates installed.

---

## Step 2.2: GitHub Actions Workflow

- **Path:** `.github/workflows/build-copilot-python-image.yml`
- **STATUS: MATCH**

All requirements verified:

| Requirement | Present | Details |
|---|---|---|
| Trigger on Dockerfile.sidecar changes | Yes | `pull_request` paths filter on `src/Biotrackr.Reporting.Api/Dockerfile.sidecar` |
| Build Docker image | Yes | `docker build -f Dockerfile.sidecar` step |
| Push to ACR as biotrackr-copilot-python | Yes | `APP_NAME: biotrackr-copilot-python`, pushes to ACR |

Additional features: `workflow_dispatch` trigger, Azure login, Trivy vulnerability scanner, tagged with `github.sha`.

---

## Summary

| Step | File | Status |
|---|---|---|
| 1.1 | `infra/modules/storage/storage-account.bicep` | **MATCH** |
| 1.2 | `infra/modules/host/container-app-http-sidecar.bicep` | **MATCH** |
| 1.3 | `infra/apps/reporting-api/main.bicep` | **MATCH** |
| 1.4 | APIM route in `infra/core/main.bicep` | **DEVIATION** — route defined in `infra/apps/reporting-api/main.bicep` instead (consistent with project pattern) |
| 2.1 | `src/Biotrackr.Reporting.Api/Dockerfile.sidecar` | **MATCH** |
| 2.2 | `.github/workflows/build-copilot-python-image.yml` | **MATCH** |
