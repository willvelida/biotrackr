# Copilot SDK Container Hosting Research

## Research Topics

1. Container hosting feasibility — headless Docker operation
2. Authentication in headless mode — service-to-service auth
3. Copilot CLI installation in Docker — container image requirements
4. Session management for server use — concurrency and scaling
5. Cost model — billing, premium requests, model multipliers
6. Python code execution — bash tool behavior, streaming, artifacts
7. Tool permission model — granular filtering and restrictions
8. BYOK limitations — managed identity constraints

---

## 1. Container Hosting Feasibility

**Verdict: Fully supported.** The Copilot CLI is designed to run headlessly in Docker containers.

### Architecture

The SDK communicates with the Copilot CLI over JSON-RPC. Two transport modes:

- **stdio** — CLI spawned as child process (auto-managed by SDK)
- **TCP** — CLI runs as a headless external server; SDK connects via `cliUrl`

For containers, the **TCP / headless server** mode is the intended pattern.

### CLI Headless Mode

Start the CLI as a background server:

```bash
copilot --headless --port 4321
# Or let it pick a random port:
copilot --headless
# Output: Listening on http://localhost:52431
```

Key CLI flags for server mode:

- `--headless` — Run as a JSON-RPC server (no interactive UI)
- `--port <N>` — Bind to specific port
- `--bind 0.0.0.0` — Bind to all interfaces (needed in containers)
- `--auth-token-env <ENV_VAR>` — Read auth token from named env var
- `--no-auto-update` — Disable auto-update (added automatically by SDKs)
- `--no-auto-login` — Don't attempt interactive login
- `--stdio` — Use stdio transport instead of TCP

### Docker Compose Example (from official docs)

```yaml
version: "3.8"
services:
  copilot-cli:
    image: ghcr.io/github/copilot-cli:latest
    command: ["--headless", "--port", "4321"]
    environment:
      - COPILOT_GITHUB_TOKEN=${COPILOT_GITHUB_TOKEN}
    ports:
      - "4321:4321"
    restart: always
    volumes:
      - session-data:/root/.copilot/session-state
  api:
    build: .
    environment:
      - CLI_URL=copilot-cli:4321
    depends_on:
      - copilot-cli
    ports:
      - "3000:3000"
volumes:
  session-data:
```

### Container-Proxy Pattern

The repo includes a complete `test/scenarios/bundling/container-proxy/` example demonstrating:

- Copilot CLI running in Docker with no secrets in the image
- External proxy on the host intercepts LLM traffic and injects credentials
- No API keys passed into the container at all

### Backend Services Limitations

| Limitation | Details |
|------------|---------|
| Single CLI server = single point of failure | See Scaling guide for HA patterns |
| No built-in auth between SDK and CLI | Secure the network path (same host, VPC, etc.) |
| Session state on local disk | Mount persistent storage for container restarts |
| 30-minute idle timeout | Sessions without activity are auto-cleaned |

### References

- `docs/setup/backend-services.md` — Full headless server setup guide
- `docs/setup/scaling.md` — Horizontal scaling, Kubernetes, ACI examples
- `test/scenarios/bundling/container-proxy/` — Docker container example
- `test/scenarios/bundling/app-backend-to-server/` — Web backend to external CLI server
- `test/scenarios/bundling/app-direct-server/` — Direct SDK-to-server connection

---

## 2. Authentication in Headless Mode

**Verdict: Multiple options available.** Environment variables, explicit tokens, and BYOK all work without interactive login.

### Authentication Priority Order

1. **Explicit `githubToken`** — Token passed directly to SDK constructor
2. **HMAC key** — `CAPI_HMAC_KEY` or `COPILOT_HMAC_KEY` environment variables
3. **Direct API token** — `GITHUB_COPILOT_API_TOKEN` with `COPILOT_API_URL`
4. **Environment variable tokens** — `COPILOT_GITHUB_TOKEN` → `GH_TOKEN` → `GITHUB_TOKEN`
5. **Stored OAuth credentials** — From previous `copilot` CLI login
6. **GitHub CLI** — `gh auth` credentials

### Server-Side Auth Methods

| Method | Use Case | Requires Copilot Subscription |
|--------|----------|------|
| Environment Variables | CI/CD, automation, server-to-server | Yes |
| OAuth GitHub App | Apps acting on behalf of users | Yes |
| GitHub Signed-in User | Interactive apps | Yes |
| BYOK | Your own API keys | **No** |

### Environment Variable Auth (simplest for servers)

```bash
export COPILOT_GITHUB_TOKEN="gho_service_account_token"
copilot --headless --port 4321
```

The SDK auto-detects env vars — no code changes needed:

```python
from copilot import CopilotClient
client = CopilotClient()  # Reads GITHUB_TOKEN automatically
```

### Per-User Tokens (OAuth)

Pass individual user tokens when creating sessions:

```python
client = CopilotClient({"github_token": user_token, "use_logged_in_user": False})
```

### CLI Auth Flag

For headless TCP mode, use `--auth-token-env`:

```bash
copilot --headless --port 3000 --auth-token-env GITHUB_TOKEN
```

### Supported Token Types

- `gho_` — OAuth user access tokens
- `ghu_` — GitHub App user access tokens
- `github_pat_` — Fine-grained personal access tokens
- **NOT supported:** `ghp_` (classic PATs, deprecated)

### Auth Status Check

Auth types returned by `auth.getStatus` RPC: `"user"`, `"env"`, `"gh-cli"`, `"hmac"`, `"api-key"`, `"token"`

### References

- `docs/auth/index.md` — Full authentication overview
- `docs/setup/backend-services.md#authentication-for-backend-services`
- `docs/setup/github-oauth.md` — OAuth flow implementation

---

## 3. Copilot CLI Installation in Docker

**Verdict: Simple.** A single static binary, minimal base image required.

### Official Dockerfile (from `test/scenarios/bundling/container-proxy/Dockerfile`)

```dockerfile
FROM debian:bookworm-slim
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/*
ARG COPILOT_CLI_PATH=copilot
COPY ${COPILOT_CLI_PATH} /usr/local/bin/copilot
RUN chmod +x /usr/local/bin/copilot
EXPOSE 3000
ENTRYPOINT ["copilot", "--headless", "--port", "3000", "--bind", "0.0.0.0", "--auth-token-env", "GITHUB_TOKEN"]
```

### Container Image

- Official image: `ghcr.io/github/copilot-cli:latest`
- Manual: Copy pre-built binary to `debian:bookworm-slim`, only needs `ca-certificates`
- No Node.js/Python/Go runtime needed in the CLI container itself

### Docker Run

```bash
docker run -d --name copilot-cli \
    -p 4321:4321 \
    -e COPILOT_GITHUB_TOKEN="$TOKEN" \
    ghcr.io/github/copilot-cli:latest \
    --headless --port 4321
```

### SDK Installation (separate from CLI)

- **Python:** `pip install github-copilot-sdk` (requires Python 3.11+)
- **Node.js:** `npm install @github/copilot-sdk` (requires Node.js 20+)
- **Go:** `go get github.com/github/copilot-sdk/go` (requires Go 1.24+)
- **.NET:** `dotnet add package GitHub.Copilot.SDK` (requires .NET 8.0+)

### Bundled CLI

The `@github/copilot` npm package bundles the CLI binary. SDKs auto-discover it:

```bash
npm install @github/copilot
```

Or set `COPILOT_CLI_PATH` env var to point to your binary.

### Kubernetes Deployment

```yaml
containers:
  - name: copilot-cli
    image: ghcr.io/github/copilot-cli:latest
    args: ["--headless", "--port", "4321"]
    env:
      - name: COPILOT_GITHUB_TOKEN
        valueFrom:
          secretKeyRef:
            name: copilot-secrets
            key: github-token
    ports:
      - containerPort: 4321
    volumeMounts:
      - name: session-state
        mountPath: /root/.copilot/session-state
```

### References

- `test/scenarios/bundling/container-proxy/Dockerfile`
- `test/scenarios/bundling/container-proxy/docker-compose.yml`
- `docs/setup/backend-services.md` — Docker and systemd examples
- `docs/setup/scaling.md#container-deployments` — Kubernetes and ACI examples
- `docs/setup/bundled-cli.md` — Bundling the CLI with your app

---

## 4. Session Management for Server Use

**Verdict: A single CLI server handles multiple concurrent sessions.** The server supports both ephemeral and persistent sessions with various isolation patterns.

### Concurrent Sessions

- A single CLI server can handle many concurrent sessions
- Each session has its own state, system prompt, and tools
- Sessions are isolated by unique session IDs
- Tested in `test/scenarios/sessions/concurrent-sessions/` — creates two sessions with different personas simultaneously

### Session Types

**Ephemeral:** Created per request, destroyed after response. No persistence.

**Persistent:** Named session ID, survives restarts, resumable.

**Infinite:** Automatic context compaction at configurable thresholds:
- `backgroundCompactionThreshold` (default 0.80) — Start background compaction
- `bufferExhaustionThreshold` (default 0.95) — Force compaction before next message

### Isolation Patterns

| Pattern | Isolation | Resource Usage | Best For |
|---------|-----------|---------------|----------|
| **Isolated CLI per user** | Complete | High (CLI per user) | Multi-tenant SaaS |
| **Shared CLI + session isolation** | Logical | Low (one CLI) | Internal tools |
| **Shared sessions** | None (collaborative) | Low | Team collaboration |

### Horizontal Scaling

- Multiple CLI servers behind a load balancer
- **Sticky sessions:** Pin users to specific servers (simpler, no shared storage)
- **Shared storage:** Any CLI handles any session (requires NFS/cloud storage for `~/.copilot/session-state/`)
- Session state is file-based — written to `~/.copilot/session-state/{sessionId}/`

### Multi-User Patterns

- **Multi-user short-lived:** Each request creates a new session, destroys after response
- **Multi-user long-lived:** Per-user `configDir` for full isolation, sessions persist across clients
- Session IDs should encode user identity for access control

### Production Concerns

| Concern | Recommendation |
|---------|---------------|
| Session cleanup | Periodic cleanup to delete sessions older than TTL |
| Health checks | Ping CLI server periodically; restart if unresponsive |
| Storage | Persistent volumes for `~/.copilot/session-state/` |
| Session locking | Redis or similar for shared session access |
| Graceful shutdown | Drain active sessions before stopping CLI servers |
| Idle timeout | 30-minute auto-cleanup by CLI |

### Scaling Limitations

- CLI is single-process — scale by adding more CLI server instances
- No built-in session locking — implement at application level
- No built-in load balancing — use external LB or service mesh
- Session state is file-based — requires shared filesystem for multi-server

### References

- `docs/setup/scaling.md` — Full scaling and multi-tenancy guide
- `docs/features/session-persistence.md` — Session resume across restarts
- `test/scenarios/sessions/concurrent-sessions/` — Concurrent session demo
- `test/scenarios/sessions/multi-user-short-lived/` — Stateless backend pattern
- `test/scenarios/sessions/multi-user-long-lived/` — Persistent multi-user sessions
- `test/scenarios/sessions/infinite-sessions/` — Long-running session compaction

---

## 5. Cost Model

**Verdict: Per-prompt billing against premium request quota.** Each model has a billing multiplier relative to a base rate. BYOK bypasses Copilot billing entirely.

### Billing Overview

- SDK usage is billed the same as Copilot CLI
- Each prompt counts against premium request quota
- A GitHub Copilot subscription is required (free tier available with limited usage), **unless using BYOK**
- Billing reference: [Requests in GitHub Copilot](https://docs.github.com/en/copilot/concepts/billing/copilot-requests)

### Model Billing Multiplier

Each model exposes a `billing.multiplier` field (relative to the base rate):

```typescript
interface ModelBilling {
    multiplier: number;  // e.g., 1.0 for base, higher for premium models
}
```

The SDK provides `models.list` RPC to enumerate available models with their billing info at runtime.

### Usage Tracking (per-request)

The `assistant.usage` event exposes:

| Field | Type | Description |
|-------|------|-------------|
| `model` | string | Model identifier |
| `inputTokens` | number | Input tokens consumed |
| `outputTokens` | number | Output tokens produced |
| `cacheReadTokens` | number | Tokens read from prompt cache |
| `cacheWriteTokens` | number | Tokens written to prompt cache |
| `cost` | number | Model multiplier cost for billing |
| `duration` | number | API call duration in ms |

### Quota API

The SDK exposes an `account.getQuota` RPC (not yet implemented in CLI as of current version) that returns:

```typescript
quotaSnapshots: {
    [quotaType: string]: {
        entitlementRequests: number;    // Total allowed
        usedRequests: number;           // Used so far
        remainingPercentage: number;    // % remaining
        overage: number;                // Overage count
        overageAllowedWithExhaustedQuota: boolean;
        resetDate?: string;             // ISO 8601
    }
}
```

Quota types include: `chat`, `completions`, `premium_interactions`.

### CopilotUsage (per-request)

```typescript
copilotUsage: {
    tokenDetails: Array<{
        batchSize: number;  // Tokens in this billing batch
        // cost per batch
    }>;
    totalNanoAiu: number;  // Total cost in nano-AIU (AI Units)
}
```

### BYOK Billing

When using BYOK, billing goes directly to the provider:
- Does **not** count against Copilot premium request quotas
- Subject to the provider's own rate limits and quotas
- No GitHub Copilot subscription required

### References

- `README.md#faq` — Billing FAQ
- `docs/features/streaming-events.md#assistant-usage` — Usage event format
- Generated types in `python/copilot/generated/rpc.py`, `go/rpc/generated_rpc.go`, etc.

---

## 6. Python Code Execution Specifics

**Verdict: The CLI has a built-in `bash` tool for code execution.** The SDK streams output events. Tool execution results are captured and returned to the model.

### How Tool Execution Works

1. Agent decides to use a tool (e.g., `bash` to run Python)
2. `onPermissionRequest` callback fires — integrator approves/denies
3. `tool.execution_start` event fires with tool name
4. Tool executes in CLI's working directory
5. `tool.execution_complete` event fires with result (success/error, output)
6. Result is fed back to the model

### Streaming

When `streaming: true` is enabled:
- `assistant.message_delta` events stream as the response generates
- `assistant.reasoning_delta` events stream reasoning/thinking content
- Tool execution events fire in real-time

### Event Flow for Tool Use

```
user.message → assistant.message (with tool_requests) →
  tool.execution_start → tool.execution_complete →
assistant.message (interpreting results) → session.idle
```

### Built-in Tools

The CLI includes built-in tools like: `bash`, `grep`, `glob`, `view`, `edit`, `create_file`, `str_replace_editor`, etc.

The `bash` tool allows arbitrary command execution including `python3 -c "..."` or running Python scripts.

### Capturing Output

Tool results are returned via the `tool.execution_complete` event which includes:
- `success: boolean`
- Output/error message content
- The full result is available in post-tool hooks

### File Artifacts

The CLI operates in a working directory. Files created by tools (via `bash`, `create_file`, `edit`) persist on the filesystem. Mount volumes to persist artifacts across container restarts.

### References

- `docs/features/streaming-events.md` — All session event types
- `test/scenarios/modes/default/` — Default mode with grep tool usage
- `test/scenarios/callbacks/permissions/` — Permission callbacks for tool execution

---

## 7. Tool Permission Model

**Verdict: Highly granular.** Three layers of control: declarative filtering, permission callbacks, and pre-tool-use hooks.

### Layer 1: Declarative Tool Filtering

**Whitelist** (`availableTools`):

```python
session = await client.create_session({
    "available_tools": ["grep", "glob", "view"],  # Only these tools
})
```

**Blacklist** (`excludedTools`):

```python
session = await client.create_session(
    on_permission_request=PermissionHandler.approve_all,
    excluded_tools=["bash", "edit", "create_file"],  # Remove these, keep rest
)
```

**No tools** (`availableTools: []`):

```python
session = await client.create_session({
    "available_tools": [],  # Text-only, no tool access
})
```

### Layer 2: Permission Callbacks (`onPermissionRequest`)

Called before any tool requires permission. Full control over approval:

```python
async def on_permission_request(request, invocation):
    if request.kind == "shell":
        return {"kind": "denied-interactively-by-user"}
    return {"kind": "approved"}
```

Permission result kinds:
- `approved` — Allow the tool to run
- `denied-interactively-by-user` — User denied
- `denied-no-approval-rule-and-could-not-request-from-user` — No rule matched
- `denied-by-rules` — Policy rule denied
- `denied-by-content-exclusion-policy` — Content exclusion denied

Permission request types include: `shell`, `read`, `write`, `custom-tool`, `mcp`.

### Layer 3: Pre-Tool-Use Hooks (`onPreToolUse`)

Most granular control — inspect tool name, arguments, and make decisions:

```typescript
hooks: {
    onPreToolUse: async (input) => {
        if (input.toolName === "bash") {
            const cmd = String(input.toolArgs?.command || "");
            if (/rm\s+-rf/i.test(cmd)) {
                return {
                    permissionDecision: "deny",
                    permissionDecisionReason: "Destructive commands are not allowed.",
                };
            }
        }
        return { permissionDecision: "allow" };
    },
}
```

Hook output options:

| Field | Type | Description |
|-------|------|-------------|
| `permissionDecision` | `"allow"` / `"deny"` / `"ask"` | Whether to permit execution |
| `permissionDecisionReason` | string | Reason shown if denied |
| `modifiedArgs` | object | Modified arguments for the tool |
| `additionalContext` | string | Extra context injected into conversation |
| `suppressOutput` | boolean | If true, tool output hidden from model |

### Bash Command Restriction Example

```typescript
hooks: {
    onPreToolUse: async (input) => {
        if (input.toolName === "bash") {
            const cmd = String(input.toolArgs?.command || "");
            if (/rm\s+-rf/i.test(cmd) || /Remove-Item.*-Recurse/i.test(cmd)) {
                return { permissionDecision: "deny", permissionDecisionReason: "Destructive commands blocked." };
            }
        }
        return { permissionDecision: "allow" };
    },
}
```

### Per-Agent Tool Scoping

Custom agents can have their own tool restrictions:

```typescript
customAgents: [
    { name: "reader", tools: ["grep", "glob", "view"] },   // Read-only
    { name: "writer", tools: ["view", "edit", "bash"] },    // Write access
    { name: "unrestricted", tools: null },                  // All tools
]
```

### Custom Tool `skipPermission`

Custom tools can opt out of permission prompts:

```typescript
defineTool("safe_lookup", {
    skipPermission: true,
    handler: async ({ id }) => { /* safe read-only logic */ },
})
```

### References

- `docs/hooks/pre-tool-use.md` — Full pre-tool-use hook reference
- `docs/features/hooks.md#use-case-permission-control` — Permission control patterns
- `test/scenarios/tools/tool-filtering/` — Whitelist demo
- `test/scenarios/tools/no-tools/` — No tools demo
- `test/scenarios/callbacks/permissions/` — Permission callback demo

---

## 8. BYOK Limitations

**Verdict: Key-based auth only. No managed identity, no Entra ID, no OIDC.** But a workaround exists using `bearerToken` with manual token refresh.

### Identity Limitations

BYOK uses **static credentials only**:

- ❌ Microsoft Entra ID (Azure AD) — No managed identity or service principal support
- ❌ Third-party identity providers — No OIDC, SAML, or federated identity
- ❌ Managed identities — Azure Managed Identity is NOT natively supported
- Must use API key or static bearer token that you manage yourself

**Why no Entra ID?** The `bearerToken` option only accepts a **static token string**. There is no callback mechanism for the SDK to request fresh tokens. Entra tokens are short-lived (~1 hour) and require automatic refresh.

### Workaround: Azure Managed Identity via Bearer Token

The official docs include a dedicated guide at `docs/setup/azure-managed-identity.md`:

1. Use `DefaultAzureCredential` to obtain a token for `https://cognitiveservices.azure.com/.default`
2. Pass as `bearer_token` in the provider config
3. Refresh the token before it expires by creating new sessions with fresh tokens

```python
from azure.identity import DefaultAzureCredential

credential = DefaultAzureCredential()
token = credential.get_token("https://cognitiveservices.azure.com/.default").token

session = await client.create_session(SessionConfig(
    model="gpt-4.1",
    provider=ProviderConfig(
        type="openai",
        base_url=f"{foundry_url}/openai/v1/",
        bearer_token=token,  # Short-lived, must refresh
        wire_api="responses",
    ),
))
```

For long-running apps, refresh the token before creating each session.

### Feature Limitations

- Model availability — Only models from your provider
- Rate limiting — Subject to provider's limits, not Copilot's
- Usage tracking — Tracked by your provider, not GitHub
- Premium requests — Do NOT count against Copilot quotas

### Provider-Specific Limitations

| Provider | Limitations |
|----------|-------------|
| Azure AI Foundry | No Entra ID auth natively; use API keys or bearer token workaround |
| Ollama | No API key; local only; model support varies |
| Microsoft Foundry Local | Local only; hardware-dependent model availability; no API key |
| OpenAI | Subject to OpenAI rate limits and quotas |

### Supported Providers

| Provider | `type` | Notes |
|----------|--------|-------|
| OpenAI | `"openai"` | OpenAI API and compatible endpoints |
| Azure OpenAI / AI Foundry | `"azure"` | Azure-hosted models |
| Anthropic | `"anthropic"` | Claude models |
| Ollama | `"openai"` | Local via OpenAI-compatible API |
| Microsoft Foundry Local | `"openai"` | Local on-device |
| vLLM, LiteLLM, etc. | `"openai"` | Any OpenAI-compatible endpoint |

### Provider Config

| Field | Type | Description |
|-------|------|-------------|
| `type` | `"openai"` / `"azure"` / `"anthropic"` | Provider type |
| `baseUrl` | string | **Required.** API endpoint URL |
| `apiKey` | string | API key (optional for local) |
| `bearerToken` | string | Bearer token (takes precedence over apiKey; **static only**) |
| `wireApi` | `"completions"` / `"responses"` | API format (default: `"completions"`) |
| `azure.apiVersion` | string | Azure API version (default: `"2024-10-21"`) |

### References

- `docs/auth/byok.md` — Full BYOK documentation
- `docs/setup/azure-managed-identity.md` — Managed identity workaround
- `README.md#faq` — BYOK FAQ
- `test/scenarios/auth/byok-azure/` — Azure BYOK examples
- `test/scenarios/auth/byok-openai/` — OpenAI BYOK examples
- `test/scenarios/auth/byok-anthropic/` — Anthropic BYOK examples
- `test/scenarios/auth/byok-ollama/` — Ollama BYOK examples

---

## General Notes

- **SDK Status:** Technical Preview — functional for development/testing, may not yet be suitable for production
- **SDKs Available:** Python, TypeScript/Node.js, Go, .NET, Java
- **Community SDKs:** Rust, Clojure, C++ (unofficial)
- **Transport Modes:** stdio (child process), TCP (external server), WASM (in-process)
- **Repo:** [github.com/github/copilot-sdk](https://github.com/github/copilot-sdk)

---

## Open Questions / Gaps

- [ ] Confirm the official `ghcr.io/github/copilot-cli:latest` image is publicly accessible (not gated by Copilot subscription)
- [ ] `account.getQuota` RPC is defined in schema but marked "not yet implemented in CLI" — may not be available at runtime yet
- [ ] Exact premium request multipliers per model are not documented in the SDK repo — check [GitHub Copilot pricing docs](https://docs.github.com/en/copilot/concepts/billing/copilot-requests) for current values
- [ ] The concurrent sessions test is skipped in Python and Go with "Known race condition" — may affect stability in high-concurrency scenarios
- [ ] Investigate Azure Dynamic Sessions integration for per-user CLI isolation in ACA
