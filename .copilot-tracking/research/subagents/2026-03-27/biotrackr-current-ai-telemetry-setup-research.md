# Biotrackr Current AI & Telemetry Setup Research

## Research Topics

1. How Claude is called (Anthropic integration, ChatClient, agent configuration)
2. Current OpenTelemetry setup (Chat.Api and MCP Server)
3. Current Application Insights setup
4. Infrastructure (Azure resources deployed via Bicep)
5. Chat.Api agent building (Microsoft Agent Framework)
6. NuGet packages (Chat.Api and MCP Server)

---

## 1. How Claude Is Called

### Direct Anthropic SDK Integration

Claude is invoked **directly via the Anthropic SDK** — not through Azure AI Services or Foundry.

**Chat.Api — Primary Agent (ChatAgentProvider.cs):**

```csharp
AnthropicClient anthropicClient = new() { ApiKey = _settings.AnthropicApiKey };

AIAgent chatAgent = anthropicClient.AsAIAgent(
    model: _settings.ChatAgentModel,     // 'claude-sonnet-4-6'
    name: "BiotrackrChatAgent",
    instructions: _settings.ChatSystemPrompt,
    tools: [.. wrappedTools]);
```

- Uses `Microsoft.Agents.AI.Anthropic` (v1.0.0-rc4) which provides `AnthropicClient.AsAIAgent()` extension.
- The `AnthropicClient` is constructed with a raw API key from Anthropic (stored in Key Vault, referenced via App Configuration).
- Model is configurable via App Config key `Biotrackr:ChatAgentModel`, default `claude-sonnet-4-6`.

**Chat.Api — Reviewer Agent (ReportReviewerService.cs):**

```csharp
AnthropicClient anthropicClient = new() { ApiKey = _settings.AnthropicApiKey };
AIAgent reviewer = anthropicClient.AsAIAgent(
    model: _settings.ChatAgentModel,
    name: "BiotrackrReportReviewer",
    instructions: _settings.ReviewerSystemPrompt);
```

- A second agent is created per-review for report validation.
- Stateless, no tools attached.
- Also uses direct Anthropic API key.

**Key Configuration (Settings.cs):**

- `AnthropicApiKey` — stored in Key Vault, referenced via App Config Key Vault reference
- `ChatAgentModel` — defaults to `claude-sonnet-4-6`
- `ChatSystemPrompt` — loaded from Key Vault
- `ReviewerSystemPrompt` — loaded from Key Vault

### Transport Protocol

- Chat.Api exposes AGUI (Protocol Streaming) over HTTP SSE via `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`.
- `app.MapAGUI("/", dynamicAgent)` maps the streaming endpoint.

### Agent Lifecycle

- **Dynamic agent pattern**: A `ChatAgentProvider` singleton builds/caches the `AIAgent`.
- Agent is rebuilt when MCP tool count changes (e.g., MCP Server comes online after degraded start).
- A delegating wrapper in `Program.cs` overrides `RunStreamingAsync` to always use the latest agent.

---

## 2. Current OpenTelemetry Setup

### Chat.Api OpenTelemetry

**Source**: `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Program.cs` (lines 96-129)

```csharp
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

**What's instrumented**:

- ASP.NET Core HTTP requests (tracing + metrics)
- HTTP client outbound calls (tracing + metrics)
- Structured logging via OpenTelemetry log exporter
- All exported to Azure Monitor (Application Insights)

**What's NOT instrumented**:

- No custom `ActivitySource` or `Meter` defined in Chat.Api code
- No `AddSource("Biotrackr.Chat.Api")` in tracing config
- No LLM/AI-specific instrumentation (no `UseOpenTelemetry()` on the chat client)
- No `Microsoft.Extensions.AI.OpenTelemetry` package referenced
- Tool calls are logged via `ILogger` (structured logs) but not as OTel activities/spans

### MCP Server OpenTelemetry

**Source**: `src/Biotrackr.Mcp.Server/Biotrackr.Mcp.Server/Program.cs` (lines 61-95)

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.SetResourceBuilder(resourceBuilder)
        .AddSource("Biotrackr.Mcp.Server")          // ← Custom source!
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation(o => { ... })   // ← Subscription key redaction
        .AddAzureMonitorTraceExporter(...))
    .WithMetrics(b => b.SetResourceBuilder(resourceBuilder)
        .AddMeter("Biotrackr.Mcp.Server")            // ← Custom meter!
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAzureMonitorMetricExporter(...));

builder.Logging.AddOpenTelemetry(log => { ... AddAzureMonitorLogExporter(...) });
```

**BaseTool.cs — Custom OTel Instrumentation:**

```csharp
private static readonly ActivitySource _activitySource = new("Biotrackr.Mcp.Server");
private static readonly Meter _meter = new("Biotrackr.Mcp.Server");
private static readonly Counter<long> _invocationCounter = _meter.CreateCounter<long>("mcp.tool.invocations");
private static readonly Counter<long> _errorCounter = _meter.CreateCounter<long>("mcp.tool.errors");
private static readonly Histogram<double> _latencyHistogram = _meter.CreateHistogram<double>("mcp.tool.duration", unit: "ms");
```

Each tool call creates:

- A distributed tracing `Activity` (span) with tags: `mcp.tool.operation`, `mcp.tool.endpoint`, `mcp.tool.error`, `mcp.tool.status_code`
- Counter increments for invocations and errors (with operation and reason tags)
- Latency histogram recording

**MCP Server has significantly richer OTel instrumentation than Chat.Api.**

### Reporting.Api OpenTelemetry

- Same pattern as Chat.Api: ASP.NET Core + HTTP client instrumentation + Azure Monitor exporters
- No custom ActivitySource or Meter

---

## 3. Current Application Insights Setup

### SDK Approach

**No Application Insights SDK** is used anywhere in the codebase. There are zero references to:

- `Microsoft.ApplicationInsights` package
- `AddApplicationInsightsTelemetry()`
- `TelemetryClient`

Instead, all services use the **OpenTelemetry → Azure Monitor Exporter** pattern:

- `Azure.Monitor.OpenTelemetry.Exporter` (v1.4.0) for traces, metrics, and logs
- Connection string passed as environment variable `applicationinsightsconnectionstring`

### Infrastructure

All 11+ Container Apps receive the Application Insights connection string via environment variable injection in their Bicep deployment.

### Alert Rules

The `agent-alerts.bicep` module defines scheduled query alert rules over Application Insights data:

1. **Excessive Tool Calls Alert** — fires when a session exceeds 50 tool calls in 5 minutes (Severity 2)
2. **Auth Failure Spike Alert** — fires when HTTP 401/403 responses exceed 10 in 15 minutes (Severity 1)

Both use Log Analytics KQL queries over `AppTraces` and `AppRequests` tables, with email notification via Action Group.

---

## 4. Infrastructure (Azure Resources)

### Currently Deployed (from `infra/core/main.bicep`)

| Module | Resource Type |
|---|---|
| `log-analytics` | Log Analytics Workspace (30-day retention, PerGB2018 SKU) |
| `app-insights` | Application Insights (web, linked to Log Analytics) |
| `container-app-env` | Container Apps Environment |
| `user-assigned-identity` | User-Assigned Managed Identity |
| `acr` | Azure Container Registry |
| `key-vault` | Azure Key Vault |
| `app-config` | Azure App Configuration |
| `budget` | Azure Budget |
| `cosmos` | Azure Cosmos DB (serverless) |
| `apim` | API Management (Consumption tier) |

### App Deployments (from `infra/apps/`)

11 Container App deployments: activity-api, activity-service, auth-service, chat-api, food-api, food-service, mcp-server, reporting-api, sleep-api, sleep-service, ui, weight-api, weight-service.

### What's NOT Deployed

- **No Azure AI Services** (Microsoft.CognitiveServices/accounts)
- **No Azure AI Foundry** project or hub
- **No Azure OpenAI** resource
- **No AI model deployments** in Azure

Confirmed via grep: zero matches for `Foundry`, `CognitiveServices`, `AI Services`, `aiServices`, or `Microsoft.CognitiveServices` in any Bicep file.

**Claude is called directly via Anthropic's API** — the API key is stored in Key Vault and referenced via App Configuration.

---

## 5. Chat.Api Agent Building

### Architecture Summary

**File**: `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Services/ChatAgentProvider.cs`

1. `ChatAgentProvider` (singleton) builds and caches the `AIAgent`
2. `McpToolService` (singleton + hosted service) manages MCP client lifecycle
3. On startup, MCP connection attempt + initial agent build (may have 0 tools)
4. Dynamic wrapper in `Program.cs` delegates to latest agent on each request
5. Agent rebuilds when tool count changes

### Agent Build Pipeline

```
AnthropicClient → .AsAIAgent(model, name, instructions, tools)
  → ToolPolicyMiddleware (budget + validation)
    → ConversationPersistenceMiddleware (Cosmos DB history)
      → GracefulDegradationMiddleware (HTTP error handling)
```

### Tools Available to Agent

- **12 MCP tools** (via MCP Server): Activity(3) + Food(3) + Sleep(3) + Weight(3)
  - Each wrapped with `CachingMcpToolWrapper` for response caching
- **2 Native function tools**: `RequestReport`, `GetReportStatus`
  - Registered as `AIFunction` in DI and added directly to agent tools

### Multi-Agent Pattern

- **Primary Agent** (BiotrackrChatAgent): Full tool set, streaming, middleware pipeline
- **Reviewer Agent** (BiotrackrReportReviewer): Stateless, no tools, created per-review in `ReportReviewerService`
- Both use same Anthropic API key and model

---

## 6. NuGet Packages

### Chat.Api (`Biotrackr.Chat.Api.csproj`)

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Agents.AI` | 1.0.0-rc4 | Agent framework core (AIAgent, AgentSession) |
| `Microsoft.Agents.AI.Anthropic` | 1.0.0-rc4 | Anthropic/Claude integration (AsAIAgent extension) |
| `Microsoft.Agents.AI.Hosting` | 1.0.0-preview.260311.1 | Agent hosting services |
| `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` | 1.0.0-preview.260311.1 | AG-UI SSE transport |
| `ModelContextProtocol` | 1.1.0 | MCP client SDK |
| `Azure.Monitor.OpenTelemetry.Exporter` | 1.4.0 | Azure Monitor trace/metric/log export |
| `OpenTelemetry.Extensions.Hosting` | 1.12.0 | OTel DI hosting |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.12.0 | HTTP request instrumentation |
| `OpenTelemetry.Instrumentation.Http` | 1.12.0 | HTTP client instrumentation |
| `Azure.Identity` | 1.18.0 | Managed identity auth |
| `Azure.Storage.Blobs` | 12.24.0 | Blob storage (report artifacts) |
| `Microsoft.Azure.Cosmos` | 3.57.1 | Cosmos DB client |
| `Microsoft.Azure.AppConfiguration.AspNetCore` | 8.5.0 | App Configuration |
| `Microsoft.Identity.Web` | 4.5.0 | Identity Web + agent identities |
| `Newtonsoft.Json` | 13.0.4 | JSON serialization |

### MCP Server (`Biotrackr.Mcp.Server.csproj`)

| Package | Version | Purpose |
|---|---|---|
| `ModelContextProtocol` | 1.1.0 | MCP server SDK |
| `ModelContextProtocol.AspNetCore` | 1.1.0 | MCP ASP.NET Core transport |
| `Azure.Monitor.OpenTelemetry.Exporter` | 1.4.0 | Azure Monitor export |
| `OpenTelemetry.Extensions.Hosting` | 1.15.0 | OTel hosting |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.15.0 | HTTP request instrumentation |
| `OpenTelemetry.Instrumentation.Http` | 1.15.0 | HTTP client instrumentation |
| `Microsoft.Extensions.Http.Resilience` | 10.2.0 | HTTP resilience |
| `Microsoft.Extensions.Configuration.AzureAppConfiguration` | 8.4.0 | App Configuration |

### Reporting.Api (`Biotrackr.Reporting.Api.csproj`)

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Agents.AI` | 1.0.0-rc4 | Agent framework core |
| `Microsoft.Agents.AI.GitHub.Copilot` | 1.0.0-preview.260311.1 | Copilot SDK integration |
| `Microsoft.Agents.AI.Hosting` | 1.0.0-preview.260311.1 | Agent hosting |
| `Microsoft.Agents.AI.Hosting.A2A.AspNetCore` | 1.0.0-preview.260311.1 | A2A transport |
| `Azure.Monitor.OpenTelemetry.Exporter` | 1.4.0 | Azure Monitor export |
| OTel packages | 1.12.0 | Standard instrumentation |

### Notable Absences

- **No `Microsoft.Extensions.AI.OpenTelemetry`** — no AI-specific OTel instrumentation
- **No Azure.AI.OpenAI or Azure.AI.Inference** — no Azure AI SDK
- **No Experimental/GenAI OTel semantic conventions** package

---

## Gap Analysis: Foundry Integration

### Current State

- Claude called directly via Anthropic API key
- No Azure AI Services resource deployed
- No Foundry project or hub
- No AI model deployments in Azure

### What Would Be Needed for Foundry

1. **Azure AI Services resource** (Microsoft.CognitiveServices/accounts) — Bicep module
2. **Azure AI Foundry project** (optional) — for model management, evaluation
3. **Model deployment** — Claude via Azure AI Model Catalog (if available) or continue with direct Anthropic
4. **SDK change** — Replace `AnthropicClient` with Azure AI Foundry client or `Azure.AI.Inference` SDK
5. **Auth change** — Switch from API key to Managed Identity / Entra token auth
6. **OTel enhancement** — Add `Microsoft.Extensions.AI.OpenTelemetry` for gen_ai semantic conventions (LLM call tracing, token usage, model metadata)

### AI Telemetry Gap

The most significant current gap is **no LLM-specific instrumentation**:

- No tracing of individual Claude API calls as OTel spans
- No token usage metrics (input/output tokens, costs)
- No model performance metrics (latency per LLM call, streaming time-to-first-token)
- No gen_ai semantic conventions (gen_ai.system, gen_ai.request.model, etc.)
- Tool call telemetry exists only as structured logs, not OTel spans with parent-child relationships
- The MCP Server has rich OTel instrumentation, but Chat.Api's agent layer has none

### Quick Wins (No Foundry Required)

1. Add `Microsoft.Extensions.AI.OpenTelemetry` to Chat.Api
2. Wrap the `AnthropicClient` with `.UseOpenTelemetry()` pipeline
3. Add `.AddSource("Microsoft.Extensions.AI")` to tracing configuration
4. This would instrument all LLM calls with gen_ai semantic conventions automatically

---

## References

- `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Program.cs` — Service registration, OTel setup
- `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Services/ChatAgentProvider.cs` — Agent building, Anthropic client creation
- `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Services/McpToolService.cs` — MCP client lifecycle
- `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Tools/ReportReviewerService.cs` — Reviewer agent (second Claude call)
- `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Tools/RequestReportTool.cs` — Native report tool
- `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Tools/GetReportStatusTool.cs` — Report status + reviewer invocation
- `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Configuration/Settings.cs` — All config properties
- `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Biotrackr.Chat.Api.csproj` — Package references
- `src/Biotrackr.Mcp.Server/Biotrackr.Mcp.Server/Program.cs` — MCP Server OTel setup
- `src/Biotrackr.Mcp.Server/Biotrackr.Mcp.Server/Tools/BaseTool.cs` — Custom ActivitySource + Meter + Counters
- `src/Biotrackr.Mcp.Server/Biotrackr.Mcp.Server/Biotrackr.Mcp.Server.csproj` — Package references
- `src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api.csproj` — Reporting API packages
- `infra/core/main.bicep` — Core infrastructure modules
- `infra/apps/chat-api/main.bicep` — Chat API deployment + App Config settings
- `infra/modules/monitoring/app-insights.bicep` — Application Insights module
- `infra/modules/monitoring/agent-alerts.bicep` — Agent alert rules
- `infra/modules/monitoring/log-analytics.bicep` — Log Analytics workspace

---

## Follow-On Questions (Directly Relevant)

1. Does `Microsoft.Agents.AI.Anthropic` v1.0.0-rc4 support the `IChatClient` abstraction from `Microsoft.Extensions.AI`, enabling `.UseOpenTelemetry()` pipeline decoration?
2. What gen_ai semantic conventions are supported by `Microsoft.Extensions.AI.OpenTelemetry` for Anthropic models specifically?
3. Can Anthropic Claude models be deployed via Azure AI Foundry Model Catalog, or would the Foundry integration require switching to Azure OpenAI models?

## Clarifying Questions

None — all research questions answered from codebase analysis.
