# Azure AI Foundry .NET Integration Research

## Research Topics

1. Custom agent registration in Foundry for external models
2. Local tracing with the SDK
3. Production tracing setup
4. .NET Azure.AI.Projects SDK capabilities
5. Evaluation SDK - model deployment requirements
6. Region support for Foundry features
7. OpenTelemetry gen_ai.* semantic conventions for Anthropic Claude in .NET
8. Azure.AI.Projects NuGet package evaluation capabilities (.NET vs Python)
9. Foundry pricing model without model deployment

---

## 1. Custom Agent Registration in Foundry

### Status: 404 - Documentation Not Found

The URL `https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/monitor/custom-agents` returns HTTP 404. This documentation page does not exist under the new Foundry portal documentation.

### Alternative: Agent Registration via Azure.AI.Projects .NET SDK

The latest Azure.AI.Projects SDK (GitHub main branch, updated March 2026) provides a new `Agents` property on `AIProjectClient` for creating/managing agents:

```csharp
DeclarativeAgentDefinition agentDefinition = new(model: modelDeploymentName)
{
    Instructions = "You are a prompt agent."
};
ProjectsAgentVersion agentVersion = projectClient.Agents.CreateAgentVersion(
    agentName: "myAgent1",
    options: new(agentDefinition));
```

**Key operations available:**
- `CreateAgentVersion` / `CreateAgentVersionAsync`
- `GetAgent` / `GetAgentAsync`
- `GetAgents` / `GetAgentsAsync`
- `DeleteAgentVersion` / `DeleteAgentVersionAsync`

**Requirements:**
- Requires a Foundry project endpoint (`FOUNDRY_PROJECT_ENDPOINT`)
- `model` parameter references a deployment name in the Foundry project ("Models + endpoints" tab)
- Uses `DefaultAzureCredential` for authentication
- Agents are versioned (name + version)

**Key Limitation for External Models:** The agent registration requires a `modelDeploymentName` that references a model deployed within the Foundry project. There is no documented mechanism to register an agent that uses an external model (e.g., Anthropic Claude called directly via its own API) without deploying that model through Foundry first. The Classic Agent path (`GetPersistentAgentsClient`) also requires `model: modelDeploymentName`.

### Finding: Custom Agent Registration with External Models

There is no documented path to register a "custom agent" in Foundry that uses an external model provider (like direct Anthropic API calls) without having a model deployment in the Foundry project. The agent registration APIs require a model deployment name. However, tracing and evaluation can work independently of agent registration (see sections below).

---

## 2. Local Tracing with SDK

### Source: `https://learn.microsoft.com/en-us/azure/foundry-classic/how-to/develop/trace-application`

Note: This documentation is marked as **"Foundry (classic)"** and may not apply to the new Foundry portal. The page is focused on **Python** SDK instrumentation.

### Python-Focused Local Tracing

Packages: `azure-ai-projects`, `azure-monitor-opentelemetry`, `opentelemetry-instrumentation-openai-v2`

Setup:

```python
from azure.ai.projects import AIProjectClient
from azure.monitor.opentelemetry import configure_azure_monitor
from opentelemetry.instrumentation.openai_v2 import OpenAIInstrumentor

project_client = AIProjectClient(credential=DefaultAzureCredential(), endpoint="...")
connection_string = project_client.telemetry.get_application_insights_connection_string()
configure_azure_monitor(connection_string=connection_string)
OpenAIInstrumentor().instrument()
```

**Console Tracing (Python):**

```python
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import SimpleSpanProcessor, ConsoleSpanExporter
tracer_provider = TracerProvider()
tracer_provider.add_span_processor(SimpleSpanProcessor(ConsoleSpanExporter()))
trace.set_tracer_provider(tracer_provider)
```

### .NET Local Tracing (from GitHub README, latest)

The .NET SDK now supports tracing natively:

**Enable GenAI Tracing:**

```csharp
AppContext.SetSwitch("Azure.Experimental.EnableGenAITracing", true);
// OR set environment variable: AZURE_EXPERIMENTAL_ENABLE_GENAI_TRACING=true
```

**Console Tracing (.NET):**

```csharp
dotnet add package OpenTelemetry.Exporter.Console

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("Azure.AI.Projects.*")
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AgentTracingSample"))
    .AddConsoleExporter()
    .Build();
```

**Azure Monitor Tracing (.NET):**

```csharp
dotnet add package Azure.Monitor.OpenTelemetry.Exporter
// OR
dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("Azure.AI.Projects.*")
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AgentTracingSample"))
    .AddAzureMonitorTraceExporter()
    .Build();
```

**Content Recording:**

```csharp
AppContext.SetSwitch("Azure.Experimental.TraceGenAIMessageContent", true);
// OR set: OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT=true
```

### Key Finding: .NET Tracing is Available

The .NET SDK has native tracing support through OpenTelemetry. However, the trace source name is `Azure.AI.Projects.*`, meaning it instruments operations done through the Azure.AI.Projects SDK. For external model calls (direct Anthropic API), you would need to emit custom gen_ai spans manually.

---

## 3. Production Tracing Setup

### Source: `https://learn.microsoft.com/en-us/azure/foundry-classic/how-to/develop/trace-production-sdk`

This page is **Foundry (classic)** legacy documentation for hub-based projects. It covers PromptFlow deployment tracing, not modern Foundry project tracing.

### Key Points:

- Enable tracing in deployment YAML: `app_insights_enabled: true`
- Or use environment variable: `APPLICATIONINSIGHTS_CONNECTION_STRING: <connection_string>`
- Traces follow OpenTelemetry specification
- System metrics collected: `token_consumption`, `flow_latency`, `flow_request`, `node_latency`, `node_request`, `rpc_latency`, `rpc_request`
- Supports custom OTLP collector export via `OTEL_EXPORTER_OTLP_ENDPOINT`

### Modern .NET Production Tracing

Based on the .NET SDK documentation, production tracing uses the same pattern as local tracing but exports to Azure Monitor:

```csharp
// Set environment variable:
// APPLICATIONINSIGHTS_CONNECTION_STRING=<your-connection-string>

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("Azure.AI.Projects.*")
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyService"))
    .AddAzureMonitorTraceExporter()
    .Build();
```

Prerequisites:
1. Associate Application Insights resource with Foundry project
2. Assign Log Analytics Reader role for Application Insights resource
3. Contributor access to Foundry resource to connect Application Insights

---

## 4. .NET Azure.AI.Projects SDK Capabilities

### Source: `https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.projects-readme` and GitHub README (latest)

**Package:** `Azure.AI.Projects` v1.1.0 (stable) + prerelease versions
**NuGet:** `dotnet add package Azure.AI.Projects --prerelease`
**Target Frameworks:** .NET 8.0, .NET Standard 2.0

### Full Feature Set (from latest GitHub README):

| Feature | Status | Notes |
|---|---|---|
| Classic Agents (PersistentAgentsClient) | GA | Via `GetPersistentAgentsClient()` |
| Agents (new API) | Available | Via `projectClient.Agents` property |
| Deployments | GA | List/get model deployments |
| Connections | GA | Enumerate connected Azure resources |
| Datasets | GA | Upload files/folders, CRUD operations |
| Indexes | GA | AI Search index management |
| Files | Available | OpenAI Files API |
| Fine-Tuning | Available | Supervised fine-tuning jobs |
| Memory Stores | Experimental | Long-term agent memory (AAIP001 warning) |
| **Evaluations** | **Available** | **Full evaluation support in .NET** |
| **Tracing** | **Preview** | **OpenTelemetry-based, Azure Monitor + Console** |
| **Red Teams** | **Experimental** | **Security testing (AAIP001 warning)** |

### Client Initialization:

```csharp
var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT");
AIProjectClient projectClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
```

Note: Hub-based project support discontinued. Use Foundry project endpoint.

---

## 5. Evaluation SDK - Model Deployment Requirements

### Python Evaluation SDK: `azure-ai-evaluation`

Source: `https://learn.microsoft.com/en-us/azure/foundry-classic/how-to/develop/evaluate-sdk`

**Key Requirement:** AI-assisted quality evaluators (except `GroundednessProEvaluator`) require a **GPT model deployment** (`gpt-35-turbo`, `gpt-4`, `gpt-4o`, `gpt-4o-mini`) via `model_config` (AzureOpenAIModelConfiguration). The GPT model acts as a **judge** to score evaluation data.

**Safety evaluators** and `GroundednessProEvaluator` require `azure_ai_project` info (accesses backend evaluation service).

**NLP evaluators** (F1, BLEU, ROUGE, GLEU, METEOR, SimilarityEvaluator) do NOT require model deployment - they use mathematical metrics.

### .NET Evaluation SDK (Azure.AI.Projects)

**MAJOR FINDING: .NET now has full evaluation capabilities.**

The latest Azure.AI.Projects .NET SDK includes comprehensive evaluation support via `EvaluationClient`:

#### Available Evaluation Scenarios:

1. **Agent Evaluation** - Evaluate agents with built-in evaluators (violence_detection, fluency, task_adherence)
2. **Model Evaluation** - Evaluate model deployments directly
3. **Uploaded Dataset Evaluation** - Use uploaded JSONL datasets
4. **Custom Prompt-Based Evaluators** - Define custom evaluation prompts
5. **Custom Code-Based Evaluators** - Define Python code-based evaluation logic
6. **Evaluation with Application Insights Traces** - Evaluate from trace data using Kusto queries
7. **Evaluating Responses** - Evaluate OpenAI response items
8. **Evaluation Rules** - Continuous evaluation triggered by events (e.g., ResponseCompleted)

#### Built-in Evaluators Available:

- `builtin.violence` - Violence detection
- `builtin.fluency` - Fluency evaluation
- `builtin.task_adherence` - Task adherence evaluation
- Others referenced but not exhaustively listed in SDK samples

#### Evaluation Requirements in .NET:

- Requires `EvaluationClient` from `AIProjectClient`
- Built-in evaluators that need an LLM judge require `initialization_parameters` with `deployment_name` (references a deployed model in Foundry)
- Safety evaluators (violence_detection) may NOT require a deployment_name
- Agent evaluation requires the agent to be registered in Foundry (name + version)
- Model evaluation requires the model to be deployed in Foundry
- Trace-based evaluation requires Application Insights connection + Log Analytics Reader role

#### Key Implication for External Models:

For evaluating responses from an external model (Anthropic Claude):
- **NLP evaluators** would work without model deployment
- **AI-assisted evaluators** (fluency, task_adherence, groundedness) need a GPT-family deployment as the judge model
- You can evaluate pre-collected data from uploaded datasets without needing the target model deployed
- Trace-based evaluation from Application Insights would work if traces are emitted with correct gen_ai attributes

---

## 6. Region Support for Foundry Features

### Source: `https://learn.microsoft.com/en-us/azure/foundry/reference/region-support`

### Foundry Project Regions:

Australia East, Brazil South, Canada Central, Canada East, Central India, Central US, East Asia, East US, East US 2, France Central, Germany West Central, Italy North, Japan East, Korea Central, North Central US, North Europe, Norway East, Qatar Central, South Africa North, South Central US, South India, Southeast Asia, Spain Central, Sweden Central, Switzerland North, UAE North, UK South, West Europe, West US, West US 3

### Feature-Specific Availability:

- **Azure OpenAI:** Model availability varies by region. Check quotas/limits.
- **Speech:** Hardware-dependent regional availability.
- **Content Safety:** Region-specific.
- **Agent Service:** Azure OpenAI model + tool availability varies by region.

### Verification Process:

1. Select candidate region from Foundry projects list
2. Verify model/quota availability
3. Verify feature-specific support
4. Confirm via Azure global infrastructure products page

### Key Consideration for Biotrackr:

Since Biotrackr uses Anthropic Claude directly (not through Foundry model deployments), the region choice for a Foundry project is less constrained by model availability. The main considerations would be:
- Proximity to existing Azure resources (Application Insights, Cosmos DB)
- Availability of evaluation features
- Agent Service availability if registering agents

---

## 7. OpenTelemetry gen_ai.* Semantic Conventions for Anthropic Claude

### Sources:
- `https://opentelemetry.io/docs/specs/semconv/gen-ai/gen-ai-spans/` (Semantic Conventions 1.40.0)
- `https://opentelemetry.io/docs/specs/semconv/gen-ai/anthropic/`
- `https://opentelemetry.io/docs/specs/semconv/gen-ai/gen-ai-metrics/`

### Status: Development (not yet stable)

### Anthropic-Specific Conventions

`gen_ai.provider.name` MUST be set to `"anthropic"` for Anthropic client operations.

### Required Span Attributes for Inference:

| Attribute | Requirement | Example |
|---|---|---|
| `gen_ai.operation.name` | Required | `"chat"` |
| `gen_ai.provider.name` | Required | `"anthropic"` |
| `gen_ai.request.model` | Conditionally Required | `"claude-sonnet-4-6"` |
| `error.type` | Conditionally Required (on error) | `"timeout"` |

### Recommended Span Attributes:

| Attribute | Example |
|---|---|
| `gen_ai.response.model` | `"claude-sonnet-4-6"` |
| `gen_ai.response.id` | `"msg_abc123"` |
| `gen_ai.response.finish_reasons` | `["end_turn"]` |
| `gen_ai.usage.input_tokens` | `100` |
| `gen_ai.usage.output_tokens` | `180` |
| `gen_ai.usage.cache_creation.input_tokens` | `25` |
| `gen_ai.usage.cache_read.input_tokens` | `50` |
| `gen_ai.request.max_tokens` | `4096` |
| `gen_ai.request.temperature` | `0.7` |
| `gen_ai.request.top_p` | `1.0` |
| `server.address` | `"api.anthropic.com"` |

### Anthropic-Specific Token Computation:

**IMPORTANT:** Anthropic `input_tokens` excludes cached tokens. The correct computation:

```
gen_ai.usage.input_tokens = input_tokens + cache_read_input_tokens + cache_creation_input_tokens
```

### Opt-In Attributes (sensitive data):

| Attribute | Description |
|---|---|
| `gen_ai.input.messages` | Chat history as structured JSON |
| `gen_ai.output.messages` | Model response messages |
| `gen_ai.system_instructions` | System prompt |
| `gen_ai.tool.definitions` | Available tool definitions |

### Span Naming Convention:

Span name: `{gen_ai.operation.name} {gen_ai.request.model}`
Example: `"chat claude-sonnet-4-6"`
Span kind: `CLIENT`

### Well-Known Operation Names:

`chat`, `create_agent`, `embeddings`, `execute_tool`, `generate_content`, `invoke_agent`, `invoke_workflow`, `retrieval`, `text_completion`

### Metrics (gen_ai.client.*):

| Metric | Type | Unit | Description |
|---|---|---|---|
| `gen_ai.client.token.usage` | Histogram | `{token}` | Input/output token usage |
| `gen_ai.client.operation.duration` | Histogram | `s` | Operation duration |

Required metric attributes: `gen_ai.operation.name`, `gen_ai.provider.name`, `gen_ai.token.type` (for token usage)

### How to Emit in .NET for Anthropic Claude:

Since there is no official OpenTelemetry Anthropic instrumentation for .NET, you must emit spans manually:

```csharp
using System.Diagnostics;

// Create an ActivitySource following gen_ai conventions
private static readonly ActivitySource GenAiSource = new("gen_ai.anthropic");

// Before making Claude API call:
using var activity = GenAiSource.StartActivity("chat claude-sonnet-4-6", ActivityKind.Client);
activity?.SetTag("gen_ai.operation.name", "chat");
activity?.SetTag("gen_ai.provider.name", "anthropic");
activity?.SetTag("gen_ai.request.model", "claude-sonnet-4-6");
activity?.SetTag("server.address", "api.anthropic.com");
activity?.SetTag("server.port", 443);
activity?.SetTag("gen_ai.request.max_tokens", 4096);
activity?.SetTag("gen_ai.request.temperature", 0.7);

// After receiving response:
activity?.SetTag("gen_ai.response.model", response.Model);
activity?.SetTag("gen_ai.response.id", response.Id);
activity?.SetTag("gen_ai.response.finish_reasons", new[] { response.StopReason });
activity?.SetTag("gen_ai.usage.input_tokens", response.Usage.InputTokens + response.Usage.CacheReadInputTokens + response.Usage.CacheCreationInputTokens);
activity?.SetTag("gen_ai.usage.output_tokens", response.Usage.OutputTokens);
activity?.SetTag("gen_ai.usage.cache_read.input_tokens", response.Usage.CacheReadInputTokens);
activity?.SetTag("gen_ai.usage.cache_creation.input_tokens", response.Usage.CacheCreationInputTokens);
```

Register this source with the tracer provider:

```csharp
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("gen_ai.anthropic")
    .AddSource("Azure.AI.Projects.*") // If also using Foundry SDK
    .AddAzureMonitorTraceExporter()
    .Build();
```

---

## 8. Azure.AI.Projects NuGet: Evaluation in .NET vs Python

### Finding: .NET NOW has Evaluation Support

**CRITICAL FINDING:** Contrary to earlier assumptions, the .NET Azure.AI.Projects SDK (latest prerelease on GitHub main branch) includes **full evaluation capabilities**.

### .NET Evaluation Features:

The `EvaluationClient` accessible via `AIProjectClient` supports:

- **Creating evaluations** with built-in and custom evaluators
- **Running evaluation runs** against agents, models, datasets, traces
- **Agent evaluation** - evaluate agent responses with quality and safety metrics
- **Model evaluation** - evaluate model deployments
- **Dataset evaluation** - evaluate uploaded JSONL datasets
- **Trace-based evaluation** - evaluate from Application Insights traces using Kusto queries
- **Response evaluation** - evaluate OpenAI response items
- **Custom prompt-based evaluators** - upload custom evaluation prompts
- **Custom code-based evaluators** - upload Python code-based evaluation logic
- **Evaluation rules** - continuous evaluation triggered by events (e.g., `ResponseCompleted`)
- **Red teaming** - security testing with attack strategies

### Python Evaluation Features (azure-ai-evaluation):

- All built-in evaluators (quality, safety, NLP, agentic, Azure OpenAI graders)
- Local evaluation on test datasets
- Local evaluation on a target (callable class)
- Evaluation results logging to Foundry project
- Single-turn and conversation support for text
- Image and multimodal evaluation

### Comparison:

| Feature | Python | .NET |
|---|---|---|
| Built-in evaluators (quality) | Yes (local SDK) | Yes (server-side via API) |
| Built-in evaluators (safety) | Yes (local SDK) | Yes (server-side via API) |
| NLP evaluators (F1, BLEU, etc.) | Yes (local SDK) | Via API/evaluator catalog |
| Custom prompt evaluators | Yes | Yes (via catalog upload) |
| Custom code evaluators | Yes | Yes (via catalog upload) |
| Agent evaluation | Yes | Yes |
| Model evaluation | Yes | Yes |
| Trace-based evaluation | Yes | Yes (Application Insights) |
| Evaluation rules | Not documented | Yes (continuous evaluation) |
| Red teaming | Yes | Yes (experimental) |
| Local-only evaluation (no Azure) | Yes (NLP metrics) | Not documented |
| `evaluate()` batch API | Yes | Via API calls |

### Key Difference:

- **Python** evaluators run locally (LLM judge calls happen from client)
- **.NET** evaluators run **server-side** via the Foundry API (evaluation is created and run as a job)
- Both require a GPT-family model deployment for AI-assisted quality evaluators

---

## 9. Foundry Pricing Without Model Deployment

### Sources:
- `https://azure.microsoft.com/en-us/pricing/details/ai-foundry/`
- `https://learn.microsoft.com/en-us/azure/foundry-classic/how-to/costs-plan-manage`

### Foundry Platform Components and Costs:

| Component | Pricing Category | Notes |
|---|---|---|
| **Foundry Models** | Pay-per-token or commitment | Only if deploying/using models through Foundry |
| **Agents Service** | Pay-per-use | Agent execution costs |
| **Knowledge and Tools** | Various | AI Search, Content Safety, etc. |
| **Observability and Trust** | See below | Tracing, evaluation costs |
| **Local and Edge** | Various | Self-hosted/local models |

### Costs WITHOUT Model Deployment:

If you use Foundry purely for observability (tracing, evaluation) with an external model (Anthropic Claude):

1. **Application Insights / Log Analytics** - Standard Azure Monitor pricing applies:
   - Data ingestion: ~$2.76/GB (pay-as-you-go)
   - Data retention: First 31 days free, then ~$0.10/GB/day
   - Queries: Free for interactive queries

2. **Storage Account** - Required for evaluation datasets:
   - Blob Storage: ~$0.018/GB/month (Hot tier)
   - Transactions

3. **Evaluation Service** - AI-assisted evaluators use GPT model tokens:
   - Requires a GPT deployment in Foundry for quality evaluators
   - GPT-4o-mini: Cheapest option (~$0.15/1M input tokens, ~$0.60/1M output tokens)
   - Each evaluation row consumes judge model tokens (`max_token` set to 800-3000)

4. **Key Vault** - Secrets storage: ~$0.03/10K operations

5. **Foundry Project Resource** - The Azure AI Services resource itself:
   - No base cost for the resource
   - Pay for consumed services

### Summary of Minimum Costs Without Model Deployment:

- **Foundry project creation: Free** (the project itself has no cost)
- **Tracing: Application Insights pricing** (data ingestion + retention)
- **Evaluation: Requires at least one GPT deployment** for AI-assisted evaluators
- **NLP evaluators (F1, BLEU, etc.):** May work without deployment (Python only, run locally)
- **Safety evaluators:** Requires Foundry project (uses backend service)

### Billing Models:

1. **Serverless API (pay-per-use):** Billed per token consumed
2. **Commitment Tiers:** Fixed fee for committed usage
3. **Azure Marketplace:** Third-party model providers bill separately

---

## Key Discoveries Summary

### Critical Findings:

1. **Evaluation IS available in .NET** - The Azure.AI.Projects SDK now includes full evaluation support (agent, model, dataset, trace-based, custom evaluators, evaluation rules, red teaming). This is a recent addition not reflected in the stable docs.

2. **Tracing IS available in .NET** - The SDK supports OpenTelemetry-based tracing to Azure Monitor and Console, with content recording options.

3. **External model challenge** - Foundry's agent registration and evaluation features are designed around models deployed within Foundry. Using an external model (direct Anthropic API) requires:
   - Manual OpenTelemetry span emission following gen_ai semantic conventions
   - A GPT deployment in Foundry for AI-assisted evaluation judges
   - Custom trace data to feed into Foundry's trace-based evaluation

4. **Anthropic has official OTel semantic conventions** - Provider name `"anthropic"`, with specific token computation rules for cached tokens.

5. **No .NET Anthropic OTel instrumentation library exists** - Must emit gen_ai spans manually in .NET code.

6. **Foundry project itself is free** - You pay for consumed services (Application Insights, model tokens for evaluation, storage).

7. **Custom agent monitoring page does not exist** - The `/monitor/custom-agents` URL returns 404. No documented path to register an external agent for monitoring.

### Follow-On Questions:

1. Could the Foundry evaluation trace-based approach work by sending custom gen_ai spans from ClaChat.Api to Application Insights, then evaluating those traces through the .NET evaluation client?
2. Is there a way to register a "bring-your-own-model" agent in Foundry that calls Anthropic's API directly?
3. What is the minimum GPT deployment size/SKU needed purely for evaluation judging?
4. Can evaluation rules trigger on custom trace events (not just agent ResponseCompleted)?

---

## References

- Azure.AI.Projects .NET SDK README (GitHub main branch): `https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/ai/Azure.AI.Projects`
- NuGet Azure.AI.Projects v1.1.0: `https://www.nuget.org/packages/Azure.AI.Projects`
- OpenTelemetry GenAI Semantic Conventions: `https://opentelemetry.io/docs/specs/semconv/gen-ai/`
- OpenTelemetry Anthropic Conventions: `https://opentelemetry.io/docs/specs/semconv/gen-ai/anthropic/`
- GenAI Spans Spec: `https://github.com/open-telemetry/semantic-conventions/blob/main/docs/gen-ai/gen-ai-spans.md`
- GenAI Metrics Spec: `https://opentelemetry.io/docs/specs/semconv/gen-ai/gen-ai-metrics/`
- Azure AI Evaluation SDK (Python): `https://learn.microsoft.com/en-us/azure/foundry-classic/how-to/develop/evaluate-sdk`
- Foundry Region Support: `https://learn.microsoft.com/en-us/azure/foundry/reference/region-support`
- Foundry Pricing: `https://azure.microsoft.com/en-us/pricing/details/ai-foundry/`
- Foundry Cost Management: `https://learn.microsoft.com/en-us/azure/foundry-classic/how-to/costs-plan-manage`
- Foundry Tracing (classic): `https://learn.microsoft.com/en-us/azure/foundry-classic/how-to/develop/trace-application`
- Foundry Production Tracing (classic): `https://learn.microsoft.com/en-us/azure/foundry-classic/how-to/develop/trace-production-sdk`
