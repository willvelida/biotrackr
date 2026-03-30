<!-- markdownlint-disable-file -->
# Implementation Details: Azure AI Foundry GenAIOps Integration (Without Model Deployment)

## Context Reference

Sources: .copilot-tracking/research/2026-03-27/foundry-genaiops-without-model-deployment-research.md, .copilot-tracking/research/subagents/2026-03-27/foundry-integration-codebase-conventions.md, .copilot-tracking/research/subagents/2026-03-27/foundry-dotnet-integration-research.md

## Implementation Phase 1: Foundry Infrastructure (Bicep)

<!-- parallelizable: false -->

### Step 1.1: Create Foundry Bicep module

Create a new Bicep module for the Foundry resource (CognitiveServices account with kind `AIServices`) and nested Foundry project. This is a net-new resource type for the Biotrackr infrastructure.

The module deploys:
1. `Microsoft.CognitiveServices/accounts` with `kind: 'AIServices'` — the Foundry resource
2. `Microsoft.CognitiveServices/accounts/projects` — the Foundry project (child resource)
3. Connects the existing Application Insights instance to the Foundry project for trace correlation

```bicep
// infra/modules/ai/foundry.bicep
param name string
param projectName string
param location string
param tags object
param appInsightsResourceId string

resource foundryResource 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: name
  location: location
  tags: tags
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: name
    publicNetworkAccess: 'Enabled'
  }
}

resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2024-10-01' = {
  parent: foundryResource
  name: projectName
  location: location
  tags: tags
  properties: {
    description: 'Biotrackr GenAIOps — evaluation, monitoring, and tracing for Claude-powered agents'
  }
}

output foundryResourceName string = foundryResource.name
output foundryProjectName string = foundryProject.name
output foundryEndpoint string = foundryResource.properties.endpoint
output foundryProjectEndpoint string = foundryProject.properties.endpoint
```

Note: The exact API version and project property schema should be verified during implementation. The `2024-10-01` version is used based on research, but may need updating. The `appInsightsResourceId` parameter is declared for future use (e.g., if a Bicep property for App Insights connection is discovered), but Application Insights connection to the Foundry project currently requires a portal step after deployment — navigate to Foundry portal → project Settings → Connected resources → Add Application Insights.

Files:
* infra/modules/ai/foundry.bicep - New module (create)

Discrepancy references:
* DR-01 (Bicep API version may differ from documented examples)

Success criteria:
* Bicep module compiles without errors via `az bicep build`
* Resource names follow project convention: `ai-biotrackr-dev` for the Foundry resource, `biotrackr-genaiops` for the project

Context references:
* infra/core/main.bicep (Lines 1-50) - Existing module pattern and parameter conventions
* infra/modules/monitoring/app-insights.bicep - Application Insights resource for connection

Dependencies:
* None — first step

### Step 1.2: Create GPT judge model deployment Bicep module

Create a separate Bicep module for the GPT-4.1-mini model deployment within the Foundry resource. This is the Tier 2 judge model used exclusively by AI-assisted evaluators (coherence, fluency, relevance, groundedness, task adherence). It is NOT used for inference.

```bicep
// infra/modules/ai/foundry-model-deployment.bicep
param foundryResourceName string
param deploymentName string = 'gpt-4.1-mini'
param modelName string = 'gpt-4.1-mini'
param modelVersion string = '2025-04-14'
param skuName string = 'GlobalStandard'
param skuCapacity int = 1 // Minimum TPM (1K tokens/minute) — evaluation only

resource foundryResource 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: foundryResourceName
}

resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: foundryResource
  name: deploymentName
  sku: {
    name: skuName
    capacity: skuCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: modelVersion
    }
  }
}

output deploymentName string = modelDeployment.name
```

Note: The model version, SKU name, and capacity values must be verified against the Azure OpenAI quota for `australiaeast` region. GPT-4.1-mini may not be available in all regions — check https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models. If unavailable in `australiaeast`, use the nearest supported region or `gpt-4o-mini` as fallback.

Files:
* infra/modules/ai/foundry-model-deployment.bicep - New module (create)

Discrepancy references:
* DR-02 (GPT-4.1-mini availability in australiaeast unverified)
* DD-01 (Tier 2 selected over Tier 1)

Success criteria:
* Bicep module compiles without errors
* Deployment uses minimum capacity (1K TPM) to minimize cost
* Model deployment name `gpt-4.1-mini` is referenced by evaluation code

Context references:
* .copilot-tracking/research/subagents/2026-03-27/foundry-project-without-model-research.md (Lines 140-160) - Cost analysis for judge model

Dependencies:
* Step 1.1 — Foundry resource must exist

### Step 1.3: Wire Foundry modules into infra/core/main.bicep

Add the Foundry resource and judge model deployment to the core infrastructure entrypoint. Follow the existing pattern: declare module → wire outputs to downstream consumers.

Add after the existing APIM module block:

```bicep
// Azure AI Foundry (GenAIOps — evaluation, monitoring, tracing)
module foundry '../modules/ai/foundry.bicep' = {
  name: 'foundry'
  params: {
    name: 'ai-${baseName}-${environment}'
    projectName: '${baseName}-genaiops'
    location: location
    tags: union(tags, { Component: 'AI' })
    appInsightsResourceId: appInsights.outputs.appInsightsId
  }
}

module foundryJudgeModel '../modules/ai/foundry-model-deployment.bicep' = {
  name: 'foundry-judge-model'
  params: {
    foundryResourceName: foundry.outputs.foundryResourceName
    deploymentName: 'gpt-4.1-mini'
  }
  dependsOn: [foundry]
}
```

Also add outputs for the Foundry project endpoint to flow to app deployments.

Note: The App Insights module (`infra/modules/monitoring/app-insights.bicep`) may need a new output for `appInsightsId` (resource ID) if it currently only outputs `appInsightsName`. Verify and add the output if needed.

Files:
* infra/core/main.bicep - Modify (add Foundry module references)
* infra/modules/monitoring/app-insights.bicep - Possibly modify (add resource ID output)

Success criteria:
* `az deployment group validate` passes with dev parameters
* `az deployment group what-if` shows Foundry resource and project creation
* No impact to existing resources in what-if output

Context references:
* infra/core/main.bicep (Lines 60-90) - Existing module invocation pattern
* .copilot-tracking/research/subagents/2026-03-27/foundry-integration-codebase-conventions.md (Lines 20-50) - Module inventory

Dependencies:
* Steps 1.1 and 1.2 — Modules must exist

### Step 1.4: Add RBAC role assignments for managed identity on Foundry resource

Grant the existing User-Assigned Identity (UAI) and the GitHub Actions OIDC service principal appropriate roles on the Foundry resource. Without these role assignments, `DefaultAzureCredential` calls from the evaluation SDK and the Chat.Api container will fail with 403 Forbidden.

Required role assignments:
1. **UAI (Container App identity)** → `Cognitive Services User` on the Foundry resource — enables Chat.Api to send traces correlated with Foundry and for future SDK calls
2. **GitHub Actions OIDC identity** → `Cognitive Services Contributor` on the Foundry resource — enables evaluation workflow to create evaluations, upload datasets, and read results

Add role assignment resources in `infra/core/main.bicep` after the Foundry module, following the existing pattern for role grants on Key Vault and App Config:

```bicep
resource foundryRoleAssignmentUai 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: foundry
  name: guid(foundry.outputs.foundryResourceName, uai.outputs.identityPrincipalId, 'CognitiveServicesUser')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908') // Cognitive Services User
    principalId: uai.outputs.identityPrincipalId
    principalType: 'ServicePrincipal'
  }
}
```

Note: The exact role definition ID for `Cognitive Services User` is `a97b65f3-24c7-4388-baec-2e87135dc908`. For `Cognitive Services Contributor` it is `25fbc0a9-bd7c-42a3-aa1a-3b75d497ee68`. Verify these at implementation time.

Files:
* infra/core/main.bicep - Modify (add role assignment resources)

Discrepancy references:
* DR-12 (RBAC role assignments missing from original plan)

Success criteria:
* UAI has Cognitive Services User role on Foundry resource
* GitHub OIDC identity has Cognitive Services Contributor role on Foundry resource
* `az deployment group what-if` shows role assignments in preview

Context references:
* infra/core/main.bicep - Existing role assignment pattern for Key Vault and App Config
* infra/modules/identity/user-assigned-identity.bicep - UAI principal ID output

Dependencies:
* Step 1.3 — Foundry module wired into main.bicep

### Step 1.5: Add Foundry project endpoint to App Configuration

Add the Foundry project endpoint to Azure App Configuration under the `Biotrackr:` prefix so Chat.Api can reference it for evaluation and tracing.

New App Config keys:
* `Biotrackr:FoundryProjectEndpoint` — the Foundry project endpoint URL (non-secret, from Bicep output)
* `Biotrackr:FoundryJudgeDeploymentName` — the judge model deployment name (`gpt-4.1-mini`, non-secret)

Update `Settings.cs` to add:
```csharp
public string FoundryProjectEndpoint { get; set; }
public string FoundryJudgeDeploymentName { get; set; } = "gpt-4.1-mini";
```

Wire the Foundry project endpoint from the Bicep deployment output to App Configuration, following the existing pattern where `infra/apps/chat-api/main.bicep` reads from App Config.

Files:
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Configuration/Settings.cs - Modify (add Foundry settings)
* infra/apps/chat-api/main.bicep - Modify (pass Foundry endpoint as env var if needed)

Success criteria:
* Settings class includes Foundry properties
* App Configuration contains the new key-value pairs after deployment

Context references:
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Configuration/Settings.cs (Lines 1-15) - Existing configuration pattern
* .copilot-tracking/research/subagents/2026-03-27/foundry-integration-codebase-conventions.md (Lines 210-240) - App Config flow

Dependencies:
* Step 1.3 — Foundry module wired with outputs

## Implementation Phase 2: OpenTelemetry gen_ai Semantic Conventions

<!-- parallelizable: true -->

### Step 2.1: Create AnthropicTelemetry instrumentation class

Create a dedicated telemetry class that wraps Anthropic API calls with OpenTelemetry spans following the gen_ai semantic conventions documented at https://opentelemetry.io/docs/specs/semconv/gen-ai/anthropic/.

The class provides a method that creates properly attributed `Activity` spans before and after each Claude API call. This replaces the need for auto-instrumentation (which only works for OpenAI SDK calls).

```csharp
// src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Telemetry/AnthropicTelemetry.cs
using System.Diagnostics;

namespace Biotrackr.Chat.Api.Telemetry;

public static class AnthropicTelemetry
{
    public static readonly ActivitySource Source = new("gen_ai.anthropic");

    /// <summary>
    /// Starts an Activity for a Claude chat operation following OpenTelemetry GenAI semantic conventions.
    /// Caller disposes the returned Activity after the API call completes and sets response attributes.
    /// </summary>
    public static Activity? StartChatActivity(string model, string? agentId = null)
    {
        var activity = Source.StartActivity($"chat {model}", ActivityKind.Client);
        if (activity is null) return null;

        activity.SetTag("gen_ai.operation.name", "chat");
        activity.SetTag("gen_ai.provider.name", "anthropic");
        activity.SetTag("gen_ai.request.model", model);
        activity.SetTag("server.address", "api.anthropic.com");
        activity.SetTag("server.port", 443);

        if (agentId is not null)
        {
            activity.SetTag("gen_ai.agents.id", agentId);
        }

        return activity;
    }

    /// <summary>
    /// Records response attributes on the Activity after a Claude API call completes.
    /// Token computation follows Anthropic convention: input_tokens excludes cached tokens.
    /// Total input = input_tokens + cache_read_input_tokens + cache_creation_input_tokens
    /// </summary>
    public static void RecordResponse(
        Activity? activity,
        string? responseModel,
        string? responseId,
        string? finishReason,
        int inputTokens,
        int outputTokens,
        int cacheReadInputTokens = 0,
        int cacheCreationInputTokens = 0)
    {
        if (activity is null) return;

        activity.SetTag("gen_ai.response.model", responseModel);
        activity.SetTag("gen_ai.response.id", responseId);
        activity.SetTag("gen_ai.usage.input_tokens", inputTokens + cacheReadInputTokens + cacheCreationInputTokens);
        activity.SetTag("gen_ai.usage.output_tokens", outputTokens);

        if (cacheReadInputTokens > 0)
            activity.SetTag("gen_ai.usage.cache_read.input_tokens", cacheReadInputTokens);
        if (cacheCreationInputTokens > 0)
            activity.SetTag("gen_ai.usage.cache_creation.input_tokens", cacheCreationInputTokens);

        if (finishReason is not null)
            activity.SetTag("gen_ai.response.finish_reasons", new[] { finishReason });
    }

    /// <summary>
    /// Records an error on the Activity.
    /// </summary>
    public static void RecordError(Activity? activity, Exception ex)
    {
        if (activity is null) return;
        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity.SetTag("error.type", ex.GetType().Name);
    }
}
```

Key design decisions:
* Static class — no DI needed, follows `System.Diagnostics` patterns
* `ActivitySource` name `gen_ai.anthropic` follows OpenTelemetry GenAI naming convention
* Span name `chat {model}` follows the convention `{gen_ai.operation.name} {gen_ai.request.model}`
* Token computation follows Anthropic's convention where `input_tokens` in their API excludes cached tokens
* `gen_ai.agents.id` attribute enables Foundry custom agent trace correlation

Files:
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Telemetry/AnthropicTelemetry.cs - New file (create)

Discrepancy references:
* DR-03 (Anthropic SDK response object shape must be validated against actual Microsoft.Agents.AI.Anthropic types)

Success criteria:
* File compiles without errors
* `ActivitySource` name matches OpenTelemetry Anthropic spec
* Token computation correctly aggregates cached + non-cached input tokens

Context references:
* .copilot-tracking/research/subagents/2026-03-27/foundry-dotnet-integration-research.md (Lines 340-400) - OTel gen_ai semantic conventions for Anthropic
* src/Biotrackr.Mcp.Server/ - Existing ActivitySource usage pattern

Dependencies:
* None — standalone class

### Step 2.2: Integrate instrumentation into ChatAgentProvider and ReportReviewerService

Wrap the `AnthropicClient.AsAIAgent()` calls and agent invocations with telemetry spans. The exact integration point depends on where the Anthropic API is called during agent processing.

There are two integration approaches depending on the Microsoft Agent Framework's extensibility:

**Approach A (Preferred): Middleware/event hook on the agent**

If the Agent Framework provides an `OnLlmCall` event or middleware pipeline, register the telemetry there. This captures every LLM call during agent processing (including tool-use continuation calls).

**Approach B (Fallback): Wrap agent invocation**

If no per-LLM-call hook exists, wrap the entire agent invocation (the method that processes a user turn) with a parent span. This provides coarser granularity but still captures timing, token usage, and errors at the conversation-turn level.

Implementation for ChatAgentProvider.cs:
```csharp
// In the method that handles a user message turn:
using var activity = AnthropicTelemetry.StartChatActivity(
    model: _settings.ChatAgentModel,
    agentId: "BiotrackrChatAgent");
try
{
    // Existing agent invocation code
    var result = await chatAgent.ProcessAsync(message, cancellationToken);
    
    // Extract token usage from result if available
    // AnthropicTelemetry.RecordResponse(activity, ...);
    
    return result;
}
catch (Exception ex)
{
    AnthropicTelemetry.RecordError(activity, ex);
    throw;
}
```

Implementation for ReportReviewerService.cs:
```csharp
using var activity = AnthropicTelemetry.StartChatActivity(
    model: _settings.ChatAgentModel,
    agentId: "BiotrackrReportReviewer");
// ... same pattern
```

Note: The exact method names (`ProcessAsync`, token usage extraction) must be verified against the `Microsoft.Agents.AI.Anthropic` v1.0.0-rc4 API surface. The agent framework may expose usage metadata on the response object or via a separate telemetry API.

Files:
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Services/ChatAgentProvider.cs - Modify (add telemetry wrapping)
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Services/ReportReviewerService.cs - Modify (add telemetry wrapping)

Discrepancy references:
* DR-03 (Agent framework response object shape for token extraction)

Success criteria:
* Both agents emit gen_ai spans on every invocation
* Token usage (input/output) is captured when available from agent response
* Errors are recorded on the span with `error.type`

Context references:
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Services/ChatAgentProvider.cs - Current agent construction
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Services/ReportReviewerService.cs - Current reviewer construction

Dependencies:
* Step 2.1 — AnthropicTelemetry class must exist

### Step 2.3: Register gen_ai.anthropic ActivitySource with OpenTelemetry in Program.cs

Add the `gen_ai.anthropic` `ActivitySource` to the existing OpenTelemetry tracing configuration so spans are exported to Azure Monitor.

Modify the existing `.WithTracing()` call in `Program.cs`:

```csharp
// Existing:
.WithTracing(tracing =>
{
    tracing.SetResourceBuilder(resourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAzureMonitorTraceExporter(options => ...);
})

// Modified (add one line):
.WithTracing(tracing =>
{
    tracing.SetResourceBuilder(resourceBuilder)
        .AddSource("gen_ai.anthropic")  // <-- Add Anthropic GenAI spans
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAzureMonitorTraceExporter(options => ...);
})
```

This single-line addition ensures all `Activity` objects created by `AnthropicTelemetry.Source` are captured by the OpenTelemetry pipeline and exported to Application Insights.

Files:
* src/Biotrackr.Chat.Api/Program.cs - Modify (add `.AddSource("gen_ai.anthropic")`)

Success criteria:
* `gen_ai.anthropic` source is registered in tracing pipeline
* Anthropic spans appear in Application Insights traces after deployment

Context references:
* src/Biotrackr.Chat.Api/Program.cs - Current OTel setup (tracing, metrics, logging)
* .copilot-tracking/research/subagents/2026-03-27/foundry-integration-codebase-conventions.md (Lines 155-195) - Current OTel configuration

Dependencies:
* Step 2.1 — ActivitySource name must match

## Implementation Phase 3: Evaluation Pipeline

<!-- parallelizable: true -->

### Step 3.1: Add Azure.AI.Projects NuGet package to Chat.Api test project

Add the `Azure.AI.Projects` NuGet package to the Chat.Api integration/evaluation test project. This provides the `EvaluationClient` for dataset-based evaluation.

```xml
<PackageReference Include="Azure.AI.Projects" Version="1.1.0-beta.*" />
```

If no dedicated evaluation test project exists, create one:
* `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api.Evaluation.Tests/Biotrackr.Chat.Api.Evaluation.Tests.csproj`

This project is separate from unit tests (which run on every PR) because evaluation tests require:
1. A running Foundry project endpoint (external dependency)
2. Pre-computed evaluation datasets (JSONL)
3. Longer execution time (server-side evaluation jobs)

Files:
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api.Evaluation.Tests/Biotrackr.Chat.Api.Evaluation.Tests.csproj - New project (create)

Success criteria:
* NuGet package restores successfully
* `AIProjectClient` type is resolvable in the test project

Context references:
* .copilot-tracking/research/subagents/2026-03-27/foundry-dotnet-integration-research.md (Lines 205-260) - .NET SDK evaluation capabilities

Dependencies:
* None — standalone project creation

### Step 3.2: Create evaluation dataset JSONL files

Create JSONL files containing representative query/response pairs for both the Chat Agent and Report Reviewer scenarios. These datasets are pre-computed (not generated at test time) and represent golden-path test cases.

File location: `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api.Evaluation.Tests/Datasets/`

**chat-agent-eval.jsonl** — Health data queries and Claude's expected responses:
```json
{"query": "What was my average sleep last week?", "response": "Based on your Fitbit sleep data from March 20-26, your average sleep duration was 7 hours and 23 minutes. Your best night was Tuesday at 8h 12m, and shortest was Friday at 5h 45m.", "context": "{\"sleep_records\": [{\"date\": \"2026-03-20\", \"duration_minutes\": 415}, ...]}", "ground_truth": "Average sleep: 7h 23m. Range: 5h 45m to 8h 12m."}
{"query": "How many calories did I burn today from exercise?", "response": "According to your Fitbit activity data for today (March 27), you burned approximately 487 calories from exercise. This includes a 30-minute run (312 cal) and 15 minutes of cycling (175 cal).", "context": "{\"activity_records\": [{\"type\": \"Run\", \"calories\": 312}, {\"type\": \"Cycling\", \"calories\": 175}]}", "ground_truth": "Total exercise calories: 487. Run: 312 cal, Cycling: 175 cal."}
```

**report-reviewer-eval.jsonl** — Report quality and safety scenarios:
```json
{"query": "Review this weekly health report for accuracy", "response": "The report accurately reflects the source data. Sleep averages match the recorded values. Weight trend correctly shows a 0.7kg decrease. No hallucinated data points detected.", "context": "{\"report_content\": \"...\", \"source_data\": \"...\"}", "ground_truth": "Report is accurate. All data points match source."}
```

Note: Initial datasets should contain 20-50 test cases per file. Expand over time based on production trace sampling.

Files:
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api.Evaluation.Tests/Datasets/chat-agent-eval.jsonl - New file (create)
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api.Evaluation.Tests/Datasets/report-reviewer-eval.jsonl - New file (create)

Success criteria:
* Valid JSONL format (one JSON object per line)
* Each record contains `query`, `response`, `context`, and `ground_truth` fields
* At least 20 records per dataset covering diverse health data scenarios

Context references:
* .copilot-tracking/research/2026-03-27/foundry-genaiops-without-model-deployment-research.md (Lines 315-325) - JSONL format example

Dependencies:
* None — dataset authoring is manual

### Step 3.3: Build evaluation runner class

Create an evaluation runner that uses the .NET `AIProjectClient.GetEvaluationClient()` to run server-side evaluations against uploaded datasets.

The runner supports two evaluation modes:
1. **Safety-only** (Tier 1, no judge model) — violence, self-harm, sexual, hate/unfairness
2. **Full evaluation** (Tier 2, with judge) — safety + coherence, fluency, relevance, groundedness, task adherence

```csharp
// src/Biotrackr.Chat.Api/Biotrackr.Chat.Api.Evaluation.Tests/FoundryEvaluationRunner.cs
using Azure.AI.Projects;
using Azure.Identity;

namespace Biotrackr.Chat.Api.Evaluation.Tests;

public class FoundryEvaluationRunner
{
    private readonly AIProjectClient _projectClient;
    private readonly string _judgeDeploymentName;

    public FoundryEvaluationRunner(string foundryEndpoint, string judgeDeploymentName)
    {
        _projectClient = new AIProjectClient(new Uri(foundryEndpoint), new DefaultAzureCredential());
        _judgeDeploymentName = judgeDeploymentName;
    }

    public async Task<EvaluationResult> RunSafetyEvaluationAsync(string datasetPath, string datasetName, string version)
    {
        // Upload dataset
        var dataset = _projectClient.Datasets.UploadFile(
            name: datasetName,
            version: version,
            filePath: datasetPath,
            connectionName: "default");

        // Safety evaluators — no judge model required
        var testingCriteria = new object[]
        {
            new { type = "azure_ai_evaluator", evaluator_name = "builtin.violence",
                  data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" } },
            new { type = "azure_ai_evaluator", evaluator_name = "builtin.self_harm",
                  data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" } },
            new { type = "azure_ai_evaluator", evaluator_name = "builtin.sexual",
                  data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" } },
            new { type = "azure_ai_evaluator", evaluator_name = "builtin.hate_unfairness",
                  data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" } },
        };

        // Create and run evaluation
        // ... (exact API shape depends on SDK version)
        return await RunEvaluationAsync(dataset.Id, testingCriteria);
    }

    public async Task<EvaluationResult> RunFullEvaluationAsync(string datasetPath, string datasetName, string version)
    {
        var dataset = _projectClient.Datasets.UploadFile(
            name: datasetName, version: version,
            filePath: datasetPath, connectionName: "default");

        // Safety + quality evaluators (quality requires judge model)
        var testingCriteria = new object[]
        {
            // Safety (no judge)
            new { type = "azure_ai_evaluator", evaluator_name = "builtin.violence",
                  data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" } },
            new { type = "azure_ai_evaluator", evaluator_name = "builtin.self_harm",
                  data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" } },
            // Quality (with GPT judge)
            new { type = "azure_ai_evaluator", evaluator_name = "builtin.coherence",
                  initialization_parameters = new { deployment_name = _judgeDeploymentName },
                  data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" } },
            new { type = "azure_ai_evaluator", evaluator_name = "builtin.fluency",
                  initialization_parameters = new { deployment_name = _judgeDeploymentName },
                  data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" } },
            new { type = "azure_ai_evaluator", evaluator_name = "builtin.groundedness",
                  initialization_parameters = new { deployment_name = _judgeDeploymentName },
                  data_mapping = new { query = "{{item.query}}", response = "{{item.response}}", context = "{{item.context}}" } },
            new { type = "azure_ai_evaluator", evaluator_name = "builtin.task_adherence",
                  initialization_parameters = new { deployment_name = _judgeDeploymentName },
                  data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" } },
        };

        return await RunEvaluationAsync(dataset.Id, testingCriteria);
    }
}
```

Note: The exact `EvaluationClient` API surface, `EvaluationResult` type, and `RunEvaluationAsync` implementation depend on the `Azure.AI.Projects` NuGet package version at implementation time. The SDK is in prerelease and the API may have changed. Refer to the GitHub README samples for the latest patterns.

Files:
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api.Evaluation.Tests/FoundryEvaluationRunner.cs - New file (create)
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api.Evaluation.Tests/EvaluationTests.cs - New file (create, test methods calling the runner)

Discrepancy references:
* DR-03 (SDK API surface in prerelease — exact types and methods may differ)
* DD-01 (Tier 2 approach: includes quality evaluators with judge model)

Success criteria:
* Evaluation runner compiles
* Safety evaluation runs without specifying a judge model deployment
* Full evaluation runs using gpt-4.1-mini as judge
* Results are viewable in Foundry portal after run

Context references:
* .copilot-tracking/research/subagents/2026-03-27/foundry-prompt-mgmt-eval-dataset-research.md (Lines 80-140) - .NET dataset evaluation code samples
* .copilot-tracking/research/subagents/2026-03-27/foundry-dotnet-integration-research.md (Lines 230-290) - .NET EvaluationClient capabilities

Dependencies:
* Step 3.1 — NuGet package must be installed
* Step 3.2 — Dataset files must exist

### Step 3.4: Create GitHub Actions evaluation workflow

Create `evaluation.yml` workflow following the existing template-driven CI/CD pattern. The evaluation workflow runs:
1. Safety evaluations on every PR (fast, no judge model cost)
2. Full evaluations on merge to main (includes quality evaluators with judge model)

```yaml
# .github/workflows/evaluation.yml
name: Evaluate AI Agents
on:
  pull_request:
    paths:
      - 'src/Biotrackr.Chat.Api/**'
      - 'scripts/chat-system-prompt/**'
      - 'scripts/reporting-api-prompts/**'
  push:
    branches: [main]
    paths:
      - 'src/Biotrackr.Chat.Api/**'
      - 'scripts/chat-system-prompt/**'
      - 'scripts/reporting-api-prompts/**'

jobs:
  safety-evaluation:
    runs-on: ubuntu-latest
    environment: development
    permissions:
      id-token: write
      contents: read
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Azure Login
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - name: Run Safety Evaluation
        run: dotnet test src/Biotrackr.Chat.Api/Biotrackr.Chat.Api.Evaluation.Tests --filter "Category=Safety"
        env:
          FOUNDRY_PROJECT_ENDPOINT: ${{ secrets.FOUNDRY_PROJECT_ENDPOINT }}

  quality-evaluation:
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    environment: development
    permissions:
      id-token: write
      contents: read
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Azure Login
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - name: Run Full Evaluation
        run: dotnet test src/Biotrackr.Chat.Api/Biotrackr.Chat.Api.Evaluation.Tests --filter "Category=Quality"
        env:
          FOUNDRY_PROJECT_ENDPOINT: ${{ secrets.FOUNDRY_PROJECT_ENDPOINT }}
          FOUNDRY_JUDGE_DEPLOYMENT_NAME: gpt-4.1-mini
```

Note: Requires `FOUNDRY_PROJECT_ENDPOINT` as a GitHub secret. OIDC federated identity auth (existing pattern) handles Azure auth for the evaluation SDK's `DefaultAzureCredential`.

Files:
* .github/workflows/evaluation.yml - New file (create)

Success criteria:
* Workflow triggers on Chat.Api source changes and system prompt changes
* Safety evaluation runs on PRs (fast, no judge cost)
* Quality evaluation runs only on merge to main (includes judge model cost)
* Uses existing OIDC auth pattern with federated identity

Context references:
* .github/workflows/deploy-chat-api.yml - Existing CI/CD pipeline pattern
* .copilot-tracking/research/subagents/2026-03-27/foundry-integration-codebase-conventions.md (Lines 200-240) - Workflow conventions

Dependencies:
* Steps 3.1-3.3 — Test project and runner must exist

## Implementation Phase 4: Custom Agent Registration and Monitoring

<!-- parallelizable: false -->

### Step 4.1: Deploy Foundry infrastructure to dev environment

Run the Bicep deployment to create the Foundry resource, project, and judge model deployment in the dev environment.

```bash
az deployment group create \
  --resource-group rg-biotrackr-dev \
  --template-file infra/core/main.bicep \
  --parameters infra/core/main.dev.bicepparam
```

Post-deployment verification:
1. Foundry resource `ai-biotrackr-dev` exists in Azure portal
2. Foundry project `biotrackr-genaiops` accessible in Foundry portal
3. GPT-4.1-mini deployment `gpt-4.1-mini` visible in project's Models + endpoints
4. Application Insights connected to Foundry project (may require portal confirmation step)

Files:
* infra/core/main.bicep - Execute deployment (no modification)
* infra/core/main.dev.bicepparam - May need parameter additions for Foundry

Success criteria:
* Deployment completes without errors
* Foundry portal shows the project with connected Application Insights
* No existing resources are modified or deleted

Context references:
* .copilot-tracking/research/subagents/2026-03-27/foundry-project-without-model-research.md (Lines 80-110) - Foundry resource creation

Dependencies:
* Phase 1 complete — all Bicep modules exist and validated

### Step 4.2: Register Chat.Api as custom agent in Foundry Control Plane

Portal-only step. Navigate to Foundry portal and register the Chat.Api as a custom agent.

Steps:
1. Navigate to Foundry portal → Operate → Overview → Register agent
2. Fill in registration details:
   * Agent name: `BiotrackrChatAgent`
   * Agent URL: `https://apim-biotrackr-dev.azure-api.net/chat` (through existing APIM, or direct Container App URL)
   * Protocol: HTTP
   * Project: `biotrackr-genaiops`
   * Description: "Claude Sonnet 4.6 powered health data chat agent using Microsoft Agent Framework + AGUI"
3. Note the generated proxy URL from Foundry's AI Gateway
4. Optionally register the Report Reviewer as a second agent (`BiotrackrReportReviewer`)

Note: This step requires the AI Gateway (API Management) to be configured in the Foundry resource. If the existing APIM instance (`apim-biotrackr-dev`) can serve as the AI Gateway, no additional APIM is needed. Otherwise, Foundry provisions its own APIM, which has cost implications.

Files:
* None — portal-only operation

Success criteria:
* Chat.Api appears in Foundry Control Plane agent list
* Foundry generates a proxy URL for the agent
* Agent status shows as "Active"

Context references:
* .copilot-tracking/research/subagents/2026-03-27/foundry-prompt-mgmt-eval-dataset-research.md (Lines 170-210) - Custom agent registration workflow

Dependencies:
* Step 4.1 — Foundry project deployed
* Phase 2 — Chat.Api emitting gen_ai spans

### Step 4.3: Configure continuous evaluation rules on monitoring dashboard

Configure continuous evaluation on the registered custom agent in Foundry's monitoring dashboard. This enables automatic quality and safety scoring on a sample of production requests.

Steps:
1. Navigate to Foundry Control Plane → Agent page for `BiotrackrChatAgent` → Monitor tab
2. Open Settings → Continuous evaluation
3. Add evaluators:
   * Safety evaluators (no judge cost): violence, self_harm, sexual, hate_unfairness
   * Quality evaluators (uses judge model): fluency, task_adherence
4. Set sample rate: 10% of requests (adjust based on volume and cost)
5. Configure alerts (preview): latency threshold (e.g., >5s P95), evaluation score threshold (e.g., fluency <3)

If available programmatically via the .NET SDK's `EvaluationRules`:
```csharp
var evalClient = projectClient.GetEvaluationClient();
var rule = new EvaluationRule(
    action: new EvaluationRuleAction(...),
    eventType: EvaluationRuleEventType.ResponseCompleted,
    enabled: true)
{
    Filter = new EvaluationRuleFilter(agentName: "BiotrackrChatAgent"),
};
```

Note: Continuous evaluation depends on the agent emitting OpenTelemetry traces with `gen_ai.agents.id = "BiotrackrChatAgent"` to the same Application Insights instance. This is handled by Phase 2.

Files:
* None — portal or SDK configuration

Success criteria:
* Continuous evaluation is enabled for the registered agent
* Safety evaluators run without judge model
* Quality evaluators run using gpt-4.1-mini judge
* Sample rate is set to 10%

Context references:
* .copilot-tracking/research/subagents/2026-03-27/foundry-prompt-mgmt-eval-dataset-research.md (Lines 290-310) - Continuous evaluation with custom agents

Dependencies:
* Step 4.2 — Agent registered
* Phase 2 — gen_ai spans emitting with correct agent ID

## Implementation Phase 5: Decision Record and Documentation

<!-- parallelizable: true -->

### Step 5.1: Create ADR for Foundry GenAIOps adoption strategy

Create a decision record documenting the Tier 2 adoption approach, alternatives considered, and rationale. Follow the existing decision record format in `docs/decision-records/`.

File: `docs/decision-records/2026-03-27-foundry-genaiops-adoption.md`

Content outline:
* Context: Claude Sonnet 4.6 via Anthropic API, no Foundry model catalog access, need for evaluation/monitoring/tracing
* Decision: Tier 2 — Foundry project with GPT-4.1-mini judge model for evaluation, custom agent registration for monitoring, OpenTelemetry gen_ai conventions for tracing
* Alternatives considered:
  * Tier 1 (zero model) — safety + NLP evaluators only
  * OpenAI API as judge — less secure, adds external dependency
  * Claude as judge — reduces evaluation independence
  * No Foundry (App Insights + custom workbooks only) — misses continuous evaluation and monitoring dashboard
* Consequences: ~$2-5/month incremental cost, net-new CognitiveServices resource, ongoing GPT judge usage for evaluation
* References: Research document path

Files:
* docs/decision-records/2026-03-27-foundry-genaiops-adoption.md - New file (create)

Success criteria:
* Decision record follows existing format in docs/decision-records/decision-record-template.md
* All four alternatives are documented with trade-offs

Context references:
* docs/decision-records/decision-record-template.md - Template format
* .copilot-tracking/research/2026-03-27/foundry-genaiops-without-model-deployment-research.md (Lines 275-340) - Alternatives analysis

Dependencies:
* None — documentation task

### Step 5.2: Update infrastructure documentation

Update or create documentation describing the Foundry infrastructure components, their purpose, and the evaluation pipeline.

If `docs/` contains an infrastructure overview document, update it. Otherwise create a brief overview at `docs/foundry-genaiops-setup.md` covering:
* Foundry resource and project purpose (GenAIOps only, no production inference)
* GPT-4.1-mini judge model purpose (evaluation scoring)
* Custom agent registration workflow
* Evaluation pipeline (JSONL datasets → safety + quality evaluators → Foundry portal results)
* OpenTelemetry gen_ai semantic conventions for Anthropic
* Cost expectations

Files:
* docs/foundry-genaiops-setup.md - New file (create)

Success criteria:
* Document explains the Foundry-without-production-model architecture
* Links to Foundry portal for agent monitoring and evaluation results

Context references:
* docs/bicep-modules-structure.md - Existing infrastructure documentation style

Dependencies:
* None — documentation task

## Implementation Phase 6: Validation

<!-- parallelizable: false -->

### Step 6.1: Run full project validation

Execute all validation commands:
* `dotnet build` across Chat.Api and new evaluation test project
* `dotnet test` for Chat.Api with 70% coverage gate
* `az bicep lint` on all modified and new Bicep modules
* `az deployment group validate` with dev parameters
* GitHub Actions workflow syntax validation

### Step 6.2: Fix minor validation issues

Iterate on lint errors, build warnings, and test failures from Step 6.1. Apply fixes directly when corrections are straightforward and isolated.

### Step 6.3: Report blocking issues

When validation failures require changes beyond minor fixes:
* Document the issues and affected files
* Provide the user with next steps
* Recommend additional research and planning rather than inline fixes
* Avoid large-scale refactoring within this phase

## Dependencies

* `Azure.AI.Projects` NuGet package (latest prerelease) — .NET evaluation SDK
* Azure subscription with Cognitive Services / AI Services resource creation permissions
* Azure OpenAI GPT-4.1-mini quota in australiaeast (or nearest supported region)
* Foundry portal access for custom agent registration (portal-only step)
* Existing OIDC federated identity for GitHub Actions Azure auth

## Success Criteria

* Foundry resource and project deploy via Bicep — zero production model deployments
* Chat.Api emits gen_ai.* OpenTelemetry spans visible in Application Insights
* Safety evaluation runs on JSONL dataset without judge model
* Quality evaluation runs on JSONL dataset with GPT-4.1-mini judge
* Chat.Api appears in Foundry monitoring dashboard with latency/token/error metrics
* GitHub Actions evaluation workflow triggers on Chat.Api changes
* Decision record documents the adoption approach
