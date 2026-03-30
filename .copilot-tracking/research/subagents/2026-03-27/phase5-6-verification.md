# Phase 5 & 6 Verification Report

## Research Questions

Verify that planned files for Phase 5 (Chat.Api Integration) and Phase 6 (CI/CD) exist and match specifications.

---

## Step 5.1: RequestReportTool

- **Path**: src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Tools/RequestReportTool.cs
- **Status**: **PARTIAL**

### What matches

- AIFunction registration via `AsAIFunction()` method using `AIFunctionFactory.Create`
- Accepts `reportType`, `startDate`, `endDate`, `taskMessage` parameters
- Sends POST to Reporting.Api `/api/reports/generate`
- Receives job ID from `GenerateResponse` and returns user-friendly confirmation
- Description includes available report types: weekly_summary, monthly_summary, trend_analysis, diet_analysis, correlation_report

### What's missing or different

1. **No input validation**: The plan specified validation for:
   - Report type (valid enum check) — not implemented
   - Date format (yyyy-MM-dd) — not validated
   - Date range max 365 days — not enforced
   The tool sends whatever Claude passes directly to the API with no local guards.

2. **Data gathering approach**: The plan said the tool should "gather health data via MCP tools and package as structured JSON". Instead, the implementation delegates this entirely to Claude — the `taskMessage` parameter description says "Natural language instruction describing what to include in the report, along with the structured health data as a JSON block", and `sourceDataSnapshot` is an empty object with the comment `// Claude will embed the data in taskMessage`. This is a design deviation — Claude packages data inline rather than the tool fetching it programmatically.

---

## Step 5.2: GetReportStatusTool

- **Path**: src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Tools/GetReportStatusTool.cs
- **Status**: **MATCH**

### What matches

- Queries `GET /api/reports/{jobId}` on Reporting.Api
- Handles all three statuses: `generating` (wait message), `failed` (error + retry suggestion), `generated`/`reviewed` (proceeds to review)
- Invokes `ReportReviewerService.ReviewReportAsync()` for generated reports
- Presents SAS URLs as markdown download links from `ArtifactUrls` dictionary
- Presents validated summary from reviewer
- AIFunction registration via `AsAIFunction()`
- Handles not-found (404) and error responses gracefully

### No deviations found

---

## Step 5.2b: ReportReviewerService

- **Path**: src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Tools/ReportReviewerService.cs
- **Status**: **PARTIAL**

### What matches

- Located in Tools/ directory (plan noted it might be in Services/ or Tools/)
- Creates a one-shot Claude AIAgent with reviewer prompt via `AnthropicClient.AsAIAgent()`
- Passes report summary + source data snapshot + report type
- Parses JSON response with `approved`/`concerns`/`validatedSummary` fields
- Fallback on error: returns approved=true with warning disclaimer appended
- Falls through to default if JSON parsing fails

### What's different

1. **Agent creation pattern**: Uses `AnthropicClient` directly and calls `.AsAIAgent()` extension method, not the Microsoft Agent Framework `AIAgent` constructor from the plan. The plan referenced Microsoft Agent Framework's `AIAgent` class — this uses Anthropic SDK's extension. Functionally equivalent but different API surface.
2. **Reviewer prompt source**: Uses `_settings.ReviewerSystemPrompt` (loaded from configuration). If the prompt is not configured, it skips review entirely (returns approved). This is reasonable graceful degradation but means the prompt must be deployed to App Configuration / Key Vault for the reviewer to function.

---

## Step 5.3: Reviewer Prompt

- **Path**: scripts/reporting-api-prompts/reviewer-prompt.txt
- **Status**: **MATCH**

### What matches

- Instructions for validating report output against source data
- Checks calorie targets (flags below 1200/day or above 4000/day)
- Checks trend accuracy (verifies trend descriptions match data direction)
- Data consistency checks (cross-references summary statistics)
- Health disclaimers check (adds if missing)
- Output JSON format: `{ "approved", "concerns", "validatedSummary" }`
- Conservative review stance: "flag potential issues rather than missing them"
- Rules against providing medical advice

### No deviations found

---

## Step 5.4: Chat.Api System Prompt Update

- **Path**: scripts/chat-system-prompt/system-prompt.txt
- **Status**: **MATCH**

### What matches

- Contains "Report Generation" section with routing guidance
- Directs use of `RequestReport` tool for reports, charts, visualizations, diet programs, trend analyses
- Directs use of `GetReportStatus` tool with job ID for status checks
- Lists available report types: weekly_summary, monthly_summary, trend_analysis, diet_analysis, correlation_report
- **Negative guidance present**: "Do NOT use RequestReport for simple health data queries" with examples (sleep last night, step count on Monday)
- Clear boundary: only use RequestReport for explicit report/PDF/chart/visualization/diet program/multi-day analysis requests

### No deviations found

---

## Step 6.1: deploy-reporting-api.yml

- **Path**: .github/workflows/deploy-reporting-api.yml
- **Status**: **MATCH**

### What matches

- **Trigger**: PR to main for `infra/apps/reporting-api/**` and `src/Biotrackr.Reporting.Api/**`
- **Unit tests**: Uses template-dotnet-run-unit-tests.yml with 70% coverage threshold
- **Contract tests**: Uses template-dotnet-run-contract-tests.yml
- **Container image**: Build and push via template-acr-push-image.yml
- **Bicep pipeline**: lint → validate → preview (what-if) → deploy-dev
- **E2E tests**: Runs after deployment against dev environment
- **Follows deploy-chat-api.yml pattern**: Same job structure (env-setup → unit tests + contract tests → build image → retrieve image → lint → validate → preview → deploy → e2e tests)

### Minor differences from deploy-chat-api.yml pattern

1. **Sidecar image parameter**: Reporting API passes both `imageName` and `sidecarImageName` parameters to Bicep templates (for the Python copilot sidecar), while Chat API only passes `imageName`. This is expected — Reporting.Api has a Python sidecar.
2. **Permissions**: Both have identical permissions block (contents:write, id-token:write, pull-requests:write, checks:write)

### No deviations from plan

---

## Step 6.2: App Configuration Entries

- **Status**: **MATCH** — defined in Bicep

### Entries found in infra/apps/reporting-api/main.bicep

| Entry | Key | Source |
|---|---|---|
| Reporting API APIM endpoint | `reportingApiEndpointConfigName` (param) | APIM gateway URL |
| Blob Storage endpoint | `Biotrackr:ReportingBlobStorageEndpoint` | Storage account primary blob endpoint |
| Copilot CLI URL | `Biotrackr:CopilotCliUrl` | `http://localhost:4321` (sidecar) |
| Chat.Api UAI Principal ID | `Biotrackr:ChatApiUaiPrincipalId` | UAI principal ID (for A2A auth) |
| Report Generator System Prompt | `Biotrackr:ReportGeneratorSystemPrompt` | Key Vault reference |
| GitHub Copilot Token | Key Vault secret `GitHubCopilotToken` | Populated manually |

All entries are defined in Bicep, not just documented.

---

## Summary Table

| Item | Status | Key Issues |
|---|---|---|
| Step 5.1: RequestReportTool | **PARTIAL** | No input validation (type, date format, 365-day max); data gathering delegated to Claude instead of tool fetching via MCP |
| Step 5.2: GetReportStatusTool | **MATCH** | — |
| Step 5.2b: ReportReviewerService | **PARTIAL** | Uses Anthropic SDK `.AsAIAgent()` not MS Agent Framework constructor; prompt loaded from settings not hardcoded |
| Step 5.3: Reviewer prompt | **MATCH** | — |
| Step 5.4: System prompt update | **MATCH** | — |
| Step 6.1: deploy-reporting-api.yml | **MATCH** | Adds sidecar image param (expected) |
| Step 6.2: App Configuration | **MATCH** | 6 entries defined in Bicep |

## Clarifying Questions

None — all files could be read and evaluated.
