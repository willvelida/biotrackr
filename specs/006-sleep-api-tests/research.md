# Research: Enhanced Test Coverage for Sleep API

**Feature**: 006-sleep-api-tests  
**Date**: 2025-10-31  
**Status**: Complete

## Overview

This document consolidates research findings for implementing comprehensive test coverage for the Sleep API. Since this feature follows established patterns from Weight API and Activity API implementations, most technical decisions reference proven approaches documented in decision records and common resolutions.

## Research Questions

### 1. Test Organization Strategy

**Decision**: Use separate Contract and E2E namespaces within IntegrationTests project

**Rationale**: 
- Contract tests (fast, no external dependencies) can run in parallel with unit tests
- E2E tests (require Cosmos DB Emulator) run sequentially after contract tests
- Enables test filtering via xUnit `FullyQualifiedName~Contract` and `FullyQualifiedName~E2E`
- Proven pattern from Weight API and Activity API implementations

**Alternatives Considered**:
- Separate test projects for Contract vs E2E: Rejected due to solution complexity and extra project maintenance
- Single integration namespace: Rejected because it forces all integration tests to wait for Cosmos DB Emulator setup, slowing CI/CD

**Reference**: `docs/decision-records/2025-10-28-integration-test-project-structure.md`

---

### 2. Cosmos DB Emulator Connection Mode

**Decision**: Use Gateway connection mode (HTTPS) for test database connections

**Rationale**:
- Direct mode (TCP+SSL via rntbd://) fails with SSL negotiation errors on Cosmos DB Emulator's self-signed certificates
- Gateway mode (HTTPS via port 8081) respects `ServerCertificateCustomValidationCallback`
- Slightly higher latency (~2-3ms) acceptable for test environments
- Required for reliable CI/CD execution with containerized Emulator

**Alternatives Considered**:
- Direct mode with system-level certificate trust: Rejected due to CI/CD complexity and security concerns
- Mock database entirely: Rejected because E2E tests need real database behavior validation

**Reference**: `.specify/memory/common-resolutions.md` section "E2E Tests Fail with SSL negotiation failed"

**Implementation**:
```csharp
new CosmosClient(endpoint, key, new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway,
    HttpClientFactory = () => new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    })
});
```

---

### 3. Test Isolation Strategy

**Decision**: Clear Cosmos DB container before each E2E test method

**Rationale**:
- xUnit Collection Fixtures share database instance across all tests in collection
- Tests querying by date find documents from other tests (all run on same date)
- Clearing container ensures predictable test state
- Prevents flaky tests from data contamination

**Alternatives Considered**:
- Generate unique dates per test: Rejected because it doesn't prevent conflicts and makes test data harder to reason about
- Delete only specific documents: Rejected due to complexity of tracking all created IDs across test lifecycle
- No shared fixtures (new DB per test): Rejected due to severe performance impact (15+ minute test runs)

**Reference**: `.specify/memory/common-resolutions.md` section "E2E Tests Find More Documents Than Expected"

**Implementation**:
```csharp
private async Task ClearContainerAsync()
{
    var query = new QueryDefinition("SELECT c.id, c.documentType FROM c");
    var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);
    
    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();
        foreach (var item in response)
        {
            await _fixture.Container.DeleteItemAsync<dynamic>(
                item.id.ToString(),
                new PartitionKey(item.documentType.ToString()));
        }
    }
}
```

---

### 4. Code Coverage Exclusion Pattern

**Decision**: Use `[ExcludeFromCodeCoverage]` attribute directly in Program.cs

**Rationale**:
- Works consistently across all coverage tools (coverlet.collector, coverlet.msbuild, dotCover)
- .csproj `<ExcludeByFile>` only works with coverlet.msbuild (local), not coverlet.collector (CI/CD)
- Runsettings files add unnecessary complexity
- Proven pattern from Weight Service implementation

**Alternatives Considered**:
- .csproj ExcludeByFile property: Rejected because it doesn't work in GitHub Actions with `dotnet test --collect:"XPlat Code Coverage"`
- Runsettings file: Rejected due to additional file maintenance and template modifications needed

**Reference**: `.specify/memory/common-resolutions.md` section "Program.cs Not Excluded from Code Coverage in CI/CD"

**Implementation**:
```csharp
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        // ... application setup
    }
}
```

---

### 5. Service Registration Pattern for Tests

**Decision**: Never use duplicate service registrations; only register via AddHttpClient<T> for HttpClient-based services

**Rationale**:
- `AddHttpClient<TInterface, TImplementation>()` automatically registers the service as transient
- Duplicate registrations (e.g., `AddScoped` then `AddHttpClient`) cause second to override first
- Creates confusion about actual service lifetime
- Test expectations don't match runtime behavior

**Alternatives Considered**:
- Keep both registrations: Rejected because it's misleading and error-prone
- Force scoped lifetime for HttpClient services: Rejected because it circumvents HttpClientFactory's intended design

**Reference**: `docs/decision-records/2025-10-28-service-lifetime-registration.md`

**Service Lifetime Guidelines**:
| Service Type | Lifetime | Registration Pattern |
|-------------|----------|---------------------|
| CosmosClient, SecretClient | Singleton | `AddSingleton(new CosmosClient(...))` |
| ICosmosRepository, IWeightService | Scoped | `AddScoped<TInterface, TImplementation>()` |
| IFitbitService (HttpClient-based) | Transient | `AddHttpClient<TInterface, TImplementation>()` only |

---

### 6. GitHub Actions Workflow Requirements

**Decision**: Include `checks: write` permission in workflow permissions block

**Rationale**:
- Required for `dorny/test-reporter@v1` to create GitHub check runs with test results
- Without it, test reporting step fails even when tests pass
- Consistent with Weight API and Activity API workflows

**Alternatives Considered**:
- Different test reporting action: Rejected to maintain consistency across all Biotrackr workflows
- Skip test reporting: Rejected because it reduces visibility into test results

**Reference**: `.specify/memory/common-resolutions.md` section "Test Reporter Action Failing with Permissions Error"

**Implementation**:
```yaml
permissions:
  contents: read
  id-token: write
  pull-requests: write
  checks: write  # Required for dorny/test-reporter@v1
```

---

### 7. Working Directory Configuration for Reusable Templates

**Decision**: Pass specific test project directory path, not solution directory

**Rationale**:
- Reusable workflow templates expect test project paths (e.g., `./src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests`)
- Solution paths cause "project not found" errors
- Consistent with corrected Weight Service workflow patterns

**Alternatives Considered**:
- Modify templates to handle solution paths: Rejected to avoid breaking existing workflows
- Use relative paths from solution: Rejected due to inconsistency and error-proneness

**Reference**: `.specify/memory/common-resolutions.md` section "Incorrect Working Directory for Reusable Workflow Templates"

---

### 8. Test Coverage Analysis Baseline

**Current State Assessment**:

Ran existing Sleep API unit tests with coverage:
```bash
dotnet test src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.sln --collect:"XPlat Code Coverage"
```

**Results**:
- 83 unit tests exist in `SleepHandlersShould.cs`
- All tests pass consistently
- Coverage report generated at `TestResults/.../coverage.cobertura.xml`

**Coverage Gaps to Address** (estimated based on Weight/Activity API patterns):
- Models: Settings, PaginationRequest, Fitbit entities likely under-tested
- Extensions: EndpointRouteBuilderExtensions may lack coverage
- Repository: CosmosRepository edge cases (null handling, exceptions)
- Program.cs: Needs `[ExcludeFromCodeCoverage]` attribute

**Decision**: Analyze coverage report to identify specific gaps, then systematically add tests for uncovered paths

**Reference**: Similar approach used in `specs/001-weight-api-tests` and `specs/004-activity-api-tests`

---

## Technology Stack Summary

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| Language | C# | .NET 9.0 | Runtime and compilation |
| Test Framework | xUnit | 2.9.3 | Test execution and organization |
| Assertions | FluentAssertions | 8.4.0 | Readable test assertions |
| Mocking | Moq | 4.20.72 | Dependency isolation |
| Test Data | AutoFixture | 4.18.1 | Test data generation |
| Coverage | coverlet.collector | 6.0.4 | Code coverage collection |
| Integration | AspNetCore.Mvc.Testing | 9.0.0 | WebApplicationFactory |
| Test Database | Cosmos DB Emulator | latest | E2E test database |
| CI/CD | GitHub Actions | N/A | Test automation |

---

## Best Practices Applied

1. **Test Pyramid**: Unit (≥80%) > Contract (fast integration) > E2E (full integration)
2. **Test Independence**: Each test can run in isolation without side effects
3. **Test Isolation**: Clear database between E2E tests to prevent contamination
4. **Gateway Mode**: Use HTTPS for Cosmos DB Emulator to avoid SSL issues
5. **No Duplicate Registrations**: Single registration point per service
6. **Coverage Exclusion**: Use code attributes not build configuration
7. **Parallel Execution**: Contract tests run parallel with unit tests
8. **Clear Naming**: Test names describe what/when/then clearly
9. **Shared Fixtures**: Reuse expensive resources (app factory, database) via Collection Fixtures
10. **Time Limits**: Explicit timeouts per test category to catch performance regressions

---

## References

- Decision Records: `docs/decision-records/2025-10-28-*.md`
- Common Resolutions: `.specify/memory/common-resolutions.md`
- Weight API Tests: `specs/001-weight-api-tests/`
- Activity API Tests: `specs/004-activity-api-tests/`
- GitHub Copilot Instructions: `.github/copilot-instructions.md`
