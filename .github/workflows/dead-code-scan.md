---
on:
  schedule: weekly
  workflow_dispatch:
engine:
  id: copilot
permissions:
  contents: read
  issues: read
safe-outputs:
  create-issue:
    title-prefix: "[dead-code] "
    labels: [cleanup, automated]
    close-older-issues: true
    max: 1
timeout-minutes: 30
---

# Dead Code Scan

You are a code hygiene analyst for the Biotrackr repository. Scan services for dead code and report findings as a structured GitHub issue.

## Knowledge Base

{{#runtime-import .github/workflows/shared/dotnet-knowledge.md}}

## Service Inventory

| Service | Main Project | Test Projects |
|---------|-------------|---------------|
| Activity API | `src/Biotrackr.Activity.Api/Biotrackr.Activity.Api/` | `*.UnitTests/`, `*.IntegrationTests/` |
| Activity Svc | `src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc/` | `*.UnitTests/`, `*.IntegrationTests/` |
| Auth Svc | `src/Biotrackr.Auth.Svc/Biotrackr.Auth.Svc/` | `*.UnitTests/`, `*.IntegrationTests/` |
| Chat API | `src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/` | `*.UnitTests/`, `*.IntegrationTests/` |
| Food API | `src/Biotrackr.Food.Api/Biotrackr.Food.Api/` | `*.UnitTests/`, `*.IntegrationTests/` |
| Food Svc | `src/Biotrackr.Food.Svc/Biotrackr.Food.Svc/` | `*.UnitTests/`, `*.IntegrationTests/` |
| MCP Server | `src/Biotrackr.Mcp.Server/Biotrackr.Mcp.Server/` | `*.UnitTests/`, `*.IntegrationTests/` |
| Reporting API | `src/Biotrackr.Reporting.Api/Biotrackr.Reporting.Api/` | `*.UnitTests/`, `*.IntegrationTests/` |
| Reporting Svc | `src/Biotrackr.Reporting.Svc/Biotrackr.Reporting.Svc/` | `*.UnitTests/`, `*.IntegrationTests/` |
| Sleep API | `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api/` | `*.UnitTests/`, `*.IntegrationTests/` |
| Sleep Svc | `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc/` | `*.UnitTests/`, `*.IntegrationTests/` |
| UI | `src/Biotrackr.UI/Biotrackr.UI/` | `*.UnitTests/` |
| Vitals API | `src/Biotrackr.Vitals.Api/Biotrackr.Vitals.Api/` | `*.UnitTests/`, `*.IntegrationTests/` |
| Vitals Svc | `src/Biotrackr.Vitals.Svc/Biotrackr.Vitals.Svc/` | `*.UnitTests/`, `*.IntegrationTests/` |

## Rotation Strategy

This workflow scans **1-2 services per run** using a rotation strategy:

1. Search for the most recent issue with the `[dead-code]` title prefix.
2. Read its body to find the **Service Scan Tracker** table showing last-scanned dates.
3. Select the **2 services** with the oldest (or missing) scan dates.
4. If a `workflow_dispatch` was triggered, check if the triggering user specified a service name in the run context — if so, scan that service instead.

If no previous `[dead-code]` issue exists, start with the first two services alphabetically (Activity API, Activity Svc).

## Detection Categories

For the selected service(s), read all `.cs` files in the main project and test projects. Scan for:

### 1. Unused Private Methods

Find `private` methods that are never called by any other method in the same class. Check every method call, delegate reference, and `nameof()` usage in the class.

**Skip:** Methods with `[ExcludeFromCodeCoverage]`, event handler methods (matching `On*` pattern subscribed via `+=`), methods referenced by attributes or source generators.

### 2. Unused Private Fields

Find `private` fields (`_camelCase` pattern) that are declared but never read or written after initialization. Check constructor assignments, property accessors, and method bodies.

**Skip:** Fields injected via constructor and used by any method. Fields backing `[Parameter]` attributes in Blazor components.

### 3. Dead Using Directives

Find `using` statements that import namespaces not referenced by any type in the file. Account for `ImplicitUsings` being enabled (common namespaces like `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks` are automatically available).

**Skip:** `using` directives in files with `global using` statements. Be conservative — if unsure whether an extension method requires the using, do not flag it.

### 4. Orphaned Test Helpers

In test projects, find public methods and classes in `Helpers/`, `Fixtures/`, and `Tools/` folders that are never referenced by any test class (`*Should.cs` files).

**Skip:** `IntegrationTestFixture`, `ContractTestFixture`, and classes implementing `IAsyncLifetime` or `IClassFixture<>` — these are wired via xUnit attributes.

### 5. Unreferenced Internal Classes

Find `internal` or `public` classes/interfaces in the main project that are never instantiated, inherited, or referenced by any other file in the same project. Cross-check against:
- DI registrations in `Program.cs` and `*Extensions.cs` files
- Interface implementations
- Attribute-based discovery (`[ApiController]`, `[McpServerToolType]`, etc.)
- `WebApplicationFactory` usage in test projects

**Skip:** Document model classes (`*Document.cs`), DTO/response types returned by handlers, middleware classes registered in the pipeline.

## Process

1. Determine which service(s) to scan using the Rotation Strategy above.
2. List all `.cs` files in the main project directory and test project directories.
3. Read each file and apply the detection categories.
4. For test projects, focus on files in `Helpers/`, `Fixtures/`, and `Tools/` folders.
5. Track findings by file path, line number, category, and confidence level (High/Medium).

## Output

If findings exist, create an issue with:

### Title

`[dead-code] {Service Name(s)} scan — {date} — {count} findings`

### Body Structure

```
## Summary

| Category | Count | High Confidence | Medium Confidence |
|----------|-------|-----------------|-------------------|
| Unused private methods | N | N | N |
| Unused private fields | N | N | N |
| Dead using directives | N | N | N |
| Orphaned test helpers | N | N | N |
| Unreferenced classes | N | N | N |
| **Total** | **N** | **N** | **N** |

## Findings

### {Service Name}

#### Unused Private Methods
- `ClassName.MethodName()` in `path/to/file.cs` (line N) — **High confidence**
  - Reason: Not called anywhere in the class

#### Dead Using Directives
- `using Namespace;` in `path/to/file.cs` (line N) — **Medium confidence**
  - Reason: No types from this namespace are referenced

[...repeat for each category with findings...]

## Service Scan Tracker

| Service | Last Scanned | Findings |
|---------|-------------|----------|
| Activity API | {date or "—"} | {count or "—"} |
| Activity Svc | {date or "—"} | {count or "—"} |
| Auth Svc | {date or "—"} | {count or "—"} |
| Chat API | {date or "—"} | {count or "—"} |
| Food API | {date or "—"} | {count or "—"} |
| Food Svc | {date or "—"} | {count or "—"} |
| MCP Server | {date or "—"} | {count or "—"} |
| Reporting API | {date or "—"} | {count or "—"} |
| Reporting Svc | {date or "—"} | {count or "—"} |
| Sleep API | {date or "—"} | {count or "—"} |
| Sleep Svc | {date or "—"} | {count or "—"} |
| UI | {date or "—"} | {count or "—"} |
| Vitals API | {date or "—"} | {count or "—"} |
| Vitals Svc | {date or "—"} | {count or "—"} |

Update this table with the current scan date and findings count for the scanned service(s). Carry forward previous dates from the last `[dead-code]` issue.

## Notes

- Medium confidence findings may be false positives. Verify before removing.
- Extension method usings cannot be reliably detected without compiler analysis.
- DI-registered classes may appear unreferenced but are resolved at runtime.
- Full coverage cycle: ~7 weeks (2 services/week).
```

If no findings are detected in the scanned service(s), call `noop` with: "Dead code scan complete — {Service Name(s)}: 0 findings."
