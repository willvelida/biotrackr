---
on:
  pull_request:
    types: [opened, synchronize]
engine:
  id: copilot
permissions:
  contents: read
  pull-requests: read
  actions: read
  discussions: read
  issues: read
  security-events: read
rate-limit:
  max: 5
  window: 60
safe-outputs:
  add-comment:
    max: 1
    target: "triggering"
timeout-minutes: 15
tools:
  github:
    toolsets: [all]
---

# Breaking Change Checker

Detect breaking changes across the Biotrackr microservices platform and post deployment ordering recommendations.

{{#runtime-import instructions/cosmos-conventions.instructions.md}}

## Instructions

### Step 1: Fetch and Filter

1. Fetch the PR diff and identify all changed files.
2. Exit with `noop` if no changes affect any of the following: API endpoint definitions, document models (`*Document.cs`), APIM Bicep configuration (`infra/apps/*/main.bicep`), MCP tool definitions, Cosmos DB schema attributes, handler method signatures, or inter-service client contracts.

### Step 2: Detect Breaking Changes

Analyze the diff for these 7 categories of breaking changes:

1. **API route changes**: Look for modifications to `MapGet`, `MapPost`, `MapPut`, `MapDelete` calls in `*EndpointRouteBuilderExtensions.cs` files. Flag route path changes, HTTP method changes, or removed endpoints.
2. **Response model changes**: Look for property removals or renames in `*Document.cs` files. Adding new optional properties is safe; removing or renaming existing properties is breaking.
3. **APIM config changes**: Look for changes to `urlTemplate` or `method` values in `infra/apps/*/main.bicep` files. These must stay synchronized with API route definitions.
4. **MCP tool schema changes**: Look for changes to methods decorated with `[McpServerTool]` — parameter additions/removals/renames, return type changes, or method renames. The MCP SDK auto-converts PascalCase to snake_case, so method renames change the tool name.
5. **Cosmos DB schema changes**: Look for changes to `[JsonPropertyName]` attribute values in document model classes. These affect stored data and query compatibility.
6. **Query parameter changes**: Look for changes to handler method signatures (parameters added, removed, renamed, or type-changed) in `*Handlers.cs` files. These affect API consumers.
7. **Inter-service client drift**: When a provider service changes its API contract, check whether consuming services have been updated in the same PR. Flag cases where a provider changed without a corresponding consumer update.

### Step 3: Cross-Reference Service Dependencies

Use this dependency map to identify affected consumers:

| Provider | Consumers |
|----------|-----------|
| Activity API | MCP Server (`get_activity_*` tools), UI (`BiotrackrApiService`), APIM Bicep (`infra/apps/activity-api/`) |
| Food API | MCP Server (`get_food_*` tools), UI (`BiotrackrApiService`), APIM Bicep (`infra/apps/food-api/`) |
| Sleep API | MCP Server (`get_sleep_*` tools), UI (`BiotrackrApiService`), APIM Bicep (`infra/apps/sleep-api/`) |
| Vitals API | MCP Server (`get_weight_*` tools), UI (`BiotrackrApiService`), APIM Bicep (`infra/apps/vitals-api/`) |
| Chat API | UI (`ChatApiService`), APIM Bicep (`infra/apps/chat-api/`) |
| Reporting API | Reporting Svc (`ReportingApiService`), APIM Bicep (`infra/apps/reporting-api/`) |
| MCP Server | Chat API (`McpToolService`) |

### Step 4: Classify and Report

Classify each finding into one of three severity levels:

- **SAFE**: No cross-service impact. The change can be deployed independently without affecting other services.
- **ORDERED**: Cross-service impact exists but can be handled with phased deployment. Deploy the provider first, then consumers. Or deploy consumers first if the change is additive.
- **COORDINATED**: Breaking change requires all affected services to be deployed together in the same release. Downtime risk if deployed independently.

### Step 5: Post Comment

Post a single structured comment on the PR with:

```
## Breaking Change Analysis

### Summary
{number} potential breaking change(s) detected across {number} category/categories.

### Findings

| # | Category | File | Change | Severity | Affected Consumers |
|---|----------|------|--------|----------|---------------------|
| 1 | {category} | {file path} | {description} | {SAFE/ORDERED/COORDINATED} | {consumer list} |

### Deployment Recommendations

{For ORDERED findings: specify deployment sequence}
{For COORDINATED findings: list all services that must deploy together}
{For SAFE findings: note independent deployment is fine}

### Notes
- SAFE: Deploy independently
- ORDERED: Deploy in specified sequence (provider → consumer or consumer → provider)
- COORDINATED: Deploy all affected services together
```

If no breaking changes are detected after analysis, call `noop`.
