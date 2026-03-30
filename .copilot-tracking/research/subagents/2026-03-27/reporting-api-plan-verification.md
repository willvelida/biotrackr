# Reporting.Api Plan Verification — Phase 3 & 4

## Research Questions

Verify that all planned files for Phase 3 (Service Scaffold) and Phase 4 (Copilot Agent Workflow) exist and match their specifications from `docs/plans/2026-03-25-reporting-api-copilot-sdk-plan.md`.

---

## Phase 3: Reporting.Api Service Scaffold

### Step 3.1: Project Scaffold

| File | Status | Notes |
|------|--------|-------|
| `Biotrackr.Reporting.Api.csproj` | **PARTIAL** | Has `Microsoft.Agents.AI`, `Microsoft.Agents.AI.GitHub.Copilot`, `Azure.Storage.Blobs`, `Azure.Identity`, OpenTelemetry. Uses `Microsoft.Azure.AppConfiguration.AspNetCore` (ASP.NET Core wrapper) instead of `Microsoft.Extensions.Configuration.AzureAppConfiguration`. No explicit `GitHub.Copilot.SDK` package — it is a transitive dependency via `Microsoft.Agents.AI.GitHub.Copilot` (code compiles against `GitHub.Copilot.SDK` namespace). No Anthropic or MCP client packages. Extra packages: `Microsoft.Agents.AI.Hosting`, `Microsoft.Agents.AI.Hosting.A2A.AspNetCore`, `Microsoft.AspNetCore.OpenApi`, `Azure.Monitor.OpenTelemetry.Exporter`. |
| `Program.cs` | **MATCH** | Registers services (`BlobStorageService`, `CopilotService`, `ReportGenerationService`). App Config with Key Vault. Health checks (`AddHealthChecks` + `/api/healthz`). Minimal API endpoints (`MapGenerateEndpoints`, `MapReportEndpoints`, `MapA2AEndpoints`). OpenTelemetry tracing + metrics. Loads system prompt from config. |
| `Configuration/Settings.cs` | **MATCH** | Contains `ReportingBlobStorageEndpoint`, `CopilotCliUrl` (default `http://localhost:4321`), `ChatApiUaiPrincipalId`, `ReportingApiUrl`, `ReportGeneratorSystemPrompt`, plus ASI security settings: `ReportGenerationEnabled`, `MaxConcurrentJobs`, `ReportGenerationTimeoutMinutes`, `MaxArtifactSizeBytes`. |
| `Dockerfile` | **MATCH** | Standard multi-stage ASP.NET 10 build. Targets `mcr.microsoft.com/dotnet/aspnet:10.0` and `sdk:10.0`. |
| `appsettings.json` | **MATCH** | Contains Biotrackr settings section with all expected keys. |
| `UnitTests.csproj` | **MATCH** | xUnit, Moq, FluentAssertions, AutoFixture, coverlet. References main project. |
| `IntegrationTests.csproj` | **MATCH** | xUnit, coverlet. References main project. |
| `Biotrackr.Reporting.Api.slnx` | **MATCH** | Contains all three projects. |
| `coverage.runsettings` | **MATCH** | Excludes `Program` and OpenAPI generated code. |

#### Step 3.1 Deviations

1. **Package name**: Plan specified `Microsoft.Extensions.Configuration.AzureAppConfiguration`. Actual: `Microsoft.Azure.AppConfiguration.AspNetCore` (v8.5.0). This is the ASP.NET Core wrapper package and is functionally equivalent — considered acceptable.
2. **GitHub.Copilot.SDK**: Not an explicit `PackageReference`. It is a transitive dependency brought in by `Microsoft.Agents.AI.GitHub.Copilot`. The code successfully uses `GitHub.Copilot.SDK` namespace. Functionally correct but not explicitly listed.

---

### Step 3.2: BlobStorageService

**File**: `Services/BlobStorageService.cs` — **MATCH**

All required elements present:

- `BlobServiceClient` construction: Lazy-initialized with `DefaultAzureCredential` using `ManagedIdentityClientId`.
- `UploadReportAsync`: Uploads PDF + chart images, updates metadata.
- `UpdateJobStatusAsync`: Updates status and optional error in metadata.
- `metadata.json` fields: `jobId`, `status`, `reportType`, `dateRange` (start/end), `summary`, `artifacts`, `sourceDataSnapshot`, `error` — all present in `ReportMetadata.cs`.
- SAS URL generation: Uses `GetUserDelegationKeyAsync` (user delegation) with `BlobSasPermissions.Read` (read-only). 24-hour expiry.
- `CreateJobAsync`: Creates job with GUID, initial `Generating` status, writes metadata.
- `GetMetadataAsync`: Scans container for matching jobId.
- Additional: `ListReportsAsync` for filtered listing, `ASI09` disclaimer prepended to summaries, separate `IBlobStorageService` interface.

---

### Step 3.3: CopilotClientFactory / CopilotService

**File**: `Services/CopilotClientFactory.cs` — **PARTIAL**

All required functionality present but filename is inconsistent:

- **Class name**: `CopilotService` (matches plan's "now CopilotService" note).
- **Filename**: Still `CopilotClientFactory.cs` — should be renamed to `CopilotService.cs` for consistency.
- `CopilotClient` lifecycle: Lazy-initialized, `IAsyncDisposable`, recreatable.
- Connects via `CopilotClientOptions.CliUrl` from `settings.Value.CopilotCliUrl` (defaults to `http://localhost:4321`).
- `SessionConfig` with `OnPermissionRequest` callback.
- Permission handler: Allows `["shell", "read", "write"]`, denies everything else with `Kind = "denied-by-rules"`. Audit logs every request.
- Sidecar health check: `IsHealthyAsync()` via TCP connect with 5-second timeout.
- Interface `ICopilotService` defined in same file.

#### Step 3.3 Deviation

1. **Filename mismatch**: File is `CopilotClientFactory.cs` but class is `CopilotService`. The plan acknowledges this rename ("now CopilotService") but the file was not renamed to match.

---

### Step 3.4: A2A Agent Endpoint

**Files**: `Endpoints/A2AEndpoints.cs`, `Models/A2AModels.cs` — **DEVIATION**

- No `A2AAgentHandler.cs` or `AgentCard.cs` files as originally planned.
- A2A is implemented via `Microsoft.Agents.AI.Hosting.A2A.AspNetCore` SDK pattern instead:
  - `A2AEndpoints.cs`: Extension method `MapA2AEndpoints` calls `app.MapA2A(reportAgent, ...)` with inline agent card.
  - Agent card embedded in code: Name, Description, Version.
  - A2A path: `/a2a/report` (card at `/a2a/report/v1/card`, messages at `/a2a/report/v1/message:stream`).
- `A2AModels.cs` contains `ReportJobResult` (jobId, status, message) rather than an agent card model.
- **No explicit managed identity caller validation** in `A2AEndpoints.cs`. The M.Agents SDK may handle authentication at the transport layer, but no explicit `ChatApiUaiPrincipalId` validation is visible.

#### Step 3.4 Deviations

1. **File structure**: Plan expected `A2AAgentHandler.cs` + `AgentCard.cs`. Actual: `A2AEndpoints.cs` + `A2AModels.cs` using M.Agents SDK.
2. **Managed identity validation**: No explicit caller validation against `ChatApiUaiPrincipalId`. This may be handled by the A2A SDK middleware or may be missing.

---

## Phase 4: Copilot Agent Workflow

### Step 4.1: System Prompt

**File**: `scripts/reporting-api-prompts/report-generator-prompt.txt` — **MATCH**

All required elements present:

- Parse JSON health data from messages.
- Write Python scripts, execute via bash.
- Uses `pandas`, `matplotlib`, `seaborn`, `reportlab`, `numpy`.
- `matplotlib.use('Agg')` before pyplot import.
- All output to `/tmp/reports/`.
- No network requests constraint.
- Report type guidelines: weekly_summary, monthly_summary, trend_analysis, diet_analysis, correlation_report.
- AI disclaimer on every PDF.
- System prompt non-disclosure instruction.

---

### Step 4.2: ReportGenerationService

**File**: `Services/ReportGenerationService.cs` — **MATCH**

All required elements present:

- Receives task via `StartReportGenerationAsync(reportType, startDate, endDate, taskMessage, sourceDataSnapshot)`.
- Creates `CopilotClient` session using `_copilotService.Client` / `AsAIAgent(sessionConfig)`.
- Sends task via `agent.RunAsync(taskMessage)`.
- Scans `/tmp/reports` for `.pdf`, `.png`, `.jpg`, `.svg` artifacts.
- Uploads via `_blobStorageService.UploadReportAsync`.
- Status transitions: `Generating` → `Generated` (success) or `Failed` (error/timeout).
- Retry logic: `MaxRetries = 2` (3 total attempts), with directory cleanup between retries.
- Sidecar health check: `_copilotService.IsHealthyAsync()` called before starting.
- Additional security features: kill switch (ASI10), concurrency limiter (ASI08), timeout (ASI08), code validation gate (ASI05), artifact size limits (ASI10).

---

### Step 4.3: REST Report Retrieval Endpoints

**Files**: `Endpoints/GenerateEndpoints.cs`, `Endpoints/ReportEndpoints.cs` — **MATCH**

All required elements present:

- **POST `/api/reports/generate`** (GenerateEndpoints.cs): Validates report type, date format, date range (max 365 days), task message length (max 5000 chars), prompt injection detection (ASI01). Returns `202 Accepted` with `ReportJobResult`. Returns `503` if generation disabled.
- **GET `/api/reports/{jobId}`** (ReportEndpoints.cs): Returns metadata + SAS URLs for artifacts if status is `Generated` or `Reviewed`.
- Additional: **GET `/api/reports`** for filtered listing by report type, start/end dates.

---

## Summary

| Component | Status | Key Deviations |
|-----------|--------|---------------|
| Step 3.1: Project scaffold (9 files) | **MATCH** | `GitHub.Copilot.SDK` transitive only; App Config package name differs but functionally equivalent |
| Step 3.2: BlobStorageService | **MATCH** | None |
| Step 3.3: CopilotService | **PARTIAL** | Filename still `CopilotClientFactory.cs` — class is `CopilotService` |
| Step 3.4: A2A Endpoint | **DEVIATION** | Uses M.Agents SDK `MapA2A` pattern instead of `A2AAgentHandler.cs`/`AgentCard.cs`; no explicit caller validation |
| Step 4.1: System prompt | **MATCH** | None |
| Step 4.2: ReportGenerationService | **MATCH** | None |
| Step 4.3: REST endpoints | **MATCH** | None |

## Follow-on Questions

1. Is the M.Agents A2A SDK middleware handling managed identity caller validation at the transport layer, or does explicit `ChatApiUaiPrincipalId` validation need to be added?
2. Should `CopilotClientFactory.cs` be renamed to `CopilotService.cs` to match the class name?
