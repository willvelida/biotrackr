# Foundry Integration — Codebase Conventions & Infrastructure Research

## Research Topics

1. Existing Bicep infrastructure — file inventory, resource organization, CognitiveServices presence, App Insights module
2. Existing OpenTelemetry setup in Chat.Api — ActivitySource, packages, App Insights connection string
3. GitHub Actions workflows — full inventory, Chat.Api pipeline, evaluation workflows
4. Project configuration patterns — Settings.cs, App Configuration / Key Vault, copilot-instructions
5. Existing decision records — inventory, Foundry/evaluation relevance

---

## Q1: Existing Bicep Infrastructure

### Core entry point

`infra/core/main.bicep` — deploys all shared infrastructure. Parameters: `location`, `baseName`, `environment`, `tags`.

Modules invoked (in order):

| Module | Bicep Path |
|---|---|
| Log Analytics | `infra/modules/monitoring/log-analytics.bicep` |
| Application Insights | `infra/modules/monitoring/app-insights.bicep` |
| Container App Environment | `infra/modules/host/container-app-environment.bicep` |
| User-Assigned Identity | `infra/modules/identity/user-assigned-identity.bicep` |
| Container Registry | `infra/modules/host/container-registry.bicep` |
| Key Vault | `infra/modules/security/key-vault.bicep` |
| Azure App Configuration | `infra/modules/configuration/azure-app-config.bicep` |
| Budget | `infra/modules/monitoring/budget.bicep` |
| Cosmos DB (Serverless) | `infra/modules/database/serverless-cosmos-db.bicep` |
| APIM (Consumption) | `infra/modules/apim/apim-consumption.bicep` |

### Complete Bicep file inventory

**infra/modules/** subdirectories and files:

- `monitoring/` — `log-analytics.bicep`, `app-insights.bicep`, `budget.bicep`, `agent-alerts.bicep`
- `host/` — `container-app-environment.bicep`, `container-app-http.bicep`, `container-app-http-sidecar.bicep`, `container-app-jobs.bicep`, `container-registry.bicep`
- `database/` — `serverless-cosmos-db.bicep`
- `identity/` — `user-assigned-identity.bicep`
- `security/` — `key-vault.bicep`
- `storage/` — `storage-account.bicep`
- `configuration/` — `azure-app-config.bicep`
- `apim/` — `apim-consumption.bicep`, `apim-named-values.bicep`, `apim-products.bicep`

**infra/apps/** — per-service deployment Bicep (each has `main.bicep` + `main.dev.bicepparam`):

- `activity-api/`, `activity-service/`, `auth-service/`, `chat-api/`, `food-api/`, `food-service/`, `mcp-server/`, `reporting-api/`, `sleep-api/`, `sleep-service/`, `ui/`, `weight-api/`, `weight-service/`

### CognitiveServices check

**No `Microsoft.CognitiveServices` resource exists** anywhere in the Bicep files. Grep returned zero matches.

### Application Insights module

Resource name pattern: `appins-${baseName}-${environment}` (e.g., `appins-biotrackr-dev`).

Output: `appInsightsName` (string).

In `infra/apps/chat-api/main.bicep`, Application Insights is referenced as an existing resource:

```bicep
resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}
```

The connection string is passed to the Container App as an environment variable:

```bicep
{
  name: 'applicationinsightsconnectionstring'
  value: appInsights.properties.ConnectionString
}
```

### Naming conventions (from `main.dev.bicepparam`)

- baseName = `biotrackr`
- environment = `dev`
- Resource naming pattern: `{type}-${baseName}-${environment}` (e.g., `appins-biotrackr-dev`, `kv-biotrackr-dev`, `config-biotrackr-dev`)
- Location: `australiaeast`
- Tags pattern: `{ ApplicationName: 'Biotrackr', Component: '<component>', Environment: 'Dev' }`

---

## Q2: Existing OpenTelemetry Setup in Chat.Api

### NuGet packages (from `.csproj`)

```xml
<PackageReference Include="Azure.Monitor.OpenTelemetry.Exporter" Version="1.4.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.12.0" />
```

### OpenTelemetry configuration in `Program.cs`

Resource attributes set with `service.name = "Biotrackr.Chat.Api"` and `service.version = "1.0.0"`.

Full OTel setup:

```csharp
var appInsightsConnectionString = builder.Configuration["applicationinsightsconnectionstring"];

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAzureMonitorTraceExporter(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAzureMonitorMetricExporter(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });
    });

builder.Logging.AddOpenTelemetry(log =>
{
    log.SetResourceBuilder(resourceBuilder);
    log.AddAzureMonitorLogExporter(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
});
```

### Application Insights connection string flow

1. Bicep `infra/apps/chat-api/main.bicep` reads `appInsights.properties.ConnectionString` and sets env var `applicationinsightsconnectionstring`
2. `Program.cs` reads `builder.Configuration["applicationinsightsconnectionstring"]`
3. Passed to all three Azure Monitor exporters (traces, metrics, logs)

### ActivitySource usage

No custom `ActivitySource` or manual `Activity` creation found in `src/Biotrackr.Chat.Api/`. Only automatic ASP.NET Core and HttpClient instrumentation.

---

## Q3: Existing GitHub Actions Workflows

### Full inventory (26 files)

**Service deploy workflows:**

- `deploy-activity-api.yml`
- `deploy-activity-service.yml`
- `deploy-auth-service.yml`
- `deploy-chat-api.yml`
- `deploy-core-infra.yml`
- `deploy-food-api.yml`
- `deploy-food-service.yml`
- `deploy-mcp-server.yml`
- `deploy-reporting-api.yml`
- `deploy-sleep-api.yml`
- `deploy-sleep-service.yml`
- `deploy-ui.yml`
- `deploy-weight-api.yml`
- `deploy-weight-service.yml`

**Reusable templates:**

- `template-dotnet-run-unit-tests.yml`
- `template-dotnet-run-e2e-tests.yml`
- `template-dotnet-run-contract-tests.yml`
- `template-bicep-whatif.yml`
- `template-bicep-validate.yml`
- `template-bicep-linter.yml`
- `template-bicep-deploy.yml`
- `template-acr-push-image.yml`
- `template-aca-api-integration-tests.yml`

**Other:**

- `task-purge-old-images.yml`
- `codeql.yml`
- `build-copilot-python-image.yml`

### Chat.Api CI/CD pipeline (`deploy-chat-api.yml`)

Triggers on PR to `main` with paths `infra/apps/chat-api/**` or `src/Biotrackr.Chat.Api/**`.

Pipeline stages:

1. `env-setup` — sets .NET version (10.0.x)
2. `run-unit-tests` — uses template, coverage threshold 70%, fails if below
3. `run-contract-tests` — uses template, filters by `Contract` in test name
4. `build-container-image-dev` — pushes to ACR
5. `retrieve-container-image-dev` — reads ACR login server
6. `lint` — Bicep linter on `infra/apps/chat-api/main.bicep`
7. `validate` — Bicep validate with dev params + tenant/alert-email injection
8. `preview` — Bicep what-if
9. `deploy-dev` — Bicep deploy to dev environment
10. `run-e2e-tests` — runs E2E tests after deploy, filters by `E2E`

Uses OIDC (federated identity) auth with secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_RG_NAME_DEV`, `EMAIL_ADDRESS`.

### Evaluation/quality workflows

**No existing evaluation or test-quality workflows** found. Grep for `eval|evaluation|test.*quality` returned zero matches across all workflow files.

---

## Q4: Project Configuration Patterns

### Settings.cs

Located at `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Configuration/Settings.cs`.

Bound via `builder.Services.Configure<Settings>(builder.Configuration.GetSection("Biotrackr"))`.

Properties:

```csharp
public string DatabaseName { get; set; }
public string ConversationsContainerName { get; set; }
public string CosmosEndpoint { get; set; }
public string AgentIdentityId { get; set; }
public string McpServerUrl { get; set; }
public string ApiSubscriptionKey { get; set; }
public string McpServerApiKey { get; set; }
public string AnthropicApiKey { get; set; }
public string ChatAgentModel { get; set; }
public string ChatSystemPrompt { get; set; }
public int ToolCallBudgetPerSession { get; set; } = 20;
public string ReportingApiUrl { get; set; }
public string ReportingBlobStorageEndpoint { get; set; }
public string ReviewerSystemPrompt { get; set; }
public int ConversationTtlSeconds { get; set; } = 7_776_000; // 90 days
```

### App Configuration / Key Vault pattern

1. Container gets `azureappconfigendpoint` and `managedidentityclientid` as environment variables (from Bicep)
2. `Program.cs` connects to Azure App Configuration using `ManagedIdentityCredential`
3. Loads all key-value pairs (`KeyFilter.Any`, `LabelFilter.Null`)
4. Configures Key Vault references via `.ConfigureKeyVault(kv => kv.SetCredential(credential))`
5. Secrets (like `AnthropicApiKey`, `ReviewerSystemPrompt`) stored in Key Vault and referenced from App Config
6. Non-secret config (like `DatabaseName`, `CosmosEndpoint`, `McpServerUrl`) stored directly in App Config under `Biotrackr:` prefix

### copilot-instructions.md

**No `.github/copilot-instructions.md` file found** in the repository. The `.github/` directory contains: `agents/`, `dependabot.yml`, `ISSUE_TEMPLATE/`, `skills/`, `workflows/`.

---

## Q5: Existing Decision Records

### Inventory (`docs/decision-records/`)

1. `2025-10-28-backend-api-route-structure.md`
2. `2025-10-28-contract-test-architecture.md`
3. `2025-10-28-dotnet-configuration-format.md`
4. `2025-10-28-flaky-test-handling.md`
5. `2025-10-28-integration-test-project-structure.md`
6. `2025-10-28-program-entry-point-coverage-exclusion.md`
7. `2025-10-28-service-lifetime-registration.md`
8. `2025-10-29-coverlet-extension-method-coverage-anomaly.md`
9. `2025-11-12-apim-managed-identity-auth.md`
10. `2025-11-12-apim-named-values-for-jwt-config.md`
11. `2026-03-04-conditional-tenantid-injection.md`
12. `decision-record-template.md`

### Foundry/evaluation decision records

**No existing decision records about Foundry or evaluation strategy.** All records relate to testing patterns, configuration, APIM, and CI/CD conventions.

---

## Follow-on Questions

None discovered — all original questions answered with evidence.

---

## Key Observations for Foundry Integration

1. **No CognitiveServices/AI resource exists** — Foundry integration would require adding a new module (e.g., `infra/modules/ai/cognitive-services.bicep` or similar) and wiring it into `infra/core/main.bicep`
2. **App Insights is well-established** — resource name `appins-biotrackr-dev`, connection string flows through Bicep → env var → OTel exporters. Foundry can reuse the same Log Analytics workspace for tracing
3. **OTel is mature but basic** — traces, metrics, and logs all export to Azure Monitor. No custom `ActivitySource`. Adding Foundry evaluation spans would extend the existing setup
4. **Config pattern is clear** — new Foundry settings would go into `Settings.cs`, stored in App Config (non-secrets) and Key Vault (secrets like API keys), following the `Biotrackr:` prefix convention
5. **Pipeline is template-driven** — reusable workflow templates for unit tests, contract tests, E2E tests, Bicep lifecycle. A Foundry evaluation workflow would follow the same template pattern
6. **No evaluation workflow exists** — greenfield opportunity; no conflicts with existing CI/CD
7. **No copilot-instructions.md** — conventions live in `docs/decision-records/` and `docs/standards/`
