# Research: Azure AI Foundry Evaluation, Monitoring & Tracing with External Models

## Research Status: Complete

## Research Questions

1. Can Azure AI Foundry's evaluation features be used with models NOT deployed in Foundry (e.g., Claude Sonnet called directly via Anthropic API)?
2. Can Azure AI Foundry's monitoring features work with non-Foundry-deployed models?
3. Can Azure AI Foundry's tracing features work with non-Foundry models? Is OpenTelemetry integration model-agnostic?
4. What are the specific SDKs/APIs involved and their external model limitations?
5. Can Foundry's "connections" feature register an external Anthropic endpoint?

---

## Finding 1: Evaluation — Can it work without a Foundry-deployed model?

### Answer: YES (with nuances — depends on which evaluation path)

There are TWO distinct evaluation systems in the Foundry ecosystem:

### Path A: `azure-ai-evaluation` Python Package (Local SDK) — YES, fully model-agnostic

**Package**: `pip install azure-ai-evaluation` (v1.16.2 as of 2026-03-24)

This SDK evaluates **any model's outputs** regardless of where the model is deployed. It operates on data (query/response pairs), not on model endpoints.

**Key evidence from PyPI/docs**:
- Prerequisites state: *"[Optional] You must have Azure AI Foundry Project or Azure Open AI to use AI-assisted evaluators"* — Foundry project is optional, not required.
- NLP evaluators (F1Score, BLEU, ROUGE, GLEU, METEOR) require NO Azure resources at all — they are pure math.
- AI-assisted quality evaluators (Coherence, Relevance, Fluency, Groundedness, Similarity) require an **Azure OpenAI deployment as the judge model** — they use GPT as-a-judge. The model being *evaluated* can be anything.
- Safety evaluators (Violence, Sexual, SelfHarm, HateUnfairness) require an **Azure AI Foundry Project** for the backend RAI service scoring — but again, the *evaluated* model's outputs are just strings.

**How it works for external models like Claude**:
```python
from azure.ai.evaluation import evaluate, RelevanceEvaluator, BleuScoreEvaluator

# NLP evaluator — NO Azure resources needed
bleu = BleuScoreEvaluator()
result = bleu(response="Claude's response here", ground_truth="Expected answer")

# AI-assisted evaluator — needs Azure OpenAI as JUDGE, but evaluated data can come from Claude
model_config = {
    "azure_endpoint": os.environ.get("AZURE_OPENAI_ENDPOINT"),
    "api_key": os.environ.get("AZURE_OPENAI_API_KEY"),
    "azure_deployment": os.environ.get("AZURE_OPENAI_DEPLOYMENT"),  # Judge model
}
relevance = RelevanceEvaluator(model_config)
result = relevance(query="User question", response="Claude's response")

# Batch evaluation of a dataset (JSONL with Claude outputs)
result = evaluate(
    data="claude_outputs.jsonl",
    evaluators={"relevance": relevance, "bleu": bleu},
    evaluator_config={...},
    azure_ai_project=azure_ai_project,  # Optional — for tracking results in Foundry portal
    output_path="./results.json"
)
```

**Custom evaluators** are fully model-agnostic — you write Python functions that score any text:
```python
def response_length(response, **kwargs):
    return len(response)
```

### Path B: Foundry Cloud Batch Evaluation SDK (`azure-ai-projects`) — PARTIALLY, with workarounds

**Package**: `pip install azure-ai-projects>=2.0.0`

This is the newer cloud-based evaluation system that runs in Foundry's infrastructure.

**Prerequisites**: Requires a Foundry project AND an Azure OpenAI deployment (GPT model for AI-assisted evaluators).

**Supported evaluation scenarios**:

| Scenario | External model support | Notes |
|---|---|---|
| **Dataset evaluation** (pre-computed responses in JSONL) | **YES** | Evaluate any pre-computed responses. Collect Claude outputs offline, put in JSONL, evaluate. |
| **Model target evaluation** (runtime) | **NO** — requires `azure_ai_model` target | Target must be a model deployed in Foundry/Azure OpenAI. |
| **Agent target evaluation** (runtime) | **NO** — requires `azure_ai_agent` target | Target must be a Foundry Agent. |
| **Agent response evaluation** | **NO** — requires Foundry agent response IDs | Uses Foundry Responses API. |
| **Synthetic data evaluation** | **NO** — requires `azure_ai_model` or `azure_ai_agent` target | Model must be Foundry-deployed. |
| **Red team evaluation** | **NO** — requires `azure_ai_model` or `azure_ai_agent` target | Model must be Foundry-deployed. |

**Key workaround for Claude**: Use the **Dataset evaluation** path. Collect Claude's responses externally, format as JSONL, upload to Foundry, then run evaluators against the pre-computed data. This gives you all the same evaluator scores (coherence, violence, F1, etc.) in the Foundry portal.

### Verdict on Evaluation

- **NLP evaluators**: YES — work completely offline, no Azure needed
- **AI-quality evaluators (local SDK)**: YES — need Azure OpenAI as judge, but evaluated data can be from any model
- **Safety evaluators (local SDK)**: YES — need Foundry project for RAI service, but evaluated data is just strings
- **Cloud batch on pre-computed data**: YES — collect Claude outputs first, then evaluate
- **Cloud batch real-time model/agent targets**: NO — requires Foundry-deployed model or agent

---

## Finding 2: Monitoring — Can it work without a Foundry-deployed model?

### Answer: YES — via Custom Agent registration in Foundry Control Plane

The Agent Monitoring Dashboard docs explicitly state:

> *"Foundry can serve as a centralized location for your agent monitoring, even for agents not running on the platform. Within Foundry control plane, you can onboard agents running elsewhere via AI Gateway."*

**How to monitor an external agent (e.g., Biotrackr Chat API using Claude)**:

1. **Register as Custom Agent**: Use Foundry Control Plane's "Register agent" feature
   - Provide the agent's endpoint URL
   - Select protocol (HTTP or A2A)
   - Specify OpenTelemetry Agent ID
   - Associate with a Foundry project that has Application Insights

2. **Instrument the agent**: Emit OpenTelemetry-compliant traces to the same Application Insights instance as the Foundry project
   - Must comply with [OpenTelemetry semantic conventions for generative AI](https://opentelemetry.io/docs/specs/semconv/gen-ai/)
   - Include spans with `gen_ai.agents.id` attribute

3. **Set up continuous evaluation**: Once registered, continuous evaluation rules can run against sampled responses

**Foundry computes these metrics from custom agents**:
- Runs
- Error rate
- Usage (if available)
- Token usage, latency, success rates (if instrumented)

**Monitoring dashboard features available**:
- Token usage charts
- Latency metrics
- Run success rate
- Evaluation metrics (if continuous evaluation is enabled)
- Red teaming results (if scheduled)

### Verdict on Monitoring

- **YES** — external agents can be monitored IN Foundry
- Requires: Register as custom agent + instrument with OpenTelemetry + send traces to same App Insights
- The model itself (Claude, GPT, Llama, etc.) is irrelevant — Foundry monitors the agent layer

---

## Finding 3: Tracing — Can it work without a Foundry-deployed model?

### Answer: YES — tracing is fully model-agnostic via OpenTelemetry

Tracing is built on **OpenTelemetry standards** and is completely decoupled from the model provider.

**Key evidence**:
- Tracing overview: *"Built on OpenTelemetry standards and integrated with Application Insights, tracing enables debugging complex agent behaviors"*
- Supported frameworks: LangChain, Semantic Kernel, OpenAI Agents SDK, Microsoft Agent Framework — none of these are model-locked
- AI Toolkit in VS Code: Explicitly supports *"Foundry Agents Service, OpenAI, Anthropic, and LangChain through OpenTelemetry"*
- Custom agent docs: *"If you build your agent by using custom code, instrument your solution to emit traces according to the OpenTelemetry standard and send them to Application Insights."*

**How tracing works for a Claude-based agent**:

1. **Connect Application Insights** to your Foundry project
2. **Instrument your code** with OpenTelemetry, exporting to Application Insights:
   ```python
   from opentelemetry import trace
   from azure.monitor.opentelemetry.exporter import AzureMonitorTraceExporter
   
   # Export spans to App Insights
   provider.add_span_processor(
       BatchSpanProcessor(AzureMonitorTraceExporter.from_connection_string(conn_string))
   )
   ```
3. **Follow semantic conventions** for gen-ai spans (tool calls, LLM calls, agent decisions)
4. **View in Foundry portal** under Observability > Traces

**Traces capture** (regardless of model provider):
- User inputs and agent outputs
- Tool usage (tool calls and results)
- Timing signals (latency)
- LLM calls
- Agent decision flows

### Verdict on Tracing

- **YES** — fully model-agnostic, works with any model including Claude via Anthropic API
- Requires: OpenTelemetry instrumentation + Application Insights + Foundry project
- No Foundry model deployment needed

---

## Finding 4: azure-ai-evaluation Python Package Deep Dive

### Works Without Foundry Model Deployment?

| Evaluator Category | Requires Foundry Model? | What It Needs |
|---|---|---|
| NLP (F1, BLEU, ROUGE, GLEU, METEOR) | **NO** | Nothing — pure math on strings |
| Quality AI-assisted (Coherence, Relevance, Fluency, Groundedness, Similarity, Retrieval) | **NO** — but needs Azure OpenAI as **judge** | `model_config` with Azure OpenAI endpoint/deployment |
| Safety AI-assisted (Violence, Sexual, SelfHarm, HateUnfairness, IndirectAttack, ProtectedMaterial) | **NO** — but needs Foundry **project** for RAI scoring service | `azure_ai_project` dict or URL |
| Agent evaluators (TaskAdherence, ToolCallAccuracy, IntentResolution, ResponseCompleteness) | **NO** — but needs Azure OpenAI as **judge** | `model_config` |
| Custom evaluators | **NO** | Purely user-defined |
| Simulator (adversarial) | Partially — needs Foundry project for attack generation | `azure_ai_project` |
| Red Team Agent | Partially — needs Foundry project | `azure_ai_project` |

**Key insight**: The `azure-ai-evaluation` package evaluates **data** (strings), not model endpoints. The evaluated model is never called by the SDK — you provide its outputs as input data.

---

## Finding 5: Azure AI Inference SDK — External Endpoints

The Azure AI Inference SDK (`azure-ai-inference`) is separate from evaluation. It was removed as a dependency from azure-ai-evaluation in v1.0.1. The evaluation SDK does NOT use the inference SDK to call models — it only processes pre-existing outputs.

For the cloud evaluation path (`azure-ai-projects`), the `azure_ai_model` target type specifically targets models deployed in Foundry/Azure OpenAI. There is no `custom_endpoint` target type for arbitrary external APIs.

---

## Finding 6: Foundry Custom Agent Registration for Anthropic

### Can you register an external Anthropic endpoint as a "custom agent"?

**YES** — Foundry Control Plane's custom agent registration supports:
- Any agent with an HTTP endpoint
- A2A protocol support
- OpenTelemetry instrumentation for diagnostics
- The agent runs on any infrastructure (Azure, AWS, on-prem, etc.)

**Registration requirements**:
- Agent URL (the endpoint where the agent runs)
- Protocol: HTTP or A2A
- Optional: OpenTelemetry Agent ID for trace correlation
- A Foundry project with AI Gateway and Application Insights

**What you get after registration**:
- Foundry-proxied URL for the agent
- Request logging in Application Insights
- Ability to block/unblock the agent
- Monitoring dashboard integration
- Continuous evaluation capability

**Important**: Foundry acts as a proxy (via API Management). The original auth mechanism still applies — Foundry doesn't handle Anthropic API key auth for you.

**For Biotrackr specifically**: The Chat API (which wraps Claude via Microsoft Agent Framework) could be registered as a custom agent in Foundry, enabling full monitoring and continuous evaluation through Foundry's dashboard.

---

## Finding 7: Foundry Connections — External Anthropic Endpoint

Foundry "connections" are primarily for connecting Azure resources (Azure OpenAI, Application Insights, Storage, etc.). There is no documented "Anthropic" connection type in the Foundry connections framework. However:

- Custom agents registered via Control Plane effectively create a managed proxy endpoint
- The custom agent registration is the correct mechanism for external model endpoints, not the "connections" feature

---

## Summary: YES/NO Decision Matrix

| Feature | Works with external (non-Foundry) models? | Requirements/Workaround |
|---|---|---|
| **Evaluation (NLP metrics)** | **YES** | None — pure Python math |
| **Evaluation (AI quality metrics)** | **YES** | Azure OpenAI as judge model (not the evaluated model) |
| **Evaluation (Safety metrics)** | **YES** | Foundry project for RAI backend service |
| **Evaluation (Cloud batch — dataset)** | **YES** | Pre-compute Claude outputs → JSONL → upload → evaluate |
| **Evaluation (Cloud batch — model target)** | **NO** | Requires Foundry-deployed model |
| **Evaluation (Cloud batch — agent target)** | **NO** | Requires Foundry Agent |
| **Evaluation (Continuous)** | **YES** (with registration) | Register custom agent → continuous eval rules |
| **Monitoring Dashboard** | **YES** | Register custom agent + OTel instrumentation |
| **Tracing** | **YES** | OpenTelemetry + Application Insights |
| **Custom Agent Registration** | **YES** | HTTP endpoint + Foundry project with AI Gateway |
| **Red Teaming (cloud)** | **NO** | Requires Foundry model/agent target |
| **Red Teaming (local SDK)** | **YES** (with callback) | Use `RedTeam` with callback to your own endpoint |
| **Adversarial Simulation** | **YES** (with callback) | Use `Simulator` with callback to your own endpoint |

---

## SDKs and Packages Involved

| Package | Purpose | External Model Support |
|---|---|---|
| `azure-ai-evaluation` (v1.16.2) | Local evaluation SDK — evaluators, evaluate(), simulator, red team | YES — evaluates data, not endpoints |
| `azure-ai-projects` (v2.0+) | Cloud evaluation SDK — batch evaluation in Foundry infra | Partial — dataset eval YES, model/agent targets NO |
| `azure-monitor-opentelemetry-exporter` | Send OTel traces to Application Insights | YES — model-agnostic |
| `opentelemetry-sdk` | OpenTelemetry instrumentation | YES — model-agnostic |
| `langchain-azure-ai` | LangChain/LangGraph tracing integration | YES — model-agnostic |
| `azure-core-tracing-opentelemetry` | Azure SDK tracing plugin | YES — model-agnostic |

---

## Recommended Architecture for Biotrackr (Claude via Anthropic API)

1. **Tracing**: Instrument Biotrackr Chat API with OpenTelemetry → export to Application Insights connected to Foundry project. The existing OpenTelemetry instrumentation in the MCP Server is a good starting point.

2. **Evaluation (pre-production)**: Use `azure-ai-evaluation` locally:
   - Collect Claude responses from Chat API into JSONL
   - Run NLP + AI-quality evaluators (with Azure OpenAI as judge)
   - Run safety evaluators (with Foundry project for RAI service)
   - Upload results to Foundry portal for visualization

3. **Evaluation (cloud batch)**: Use `azure-ai-projects` Dataset evaluation:
   - Same JSONL approach, but run in Foundry cloud infrastructure
   - Results viewable in Foundry portal with comparison features

4. **Monitoring (production)**: Register Chat API as custom agent in Foundry Control Plane:
   - Get proxied URL via API Management
   - Monitoring dashboard with latency, errors, token usage
   - Enable continuous evaluation on sampled responses

5. **Red teaming**: Use local `RedTeam` class from `azure-ai-evaluation`:
   - Implement callback that calls Claude via your Chat API
   - Runs adversarial attacks and evaluates responses locally

---

## References

- [Observability in Generative AI (Concepts)](https://learn.microsoft.com/en-us/azure/foundry/concepts/observability)
- [Run Evaluations from the SDK](https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/cloud-evaluation)
- [Monitor Agents Dashboard](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/how-to-monitor-agents-dashboard)
- [Agent Tracing Overview](https://learn.microsoft.com/en-us/azure/foundry/observability/concepts/trace-agent-concept)
- [Set Up Tracing](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/trace-agent-setup)
- [Configure Tracing for AI Agent Frameworks](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/trace-agent-framework)
- [Register and Manage Custom Agents](https://learn.microsoft.com/en-us/azure/foundry/control-plane/register-custom-agent)
- [View Evaluation Results](https://learn.microsoft.com/en-us/azure/foundry/how-to/evaluate-results)
- [Run Evaluations from Portal](https://learn.microsoft.com/en-us/azure/foundry/how-to/evaluate-generative-ai-app)
- [azure-ai-evaluation on PyPI](https://pypi.org/project/azure-ai-evaluation/)
- [azure-ai-evaluation on GitHub](https://github.com/Azure/azure-sdk-for-python/tree/main/sdk/evaluation/azure-ai-evaluation)
- [Azure AI Evaluation API Reference](https://learn.microsoft.com/en-us/python/api/overview/azure/ai-evaluation-readme)
