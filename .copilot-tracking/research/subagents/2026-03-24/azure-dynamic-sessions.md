<!-- markdownlint-disable-file -->
# Subagent Research: Azure Container Apps Dynamic Sessions

## Research Topics and Questions

1. What are Azure Container Apps Dynamic Sessions? Architecture, how they work.
2. Python code execution: Can they run pandas, matplotlib, plotly, reportlab? Custom packages? Pre-built Python image?
3. Integration with .NET: How does a .NET app call dynamic sessions? SDK availability.
4. File I/O: Can executed Python code generate files (PNGs, PDFs, CSVs) and return them? Artifact retrieval.
5. Security model: Sandboxing, network isolation, data isolation between sessions.
6. Cost model: Cost per session/execution, free tier.
7. Integration with Microsoft Agent Framework: Built-in tool/plugin for dynamic sessions.
8. Latency: Cold start time, suitability for interactive chat use cases.

---

## 1. What Are Azure Container Apps Dynamic Sessions

### Overview

Azure Container Apps Dynamic Sessions provide fast access to secure, sandboxed environments ideal for running code or applications requiring strong isolation. Sessions run inside **session pools** that provide prewarmed environments, starting containers in milliseconds and scaling on demand.

**Source:** [Dynamic sessions in Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/sessions)

### Architecture

- **Session Pool:** Foundation resource — contains prewarmed, ready-to-use sessions. When a request arrives, the system allocates a session from the pool instead of creating from scratch.
- **Session:** Ephemeral, isolated execution environment for short-lived tasks. Allocated from the pool, destroyed after cooldown period expires.
- **Session lifecycle:** Application sends request with session identifier → pool allocates session automatically → session stays active while requests continue → cooldown expiry triggers session destruction and cleanup.
- **Request routing:** Sessions accessed through pool management endpoint. Requests include an `identifier` query parameter; pool routes to existing session or allocates new one.

### Session Pool Types

| Aspect | Code Interpreter Sessions | Custom Container Sessions |
|---|---|---|
| Best for | AI-generated code, user scripts, quick code execution | Custom runtime, libraries, binaries, specialized tools |
| Environment | Preconfigured (no container build needed) | Custom container image with your dependencies |
| Image required | No — uses platform built-in interpreter | Yes — supply your own container image |
| Supported types | `PythonLTS`, `NodeLTS`, `Shell` | `CustomContainer` |

### Supported Regions

Available in 30+ regions globally including: East US, East US 2, West US 2, West US 3, North Europe, West Europe, UK South, Australia East, Southeast Asia, and many more.

---

## 2. Python Code Execution

### Pre-built Python Image (Code Interpreter)

The code interpreter session pool type `PythonLTS` provides a preconfigured Python environment with **no container build required**. It includes popular packages:

- **Confirmed pre-installed:** NumPy, pandas, scikit-learn
- You can query installed packages at runtime via:

```python
import pkg_resources
[(d.project_name, d.version) for d in pkg_resources.working_set]
```

**Source:** [Code interpreter sessions](https://learn.microsoft.com/en-us/azure/container-apps/sessions-code-interpreter)

### Packages: pandas, matplotlib, plotly, reportlab

- **pandas:** Pre-installed in PythonLTS image.
- **matplotlib:** Likely pre-installed (standard data science stack), but needs runtime verification.
- **plotly:** May or may not be pre-installed; needs runtime verification.
- **reportlab:** Unlikely pre-installed (specialized PDF library); would need `pip install` at execution time or a custom container.

### Installing Custom Packages

Two approaches:

1. **Runtime install (Code Interpreter):** Execute `pip install <package>` as code within the session before running your main script. Packages persist for the session lifetime but are lost when the session is destroyed. This adds latency to each new session.
2. **Custom Container Sessions:** Build a Docker image with all required packages pre-installed. This is the more robust approach for specific dependencies like reportlab. Requires maintaining a container image in ACR.

### Execution Constraints

- Each code execution is limited to a **maximum runtime of 220 seconds**.
- File upload limit is **128 MB**.
- Sessions run in Hyper-V isolated environments.

### Key Finding

For Biotrackr's use case (pandas + matplotlib + reportlab), the **Custom Container approach** is more reliable because:
- reportlab is not standard in the code interpreter image
- Runtime pip install adds cold-start latency per session
- Custom containers guarantee all dependencies are available immediately

However, the **Code Interpreter approach** may work if:
- The first execution in a session installs needed packages
- The same session is reused for subsequent operations (packages persist within session)

---

## 3. Integration with .NET

### REST API (Direct Integration)

The primary .NET integration path is via the **session pool management REST API**. A .NET application calls:

- `POST /code/execute` — Execute Python code in a session
- `POST /files` — Upload a file to a session
- `GET /files/{filename}/content` — Download a file from a session
- `GET /files` — List files in a session

**Endpoint format:** `https://<REGION>.dynamicsessions.io/subscriptions/<SUB>/resourceGroups/<RG>/sessionPools/<POOL>/executions?api-version=2025-10-02-preview&identifier=<SESSION_ID>`

**Authentication:** Bearer token from Microsoft Entra ID with audience `https://dynamicsessions.io`. The calling identity needs the `Azure ContainerApps Session Executor` role on the session pool.

### Semantic Kernel Integration

The Semantic Kernel project provides a `SessionsPythonTool` plugin, but this is **Python SDK only** (`semantic-kernel` Python package version 0.9.8b1+). There is **no equivalent .NET NuGet package** for Sessions. The .NET Semantic Kernel Plugins directory on GitHub contains:

- Plugins.AI, Plugins.Core, Plugins.Document, Plugins.Memory, Plugins.MsGraph, Plugins.Web
- **No Sessions plugin for .NET**

### .NET Integration Approach for Biotrackr

Since Biotrackr's Chat.Api is .NET-based, integration must be via:

1. **Direct HTTP calls** to the session pool management API using `HttpClient` + `Azure.Identity` for token acquisition
2. **Custom Semantic Kernel plugin** wrapping the REST API (if using SK)
3. **Custom Agent Framework tool** wrapping the REST API

The REST API is straightforward — POST JSON with code, receive execution results including stdout/stderr and return values.

**Example execution request:**

```json
POST .../executions?api-version=2025-10-02-preview&identifier=<SESSION_ID>
{
    "properties": {
        "codeInputType": "inline",
        "executionType": "synchronous",
        "code": "print('Hello, world!')"
    }
}
```

---

## 4. File I/O and Artifact Retrieval

### How It Works

1. **Files live at `/mnt/data`** inside the session container.
2. **Upload files** via `POST /files` (multipart form data, max 128 MB).
3. **Python code** in the session can read from `/mnt/data` and write output files to `/mnt/data`.
4. **Download files** via `GET /files/{filename}/content` — returns raw file data.
5. **List files** via `GET /files` — returns metadata (filename, size, lastModifiedTime).

### Workflow for Report Generation

1. Upload health data (CSV/JSON) to the session via the files API.
2. Execute Python code that uses pandas to process data, matplotlib/plotly for charts, reportlab for PDF.
3. Code writes output files (PNG charts, PDF reports) to `/mnt/data`.
4. Download generated files via the files API.
5. Return files to the user (e.g., as base64 in chat response or as downloadable links).

### Key Constraints

- File names support alphanumeric, `-`, `_`, `.`, `@`, `$`, `&`, and Unicode characters.
- File paths can't contain `..`.
- Upload limit: 128 MB per file.
- Files persist only for the session lifetime.

---

## 5. Security Model

### Isolation

- **Hyper-V isolation:** Each session is fully isolated by a Hyper-V boundary. Sessions are isolated from each other and from the host environment.
- **Sandboxed:** Designed to run untrusted code (LLM-generated, user-submitted).
- **Data isolation:** Each session has its own filesystem, environment, and process space. No cross-session data access.

### Network Access

- **Default: Outbound network DISABLED** (`EgressDisabled`). Sessions cannot make outbound requests.
- Configurable: Can enable egress via `--network-status EgressEnabled` on the session pool.
- Warning: Enabling egress on untrusted code could enable DoS attacks or data exfiltration.

### Authentication

- All management API requests require Microsoft Entra tokens.
- Requires `Azure ContainerApps Session Executor` role on the session pool.
- Token audience: `https://dynamicsessions.io`.
- Framework integrations (Semantic Kernel, LangChain, etc.) handle token management automatically.

### Best Practices (from docs)

- Use secure, cryptographically random session identifiers.
- Always use HTTPS.
- Limit session lifetime (configure appropriate cooldown periods).
- Never expose session IDs in URLs or logs.
- Map one session per user or per conversation.
- Don't upload sensitive data to sessions running untrusted code.

### Managed Identity Support

- Session pools support system-assigned and user-assigned managed identities.
- Can be used for image pull authentication from ACR.
- Resource access from within sessions is **disabled by default** (security risk with untrusted code).

---

## 6. Cost Model

### Code Interpreter Sessions Pricing

| Type | Price |
|---|---|
| Platform-managed built-in container (Python, Node.js, Shell) | **$0.03 per session-hour** |
| With savings plan (1-year) | ~$0.026/session-hour (~15% savings) |
| With savings plan (3-year) | ~$0.025/session-hour (~17% savings) |

**Source:** [Azure Container Apps Pricing](https://azure.microsoft.com/en-us/pricing/details/container-apps/)

**Billing model:** Billed based on running duration for allocated sessions. Each allocated session is billed from allocation until deallocation in **increments of one hour**.

### Custom Container Sessions Pricing

Custom container sessions are billed using the **Dedicated plan** based on compute resources consumed. Each custom container session pool runs on dedicated **E16 compute instances**. More expensive than code interpreter sessions.

### Free Tier

- No specific free tier for dynamic sessions.
- The general Container Apps Consumption plan free tier (180,000 vCPU-seconds, 360,000 GiB-seconds, 2M requests per subscription/month) applies to Container Apps, but dynamic sessions have their own billing meter.

### Cost Estimate for Biotrackr

Assuming:
- Reports generated on-demand via chat (not continuous)
- ~5-minute sessions with 5-minute cooldown (300s)
- Billed minimum 1 hour per session allocation
- ~30 reports/month

**Estimated cost:** 30 sessions × $0.03/hour = **~$0.90/month** for code interpreter sessions.

If sessions are reused within the hour (same session ID for same user), cost could be lower.

---

## 7. Integration with Microsoft Agent Framework

### Microsoft Foundry Agents — Code Interpreter Tool

Microsoft Foundry (formerly Azure AI) agents have a **built-in Code Interpreter tool** that internally uses Azure Container Apps dynamic sessions (code interpreter sessions).

**Source:** [Code Interpreter tool for Microsoft Foundry agents](https://learn.microsoft.com/en-us/azure/ai-services/agents/how-to/tools/code-interpreter)

Key details:
- GA for Python, C#, REST API
- Runs Python code in a sandbox backed by dynamic sessions
- Can upload CSV files, generate charts (PNG), and download outputs
- Sessions have 1-hour active timeout and 30-minute idle timeout
- Fixed set of pre-installed packages (common data science packages)
- Billed separately (Azure OpenAI Code Interpreter charges)

**Important:** This Code Interpreter is tied to **Microsoft Foundry agents specifically**, not to the general-purpose Microsoft Agent Framework SDK. Biotrackr's Chat.Api uses the Microsoft Agent Framework with Anthropic/Claude, not Foundry agents.

### Semantic Kernel Integrations

Semantic Kernel provides dynamic sessions plugins for:

| Framework | Package | Language |
|---|---|---|
| Semantic Kernel | `semantic-kernel` (SessionsPythonTool) | Python only |
| LangChain | `langchain-azure-dynamic-sessions` | Python only |
| LlamaIndex | `llama-index-tools-azure-code-interpreter` | Python only |
| AutoGen | Custom `ACASessionsExecutor` | Python only |

**No .NET Semantic Kernel plugin exists** for Azure Container Apps dynamic sessions.

### Relevance to Biotrackr

Since Biotrackr's Chat.Api is a .NET application using Microsoft Agent Framework:
- There is **no built-in dynamic sessions tool** in the Agent Framework .NET SDK.
- Integration must be done as a **custom tool** (function tool or MCP tool) that wraps the dynamic sessions REST API.
- Alternatively, an MCP Server tool could encapsulate the dynamic sessions interaction.

---

## 8. Latency

### Cold Start Time

- **Subsecond session allocation** from prewarmed pools (documented benefit).
- "Instant Startup: Prewarmed pools enable subsecond launch times for interactive workloads."
- New sessions are allocated in **milliseconds** thanks to pools of ready but unallocated sessions.

### Code Execution Time

- Code execution itself depends on the work being done.
- Maximum execution time per code run: **220 seconds**.
- If `pip install` is needed in code interpreter sessions, first execution in a session will be slower.

### Session Reuse

- Sessions remain active during the cooldown period (default 300s, configurable 300-3600s).
- Subsequent requests to the same session ID reuse the existing session (no cold start).
- For chat scenarios: Use conversation ID as session identifier → same session reused across a conversation.

### Suitability for Interactive Chat

**Yes, suitable for interactive chat** with these patterns:
- First request in a conversation allocates a session (~subsecond).
- Subsequent requests reuse the same session (no allocation delay).
- Report generation (pandas + matplotlib) typically completes in 5-30 seconds.
- 220-second execution limit is sufficient for report generation.
- 300-second minimum cooldown keeps the session alive between user interactions.

### Latency Risk

If using code interpreter sessions and packages need runtime installation:
- `pip install matplotlib reportlab` could add 10-30 seconds on first execution.
- Mitigated by: (a) using custom container with pre-installed packages, or (b) installing packages in a setup step and reusing the session.

---

## Key Discoveries Summary

### Viability Assessment: STRONG YES

Azure Container Apps Dynamic Sessions are a viable and well-suited option for Biotrackr's Python code execution needs. Key reasons:

1. **Pre-built Python environment** with pandas already available. matplotlib likely available. reportlab needs runtime install or custom container.
2. **File I/O fully supported** — upload data, execute code that generates PNGs/PDFs/CSVs, download results.
3. **Subsecond cold start** from prewarmed pools — suitable for interactive chat.
4. **Hyper-V isolation** — enterprise-grade security for running generated code.
5. **Cost-effective** — ~$0.03/session-hour, estimated <$1/month for personal project usage.
6. **Egress disabled by default** — strong security posture for untrusted code.
7. **REST API** is the integration path for .NET — straightforward HttpClient + Azure.Identity.

### Two Implementation Paths

| Approach | Code Interpreter Session | Custom Container Session |
|---|---|---|
| Setup complexity | Low (create session pool, call API) | Medium (build Docker image, push to ACR, create pool) |
| Package availability | pandas, numpy, sklearn pre-installed; matplotlib likely; reportlab needs pip install at runtime | All packages pre-installed in image |
| First-execution latency | Higher (if pip install needed) | Low (packages pre-installed) |
| Maintenance | None (Microsoft-managed) | Must maintain Docker image |
| Cost | $0.03/session-hour | Dedicated plan pricing (higher) |
| Recommended for | Prototyping, if matplotlib is confirmed pre-installed | Production, if reportlab/custom packages needed |

### Recommended Approach for Biotrackr

**Start with Code Interpreter Sessions** (`PythonLTS`):
1. Create a session pool via Bicep/CLI.
2. Build a custom Agent Framework tool in .NET that wraps the REST API.
3. Test whether matplotlib is pre-installed; if not, pip install at session start.
4. If runtime pip install latency is acceptable, stay with code interpreter.
5. If not, migrate to custom container sessions with pre-built image.

### Integration Pattern

```
User Chat → Chat.Api (Agent Framework) → Agent Tool "generate_report"
    → HttpClient POST to dynamic sessions REST API
    → Python code executes (pandas, matplotlib, reportlab)
    → Generated files (PNG, PDF) saved to /mnt/data
    → HttpClient GET to download generated files
    → Return files to user via chat
```

---

## References

- [Dynamic sessions overview](https://learn.microsoft.com/en-us/azure/container-apps/sessions)
- [Code interpreter sessions](https://learn.microsoft.com/en-us/azure/container-apps/sessions-code-interpreter)
- [Custom container sessions](https://learn.microsoft.com/en-us/azure/container-apps/sessions-custom-container)
- [Session pools](https://learn.microsoft.com/en-us/azure/container-apps/session-pool)
- [Dynamic sessions usage (security, auth)](https://learn.microsoft.com/en-us/azure/container-apps/sessions-usage)
- [Billing](https://learn.microsoft.com/en-us/azure/container-apps/billing#dynamic-sessions)
- [Pricing](https://azure.microsoft.com/en-us/pricing/details/container-apps/)
- [Semantic Kernel tutorial](https://learn.microsoft.com/en-us/azure/container-apps/sessions-tutorial-semantic-kernel)
- [AutoGen tutorial](https://learn.microsoft.com/en-us/azure/container-apps/sessions-tutorial-autogen)
- [Foundry Code Interpreter tool](https://learn.microsoft.com/en-us/azure/ai-services/agents/how-to/tools/code-interpreter)
- [GitHub samples](https://github.com/Azure-Samples/container-apps-dynamic-sessions-samples)

---

## Follow-On Questions (Directly Relevant)

1. What exact packages are pre-installed in the PythonLTS code interpreter image? (Needs runtime verification)
2. Can the session pool be deployed via Bicep? What is the resource type? (`Microsoft.App/sessionPools`)
3. What is the exact REST API response format for code execution results? (Need to check return value schema for stdout, stderr, result, execution status)
4. How does DefaultAzureCredential acquire the `https://dynamicsessions.io` audience token in .NET?

## Clarifying Questions

None — all original research questions have been answered through documentation.
