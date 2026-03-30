<!-- markdownlint-disable-file -->
# Task Research: Azure AI Foundry GenAIOps Without Model Deployment

Can Biotrackr use Azure AI Foundry for prompt versioning, monitoring, evaluation, and tracing outlined in the [Operationalize GenAI Apps](https://learn.microsoft.com/en-us/training/paths/operationalize-gen-ai-apps/) learning path — without deploying a model within Foundry — given that the solution uses Claude Sonnet 4.6 via direct Anthropic API?

## Task Implementation Requests

* Determine which Foundry GenAIOps features work with external (non-Foundry-deployed) models
* Map each module in the learning path to Biotrackr's architecture
* Identify the minimum Azure resources required and their cost
* Provide a recommended adoption approach

## Scope and Success Criteria

* Scope: All 5 modules of the learning path evaluated against Biotrackr's Claude-via-Anthropic architecture. Covers prompt versioning, evaluation, monitoring, and tracing. Excludes Foundry Agent Service and model hosting.
* Assumptions:
  * Biotrackr continues using Claude Sonnet 4.6 via direct Anthropic API
  * The .NET Microsoft Agent Framework (v1.0.0-rc4) remains the orchestration layer
  * Existing Application Insights + OpenTelemetry instrumentation is already in place
  * No Anthropic models are available via Azure AI Foundry Model Catalog for this subscription
* Success Criteria:
  * Clear YES/NO/PARTIAL for each Foundry feature
  * Cost implications documented
  * Selected approach with implementation path

## Outline

1. Executive Summary — The Short Answer
2. Learning Path Module-by-Module Analysis
3. Feature Compatibility Matrix
4. Minimum Resource Requirements and Cost
5. Technical Scenarios (Selected Approach and Alternatives)
6. Implementation Recommendations
7. Key Discoveries
8. Potential Next Research

## Potential Next Research

* Verify Biotrackr's current OTel spans include `gen_ai.agents.id` attribute required for Foundry agent monitoring
  * Reasoning: Foundry trace correlation depends on this attribute
  * Reference: .copilot-tracking/research/subagents/2026-03-27/foundry-prompt-mgmt-eval-dataset-research.md
* Research AI Gateway (API Management) pricing/tier implications for custom agent registration
  * Reasoning: APIM is required for custom agent registration; cost depends on tier
  * Reference: .copilot-tracking/research/subagents/2026-03-27/foundry-prompt-mgmt-eval-dataset-research.md
* Prototype a dataset-only evaluation run using .NET `Azure.AI.Projects` `EvaluationClient`
  * Reasoning: Validate the safety-evaluators-without-judge-model path end-to-end
  * Reference: .copilot-tracking/research/subagents/2026-03-27/foundry-prompt-mgmt-eval-dataset-research.md
* Design custom code-based evaluators for Biotrackr health data quality (e.g., "did the agent cite tool data correctly?")
  * Reasoning: Domain-specific evaluation beyond built-in evaluators
  * Reference: .copilot-tracking/research/subagents/2026-03-27/foundry-evaluation-monitoring-external-models-research.md

## Research Executed

### File Analysis

* src/Biotrackr.Chat.Api/ — ChatAgentProvider.cs constructs `AnthropicClient` directly with API key, then calls `AsAIAgent()` from `Microsoft.Agents.AI.Anthropic` (v1.0.0-rc4). Uses `claude-sonnet-4-6` as model. ReportReviewerService.cs creates a second stateless Claude agent for report validation.
* src/Biotrackr.Mcp.Server/ — Uses `System.Diagnostics.Activity` / `ActivitySource` for OpenTelemetry-compatible tracing in `BaseTool` class. Already exports spans to Application Insights.
* scripts/chat-system-prompt/system-prompt.txt — Existing Git-versioned system prompt file, aligned with Microsoft's recommended prompt versioning pattern.
* scripts/reporting-api-prompts/report-generator-prompt.txt — Additional system prompt file, already in Git version control.

### Code Search Results

* `AnthropicClient` — found in ChatAgentProvider.cs and ReportReviewerService.cs. Direct Anthropic SDK integration, no Azure AI proxy.
* `ActivitySource` / `Activity` — found in MCP Server's BaseTool class. OpenTelemetry-compatible instrumentation already in place.
* `ApplicationInsights` — referenced in infrastructure Bicep modules. Application Insights instance already deployed and connected.

### External Research

* Learning Path (all 5 modules): `https://learn.microsoft.com/en-us/training/paths/operationalize-gen-ai-apps/`
  * All module unit pages fetched and analyzed
  * Source: .copilot-tracking/research/subagents/2026-03-27/foundry-genaiops-modules-research.md
* Azure AI Foundry evaluation SDK: `https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk`
  * Evaluator requirements and model_config details documented
  * Source: .copilot-tracking/research/subagents/2026-03-27/foundry-evaluation-monitoring-external-models-research.md
* Foundry architecture and project creation: `https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-ai-foundry`
  * "The platform is free to use and explore. Pricing occurs at the deployment level."
  * Source: .copilot-tracking/research/subagents/2026-03-27/foundry-project-without-model-research.md
* .NET Azure.AI.Projects SDK: `https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/ai/Azure.AI.Projects`
  * Full evaluation capabilities confirmed in .NET SDK, including dataset-only and trace-based scenarios
  * Source: .copilot-tracking/research/subagents/2026-03-27/foundry-dotnet-integration-research.md
* Custom Agent Registration: `https://learn.microsoft.com/en-us/azure/foundry/control-plane/register-custom-agent`
  * Portal workflow documented: Operate > Register agent, HTTP or A2A protocol, AI Gateway required
  * Source: .copilot-tracking/research/subagents/2026-03-27/foundry-prompt-mgmt-eval-dataset-research.md
* Foundry continuous evaluation: `https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/how-to-monitor-agents-dashboard`
  * Explicitly supports external agents registered via Control Plane
  * Source: .copilot-tracking/research/subagents/2026-03-27/foundry-prompt-mgmt-eval-dataset-research.md
* OpenTelemetry GenAI Semantic Conventions: `https://opentelemetry.io/docs/specs/semconv/gen-ai/anthropic/`
  * Anthropic-specific conventions documented (gen_ai.provider.name = "anthropic")
  * Source: .copilot-tracking/research/subagents/2026-03-27/foundry-dotnet-integration-research.md

### Project Conventions

* Standards referenced: docs/standards/commit-standards.md, .github/copilot-instructions.md
* Instructions followed: Git-based prompt versioning pattern, OpenTelemetry instrumentation pattern

---

## Executive Summary — The Short Answer

**YES — Foundry can be used for monitoring, evaluation, and tracing without deploying a model.** Prompt versioning is handled by Git (as Foundry itself recommends). Of the five learning path modules, approximately 80% of content is directly applicable. The remaining 20% involves Foundry-specific auto-instrumentation and agent service features that require straightforward workarounds.

**The critical insight**: Foundry evaluators evaluate **data** (strings), not model endpoints. You collect Claude's responses, format them as JSONL, and Foundry evaluates the output. The model being evaluated is never called by the evaluation SDK.

**Two tiers of adoption are possible:**

| Tier | What You Get | What You Need | Monthly Cost |
|------|-------------|---------------|--------------|
| **Tier 1: Zero-model** | Safety evaluators, NLP metrics, tracing, monitoring, dataset evaluation | Foundry project + Application Insights (existing) | **~$0** (plus App Insights ingestion) |
| **Tier 2: Judge-model** | All of Tier 1 + AI-assisted quality evaluators (coherence, fluency, relevance, groundedness, task adherence) | Tier 1 + small Azure OpenAI deployment (gpt-4.1-mini) as judge | **~$2-5/month** for periodic evaluation runs |

---

## Learning Path Module-by-Module Analysis

### Module 1: Prompt Versioning — 70% Directly Applicable

**Module**: "Manage prompts for agents in Microsoft Foundry with GitHub"

| Content Area | Works Without Foundry Model? | Notes |
|---|---|---|
| Git-based prompt file management | **YES** | Fully model-agnostic. Biotrackr already does this. |
| Branching strategies (feature/experiment/hotfix) | **YES** | Software engineering, not model-dependent |
| PR-based review workflows | **YES** | Universal practice |
| Version tagging with Git tags | **YES** | Universal practice |
| Repository structure patterns | **YES** | `scripts/chat-system-prompt/` already follows this |
| Deployment lifecycle stages | **YES** | Dev → Validation → Review → Production → Monitoring |
| `agents.create_agent()` SDK deployment | **NO** | Requires Foundry Agent Service with deployed model |
| Agent versioning within Foundry portal | **NO** | Requires `CreateAgentVersion()` with model deployment name |

**Biotrackr alignment**: System prompts in `scripts/chat-system-prompt/system-prompt.txt` and `scripts/reporting-api-prompts/report-generator-prompt.txt` already follow the exact pattern Microsoft recommends. No Foundry portal feature adds value for prompt versioning — **Git is the recommended approach**.

**Key finding**: Prompt Flow has been deprecated (classified as "Foundry classic only") and does not exist in the new Foundry portal. There is no standalone "prompt management" feature in Foundry. Microsoft explicitly recommends Git-based versioning.

### Module 2: Evaluate & Optimize Agents — 95% Directly Applicable

**Module**: "Evaluate and optimize AI agents through structured experiments"

| Content Area | Works Without Foundry Model? | Notes |
|---|---|---|
| Evaluation experiment design | **YES** | Methodology, not tooling |
| Three evaluation dimensions (quality/cost/performance) | **YES** | All metrics apply to Claude |
| Git-based experimentation workflow | **YES** | Branch-per-experiment pattern |
| Evaluation rubrics (1-5 scale scoring) | **YES** | Human evaluation methodology |
| Inter-rater reliability testing | **YES** | Statistical methodology |
| Manual scoring with evaluation.csv | **YES** | Model-agnostic data format |
| Foundry portal agent version comparison | **NO** | Requires Foundry-hosted agents |

**Assessment**: This module is 95% methodology — experiment design, rubrics, scoring, comparison workflows. None of this depends on Foundry models. Replace "GPT-4" references with "Claude Sonnet 4.6" variants.

### Module 3: Automated Evaluations — Partially Applicable (Two Tiers)

**Module**: "Automate AI evaluations with Microsoft Foundry and GitHub Actions"

**Critical finding — Two layers of model dependency:**

1. **The model being EVALUATED** — Can be ANY model. Evaluators work on `{query, response, context, ground_truth}` data. Collect from Claude, format as JSONL, pass to SDK. **No Foundry model needed.**

2. **The JUDGE model** — AI-assisted quality evaluators require a GPT model deployment for scoring.

| Evaluator Category | Foundry Model Needed? | What It Needs |
|---|---|---|
| NLP (F1, BLEU, ROUGE, GLEU, METEOR) | **NO** | Nothing — pure math on strings |
| Safety (Violence, Sexual, SelfHarm, HateUnfairness) | **NO** — but needs Foundry project | Azure AI Content Safety backend service |
| GroundednessProEvaluator | **NO** — but needs Foundry project | Content Safety backend |
| Code vulnerability | **NO** | Rules-based |
| Quality (Coherence, Fluency, Relevance, Groundedness, Similarity) | **Needs a GPT judge only** | `model_config` with Azure OpenAI or OpenAI API |
| Agent (TaskAdherence, IntentResolution, ToolCallAccuracy, ResponseCompleteness) | **Needs a GPT judge only** | `model_config` with Azure OpenAI or OpenAI API |
| Custom evaluators | **NO** | Pure Python or .NET code |
| GitHub Actions CI/CD integration | **YES** | Framework-agnostic |
| Shadow rating (human vs automated) | **YES** | Methodology |

**.NET evaluation SDK** (`Azure.AI.Projects` `EvaluationClient`):
- Supports dataset-only evaluation (JSONL, no target model)
- Supports trace-based evaluation (Application Insights data, Kusto queries)
- Safety evaluators run without `deployment_name`
- Quality evaluators require `deployment_name` pointing to a GPT judge model
- Evaluation runs execute server-side as jobs

### Module 4: Monitor Generative AI Application — ~60% Applicable

**Module**: "Monitor your generative AI application"

| Content Area | Works Without Foundry Model? | Notes |
|---|---|---|
| Four core metrics (latency, throughput, token usage, error rates) | **YES** | Conceptually universal |
| OpenTelemetry custom spans | **YES** | Universal standard |
| Application Insights integration | **YES** | Biotrackr already uses this |
| Azure Monitor dashboards and alerting | **YES** | Works with any telemetry source |
| Workbook visualization | **YES** | Custom workbooks for any data |
| Monitoring feedback loop methodology | **YES** | Process, not tooling |
| `AIInferenceInstrumentor().instrument()` auto-instrumentation | **NO** | Only instruments Azure AI inference API |
| `project.inference.get_chat_completions_client()` | **NO** | Requires Foundry-deployed model |
| Foundry "Gen AI Insights" prebuilt workbook | **PARTIAL** | Expects gen_ai.* semantic conventions from Azure SDK |
| Custom Agent Registration + Foundry monitoring dashboard | **YES** | Register external agent → get full dashboard |

**Key finding**: Foundry explicitly supports monitoring external agents — *"Foundry can serve as a centralized location for your agent monitoring, even for agents not running on the platform."* Custom agent registration through AI Gateway enables the full monitoring dashboard.

**What Biotrackr already has**: Application Insights, OpenTelemetry instrumentation on MCP Server, Azure Monitor alerts.

**Gap**: No auto-instrumentation for Anthropic SDK. Manual `gen_ai.*` semantic convention spans needed around Claude API calls.

### Module 5: Tracing — ~75% Applicable

**Module**: "Analyze and debug your generative AI app with tracing"

| Content Area | Works Without Foundry Model? | Notes |
|---|---|---|
| OpenTelemetry concepts (traces, spans, attributes) | **YES** | Universal standard |
| Custom span creation | **YES** | Already using `ActivitySource` in MCP Server |
| Model call wrapper pattern with timing/metadata | **YES** | Wrap Anthropic calls instead of OpenAI |
| Business logic and session-level tracing | **YES** | Framework-agnostic |
| Error handling patterns in traces | **YES** | Universal |
| Console-based tracing for CI/CD | **YES** | `ConsoleSpanExporter` |
| Azure Monitor export via `configure_azure_monitor()` | **YES** | Just needs App Insights connection string |
| Trace data analysis (quality, performance, reliability) | **YES** | Query-based in App Insights |
| `opentelemetry-instrumentation-openai-v2` auto-tracing | **NO** | Only instruments OpenAI SDK calls |
| Foundry portal trace viewing | **YES** | With App Insights connected to Foundry project |

**Anthropic-specific OTel conventions exist**: The OpenTelemetry specification includes [Anthropic-specific semantic conventions](https://opentelemetry.io/docs/specs/semconv/gen-ai/anthropic/) — `gen_ai.provider.name = "anthropic"`, model naming, token computation (input excludes cached tokens), and span naming (`chat claude-sonnet-4-6`).

**.NET implementation pattern** (manual instrumentation for Anthropic):

```csharp
private static readonly ActivitySource GenAiSource = new("gen_ai.anthropic");

using var activity = GenAiSource.StartActivity("chat claude-sonnet-4-6", ActivityKind.Client);
activity?.SetTag("gen_ai.operation.name", "chat");
activity?.SetTag("gen_ai.provider.name", "anthropic");
activity?.SetTag("gen_ai.request.model", "claude-sonnet-4-6");
activity?.SetTag("server.address", "api.anthropic.com");

// After receiving response:
activity?.SetTag("gen_ai.response.model", response.Model);
activity?.SetTag("gen_ai.response.id", response.Id);
activity?.SetTag("gen_ai.usage.input_tokens", response.Usage.InputTokens + response.Usage.CacheReadInputTokens + response.Usage.CacheCreationInputTokens);
activity?.SetTag("gen_ai.usage.output_tokens", response.Usage.OutputTokens);
```

---

## Feature Compatibility Matrix

### Complete YES/NO Decision Matrix

| Feature | Works with External Models? | Requirements |
|---|---|---|
| **Prompt Versioning** | | |
| Git-based prompt management | **YES** | None — just Git |
| Prompt Flow (portal) | **NO** | Deprecated (classic only), requires deployed model |
| **Evaluation** | | |
| NLP evaluators (F1, BLEU, ROUGE, GLEU, METEOR) | **YES** | Nothing — pure math |
| Safety evaluators (Violence, Sexual, SelfHarm, Hate) | **YES** | Foundry project (Content Safety backend) |
| GroundednessProEvaluator | **YES** | Foundry project (Content Safety backend) |
| Code vulnerability evaluator | **YES** | Rules-based |
| Quality evaluators (Coherence, Fluency, Relevance, etc.) | **YES (with judge)** | Azure OpenAI GPT model as judge |
| Agent evaluators (TaskAdherence, ToolCallAccuracy, etc.) | **YES (with judge)** | Azure OpenAI GPT model as judge |
| Custom evaluators | **YES** | Pure code — custom Python or .NET |
| Dataset-only batch evaluation (JSONL) | **YES** | Upload pre-computed Claude outputs |
| Trace-based evaluation (App Insights) | **YES** | OTel traces + Log Analytics Reader role |
| Cloud batch model-target evaluation | **NO** | Requires Foundry-deployed model |
| Cloud batch agent-target evaluation | **NO** | Requires Foundry Agent |
| Continuous evaluation (event-triggered) | **YES** | Register custom agent + OTel traces |
| Adversarial simulation (with callback) | **YES** | Callback to your own endpoint |
| Red teaming (local SDK) | **YES** | Callback to your own endpoint |
| Red teaming (cloud) | **NO** | Requires Foundry model/agent target |
| **Monitoring** | | |
| Custom agent registration | **YES** | AI Gateway (APIM) + HTTP/A2A endpoint |
| Foundry monitoring dashboard | **YES** | After custom agent registration |
| Application Insights metrics | **YES** | OTel instrumentation (existing) |
| Azure Monitor dashboards/alerts | **YES** | Works with any telemetry source |
| Token usage tracking | **YES** | Extract from Anthropic API responses manually |
| Auto-instrumentation (`AIInferenceInstrumentor`) | **NO** | Only Azure AI inference API |
| **Tracing** | | |
| OpenTelemetry custom spans | **YES** | Universal standard |
| Azure Monitor trace export | **YES** | App Insights connection string |
| Foundry portal trace viewing | **YES** | App Insights connected to Foundry project |
| Auto-instrumentation (`openai-v2`) | **NO** | Only OpenAI SDK calls |
| Console-based tracing (CI/CD) | **YES** | `ConsoleSpanExporter` |

---

## Minimum Resource Requirements and Cost

### Resources Needed — Tiered Approach

**Tier 1: Zero-Model Foundry Project**

```text
Resource Group (existing)
├── Foundry Resource (Microsoft.CognitiveServices/account, kind: AIServices)   [$0/month]
│   └── Foundry Project (child resource)                                        [$0/month]
├── Application Insights (EXISTING — connect to Foundry project)               [already paying]
│   └── Log Analytics Workspace (EXISTING)                                      [already paying]
└── Storage Account (optional, for eval dataset upload)                         [pennies/month]
```

Available features: Safety evaluators, NLP evaluators, custom evaluators, tracing visualization, trace-based evaluation, dataset evaluation (safety only), monitoring (with custom agent registration).

**Tier 2: Judge-Model Foundry Project (Recommended)**

```text
Resource Group (existing)
├── Foundry Resource (Microsoft.CognitiveServices/account, kind: AIServices)   [$0/month]
│   ├── Foundry Project (child resource)                                        [$0/month]
│   └── Azure OpenAI deployment: gpt-4.1-mini (judge model only)              [~$2-5/month]
├── Application Insights (EXISTING)                                             [already paying]
│   └── Log Analytics Workspace (EXISTING)                                      [already paying]
├── API Management (for custom agent registration)                              [~$0-3/month consumption tier]
└── Storage Account (for eval datasets)                                         [pennies/month]
```

Available features: Everything in Tier 1 + ALL AI-assisted quality evaluators (coherence, fluency, relevance, groundedness, task adherence, intent resolution, tool call accuracy) + continuous evaluation + full monitoring dashboard.

### Cost Breakdown

| Component | Tier 1 Cost | Tier 2 Cost |
|---|---|---|
| Foundry resource + project | $0 | $0 |
| Application Insights | Already paying | Already paying |
| Azure OpenAI gpt-4.1-mini | N/A | ~$0.40/1M input, $1.60/1M output |
| API Management (Consumption) | N/A | Pay-per-call (~$3.50/million calls) |
| Storage (eval datasets) | Pennies | Pennies |
| **Total incremental** | **~$0/month** | **~$2-5/month** |

**Key quote from Microsoft**: *"The platform is free to use and explore. Pricing occurs at the deployment level."*

---

## Technical Scenarios

### Scenario: Evaluation Pipeline for Claude-Generated Health Reports

Evaluate the quality and safety of reports generated by the Claude-powered Chat.Api and Reporting.Api agents.

**Requirements:**

* Evaluate Claude's health report responses for accuracy, safety, and quality
* Run evaluations in CI/CD (GitHub Actions) and on-demand
* Use both automated metrics and domain-specific health data checks
* View evaluation results in Foundry portal

**Preferred Approach: Tier 2 — Dataset Evaluation with GPT Judge**

```text
src/Biotrackr.Chat.Api/
    Tests/
        EvaluationDatasets/
            chat-agent-eval.jsonl        (pre-computed Claude responses)
            report-review-eval.jsonl     (reviewer agent responses)
.github/workflows/
    evaluation.yml                       (GitHub Actions evaluation pipeline)
```

**Implementation Details:**

1. **Collect evaluation data**: Capture query/response pairs from Chat API during test runs → export as JSONL:
   ```json
   {"query": "What was my average sleep last week?", "response": "Based on your Fitbit data...", "context": "...", "ground_truth": "..."}
   ```

2. **Upload to Foundry and run evaluators** (.NET):
   ```csharp
   var projectClient = new AIProjectClient(new Uri(foundryEndpoint), new DefaultAzureCredential());
   var evalClient = projectClient.GetEvaluationClient();
   
   // Upload dataset
   var dataset = projectClient.Datasets.UploadFile("chat-eval", "v1", "chat-agent-eval.jsonl", connectionName);
   
   // Define evaluators — safety (no judge) + quality (with judge)
   var testingCriteria = new[] {
       new { type = "azure_ai_evaluator", evaluator_name = "builtin.violence", /* no deployment_name */ },
       new { type = "azure_ai_evaluator", evaluator_name = "builtin.fluency", initialization_parameters = new { deployment_name = "gpt-4.1-mini" } },
       new { type = "azure_ai_evaluator", evaluator_name = "builtin.task_adherence", initialization_parameters = new { deployment_name = "gpt-4.1-mini" } },
   };
   ```

3. **NLP evaluators** (no Azure resources needed, run locally or in CI):
   ```python
   from azure.ai.evaluation import BleuScoreEvaluator, RougeScoreEvaluator
   bleu = BleuScoreEvaluator()
   result = bleu(response="Claude's actual response", ground_truth="Expected answer")
   ```

4. **Custom health data evaluator** (domain-specific, no model needed):
   ```python
   def health_data_citation_accuracy(response, context, **kwargs):
       """Check if response cites specific numbers from the source data."""
       import json
       source_data = json.loads(context)
       cited_numbers = extract_numbers(response)
       source_numbers = extract_numbers(str(source_data))
       overlap = cited_numbers.intersection(source_numbers)
       return len(overlap) / len(cited_numbers) if cited_numbers else 1.0
   ```

#### Considered Alternatives

**Alternative A: OpenAI API as judge (no Azure OpenAI deployment)**

The evaluation SDK supports `OpenAIModelConfiguration` — you could use an OpenAI API key directly as the judge model, paying OpenAI instead of Azure. This eliminates the need for any Azure OpenAI resource.

* Pro: No Azure OpenAI deployment needed
* Pro: Potentially cheaper for very small evaluation volumes
* Con: Adds another external API dependency (OpenAI)
* Con: Less secure — OpenAI API key management vs. managed identity
* Rejected because: Managed identity auth for Azure OpenAI is more secure and aligns with Biotrackr's existing Azure-first infrastructure pattern

**Alternative B: Pure NLP + Safety evaluators only (Tier 1, no judge model)**

Use only evaluators that don't require a GPT judge: F1, BLEU, ROUGE, METEOR for quality approximation, and safety evaluators for content safety.

* Pro: $0 incremental cost
* Pro: No additional Azure OpenAI deployment
* Con: Misses the most valuable quality evaluators (coherence, fluency, groundedness, task adherence)
* Con: NLP metrics are poor proxies for conversational quality
* Not selected as primary because: The GPT judge evaluators provide the most actionable quality signals. However, **Tier 1 is a valid starting point** before committing to Tier 2.

**Alternative C: Claude as judge (custom evaluator calling Anthropic)**

Write custom evaluators that call Claude instead of GPT as the judge model.

* Pro: No Azure OpenAI deployment
* Pro: Use a model you already pay for
* Con: No built-in evaluator prompt templates — must write and validate custom evaluation prompts
* Con: Not supported by built-in `azure-ai-evaluation` evaluators (they only accept GPT model configs)
* Con: "Judge is also the defendant" problem — Claude evaluating Claude reduces evaluation independence
* Rejected because: Model diversity in evaluation is a best practice (different judge than target), and the built-in evaluator prompts are battle-tested

### Scenario: Production Monitoring via Custom Agent Registration

Monitor the Claude-powered Chat.Api in production using Foundry's monitoring dashboard.

**Requirements:**

* Track latency, token usage, error rates, and run success
* Enable continuous evaluation on sampled responses
* Alert on anomalies (latency spikes, evaluation score drops)

**Preferred Approach: Register Chat.Api as Custom Agent**

1. **Prerequisites**:
   - Deploy AI Gateway (API Management) in Foundry resource
   - Connect existing Application Insights to Foundry project
   - Ensure Chat.Api emits OTel traces with `gen_ai.*` semantic conventions

2. **Register**:
   - Foundry portal → Operate → Register agent
   - Agent URL: `https://biotrackr-chat-api.{region}.azurecontainerapps.io`
   - Protocol: HTTP
   - Name: `BiotrackrChatAgent`
   - Project: Biotrackr Foundry project

3. **Instrument Chat.Api with gen_ai spans**:
   ```csharp
   private static readonly ActivitySource GenAi = new("gen_ai.anthropic");
   
   using var activity = GenAi.StartActivity("chat claude-sonnet-4-6", ActivityKind.Client);
   activity?.SetTag("gen_ai.operation.name", "chat");
   activity?.SetTag("gen_ai.provider.name", "anthropic");
   activity?.SetTag("gen_ai.request.model", "claude-sonnet-4-6");
   activity?.SetTag("gen_ai.agents.id", "BiotrackrChatAgent");
   // ... (response attributes after call)
   ```

4. **Enable continuous evaluation** on the monitoring dashboard:
   - Add safety evaluators (no judge needed)
   - Add quality evaluators (requires gpt-4.1-mini judge from Tier 2)
   - Set sample rate (e.g., 10% of requests)
   - Configure alerts for score thresholds

#### Considered Alternatives

**Alternative A: Application Insights + Custom Azure Workbooks only**

Skip Foundry custom agent registration entirely. Build Azure Monitor workbooks directly from the existing Application Insights data.

* Pro: No API Management dependency
* Pro: No Foundry portal required
* Pro: Biotrackr already has Application Insights
* Con: Must build all dashboards manually (no pre-built monitoring template)
* Con: No continuous evaluation integration
* Con: No agent blocking/management capability
* Not selected as primary because: Custom agent registration provides significant value (continuous eval, pre-built dashboard, agent management) for minimal additional complexity

---

## Key Discoveries

### Project Structure

Biotrackr already follows Foundry's recommended patterns:
- System prompts are Git-versioned in `scripts/chat-system-prompt/` and `scripts/reporting-api-prompts/`
- OpenTelemetry instrumentation exists in MCP Server via `ActivitySource`
- Application Insights is already deployed and connected

### Implementation Patterns

**Evaluators evaluate DATA, not model endpoints.** This is the most important discovery. The `azure-ai-evaluation` SDK and the .NET `EvaluationClient` both operate on pre-computed strings (`query`, `response`, `context`, `ground_truth`). The identity of the model that generated those strings is irrelevant to the evaluator.

**The judge model and production model are separate concerns.** Built-in quality evaluators use a GPT model to score responses — this judge model is completely independent from the production model. You can evaluate Claude outputs using a GPT judge.

**Custom Agent Registration is the bridge for external models.** Foundry's Control Plane explicitly supports registering externally hosted agents. After registration, the full monitoring dashboard, continuous evaluation, and agent management features become available. This is the documented path for non-Foundry-hosted agents.

**Prompt Flow is dead; Git is the answer.** Prompt Flow has been classified as "Foundry (classic) only" and does not exist in the new Foundry portal. Microsoft's explicit recommendation for prompt versioning is Git repositories with branching, PR reviews, and version tags.

**OpenTelemetry is the universal bridge.** All monitoring and tracing in the learning path is built on OpenTelemetry. Since OTel is vendor-neutral, traces from any model can flow to Application Insights and be visualized in both Azure Monitor and the Foundry portal.

### Complete Examples

**.NET Dataset Evaluation (safety evaluators, no judge model)**:

```csharp
var projectClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());

// Upload pre-computed Claude responses as dataset
var dataset = projectClient.Datasets.UploadFile(
    name: "chat-eval-safety", version: "v1",
    filePath: "eval-data.jsonl", connectionName: "default");

// Define safety evaluators — NO deployment_name needed
object dataSource = new { type = "jsonl", source = new { type = "file_id", id = dataset.Id } };
var criteria = new[] {
    new { type = "azure_ai_evaluator", evaluator_name = "builtin.violence",
          data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" } },
    new { type = "azure_ai_evaluator", evaluator_name = "builtin.self_harm",
          data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" } },
};
```

**Anthropic OTel span in .NET**:

```csharp
private static readonly ActivitySource GenAiSource = new("gen_ai.anthropic");

public async Task<ChatResponse> CallClaudeAsync(string prompt)
{
    using var activity = GenAiSource.StartActivity("chat claude-sonnet-4-6", ActivityKind.Client);
    activity?.SetTag("gen_ai.operation.name", "chat");
    activity?.SetTag("gen_ai.provider.name", "anthropic");
    activity?.SetTag("gen_ai.request.model", "claude-sonnet-4-6");
    activity?.SetTag("gen_ai.agents.id", "BiotrackrChatAgent");
    activity?.SetTag("server.address", "api.anthropic.com");

    var response = await _anthropicClient.CreateMessageAsync(prompt);

    activity?.SetTag("gen_ai.response.model", response.Model);
    activity?.SetTag("gen_ai.response.id", response.Id);
    activity?.SetTag("gen_ai.usage.input_tokens",
        response.Usage.InputTokens + response.Usage.CacheReadInputTokens + response.Usage.CacheCreationInputTokens);
    activity?.SetTag("gen_ai.usage.output_tokens", response.Usage.OutputTokens);
    activity?.SetTag("gen_ai.response.finish_reasons", new[] { response.StopReason });

    return response;
}
```

### API and Schema Documentation

* Azure.AI.Projects .NET SDK: https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/ai/Azure.AI.Projects
* azure-ai-evaluation (Python): https://pypi.org/project/azure-ai-evaluation/
* Built-in evaluators reference: https://learn.microsoft.com/en-us/azure/foundry/concepts/built-in-evaluators
* OpenTelemetry GenAI Semantic Conventions (Anthropic): https://opentelemetry.io/docs/specs/semconv/gen-ai/anthropic/
* Custom Agent Registration: https://learn.microsoft.com/en-us/azure/foundry/control-plane/register-custom-agent
* Agent Monitoring Dashboard: https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/how-to-monitor-agents-dashboard

### Configuration Examples

**Minimum Foundry project Bicep** (no model deployment):

```bicep
resource foundryResource 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: 'biotrackr-foundry'
  location: location
  kind: 'AIServices'
  sku: { name: 'S0' }
  properties: {}
}

resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2024-10-01' = {
  parent: foundryResource
  name: 'biotrackr-genaiops'
  location: location
  properties: {}
}
```

**JSONL evaluation dataset format** (model-agnostic):

```json
{"query": "What was my average sleep last week?", "response": "Based on your Fitbit sleep data from March 20-26, your average sleep duration was 7h 23m...", "context": "{\"sleep_records\": [{...}]}", "ground_truth": "Average sleep: 7h 23m"}
{"query": "Show me my weight trend this month", "response": "Your weight over March shows a slight downward trend...", "context": "{\"weight_records\": [{...}]}", "ground_truth": "Weight trend: decreasing, 82.1kg to 81.4kg"}
```

---

## Subagent Research Documents

* .copilot-tracking/research/subagents/2026-03-27/foundry-genaiops-modules-research.md — Module-by-module analysis of all 5 learning path modules
* .copilot-tracking/research/subagents/2026-03-27/foundry-evaluation-monitoring-external-models-research.md — Deep dive on evaluation SDK, monitoring, and tracing with external models
* .copilot-tracking/research/subagents/2026-03-27/foundry-project-without-model-research.md — Foundry project creation, architecture, connections, evaluator requirements, and cost
* .copilot-tracking/research/subagents/2026-03-27/foundry-dotnet-integration-research.md — .NET SDK capabilities, custom agent registration, OTel semantic conventions, and evaluation in .NET
* .copilot-tracking/research/subagents/2026-03-27/foundry-prompt-mgmt-eval-dataset-research.md — Prompt management status, dataset-only evaluation, custom agent workflow, continuous evaluation
* .copilot-tracking/research/subagents/2026-03-27/biotrackr-current-ai-telemetry-setup-research.md — Current Biotrackr AI architecture, telemetry, and infrastructure
