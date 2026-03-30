# Research: Azure AI Foundry Project Without Model Deployment

## Research Topics & Questions

1. What Azure AI Foundry resources/features can be created and used WITHOUT deploying any model?
2. Can a .NET solution using Claude Sonnet 4.6 via Anthropic API directly create a Foundry project purely for operational tooling?
3. What are the minimum Azure resources required for a Foundry project?
4. Can connections be created to external (non-Azure) APIs like Anthropic?
5. Do Foundry evaluators (GroundednessEvaluator, RelevanceEvaluator, CoherenceEvaluator) require a judge model deployed in Foundry?
6. Can AIProjectClient work with a project that has no model deployments?
7. Cost implications of a Foundry project with no model deployments?
8. Does Foundry's Application Insights integration work independently of model deployment?

---

## Key Findings

### 1. What is Microsoft Foundry and Its Components?

**Source:** https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-ai-foundry

Microsoft Foundry (formerly Azure AI Studio / Azure AI Foundry) is a unified Azure PaaS for enterprise AI operations. Key points:

- **Resource Model**: A single "Foundry resource" (`Microsoft.CognitiveServices/account` with kind `AIServices`) containing projects as child resources.
- **Projects**: Development boundaries inside the Foundry resource for organizing agents, evaluations, and files.
- **The platform is free to use and explore. Pricing occurs at the deployment level.** This is a critical statement — creating the resource and project itself has no cost.
- SDKs available for Python, C#, JavaScript/TypeScript (preview), Java (preview).

### 2. Project Creation Requirements

**Source:** https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/create-projects

Minimum requirements to create a Foundry project:

- An Azure account with an active subscription (free tier works)
- A role that allows creating a Foundry resource (Azure Account AI Owner or Azure AI Owner)
- The portal automatically creates a `Foundry` resource when you create the project
- **No model deployment is required to create a project**
- A project gets an endpoint and API key automatically
- Projects support: Model inference, Playgrounds, Agents, **Evaluations**, **Tracing**, Datasets, Indexes, Foundry SDK/API, Connections

### 3. Connections to External APIs (Anthropic)

**Source:** https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/connections-add

**YES — Foundry supports connecting to non-Azure APIs.** Relevant connection types:

| Connection Type | Description | Relevance |
|---|---|---|
| **API key** | Handle authentication to your specified target on an individual basis | **Direct fit for Anthropic API key** |
| **Custom key** | Securely store and access keys with related properties (targets, versions). Useful when you have many targets or don't need a credential to access the target. LangChain scenarios are a common example. | **Alternative for Anthropic** |
| **OpenAI** | Connect to your OpenAI models (non-Azure) | Shows external model provider support |
| Application Insights | Detect performance anomalies, diagnose issues | Observability tooling |
| Azure Key Vault | Securely store and access secrets | Secret management |

**Conclusion:** You can store your Anthropic API key as an **API key connection** or **Custom key connection** in Foundry. This provides secure secret management without deploying any Azure models.

### 4. Foundry Architecture and Required Resources

**Source:** https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/architecture

The Foundry resource is `Microsoft.CognitiveServices/account` with kind `AIServices`. Key architecture points:

- **Foundry resource**: Top-level Azure resource for governance (networking, security, model deployments)
- **Project**: Development boundary (child resource) for building and evaluating use cases
- **Managed Key Vault**: Foundry uses a managed Key Vault (not visible in your subscription) for connection secrets
- **Managed storage**: Microsoft-managed storage for file uploads (no customer storage required for basic setup)

**Minimum Azure resources for a basic Foundry project:**

1. **Foundry resource** (`Microsoft.CognitiveServices/account`, kind: `AIServices`) — **REQUIRED**
2. **Foundry project** (child resource) — **REQUIRED**
3. **Resource Group** — **REQUIRED** (standard Azure requirement)

**Optional resources (not auto-provisioned):**

- Application Insights — for tracing (connected manually)
- Storage Account — for evaluation logging to Foundry portal
- Azure AI Search — for Standard Agent deployment only
- Azure Cosmos DB — for Standard Agent deployment only
- Azure OpenAI — only if you want Azure-hosted models

**No Azure OpenAI resource or model deployment is required to create and use a Foundry project.**

### 5. Evaluators and Judge Model Requirements — CRITICAL FINDING

**Source:** https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk

**This is the most important finding for the user's scenario.**

The evaluators are split into categories with DIFFERENT model requirements:

#### Evaluators that REQUIRE a GPT judge model (`model_config`):

These "AI-assisted quality evaluators" need a GPT model deployment (Azure OpenAI or OpenAI) to act as a judge:

- **CoherenceEvaluator** — needs `model_config` with GPT deployment
- **FluencyEvaluator** — needs `model_config` with GPT deployment
- **GroundednessEvaluator** — needs `model_config` with GPT deployment
- **RelevanceEvaluator** — needs `model_config` with GPT deployment
- **SimilarityEvaluator** — needs `model_config` with GPT deployment
- **RetrievalEvaluator** — needs `model_config` with GPT deployment
- **IntentResolutionEvaluator** — needs `model_config` with GPT deployment
- **ToolCallAccuracyEvaluator** — needs `model_config` with GPT deployment
- **TaskAdherenceEvaluator** — needs `model_config` with GPT deployment
- **ResponseCompletenessEvaluator** — needs `model_config` with GPT deployment
- **QAEvaluator** (composite) — needs `model_config`

**The model_config supports `AzureOpenAIModelConfiguration` OR `OpenAIModelConfiguration`** — meaning you could use an **OpenAI API key directly** (not Azure-hosted) as the judge model. You do NOT need a model deployed in Foundry itself.

```python
# Azure OpenAI config (requires Azure OpenAI deployment):
model_config = AzureOpenAIModelConfiguration(
    azure_endpoint=os.environ.get("AZURE_ENDPOINT"),
    api_key=os.environ.get("AZURE_API_KEY"),
    azure_deployment=os.environ.get("AZURE_DEPLOYMENT_NAME"),
)

# OR OpenAI config (uses OpenAI API directly, no Azure deployment needed):
model_config = OpenAIModelConfiguration(...)
```

**Important caveat:** The docs say "you must specify a GPT model (`gpt-35-turbo`, `gpt-4`, `gpt-4-turbo`, `gpt-4o`, or `gpt-4o-mini`)" — these are specifically OpenAI GPT models. **Anthropic Claude is NOT supported as a judge model** for the built-in evaluators.

#### Evaluators that REQUIRE `azure_ai_project` (Foundry backend service):

These use the Azure AI Content Safety backend service (no user-deployed model needed, but DO need a Foundry project):

- **GroundednessProEvaluator** — uses Content Safety backend
- **ViolenceEvaluator** — uses Content Safety backend
- **SexualEvaluator** — uses Content Safety backend
- **SelfHarmEvaluator** — uses Content Safety backend
- **HateUnfairnessEvaluator** — uses Content Safety backend
- **ProtectedMaterialEvaluator** — uses Content Safety backend
- **ContentSafetyEvaluator** (composite) — uses Content Safety backend
- **IndirectAttackEvaluator** — uses Content Safety backend

**These work without any model deployment** — they call Azure AI Content Safety as a service.

#### Evaluators that need NO model at all (NLP/mathematical):

- **F1ScoreEvaluator** — pure math
- **BleuScoreEvaluator** — pure math
- **GleuScoreEvaluator** — pure math
- **RougeScoreEvaluator** — pure math
- **MeteorScoreEvaluator** — pure math
- **CodeVulnerabilityEvaluator** — rules-based
- **UngroundedAttributesEvaluator** — needs ground_truth + response only

### 6. AIProjectClient Without Model Deployments

**Source:** https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/trace-local-sdk and evaluation docs

The `AIProjectClient` (Python SDK) can work with a project that has no model deployments. Evidence:

- `project_client.telemetry.get_application_insights_connection_string()` — works independently of models
- Evaluation results can be logged to the project via `azure_ai_project` parameter
- Tracing/observability features work independently
- Connection management works independently

**However**, calling `project_client.get_openai_client()` would fail if no OpenAI-compatible model is deployed.

For the .NET SDK (`Azure.AI.Projects`), the equivalent `AIProjectClient` would similarly support:
- Connections management
- Telemetry/tracing configuration
- Evaluation result logging

### 7. Cost Implications

**Source:** https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/manage-costs

**Key statement from docs: "The platform is free to use and explore. Pricing occurs at the deployment level."**

Cost breakdown for a model-free Foundry project:

| Resource | Cost |
|---|---|
| Foundry resource (no deployments) | **$0/month** |
| Foundry project | **$0/month** |
| Custom/API key connections | **$0/month** |
| Azure Application Insights | **Pay-per-use** (ingestion: ~$2.30/GB after 5GB free/month) |
| Log Analytics workspace | **Pay-per-use** (bundled with App Insights) |
| Storage Account (for eval logging) | **Pay-per-use** (pennies for small usage) |

**Costs that would accrue:**

- If you use Content Safety evaluators (GroundednessProEvaluator, etc.), you pay for Azure AI Content Safety API calls through the Foundry resource
- If you connect Application Insights, standard App Insights/Log Analytics ingestion costs apply
- If you use AI-assisted evaluators with `OpenAIModelConfiguration`, you pay OpenAI directly (not Azure)
- If you use AI-assisted evaluators with `AzureOpenAIModelConfiguration`, you need an Azure OpenAI deployment (costs for that deployment)

**Bottom line: A bare Foundry project with no model deployments and no connected services = $0/month.**

### 8. Application Insights Integration

**Source:** https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/visualize-traces

**YES — Application Insights works independently of model deployment.**

- Foundry stores traces in Azure Application Insights using OpenTelemetry
- New Foundry resources do NOT auto-provision Application Insights — you connect one manually
- Once connected, tracing works for any project within the resource
- You can send OpenTelemetry traces from ANY application (not just Azure OpenAI)
- The tracing infrastructure uses standard OTLP — fully compatible with the existing Biotrackr OpenTelemetry setup

**Key insight for Biotrackr:** The existing Application Insights instance could be connected to a Foundry project, and traces from the Chat.Api (which calls Anthropic Claude) could be visualized in the Foundry tracing UI alongside evaluation results.

---

## Feature Availability Matrix: With vs. Without Model Deployment

| Feature | No Model Deployed | Requires Model |
|---|---|---|
| Create Foundry resource | YES | — |
| Create Foundry project | YES | — |
| API key / Custom key connections | YES | — |
| Application Insights connection | YES | — |
| Tracing / OpenTelemetry | YES | — |
| Datasets | YES | — |
| Indexes | YES (with AI Search) | — |
| Evaluation logging to portal | YES | — |
| Content Safety evaluators | YES (uses backend service) | — |
| NLP evaluators (BLEU, ROUGE, etc.) | YES | — |
| Quality evaluators (Coherence, etc.) | — | YES (GPT judge model) |
| GroundednessProEvaluator | YES (uses Content Safety) | — |
| Playground (chat) | — | YES |
| Agent Service | — | YES (for LLM backbone) |
| Model inference | — | YES |
| Fine-tuning | — | YES |

---

## Practical Scenarios for Biotrackr

### What you CAN do with a model-free Foundry project:

1. **Centralized tracing**: Connect existing Application Insights to see AI traces in Foundry portal
2. **Content Safety evaluation**: Run GroundednessProEvaluator, ViolenceEvaluator, etc. against Claude responses without deploying any model
3. **NLP evaluation**: Run F1Score, BLEU, ROUGE, METEOR against Claude responses locally
4. **Secret management**: Store Anthropic API key as an API key connection (alternative to Key Vault)
5. **Evaluation result tracking**: Log evaluation runs to the Foundry portal for visualization
6. **Dataset management**: Upload and manage evaluation datasets

### What you CANNOT do without a model deployment:

1. **AI-assisted quality evaluation** with built-in evaluators (Coherence, Fluency, Relevance, Groundedness) — these specifically need a GPT model as judge
2. **Playground**: Cannot test prompts in the Foundry portal chat playground
3. **Agent Service**: Cannot use Foundry's hosted agent infrastructure

### Workarounds for AI-assisted evaluation:

**Option A: Use OpenAI API directly as judge** — The evaluation SDK supports `OpenAIModelConfiguration` which calls OpenAI's API directly. You pay OpenAI, not Azure. No Azure OpenAI deployment needed.

**Option B: Deploy a cheap Azure OpenAI model as judge only** — Deploy `gpt-4o-mini` in the Foundry resource solely for evaluation. Cost: ~$0.15/1M input tokens, $0.60/1M output tokens. Minimal cost for periodic evaluation runs.

**Option C: Write custom evaluators** — The evaluation SDK supports custom Python callable evaluators. You could write evaluators that call Claude as the judge model instead of GPT. (Note: This is custom code, not using the built-in prompts.)

---

## Minimum Azure Resources for a "Tooling-Only" Foundry Project

```text
Resource Group
├── Foundry Resource (Microsoft.CognitiveServices/account, kind: AIServices)  [$0]
│   └── Foundry Project (child resource)                                       [$0]
├── Application Insights (optional, for tracing)                               [~$2.30/GB after 5GB free]
│   └── Log Analytics Workspace                                                [bundled with App Insights]
└── Storage Account (optional, for eval logging)                               [pennies]
```

**Total baseline cost with no model deployment: $0/month** (plus small App Insights ingestion if tracing is active)

---

## Clarifying Questions

1. **Does the .NET `Azure.AI.Projects` SDK support the same evaluation capabilities as the Python SDK?** The evaluation SDK (`azure-ai-evaluation`) is currently Python-only. The .NET SDK (`Azure.AI.Projects`) focuses on project management, connections, and agent interactions. For evaluation in a .NET project, you would need to either call the Python evaluation SDK from a separate process/script or use the REST API directly.

2. **Can Foundry tracing visualize traces from non-OpenAI LLM calls (e.g., Anthropic)?** The tracing infrastructure is standard OpenTelemetry. If the Biotrackr Chat.Api already exports OTEL traces to Application Insights, those traces would appear in the Foundry tracing UI automatically once App Insights is connected. The Foundry UI provides AI-specific trace visualization (gen_ai semantic conventions) but should work with any OTEL data.

---

## References

- [What is Microsoft Foundry?](https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-ai-foundry)
- [Create a project for Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/create-projects)
- [Add a new connection to your project](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/connections-add)
- [Microsoft Foundry architecture](https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/architecture)
- [Plan and manage costs for Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/manage-costs)
- [Evaluate with Azure AI Evaluation SDK](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk)
- [azure-ai-evaluation Python package API](https://learn.microsoft.com/en-us/python/api/azure-ai-evaluation/azure.ai.evaluation)
- [View trace results for AI applications (classic)](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/visualize-traces)
