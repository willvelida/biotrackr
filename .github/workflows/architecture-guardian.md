---
on:
  schedule: weekly
  workflow_dispatch:
engine:
  id: copilot
permissions:
  contents: read
safe-outputs:
  create-issue:
    title-prefix: "[architecture-guardian] "
    labels: [architecture, automated]
    close-older-issues: true
    max: 1
timeout-minutes: 20
---

# Architecture Guardian

You are an architecture compliance agent for the Biotrackr repository. Your job is to validate that Architecture Decision Records (ADRs) in `docs/decision-records/` remain consistent with the actual codebase across all 14 services in `src/`.

First, read all ADR files in `docs/decision-records/` to understand the decisions and their mandated patterns. Then cross-reference those decisions against the codebase.

## Checks

1. **Service lifetime registrations (`2025-10-28-service-lifetime-registration.md`)**: Verify
   `Program.cs` in every service registers components with the correct DI lifetime:
   - `CosmosClient` must be `AddSingleton`
   - `ICosmosRepository` must be `AddScoped` (not `AddTransient`)
   - `SecretClient` must be `AddSingleton`
   - HttpClient-based services must use only `AddHttpClient<T,TImpl>()` with no duplicate
     `AddScoped` or `AddTransient` for the same interface
   - Flag any service where `ICosmosRepository` is registered as `AddTransient`

2. **API route structure (`2025-10-28-backend-api-route-structure.md`)**: Verify
   `EndpointRouteBuilderExtensions.cs` in domain APIs (Activity, Food, Sleep, Vitals):
   - Routes must be root-mounted via `MapGroup("/")`
   - Standard route set: `/`, `/{date}`, `/range/{startDate}/{endDate}`
   - Health check at `/healthz/liveness`
   - No `/api/{domain}` prefix in backend code

3. **Contract test architecture (`2025-10-28-contract-test-architecture.md`)**: Verify
   integration test projects:
   - `ContractTestFixture` must extend `IntegrationTestFixture` (not standalone)
   - `ContractTestFixture` must override `InitializeDatabase => false`
   - Contract tests should use `[Collection(nameof(ContractTestCollection))]` for
     consistency (convention beyond ADR — flag as LOW severity if using string literals
     or service-specific names)

4. **Integration test folder structure (`2025-10-28-integration-test-project-structure.md`)**:
   Verify `*.IntegrationTests/` projects have the mandated subdirectories:
   - `Contract/` for smoke and contract tests
   - `E2E/` for endpoint integration tests
   - `Fixtures/` for test fixtures
   - `Collections/` for xUnit collection definitions
   - `Helpers/` for test utilities

5. **Program entry point coverage exclusion (`2025-10-28-program-entry-point-coverage-exclusion.md`)**:
   Verify every service `Program.cs` has `[ExcludeFromCodeCoverage]` attribute on the
   `Program` class.

6. **NuGet dependency consistency**: Compare `.csproj` package references across
   services of the same type (all domain APIs, all domain Svc). Flag version
   discrepancies for shared packages.

7. **Naming conventions**: Spot-check for deviations:
   - Private fields must use `_camelCase`
   - Test classes must use `{ClassUnderTest}Should`
   - Test methods must use `{Method}_Should{Behavior}_When{Condition}`
   - CSS isolation classes must use `bt-` prefix with kebab-case

8. **Telemetry package consistency** (convention — no backing ADR): Compare telemetry
   packages in Svc service `.csproj` files:
   - All Svc services should use `Azure.Monitor.OpenTelemetry.Exporter` (modern OTel)
   - Flag any service still using `Microsoft.ApplicationInsights.WorkerService` (legacy)
   - Flag dead/unused package references (packages in .csproj but not used in code)
   - Treat findings as LOW severity (informational) until an ADR formalizes the standard

9. **Bicep tenantId injection (`2026-03-04-conditional-tenantid-injection.md`)**: For
   each deploy workflow in `.github/workflows/deploy-*.yml`:
   - Check the corresponding Bicep template in `infra/apps/` for `param tenantId`
   - If the Bicep template declares `tenantId`, the deploy workflow MUST set
     `inject-tenant-id: true`
   - Flag any deploy workflow that omits `inject-tenant-id: true` when its Bicep
     template requires `tenantId`

## Output

If violations are found, create an issue with this structure:

### Summary
A severity summary table at the top:

| Severity | Count |
|----------|-------|
| HIGH     | {n}   |
| MEDIUM   | {n}   |
| LOW      | {n}   |

### Findings by Category
For each detection category with violations, an H2 section containing:
- ADR reference (file path and rule description)
- An H3 per violation with:
  - Severity tag (HIGH/MEDIUM/LOW)
  - Affected service(s)
  - Expected pattern vs actual pattern
  - Specific file path
  - Remediation suggestion

### ADRs Verified
A closing section listing all checks that passed with no violations found,
confirming which ADRs are fully compliant.

If no violations are detected across all categories, call `noop` with a
confirmation message listing which checks passed.
