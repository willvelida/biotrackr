# Azure AI Foundry — Prompt Management, .NET Evaluation Dataset Mode, and Custom Agent Registration

## Research Status: Complete

## Research Questions & Findings

---

### Q1: Foundry Prompt Management / Prompt Flow — Model-Free Usage

**Q1.1: Does Foundry have a Prompt Management or Prompt Catalog feature separate from Prompt Flow?**

**Answer: NO** — There is no standalone "Prompt Management" or "Prompt Catalog" feature in the Foundry portal. Prompt Flow is the only prompt-related feature, and it is now classified as **Foundry (classic) only** — the documentation explicitly states: *"Applies only to: Foundry (classic) portal. This article isn't available for the new Foundry portal."*

The new Foundry portal does not have Prompt Flow at all. The URL `https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/prompt-flow-tools-overview` returned **HTTP 404**, confirming the feature has been removed from the new Foundry documentation namespace.

**Q1.2: Can you version/store/manage prompts in the Foundry portal without a deployed model?**

**Answer: NO** — Prompt Flow (classic) explicitly requires: *"You need a deployed model"* as a prerequisite. Even in classic mode, you cannot use Prompt Flow without a model deployment. There is no separate prompt versioning UI in the new Foundry portal.

**Q1.3: What is the status of Prompt Flow vs newer Foundry Prompt Management?**

**Answer: Prompt Flow is deprecated (classic-only). No replacement prompt management feature exists.**

- Prompt Flow is exclusively in Foundry (classic) — it does not exist in the new Foundry portal
- The new Foundry replaced prompt development workflows with **Agent definitions** that bundle model + instructions + tools
- Agent definitions support versioning via `CreateAgentVersion()` in the SDK
- Microsoft's recommendation is **Git-based versioning** for prompts (see Q1.4)

**Q1.4: Can you use portal prompt authoring as versioning, or is Git recommended?**

**Answer: Git-based versioning is the explicit recommendation.**

The Microsoft Learn training module "Manage prompts for agents in Microsoft Foundry with GitHub" (last updated 2026) teaches:
- Prompts are **production-critical assets** that need version control like code
- Store prompts in `.txt` or `.md` files tracked by Git — not in the Foundry portal
- Use GitHub pull requests for prompt review workflows
- Deploy prompts programmatically via the SDK by reading from version-controlled files
- Three strategies are documented: embedded in code, separate files (recommended), or YAML/JSON configuration

Example from the training module:
```python
# Read prompt from version-controlled file
with open('prompts/v1_instructions.txt', 'r') as f:
    instructions = f.read().strip()

# Create agent with versioned prompt
agent = project_client.agents.create_agent(
    model=os.environ["MODEL_NAME"],
    name=os.environ["AGENT_NAME"],
    instructions=instructions
)
```

**Key quote**: *"Instead of editing prompts directly in Foundry, you store them in GitHub with full change history."*

#### Q1 Summary for Biotrackr

Biotrackr's current approach of storing system prompts in `scripts/chat-system-prompt/system-prompt.txt` with Git versioning is already **aligned with Microsoft's recommended pattern**. There is no Foundry portal feature that would add value over Git for prompt versioning.

---

### Q2: .NET Azure.AI.Projects EvaluationClient — Dataset-Only Evaluation

**Q2.1: Can the .NET EvaluationClient run evaluations against a JSONL dataset without a model deployment as the target?**

**Answer: YES** — The Azure.AI.Projects SDK explicitly supports "Dataset evaluation" where you evaluate pre-computed responses in a JSONL file. The data source type is `jsonl` with no target required. The documentation table states:

| Scenario | Data Source Type | Target |
|---|---|---|
| Dataset evaluation | `jsonl` | — (none) |

**However**, there is a critical nuance: most AI-assisted evaluators (coherence, fluency, relevance, etc.) require `initialization_parameters` with a `deployment_name` pointing to a **judge model** (e.g., GPT) that scores the responses. This is different from the target model. You still need at least one Azure OpenAI model deployed to act as the evaluator's judge, even when evaluating pre-computed external data.

**Q2.2: .NET evaluation API for dataset-only mode — code samples**

The .NET SDK README on GitHub shows the dataset-only pattern:

```csharp
// Upload dataset first
FileDataset fileDataset = projectClient.Datasets.UploadFile(
    name: datasetName,
    version: datasetVersion1,
    filePath: filePath,
    connectionName: connectionName
);

// Use uploaded dataset ID as data source (no target)
object dataSource = new
{
    type = "jsonl",
    source = new
    {
        type = "file_id",
        id = fileDataset.Id
    },
};

BinaryData runData = BinaryData.FromObjectAsJson(new
{
    eval_id = evaluationId,
    name = $"Evaluation Run for dataset {fileDataset.Name}",
    data_source = dataSource
});
```

The evaluation definition uses `BinaryData.FromObjectAsJson` with `testing_criteria` containing evaluator configs. No `target` property is needed in the `dataSource` for dataset evaluations.

**Q2.3: Which built-in evaluators run WITHOUT deployment_name?**

Based on the SDK samples and documentation:

**Do NOT require `deployment_name`** (non-AI-assisted, computed algorithmically):
- `builtin.f1_score` — token overlap between response and ground_truth
- `builtin.bleu` — n-gram overlap
- `builtin.gleu` — Google-BLEU variant
- `builtin.rouge` — recall-oriented n-gram overlap
- `builtin.meteor` — translation evaluation
- `builtin.violence` — content safety (uses Azure AI Content Safety service, not a judge model)
- `builtin.sexual` — content safety
- `builtin.self_harm` — content safety
- `builtin.hate_unfairness` — content safety
- `builtin.protected_materials` — content detection
- `builtin.code_vulnerability` — code security

Evidence from the .NET SDK README sample code:
```csharp
// violence_detection does NOT have initialization_parameters
new {
    type = "azure_ai_evaluator",
    name = "violence_detection",
    evaluator_name = "builtin.violence",
    data_mapping = new { query = "{{item.query}}", response = "{{sample.output_text}}"}
},
// fluency DOES require initialization_parameters
new {
    type = "azure_ai_evaluator",
    name = "fluency",
    evaluator_name = "builtin.fluency",
    initialization_parameters = new { deployment_name = modelDeploymentName},
    data_mapping = new { query = "{{item.query}}", response = "{{sample.output_text}}"}
},
```

**DO require `deployment_name`** (AI-assisted, use an LLM judge):
- `builtin.coherence`
- `builtin.fluency`
- `builtin.similarity`
- `builtin.relevance`
- `builtin.groundedness`
- `builtin.task_adherence`
- `builtin.intent_resolution`
- `builtin.tool_call_accuracy`
- And other agent evaluators

**Q2.4: Does .NET evaluation SDK support running from traces (Application Insights data)?**

**Answer: YES** — The .NET SDK README includes a full "Evaluation with Application Insights" section showing trace-based evaluation. Key pattern:

```csharp
// Data source config with scenario = "traces"
object dataSourceConfig = new
{
    type = "azure_ai_source",
    scenario = "traces"
};

// Run data references specific trace IDs
object dataSource = new
{
    type = "azure_ai_traces",
    trace_ids = traceIDs,
    lookback_hours = lookbackHours
};
```

This requires the Foundry project and its managed identity to have "Log Analytics Reader" role for the Application Insights resource. The traces are fetched via Kusto query filtered by trace IDs.

#### Q2 Summary for Biotrackr

Biotrackr CAN use Foundry evaluation on pre-computed Claude responses:
1. Compute responses locally using Claude via Anthropic API
2. Export query/response pairs to JSONL
3. Upload as a dataset to Foundry
4. Run safety evaluators (violence, sexual, self-harm, hate) — **no Azure OpenAI model needed**
5. For quality evaluators (coherence, fluency), deploy a **small GPT model as judge** (e.g., gpt-4.1-mini) — this is cheap and only used for scoring

The trace-based evaluation also works if Biotrackr sends OpenTelemetry traces to Application Insights (which it already does).

---

### Q3: Foundry Custom Agent Registration — Exact Workflow

**Q3.1: What is the exact workflow to register a custom agent?**

**Answer: Fully documented portal workflow exists.** Steps:

1. **Prerequisites**:
   - A Foundry project
   - An AI gateway configured in the Foundry resource (uses Azure API Management as proxy)
   - An externally deployed agent with a reachable endpoint
   - Application Insights connected to the project

2. **Verify agent requirements**:
   - Agent exposes an exclusive endpoint
   - Endpoint is network-reachable from Foundry
   - Agent uses HTTP (general) or A2A protocol
   - Agent emits OpenTelemetry traces (optional but recommended)

3. **Register in portal**:
   - Navigate to Operate > Overview > Register agent
   - Provide: Agent URL, Protocol (HTTP or A2A), Agent name, Project, Description
   - Optional: OpenTelemetry Agent ID, Admin portal URL, A2A agent card URL

4. **Post-registration**:
   - Foundry generates a **new proxy URL** through API Management
   - Clients must use this new URL (not the original)
   - Original auth scheme still applies through the proxy

**Q3.2: Does registering require a specific API protocol?**

**Answer: PARTIAL** — The agent must support either:
- **HTTP** (general — any REST/HTTP-based protocol)
- **A2A** (Agent-to-Agent protocol — more specific, with optional agent card at `/.well-known/agent-card.json`)

No specific OpenAI-compatible or other API format is required — just HTTP or A2A. The agent communicates through its own protocol; Foundry acts as a transparent proxy.

**Q3.3: What monitoring/evaluation features become available after registration?**

**Answer: Comprehensive features available:**

- **Automatic** (from API Management proxy):
  - HTTP request/response logging (traces for every call)
  - Error rate tracking
  - Run counting
  - Request blocking/unblocking capability

- **With OpenTelemetry instrumentation** (agent must emit OTel traces to the same Application Insights):
  - Tool call visibility
  - LLM call details
  - Token usage metrics
  - Latency tracking
  - Agent Monitoring Dashboard with:
    - Token usage charts
    - Latency charts
    - Run success rate
    - Evaluation metrics
    - Red teaming results

- **Continuous evaluation** (requires agent to emit traces to Application Insights):
  - Event-triggered evaluation rules
  - Configurable evaluators on sampled responses
  - Scheduled evaluations
  - Scheduled red team scans
  - Alerts for anomalies

**Q3.4: Is there a .NET SDK method for registering custom agents?**

**Answer: NOT FOUND** — The documentation only shows portal-based registration. The .NET SDK's `Agents` property supports creating/managing Foundry-native agents (`DeclarativeAgentDefinition`, `CreateAgentVersion`), but no method for registering external custom agents was found. The registration appears to be portal/REST-only operation that configures API Management.

#### Q3 Summary for Biotrackr

Biotrackr's Chat API (Claude + Microsoft Agent Framework, ASP.NET Core) can be registered as a custom agent:
- Protocol: HTTP (the AGUI SSE endpoint qualifies)
- Existing OpenTelemetry instrumentation already sends traces to Application Insights
- The `gen_ai.agents.id` and `gen_ai.agents.name` OTel attributes would need to be set for Foundry to correlate traces
- After registration: monitoring dashboard, evaluation, blocking, and error tracking all become available

---

### Q4: Foundry Continuous Evaluation — Works with External Models?

**Q4.1: Can continuous evaluation work with traces from a custom/external agent?**

**Answer: YES** — The documentation explicitly confirms this:

> *"Foundry can serve as a centralized location for your agent monitoring, even for agents not running on the platform. Within Foundry control plane, you can onboard agents running elsewhere via AI Gateway. You can then instrument your agent to send traces to the same Application Insights instance as your Foundry project. This setup enables continuous evaluations and tracking of metrics like error rate for agents not running in Foundry."*

Setup steps for custom agents:
1. Onboard custom agent to Foundry via `Register and manage custom agents`
2. Instrument agent to comply with OpenTelemetry semantic conventions for generative AI
3. Configure agent to send telemetry to the same Application Insights instance as the Foundry project
4. Go to Foundry Control Plane > Asset page > select agent > Monitor tab
5. Set up continuous evaluations via the settings panel

**Q4.2: What event types trigger continuous evaluation?**

**Answer: Limited to specific event types, but works with custom agents.**

From the .NET SDK README:
```csharp
EvaluationRule continuousRule = new(
    action: continuousAction,
    eventType: EvaluationRuleEventType.ResponseCompleted,
    enabled: true)
{
    Filter = new EvaluationRuleFilter(agentName: agentVersion.Name),
};
```

The `EvaluationRuleEventType.ResponseCompleted` is the documented trigger — it fires each time the agent sends a response. This works for custom agents registered through the Control Plane provided they emit OpenTelemetry traces with proper semantic conventions (`gen_ai.agents.id` attribute).

The monitoring dashboard settings panel shows configurable features:
- **Continuous evaluation**: Runs evaluations on sampled agent responses. Enable/disable, add evaluators, set sample rate.
- **Scheduled evaluations** (preview): Runs on a schedule against benchmarks.
- **Red team scans** (preview): Adversarial tests on a schedule.
- **Alerts** (preview): Anomaly detection for latency, token usage, evaluation scores.

**Important limitation**: The `ResponseCompleted` event depends on the agent emitting proper OpenTelemetry traces. For a custom agent using Claude (not Foundry Agent Service), you need to ensure your traces include spans that match the expected semantic conventions so Foundry recognizes response completion events.

#### Q4 Summary for Biotrackr

Continuous evaluation WILL work for Biotrackr's Claude-based agent, provided:
1. The Chat API is registered as a custom agent
2. OpenTelemetry traces are sent to the Foundry project's Application Insights (already happening)
3. Traces comply with OpenTelemetry GenAI semantic conventions (needs verification/adjustment)
4. The `gen_ai.agents.id` attribute is set in trace spans
5. A small Azure OpenAI model (e.g., gpt-4.1-mini) is deployed as judge model for AI-assisted evaluators

---

## Dead Links / Missing Documentation

| URL | Status |
|---|---|
| `https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/prompt-flow-tools-overview` | **404** — Removed from new Foundry docs |
| `https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/agent-catalog` | **404** — Removed/moved from Foundry docs |
| `https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/agent-evaluation` | **404** — Removed/moved from Foundry docs |
| `https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/prompt-flow` | **Redirected** to Foundry (classic) — `foundry-classic/concepts/prompt-flow` |
| `https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/flow-develop` | **Redirected** to Foundry (classic) — `foundry-classic/how-to/flow-develop` |
| `https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/online-evaluation` | **Redirected** to Foundry (classic) — `foundry-classic/how-to/monitor-applications` |

The old `/azure/ai-foundry/` URL namespace is being migrated:
- Classic features → `/azure/foundry-classic/`
- New features → `/azure/foundry/`

---

## Key References

- [Register and manage custom agents](https://learn.microsoft.com/en-us/azure/foundry/control-plane/register-custom-agent) — Custom agent registration workflow
- [Monitor agents dashboard](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/how-to-monitor-agents-dashboard) — Monitoring and continuous evaluation setup
- [Run evaluations from the SDK](https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/cloud-evaluation) — Batch evaluation (Python-focused, .NET patterns in SDK README)
- [Azure.AI.Projects .NET SDK README (GitHub)](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/ai/Azure.AI.Projects) — .NET code samples for all evaluation scenarios
- [Built-in evaluators reference](https://learn.microsoft.com/en-us/azure/foundry/concepts/built-in-evaluators) — Complete evaluator list
- [Prompt versioning training module](https://learn.microsoft.com/en-us/training/modules/prompt-versioning-genaiops/) — Git-based prompt management
- [Observability in generative AI](https://learn.microsoft.com/en-us/azure/foundry/concepts/observability) — Foundry observability overview

---

## Follow-on Questions Discovered

1. **OTel semantic conventions compliance**: Does Biotrackr's current OpenTelemetry instrumentation include `gen_ai.agents.id` and `operation="create_agent"` spans? This is required for Foundry to recognize the agent's traces.
2. **AI Gateway cost**: The AI gateway uses Azure API Management — what tier/cost implications exist?
3. **NuGet package version**: The `Azure.AI.Projects` NuGet changelog shows rapid iteration. The GitHub README now references `projectClient.Agents.CreateAgentVersion()` (new API). Need to verify the latest beta version exposes `EvaluationClient`.
4. **Custom evaluator for Claude-specific patterns**: Can a custom code-based evaluator be created for Biotrackr-specific health data quality checks (e.g., "did the agent cite tool data correctly?")?
