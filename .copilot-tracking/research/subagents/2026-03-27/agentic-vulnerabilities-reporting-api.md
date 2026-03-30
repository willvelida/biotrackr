# Agentic Vulnerabilities Assessment — Biotrackr.Reporting.Api

## Research Topics

- OWASP Top 10 for Agentic Applications (2026) assessment of `src/Biotrackr.Reporting.Api`
- Skill: agentic-vulnerabilities (framework revision 1.0.0)
- Reference: https://genai.owasp.org/resource/owasp-top-10-for-agentic-applications-for-2026/

## Vulnerability IDs Assessed

| ID | Title |
|---|---|
| ASI01:2026 | Agent Goal Hijack |
| ASI02:2026 | Tool Misuse and Exploitation |
| ASI03:2026 | Identity and Privilege Abuse |
| ASI04:2026 | Agentic Supply Chain Vulnerabilities |
| ASI05:2026 | Unexpected Code Execution |
| ASI06:2026 | Memory and Context Poisoning |
| ASI07:2026 | Insecure Inter-Agent Communication |
| ASI08:2026 | Cascading Failures |
| ASI09:2026 | Human-Agent Trust Exploitation |
| ASI10:2026 | Rogue Agents |

## Key Source Files Reviewed

- src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Program.cs
- src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Services/CopilotClientFactory.cs
- src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Services/ReportGenerationService.cs
- src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Services/BlobStorageService.cs
- src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Endpoints/GenerateEndpoints.cs
- src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Endpoints/ReportEndpoints.cs
- src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Endpoints/A2AEndpoints.cs
- src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Configuration/Settings.cs
- src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/Models/ReportMetadata.cs

## Findings Summary

### ASI01:2026 — Agent Goal Hijack — PARTIAL (MEDIUM)
- System prompt stored in Key Vault (good).
- `taskMessage` from HTTP request body is passed directly to `agent.RunAsync(taskMessage)` without prompt injection filtering or input sanitization.
- The endpoint has input validation for reportType, dates, and non-empty taskMessage, but no content-level filtering for injection payloads.
- Upstream Chat.Api constructs the taskMessage, but this service accepts it from any caller without validation of natural-language content.

### ASI02:2026 — Tool Misuse and Exploitation — FAIL (HIGH)
- Permission handler in `HandlePermissionRequest` approves ALL shell commands, ALL read operations, and ALL write operations with no argument inspection.
- No per-tool least-privilege profiles, rate limits, cost ceilings, or egress allowlists at the permission layer.
- Container sandboxing (only Python installed) provides some mitigation but is not defense-in-depth.
- No logging of specific tool invocations at the permission handler level.

### ASI03:2026 — Identity and Privilege Abuse — PARTIAL (MEDIUM)
- No authentication or authorization middleware in Program.cs.
- Endpoints `/api/reports/generate`, `/api/reports`, `/api/reports/{jobId}`, `/a2a/report` are all unauthenticated.
- Relies entirely on infrastructure-level security (Azure Container Apps internal ingress).
- Managed identity used for Azure services (blob storage, Key Vault) — good.
- CopilotService singleton pattern with disposal/recreation provides session isolation.

### ASI04:2026 — Agentic Supply Chain Vulnerabilities — PARTIAL (LOW)
- Third-party SDKs: GitHub.Copilot.SDK, Microsoft.Agents.AI.GitHub.Copilot.
- Copilot CLI sidecar container image integrity is not verified in code.
- Python packages in sidecar (pandas, matplotlib, etc.) are supply chain vectors.
- System prompt from Key Vault rather than hardcoded (good for integrity).
- A2A agent card defined inline (less susceptible to external tampering).
- No SBOM, AIBOM, or attestation mechanisms observed.

### ASI05:2026 — Unexpected Code Execution — FAIL (CRITICAL)
- The Copilot agent generates and executes Python code in the sidecar without any pre-execution validation, static analysis, or code review.
- All shell commands are blanket-approved by the permission handler.
- `taskMessage` (potentially untrusted input) drives the code generation — prompt injection could cause arbitrary code execution.
- No allowlist for auto-execution, no human approval for code runs.
- Container sandbox is the only mitigation layer.

### ASI06:2026 — Memory and Context Poisoning — PASS
- Ephemeral agent sessions: new CopilotClient per generation request.
- No long-term memory persistence within this service.
- `sourceDataSnapshot` stored in blob storage but not injected into agent context.
- Session isolation prevents cross-request contamination.

### ASI07:2026 — Insecure Inter-Agent Communication — PARTIAL (MEDIUM)
- Sidecar communication over HTTP to localhost:4321 (unencrypted, but same-host mitigates interception risk).
- A2A endpoint at `/a2a/report` has no explicit mutual authentication or message signing.
- No message integrity validation or nonce/timestamp protections on inter-agent messages.
- Agent card lacks cryptographic attestation.

### ASI08:2026 — Cascading Failures — PARTIAL (MEDIUM)
- Retry logic (2 retries) for Copilot session failures.
- Health check prevents new jobs when sidecar is unreachable.
- No circuit breaker pattern between service and sidecar.
- No rate limiting on `/api/reports/generate`.
- Fire-and-forget `Task.Run` — unbounded concurrent job processing.
- No blast-radius guardrails (quotas, progress caps).

### ASI09:2026 — Human-Agent Trust Exploitation — PARTIAL (LOW)
- Backend service — no direct human interaction.
- Agent-generated report summaries stored as-is without content validation.
- PDFs and charts produced by LLM-generated code served without safety checks.
- External Reviewer Agent exists in Chat.Api (outside this assessment scope).

### ASI10:2026 — Rogue Agents — PARTIAL (MEDIUM)
- No behavioral monitoring or anomaly detection for agent actions.
- No kill switch or credential revocation mechanism.
- No agent action audit trail beyond basic structured logging.
- Ephemeral sessions limit persistence risk.
- No signed behavioral manifests or attestation.

## Clarifying Questions

None — sufficient evidence gathered from source code review.
