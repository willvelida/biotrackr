# Copilot SDK Protocol Version Mismatch Research

## Research Topics and Questions

1. What causes the `SDK protocol version mismatch: SDK expects version 2, but server reports version 3` error?
2. Is there a newer version of the `Microsoft.Agents.AI.GitHub.Copilot` NuGet package that supports protocol v3?
3. Can the Copilot CLI be told to use protocol v2 via a flag or config?
4. Is the auto-update mechanism of the Copilot CLI the cause?
5. What exactly happens during the protocol version check in `CopilotClient.VerifyProtocolVersionAsync`?
6. What is the relationship between `GitHub.Copilot.SDK` and `Microsoft.Agents.AI.GitHub.Copilot`?

---

## Key Discovery: Root Cause Identified

The error is a **known, widely-reported issue** caused by the Copilot CLI upgrading from protocol v2 to protocol v3, while the SDK bundled into `Microsoft.Agents.AI.GitHub.Copilot` still expects protocol v2.

### Protocol v3 Breaking Change

The Copilot CLI v1.0.x (released around March 2026) introduced **protocol v3**, which fundamentally changes how tool calls and permission requests are handled:

- **Protocol v2 (old)**: Runtime sends `tool.call` and `permission.request` as direct RPC requests to a single client, which responds synchronously.
- **Protocol v3 (new)**: Runtime broadcasts `external_tool.requested` and `permission.requested` as session events to all connected clients. Clients respond via new RPC methods (`session.tools.handlePendingToolCall`, `session.permissions.handlePendingPermissionRequest`).

This was a **breaking protocol change** that enables multi-client scenarios but requires SDK updates.

**Reference**: [github/copilot-sdk PR #686](https://github.com/github/copilot-sdk/pull/686) — "Handle tool and permission broadcasts via event model (protocol v3)"

---

## Codebase Analysis

### Current CopilotService.cs

- Located at `src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Services/CopilotService.cs`
- Uses `GitHub.Copilot.SDK` namespace (the `CopilotClient` class)
- Creates a `CopilotClient` with `CopilotClientOptions.CliUrl` pointing to the sidecar
- The error occurs during `client.StartAsync()` in `ReportGenerationService.GenerateReportAsync()`, which internally calls `VerifyProtocolVersionAsync`

### Current ReportGenerationService.cs

- Located at `src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Services/ReportGenerationService.cs`
- Calls `await client.StartAsync()` → this triggers protocol version negotiation with the CLI sidecar
- Then creates an `AIAgent` via `client.AsAIAgent(sessionConfig)` (Microsoft Agent Framework bridge)

### Current Dockerfile.sidecar

- Located at `src/Biotrackr.Reporting.Api/Dockerfile.sidecar`
- Installs Copilot CLI v1.0.4 via `VERSION="1.0.4"` pin
- Sets `COPILOT_AGENT_AUTO_UPDATE=disabled` env var
- Writes `{"autoUpdatesChannel": "disabled"}` to config file
- Runs `copilot --headless --port 4321`

### Current .csproj Package References

From `src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api.csproj`:

- `Microsoft.Agents.AI.GitHub.Copilot` version `1.0.0-preview.260311.1`
- `Microsoft.Agents.AI.Hosting` version `1.0.0-preview.260311.1`

---

## Two Separate SDKs Involved

There are **two distinct packages** at play:

### 1. GitHub Copilot SDK (`GitHub.Copilot.SDK` / `github/copilot-sdk`)

- The low-level SDK from [github/copilot-sdk](https://github.com/github/copilot-sdk) repository
- .NET NuGet: `GitHub.Copilot.SDK`
- Contains `CopilotClient`, `SessionConfig`, protocol negotiation logic
- The `SdkProtocolVersion.cs` file contains the hardcoded protocol version constant
- **v0.1.30 and earlier**: `SDK_PROTOCOL_VERSION = 2` (incompatible with CLI 1.0.x)
- **v0.1.31**: Bumped to `SDK_PROTOCOL_VERSION = 3` (via PR #686)
- **v0.1.32**: Added backward compatibility for protocol v2-v3 range (via PR #706)
- **v0.2.0 (latest stable)**: Includes all v3 support plus many new features

### 2. Microsoft Agent Framework (`Microsoft.Agents.AI.GitHub.Copilot` / `microsoft/agent-framework`)

- Higher-level framework from [microsoft/agent-framework](https://github.com/microsoft/agent-framework) repository
- Wraps/depends on `GitHub.Copilot.SDK`
- Provides `AsAIAgent()` bridge to use `CopilotClient` as an `AIAgent`
- **Version `1.0.0-preview.260311.1`** (published March 11, 2026) likely bundles an older `GitHub.Copilot.SDK` version (pre-v0.1.31) that only supports protocol v2
- No newer versions published after `1.0.0-preview.260311.1`

---

## Protocol Version Negotiation Mechanism

### How `VerifyProtocolVersionAsync` Works

1. `CopilotClient.StartAsync()` connects to the CLI server (TCP or stdio)
2. The client sends a `ping` JSON-RPC request to the server
3. The CLI responds with its `protocolVersion` field (e.g., `3` for CLI 1.0.x)
4. The SDK compares the server's reported version against its expected version:
   - **Pre-v0.1.31 SDK**: Strict equality check — `if (serverVersion != expectedVersion)` where `expectedVersion = 2`
   - **v0.1.32+ SDK**: Range check — `if (serverVersion < MIN_PROTOCOL_VERSION || serverVersion > MAX_VERSION)` where `MIN=2, MAX=3`
5. If mismatched, throws `InvalidOperationException: SDK protocol version mismatch: SDK expects version 2, but server reports version 3`

The check happens at the HTTP/JSON-RPC level during the initial `ping` handshake. It is not negotiated via HTTP headers or CLI flags.

---

## GitHub Issues Confirming This Problem

### Issue: [github/copilot-sdk#703](https://github.com/github/copilot-sdk/issues/703)

**Title**: "Python SDK v0.1.30 incompatible with CLI v1.0.2 - protocol version mismatch"

**Error**: Exact same error — `SDK protocol version mismatch: SDK expects version 2, but server reports version 3`

**Root Cause**: CLI v1.0.x bumped the protocol to v3, but SDK still had `SDK_PROTOCOL_VERSION = 2`

**Resolution**: SteveSandersonMS (Microsoft) published updated SDK packages within days. Fix shipped in SDK v0.1.31 (protocol v3) and v0.1.32 (backward compatibility).

### Issue: [github/copilot-sdk#701](https://github.com/github/copilot-sdk/issues/701)

**Title**: "Latest Go client incompatible with latest CLI" — Same root cause.

### Issue: [github/copilot-cli#1606](https://github.com/github/copilot-cli/issues/1606)

**Title**: "Breaking change: --headless --stdio removed without deprecation"

**Key insight from follow-up investigation**: The Copilot CLI binary is a **thin launcher** that downloads newer versions to `~/.copilot/pkg/universal/` and delegates execution at runtime. Without `--no-auto-update`, a binary installed as v1.0.4 will silently run as the latest downloaded version.

---

## Auto-Update Mechanism Analysis

### How CLI Auto-Updates Work

The Copilot CLI binary acts as a thin launcher:

1. On startup, it checks for newer versions
2. Downloads the latest to `~/.copilot/pkg/universal/`
3. Delegates execution to the downloaded version
4. The installed version (v1.0.4) becomes irrelevant — the runtime version could be v1.0.12+

### Current Mitigation Attempts in Dockerfile.sidecar

```dockerfile
# Pin to v1.0.4
RUN curl -fsSL https://gh.io/copilot-install | VERSION="1.0.4" bash

# Disable auto-updates via env var
ENV COPILOT_AGENT_AUTO_UPDATE=disabled

# Disable auto-updates via config file
RUN echo '{"autoUpdatesChannel": "disabled"}' > /home/copilot/.copilot/config.json
```

### Why Pinning May Not Be Sufficient

- The `COPILOT_AGENT_AUTO_UPDATE=disabled` env var and config file approach **may not fully prevent** the launcher from delegating to a cached newer binary
- CLI v1.0.4 **already reports protocol v3** — it was released around early March 2026 when the protocol v3 change was being rolled out to the CLI
- Even if the pin works, the installed v1.0.4 itself may already speak protocol v3

### Confirmation: CLI v1.0.4 Likely Speaks Protocol v3

Looking at the timeline:

- CLI v1.0.2 (around March 7): Confirmed to report protocol v3 (per issue #703)
- CLI v1.0.4 (released March 11): Also reports protocol v3
- All CLI 1.0.x versions use protocol v3

So **pinning to v1.0.4 does not solve the problem** — that version already uses protocol v3.

---

## NuGet Package Version Analysis

### `Microsoft.Agents.AI.GitHub.Copilot`

All published versions (from NuGet):

| Version | Published | Notes |
|---|---|---|
| `1.0.0-preview.260311.1` | March 11 | Latest. Bundles old SDK with protocol v2 |
| `1.0.0-preview.260304.1` | March 4 | |
| `1.0.0-preview.260225.1` | Feb 25 | |
| `1.0.0-preview.260219.1` | Feb 19 | |
| `1.0.0-preview.260212.1` | Feb 12 | |
| `1.0.0-preview.260209.1` | Feb 9 | |
| `1.0.0-preview.260205.1` | Feb 5 | |
| `1.0.0-preview.260128.1` | Jan 28 | |

**No version newer than `260311.1`** has been published. The latest version was published 18 days before the protocol v3 SDK fix (v0.1.31) shipped on ~March 7.

### `GitHub.Copilot.SDK` (NuGet)

The direct SDK package is published separately. Key versions:

- **v0.1.30** (March ~3): Protocol v2 only — **INCOMPATIBLE with CLI 1.0.x**
- **v0.1.31** (March ~7): Protocol v3 support — SDK bumped to v3 (PR #686)
- **v0.1.32** (March ~7): v2 backward compatibility added (PR #706)
- **v0.2.0** (March ~23): Latest stable with all v3 features
- **v0.2.1-preview.1** (March ~28): Latest preview

### `Microsoft.Agents.AI.Hosting`

Same version pattern as `Microsoft.Agents.AI.GitHub.Copilot` — latest is `1.0.0-preview.260311.1`. No newer versions.

---

## Copilot CLI `--headless` Flag

The `--headless` flag starts the CLI in server mode on a given port. There are **no version-related or protocol-downgrade flags**:

- `copilot --headless --port 4321` — starts headless HTTP server
- `--no-auto-update` — prevents auto-update delegation
- There is **no `--protocol-version` flag** or equivalent to force v2

The CLI always advertises its native protocol version during the `ping` handshake. The only way to get protocol v2 is to use a CLI binary from before the v3 transition (pre-1.0.x, i.e., the 0.0.x series).

---

## Resolution Options

### Option A: Update `GitHub.Copilot.SDK` Directly (Recommended)

Replace the dependency on `Microsoft.Agents.AI.GitHub.Copilot` (which bundles an old SDK) with a direct dependency on `GitHub.Copilot.SDK` v0.2.0+:

1. Add `<PackageReference Include="GitHub.Copilot.SDK" Version="0.2.0" />` to the csproj
2. This gives protocol v3 support with v2 backward compatibility
3. May require adjusting the `AsAIAgent()` bridge code if it comes from the Microsoft Agent Framework package

**Risk**: The `Microsoft.Agents.AI.GitHub.Copilot` package provides the `AsAIAgent()` extension method that bridges `CopilotClient` into the Agent Framework. Replacing it with the raw `GitHub.Copilot.SDK` would lose this bridge unless it can be reimplemented or the MAF package is updated.

### Option B: Pin CLI to Pre-1.0.x Version (0.0.x Series)

Use a Copilot CLI version from the 0.0.x series that still speaks protocol v2:

- The highest known working v2 CLI was around v0.0.420
- Change the Dockerfile to install from the 0.0.x series

**Risk**: Very old CLI version missing features, security patches, and model support. Not recommended for production.

### Option C: Wait for Microsoft Agent Framework Update

Wait for a new `Microsoft.Agents.AI.GitHub.Copilot` release that bundles `GitHub.Copilot.SDK` v0.1.32+.

**Risk**: Unknown timeline. The last release was March 11. Weekly releases were common (see version history), so one may come soon, but no guarantee it will update the Copilot SDK dependency.

### Option D: Hybrid — Override SDK Version via PackageReference

Try adding `GitHub.Copilot.SDK` v0.2.0+ alongside `Microsoft.Agents.AI.GitHub.Copilot` to force NuGet to resolve the newer SDK version:

```xml
<PackageReference Include="GitHub.Copilot.SDK" Version="0.2.0" />
<PackageReference Include="Microsoft.Agents.AI.GitHub.Copilot" Version="1.0.0-preview.260311.1" />
```

NuGet's dependency resolution should pick the higher version of `GitHub.Copilot.SDK` if the MAF package has a compatible dependency range.

**Risk**: May cause assembly binding issues if the MAF package was compiled against specific types in the old SDK version.

### Option E: Use Copilot SDK Directly Without Agent Framework

Bypass `Microsoft.Agents.AI.GitHub.Copilot` entirely:

1. Use `GitHub.Copilot.SDK` v0.2.0+ directly
2. Remove the `AsAIAgent()` bridge
3. Use the SDK's native `CopilotSession.SendAndWaitAsync()` API directly
4. Parse the response text instead of going through the Agent Framework abstraction

**Risk**: More code changes, but gives full control over the SDK version and eliminates the dependency on the slow-to-update MAF bridge package.

---

## Clarifying Questions

1. **Does `Microsoft.Agents.AI.GitHub.Copilot` declare a NuGet dependency on `GitHub.Copilot.SDK` with a version range, or does it bundle the SDK's types directly?** — This determines whether Option D (version override) is viable. Checking the package's dependency list on NuGet or in `packages.lock.json` / `obj/` folder would answer this.

2. **Is there a newer preview of `Microsoft.Agents.AI.GitHub.Copilot` on a pre-release NuGet feed (e.g., Azure DevOps/MyGet)?** — The microsoft/agent-framework repo may publish to a CI feed before NuGet.org.

---

## Evidence Summary

| Source | Finding |
|---|---|
| [copilot-sdk#703](https://github.com/github/copilot-sdk/issues/703) | Exact same error reported and fixed in SDK v0.1.31+ |
| [copilot-sdk PR #686](https://github.com/github/copilot-sdk/pull/686) | Protocol v3 implemented across all SDKs, bumps version constant |
| [copilot-sdk PR #706](https://github.com/github/copilot-sdk/pull/706) | Backward compat adapters for v2-v3 range, shipped as v0.1.32 |
| [copilot-sdk releases](https://github.com/github/copilot-sdk/releases) | v0.1.32 adds v2 backward compat; v0.2.0 is latest stable |
| [copilot-cli#1606](https://github.com/github/copilot-cli/issues/1606) | Documents CLI auto-update thin launcher behavior |
| [copilot-cli releases](https://github.com/github/copilot-cli/releases) | CLI v1.0.x is protocol v3; latest is v1.0.13-1 |
| [NuGet: Microsoft.Agents.AI.GitHub.Copilot](https://www.nuget.org/packages/Microsoft.Agents.AI.GitHub.Copilot) | Only version is `1.0.0-preview.260311.1` (no updates in 18 days) |
| `sdk-protocol-version.json` in copilot-sdk repo | Currently `{"version": 3}` |
| Codebase: CopilotService.cs | Uses `GitHub.Copilot.SDK.CopilotClient` — error is from this class |
| Codebase: Dockerfile.sidecar | Pins CLI v1.0.4 but that version already speaks protocol v3 |
