# Foundry GenAIOps Modules Research: External Model Compatibility

## Research Questions

1. Can Microsoft Foundry (Azure AI Foundry) be used for prompt versioning, monitoring, evaluation, and tracing WITHOUT deploying a model within Foundry?
2. Specifically, when using Claude Sonnet models directly via Anthropic API (not through Foundry's model catalog), which Foundry GenAIOps features can still be leveraged?
3. For each module in the learning path, what are the exact dependencies on Foundry-deployed models?

## Source Material

- Learning Path: https://learn.microsoft.com/en-us/training/paths/operationalize-gen-ai-apps/
- All 5 module overview pages and their individual unit pages (30+ pages fetched)
- Azure AI Evaluation SDK docs: https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk
- Built-in evaluators reference: https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/built-in-evaluators
- Tracing SDK docs: https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/trace-local-sdk

---

## Module 1: Prompt Versioning (prompt-versioning-genaiops)

**Module**: "Manage prompts for agents in Microsoft Foundry with GitHub"

### Summary

This module teaches version control for AI prompts using Git/GitHub. Prompts are stored as `.txt` or `.md` files in Git repositories, and the workflow uses branches, PRs, tags, and CI/CD pipelines.

### Foundry-Deployed Model Dependency: PARTIAL (Agent Service only)

**What requires Foundry:**
- The `AIProjectClient.agents.create_agent()` call to deploy agents programmatically requires a Foundry project and a model deployed in the Foundry model catalog
- Agent versioning within Foundry (automatic version numbering) is Foundry-specific
- The Python SDK code uses `project_client = AIProjectClient.from_connection_string(conn_str=..., credential=DefaultAzureCredential())` and `project_client.agents.create_agent(model=..., name=..., instructions=...)`

**What is model-agnostic (transferable to any model):**
- Prompt file management as `.txt`/`.md` in Git repositories — fully transferable
- Git branching strategies (feature/experiment/hotfix) — fully transferable
- PR-based review workflows — fully transferable
- Version tagging with Git tags — fully transferable
- Repository structure patterns (`src/agents/<name>/prompts/v1_instructions.txt`) — fully transferable
- Deployment lifecycle stages (Development → Validation → Review → Production → Monitoring) — fully transferable

### Assessment for Biotrackr (Claude via Anthropic API)

**Usable without Foundry models**: ~70% of the module content. The prompt versioning practices, Git workflows, PR review process, and file organization patterns are completely model-independent. These are software engineering best practices applied to prompts.

**NOT usable without modification**: The Foundry agent deployment scripts (`agents.create_agent()`) would NOT work. Biotrackr uses the Microsoft Agent Framework with Claude directly, not Foundry Agent Service. The deployment automation would need to be adapted from "deploy a Foundry agent" to "update system prompt file and redeploy the app service."

---

## Module 2: Evaluate & Optimize Agents (evaluate-optimize-agents)

**Module**: "Evaluate and optimize AI agents through structured experiments"

### Summary

This module covers designing evaluation experiments with quality/cost/performance metrics, using Git-based workflows for A/B testing, creating evaluation rubrics, and making evidence-based decisions. It is heavily methodology-focused.

### Foundry-Deployed Model Dependency: MINIMAL

**What requires Foundry:**
- Module mentions "Microsoft Foundry provides built-in evaluators" for quality metrics
- Example scenarios reference "GPT-4" and "GPT-4 mini" as models to compare
- Mentions the Microsoft Foundry portal for comparing agent versions

**What is model-agnostic (transferable to any model):**
- Evaluation experiment design (defining metrics, selecting variants, systematic testing approaches) — **fully transferable**
- Three evaluation dimensions: quality (Intent Resolution, Relevance, Groundedness), cost (token usage, pricing), performance (response time, time-to-first-token) — all applicable to Claude
- Git-based experimentation workflow (experiment branches, test prompt files, response capture, evaluation CSV scoring) — **fully transferable**
- Evaluation rubrics (1-5 scale scoring criteria with concrete examples) — **fully transferable**
- Inter-rater reliability testing for human evaluators — **fully transferable**
- Manual scoring methodology with `evaluation.csv` format — **fully transferable**
- Experiment comparison and merge decisions — **fully transferable**

### Assessment for Biotrackr (Claude via Anthropic API)

**Usable without Foundry models**: ~95% of the module content. The module is primarily about evaluation methodology, not tooling. The experiment branches, rubrics, scoring, and comparison workflows apply to any model. Instead of `python agent.py` deploying a Foundry agent, you would run your Anthropic-powered agent via the Chat API and capture its responses to test prompts.

**Adaptation required**: Replace references to "GPT-4" and "GPT-4 mini" with "Claude Sonnet 4.6" model variants. Replace "deploy agent version in Microsoft Foundry" with "update system prompt and restart Chat API."

---

## Module 3: Automated Evaluations (automated-evaluation-genaiops)

**Module**: "Automate AI evaluations with Microsoft Foundry and GitHub Actions"

### Summary

This module covers using Foundry's built-in evaluators (IntentResolution, Relevance, Groundedness, etc.) programmatically via the `azure-ai-evaluation` SDK, creating evaluation datasets (JSONL), running batch evaluations, and integrating into GitHub Actions CI/CD.

### Foundry-Deployed Model Dependency: HIGH (for AI-assisted evaluators)

**Critical finding — Two layers of model dependency:**

1. **The model being EVALUATED** — Can be ANY model. Evaluators work on `{query, response, context, ground_truth}` data. You can collect query/response pairs from Claude via Anthropic API, format as JSONL, and feed them to the evaluation SDK. **No Foundry model needed for the evaluated model.**

2. **The JUDGE model (evaluator model)** — AI-assisted quality evaluators require a GPT model deployment:
   - Doc states: "For AI-assisted quality evaluators, except for `GroundednessProEvaluator`, you must specify a GPT model (`gpt-35-turbo`, `gpt-4`, `gpt-4o`, or `gpt-4o-mini`) in your `model_config`."
   - The `model_config` uses `AzureOpenAIModelConfiguration(azure_endpoint=..., api_key=..., azure_deployment=...)`
   - Safety/risk evaluators require `azure_ai_project` information (Foundry project)
   - **This means you need an Azure OpenAI deployment for the judge, even if your app model is Claude**

**Specific SDK requirements:**
- `azure-ai-evaluation` >= 2.0.0b1 for cloud evaluations
- `azure-ai-projects` SDK with `DefaultAzureCredential()`
- Cloud evaluations use `project_client.get_openai_client()` (OpenAI-compatible client)
- Batch evaluations specify `initialization_parameters: { "deployment_name": model_deployment_name }` — this is the JUDGE model

**What is model-agnostic:**
- Evaluation dataset creation (JSONL format) — **fully transferable** (just need query/response pairs)
- Custom evaluators (`EvaluatorBase` subclass) — **fully transferable** (pure Python, no model dependency)
- NLP evaluators (F1Score, BLEU, GLEU, ROUGE, METEOR) — these are **mathematical, no model needed**
- Shadow rating workflow (comparing human scores to automated scores) — **methodology transferable**
- GitHub Actions workflow for CI/CD evaluation — **framework transferable** (adapt the evaluation script)
- Human evaluation rubrics and inter-rater reliability — **fully transferable**

### Assessment for Biotrackr (Claude via Anthropic API)

**Partially usable without Foundry-deployed app model**: You CAN evaluate Claude's outputs using Foundry evaluators, but you need:
1. An Azure OpenAI deployment specifically for the JUDGE model (e.g., GPT-4o-mini) — this is NOT your app model, just the evaluator
2. A Foundry project for safety evaluators and cloud evaluation runs
3. Collect query/response pairs from your Claude-powered Chat API, format as JSONL, and pass to the SDK

**Architecture implication**: Biotrackr would need a small Azure OpenAI deployment (GPT-4o-mini) purely for evaluation purposes, even though the production app uses Claude. This is a common pattern — the evaluator model does not need to be the same as the production model.

**NLP evaluators (BLEU, ROUGE, F1, etc.)** work without any model deployment at all — they are mathematical comparisons.

**Custom evaluators** (e.g., checking health data domain accuracy) work without Azure OpenAI — they are pure Python.

---

## Module 4: Monitor Generative AI Application (monitor-generative-ai-app)

**Module**: "Monitor your generative AI application"

### Summary

This module covers monitoring key metrics (latency, throughput, token usage, error rates) using Azure Monitor, Application Insights, and OpenTelemetry. It covers integrating monitoring into code using the Foundry SDK.

### Foundry-Deployed Model Dependency: HIGH (for auto-instrumentation)

**What requires Foundry:**
- The module explicitly states: "To generate monitoring data that is captured by Application Insights and visualized in Azure Monitor, you need to run a service you deployed through the Microsoft Foundry"
- Code uses `project.inference.get_chat_completions_client()` — requires Foundry-deployed model
- `AIInferenceInstrumentor().instrument()` — auto-instruments Azure AI inference calls only
- `project.telemetry.get_connection_string()` — gets App Insights connection from Foundry project
- The "how to monitor" section states: "You can trace any AI model supporting the Azure AI model inference API" — Claude via Anthropic does NOT support this API
- Deployment configurations focus on Azure VM sizes and Foundry-managed compute

**What is model-agnostic (transferable):**
- The four core metrics (latency, throughput, token usage, error rates) — **conceptually universal**
- OpenTelemetry tracer usage with custom spans — **fully transferable**
- Azure Monitor Application Insights integration — **usable directly** (Application Insights can receive ANY OpenTelemetry data)
- Custom dashboards and alerting in Azure Monitor — **fully transferable**
- Workbook visualization — **fully transferable** (as long as data is in App Insights)
- The monitoring feedback loop methodology — **fully transferable**

### Assessment for Biotrackr (Claude via Anthropic API)

**Partially usable**: The auto-instrumentation (`AIInferenceInstrumentor`) does NOT work for Anthropic API calls. HOWEVER:
- Biotrackr already uses Application Insights and OpenTelemetry (via .NET instrumentation)
- Custom spans can be created manually around Anthropic API calls to capture the same metrics
- Azure Monitor dashboards and alerts work regardless of model provider
- Token usage tracking requires manual extraction from Anthropic API responses (Claude returns `usage.input_tokens` and `usage.output_tokens`)

**What Biotrackr already has**: Per the repo architecture, Biotrackr already has Application Insights, OpenTelemetry instrumentation on the MCP Server, and Azure Monitor alerts. The module's concepts are already implemented in a model-agnostic way.

**NOT available**: The Foundry portal's "Insights for Generative AI applications" prebuilt workbook likely expects gen_ai.* semantic conventions from the Azure AI Tracing package. Custom workbooks could replicate this.

---

## Module 5: Tracing (tracing-generative-ai-app)

**Module**: "Analyze and debug your generative AI app with tracing"

### Summary

This module covers implementing distributed tracing using OpenTelemetry, creating custom spans, debugging complex AI workflows, and analyzing trace data in the Foundry portal.

### Foundry-Deployed Model Dependency: MODERATE

**What requires Foundry:**
- `opentelemetry-instrumentation-openai-v2` — Auto-traces OpenAI SDK calls ONLY. Does NOT auto-trace Anthropic SDK calls
- `project_client.inference.get_chat_completions_client()` — requires Foundry project with deployed model
- Prerequisites state: "A Microsoft Foundry project with an associated Azure Application Insights resource"
- Tracing SDK docs prerequisite: "An AI application that uses OpenAI SDK to make calls to models hosted in Foundry"
- Viewing traces in Foundry portal requires a Foundry project

**What is model-agnostic (transferable):**
- OpenTelemetry concepts (traces, spans, attributes) — **fully universal standard**
- Custom span creation with `tracer.start_as_current_span()` — **fully transferable**
- Model call wrapper pattern with timing and metadata — **fully transferable** (wrap Anthropic calls instead of OpenAI ones)
- Business logic tracing — **fully transferable**
- Session-level tracing — **fully transferable**
- Trace hierarchy design — **fully transferable**
- Error handling patterns in traces — **fully transferable**
- Trace data analysis (quality, performance, reliability) — **fully transferable**
- Console-based tracing for CI/CD — **fully transferable** (use `ConsoleSpanExporter`)
- Exporting to Azure Monitor via `configure_azure_monitor()` — **works without Foundry** (just needs Application Insights connection string directly)

### Assessment for Biotrackr (Claude via Anthropic API)

**Largely usable with adaptations**:
- Replace `opentelemetry-instrumentation-openai-v2` (OpenAI auto-instrumentation) with manual OpenTelemetry spans around Anthropic API calls
- The `.NET` equivalent: Biotrackr already uses `System.Diagnostics.Activity` (OpenTelemetry-compatible) in the MCP Server's `BaseTool` class
- Custom spans can capture: prompt content, response content, token usage, latency, model name — all extractable from Anthropic response objects
- Export to Application Insights works the same way (just need the connection string, not necessarily via Foundry project client)
- Trace viewing can be done in Application Insights directly in Azure portal (doesn't require Foundry portal)

**Key gap**: No auto-instrumentation for Anthropic SDK. Every model call needs manual span creation. In .NET (Biotrackr's stack), this is already handled via `ActivitySource` in the Microsoft Agent Framework middleware.

---

## Cross-Cutting Analysis

### What definitely works with external models (Claude via Anthropic)

| Feature | Works? | Notes |
|---------|--------|-------|
| Prompt versioning in Git | YES | Model-independent, just file management |
| Git-based evaluation workflows | YES | Just capture query/response from any model |
| Human evaluation rubrics | YES | Methodology is universal |
| NLP evaluators (BLEU, ROUGE, F1) | YES | Mathematical, no model needed |
| Custom Python evaluators | YES | Pure Python, any input |
| OpenTelemetry custom spans | YES | Universal standard |
| Azure Monitor / App Insights | YES | Accepts any OpenTelemetry data |
| Custom dashboards / alerts | YES | Works with any telemetry source |
| GitHub Actions CI/CD | YES | Framework-agnostic |
| Evaluation dataset format (JSONL) | YES | Just query/response pairs |

### What requires an Azure OpenAI deployment (but NOT your production model)

| Feature | Requirement | Notes |
|---------|-------------|-------|
| AI-assisted quality evaluators | Azure OpenAI GPT model as JUDGE | GPT-4o-mini is cheapest option. Used to score responses, not generate them |
| Safety/risk evaluators | Foundry project + Azure AI Content Safety | Backend service, not a model deployment |
| Cloud batch evaluations | Foundry project + Azure OpenAI | For running evaluations at scale |
| Foundry portal trace viewing | Foundry project + App Insights | Can use App Insights directly instead |

### What does NOT work with external models

| Feature | Limitation | Workaround |
|---------|-----------|------------|
| `AIInferenceInstrumentor` auto-instrumentation | Only instruments Azure AI model inference API | Manual OpenTelemetry spans |
| `opentelemetry-instrumentation-openai-v2` | Only instruments OpenAI SDK calls | Manual OpenTelemetry spans |
| Foundry Agent Service (`agents.create_agent()`) | Requires Foundry-hosted agent | Use your own agent framework |
| Foundry portal "Gen AI Insights" workbook | Expects gen_ai.* semantic conventions from Azure SDK | Build custom workbook |
| Online evaluation (continuous eval on traces) | Requires traces from Foundry-instrumented apps | Can evaluate collected data offline |

---

## Specific Requirements for Biotrackr Scenario

### Current Architecture

- **App model**: Claude Sonnet 4.6 via Anthropic API
- **Framework**: Microsoft Agent Framework (ASP.NET Core)
- **Existing telemetry**: Application Insights, OpenTelemetry, Azure Monitor alerts
- **Transport**: AGUI over HTTP SSE

### Recommended Approach for Each Feature

**Prompt Versioning**: Already implemented via `scripts/chat-system-prompt/system-prompt.txt`. Could adopt the versioning patterns (v1, v2 naming, Git tags) from Module 1. No Foundry dependency needed.

**Evaluation**: Collect query/response pairs from Chat API, format as JSONL. Use `azure-ai-evaluation` SDK with a cheap Azure OpenAI deployment (GPT-4o-mini) as the judge model. ~$0.15/1M input tokens for evaluation judge.

**Monitoring**: Already have Application Insights + OpenTelemetry. Token usage from Anthropic can be extracted from response metadata and logged as custom metrics. Build custom Azure Workbook for gen AI metrics.

**Tracing**: Already have OpenTelemetry via .NET ActivitySource in MCP Server. Chat API middleware already captures conversation flow. Could enhance with gen_ai semantic conventions on custom spans.

### Minimal Azure Resources Needed (beyond what Biotrackr already has)

For full GenAIOps WITHOUT deploying Claude in Foundry:
1. **Azure OpenAI deployment** (GPT-4o-mini) — for evaluation judge model only (~$0.15/1M input tokens)
2. **Foundry project** (optional) — for cloud batch evaluations and portal visualization, or skip and use local evaluations + App Insights directly

---

## Key Discoveries

1. **Evaluators evaluate DATA, not deployments** — This is the most important finding. The query/response/context data format is model-agnostic. You can evaluate Claude outputs using Azure AI evaluators.

2. **The judge model and production model are separate concerns** — Built-in evaluators use a GPT model as a judge to score responses. This judge model is completely independent from your production model.

3. **OpenTelemetry is the universal bridge** — All monitoring and tracing in the modules is built on OpenTelemetry. Since OpenTelemetry is vendor-neutral, you can send trace data from any model to Application Insights.

4. **Auto-instrumentation is the main gap** — The `opentelemetry-instrumentation-openai-v2` and `AIInferenceInstrumentor` only work with OpenAI/Azure OpenAI SDK calls. For Anthropic, manual instrumentation is needed (which Biotrackr already has).

5. **Foundry portal is nice-to-have, not essential** — Trace viewing, metrics dashboards, and alerting can all be done via Application Insights and Azure Monitor directly, without the Foundry portal.

6. **Prompt versioning is fully model-agnostic** — The Git-based prompt management workflow has zero dependency on any specific model or platform.

## Clarifying Questions

1. Does Biotrackr plan to have any Azure OpenAI deployment at all? Even a small GPT-4o-mini for evaluation purposes would unlock the full evaluation SDK.
2. Is the Foundry portal visualization important, or is Application Insights + custom Azure Workbooks sufficient?
3. Is there interest in using the Foundry `datasets` API for managing evaluation datasets, or would a simpler Git-based approach suffice?

## References

- Learning Path Overview: https://learn.microsoft.com/en-us/training/paths/operationalize-gen-ai-apps/
- Module 1 (Prompt Versioning): https://learn.microsoft.com/en-us/training/modules/prompt-versioning-genaiops/
- Module 2 (Evaluate & Optimize): https://learn.microsoft.com/en-us/training/modules/evaluate-optimize-agents/
- Module 3 (Automated Evaluation): https://learn.microsoft.com/en-us/training/modules/automated-evaluation-genaiops/
- Module 4 (Monitoring): https://learn.microsoft.com/en-us/training/modules/monitor-generative-ai-app/
- Module 5 (Tracing): https://learn.microsoft.com/en-us/training/modules/tracing-generative-ai-app/
- Evaluation SDK: https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk
- Built-in Evaluators: https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/built-in-evaluators
- Tracing SDK: https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/trace-local-sdk
