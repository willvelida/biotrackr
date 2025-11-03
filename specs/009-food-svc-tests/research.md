# Research: Food Service Test Coverage and Integration Tests

**Feature**: 009-food-svc-tests  
**Date**: November 3, 2025  
**Status**: Complete

## Overview

This document captures research findings for implementing comprehensive test coverage and integration tests for the Food Service. Research focused on established patterns from Weight Service (003), Activity Service (005), and Sleep Service (007) to ensure consistency across the Biotrackr codebase.

## Research Areas

### 1. Test Project Structure & Organization

**Decision**: Use separate IntegrationTests project with Contract/ and E2E/ namespaces

**Rationale**:
- Follows established pattern across Weight, Activity, and Sleep services
- Contract tests can run in parallel with unit tests (no external dependencies)
- E2E tests run separately with Cosmos DB Emulator (isolated execution)
- xUnit Collection Fixtures provide proper test isolation and resource sharing
- Clear separation of concerns improves test maintainability

**Alternatives Considered**:
- Single test project with all test types: Rejected because it makes test filtering difficult and prevents parallel execution of fast tests
- Separate projects for Contract and E2E: Rejected as overkill for current scale; namespace separation is sufficient

**Implementation References**:
- `src/Biotrackr.Weight.Svc/Biotrackr.Weight.Svc.IntegrationTests/` (original pattern)
- `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests/` (refined pattern)
- Decision record: `docs/decision-records/2025-10-28-integration-test-project-structure.md`

---

### 2. Service Lifetime Registration Patterns

**Decision**: Remove duplicate `AddScoped<IFitbitService, FitbitService>()` registration; keep only `AddHttpClient<IFitbitService, FitbitService>()`

**Rationale**:
- `AddHttpClient` automatically registers the service as Transient
- Duplicate registration causes the second call to override the first
- Transient lifetime is correct for HttpClient-based services (HttpClientFactory manages pooling)
- Having two registrations is confusing and violates single registration principle

**Service Lifetime Guidelines**:
| Service Type | Lifetime | Registration Pattern |
|-------------|----------|---------------------|
| CosmosClient | Singleton | `services.AddSingleton(new CosmosClient(...))` |
| SecretClient | Singleton | `services.AddSingleton(new SecretClient(...))` |
| ICosmosRepository | Scoped | `services.AddScoped<ICosmosRepository, CosmosRepository>()` |
| IFoodService | Scoped | `services.AddScoped<IFoodService, FoodService>()` |
| IFitbitService | Transient | `services.AddHttpClient<IFitbitService, FitbitService>()` |

**Alternatives Considered**:
- Keep duplicate registration and update tests: Rejected because duplicate registration is anti-pattern
- Force scoped lifetime for FitbitService: Rejected because it circumvents HttpClientFactory design

**Implementation References**:
- `src/Biotrackr.Weight.Svc/Program.cs` (corrected pattern)
- Decision record: `docs/decision-records/2025-10-28-service-lifetime-registration.md`
- Common resolutions: `.specify/memory/common-resolutions.md` (Service Lifetime section)

---

### 3. Code Coverage Exclusion for Program.cs

**Decision**: Use `[ExcludeFromCodeCoverage]` attribute directly in Program.cs

**Rationale**:
- Works consistently across all coverage tools (coverlet.collector, coverlet.msbuild, dotCover)
- No need for runsettings files or separate configuration
- Recognized by both local and CI/CD coverage collection
- Follows established pattern from Weight Service

**Implementation Pattern**:
```csharp
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        // Host builder configuration
        host.Run();
    }
}
```

**Alternatives Considered**:
- `.csproj` `<ExcludeByFile>` property: Rejected because it only works with coverlet.msbuild, not coverlet.collector used in CI
- Runsettings file configuration: Rejected as unnecessary complexity when attribute works everywhere

**Implementation References**:
- `src/Biotrackr.Weight.Svc/Biotrackr.Weight.Svc/Program.cs` (example)
- Decision record: `docs/decision-records/2025-10-28-program-entry-point-coverage-exclusion.md`
- Common resolutions: `.specify/memory/common-resolutions.md` (Code Coverage Exclusions section)

---

### 4. Cosmos DB Connection Mode for E2E Tests

**Decision**: Use `ConnectionMode.Gateway` for all E2E tests with Cosmos DB Emulator

**Rationale**:
- Gateway mode (HTTPS port 8081) respects `ServerCertificateCustomValidationCallback`
- Direct mode (TCP+SSL port 10251) requires system-level certificate trust
- Local emulator self-signed certificates work better with HTTPS than TCP+SSL
- Slightly higher latency (~2-3ms) is acceptable for test scenarios

**Implementation Pattern**:
```csharp
services.AddSingleton<CosmosClient>(sp =>
{
    return new CosmosClient(cosmosDbEndpoint, cosmosDbAccountKey, new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway, // Force Gateway mode (HTTPS only)
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        },
        HttpClientFactory = () => new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        })
    });
});
```

**Alternatives Considered**:
- Direct mode with certificate trust: Rejected because it requires system-level changes and fails in CI/CD
- Installing emulator certificate: Rejected as too complex for test setup

**Implementation References**:
- `src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.IntegrationTests/Fixtures/IntegrationTestFixture.cs`
- Common resolutions: `.specify/memory/common-resolutions.md` (E2E Test Issues - SSL negotiation)

---

### 5. E2E Test Isolation with Container Cleanup

**Decision**: Implement `ClearContainerAsync()` method called at start of each E2E test

**Rationale**:
- xUnit Collection Fixtures share database instance across all tests in collection
- Tests querying by date can find documents from previous tests (all run on same date)
- Cleanup ensures predictable state for each test
- Prevents "expected 1 document but found 3" errors

**Implementation Pattern**:
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

[Fact]
public async Task MyTest()
{
    // Arrange - Clear container for test isolation
    await ClearContainerAsync();
    
    // Act & Assert
    // ...
}
```

**Alternatives Considered**:
- Unique test data per test: Rejected because date-based queries still find all documents with same date
- Separate collections per test: Rejected as too slow and resource-intensive
- Cleanup after test: Rejected because test failures can leave data behind

**Implementation References**:
- `src/Biotrackr.Weight.Svc/Biotrackr.Weight.Svc.IntegrationTests/E2E/WeightServiceTests.cs`
- Common resolutions: `.specify/memory/common-resolutions.md` (E2E Test Isolation section)

---

### 6. Strongly-Typed Models vs Dynamic in E2E Tests

**Decision**: Always use strongly-typed models (e.g., `FoodDocument`) when querying Cosmos DB in E2E tests

**Rationale**:
- `dynamic` types cause `RuntimeBinderException` with FluentAssertions in CI/CD
- Runtime binder cannot resolve extension methods on dynamic types in certain environments
- Strongly-typed models work consistently across all environments
- Only use `dynamic` for cleanup operations where structure doesn't matter

**Implementation Pattern**:
```csharp
// ❌ Wrong - causes RuntimeBinderException in CI/CD
var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);
var documents = new List<dynamic>();
// ... populate documents
savedDoc.id.Should().Be(expected); // RuntimeBinderException!

// ✅ Correct - works everywhere
var iterator = _fixture.Container.GetItemQueryIterator<FoodDocument>(query);
var documents = new List<FoodDocument>();
// ... populate documents
savedDoc.Id.Should().Be(expected); // Works!
```

**Alternatives Considered**:
- Cast dynamic to concrete type before assertions: Rejected as unnecessary ceremony when we know the type
- Avoid FluentAssertions with dynamic: Rejected because FluentAssertions improves test readability

**Implementation References**:
- `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests/E2E/SleepServiceTests.cs` (fixed)
- Common resolutions: `.specify/memory/common-resolutions.md` (RuntimeBinderException section)

---

### 7. GitHub Actions Workflow Test Jobs

**Decision**: Add separate jobs for contract tests (parallel) and E2E tests (sequential) in deploy-food-service.yml

**Rationale**:
- Contract tests run fast (<5s) and don't need external dependencies
- Can run in parallel with unit tests for faster feedback
- E2E tests need Cosmos DB Emulator service (slower, ~30s)
- Run E2E tests after contract tests to catch issues early
- Separate jobs allow independent failure analysis

**Workflow Pattern**:
```yaml
run-contract-tests:
    name: Run Contract Tests
    needs: env-setup
    uses: willvelida/biotrackr/.github/workflows/template-dotnet-run-contract-tests.yml@main
    with:
        dotnet-version: ${{ needs.env-setup.outputs.dotnet-version }}
        working-directory: ./src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests
        test-filter: 'FullyQualifiedName~Contract'

run-e2e-tests:
    name: Run E2E Tests
    needs: [env-setup, run-contract-tests]
    uses: willvelida/biotrackr/.github/workflows/template-dotnet-run-e2e-tests.yml@main
    with:
        dotnet-version: ${{ needs.env-setup.outputs.dotnet-version }}
        working-directory: ./src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests
        test-filter: 'FullyQualifiedName~E2E'
```

**Required Permissions**:
```yaml
permissions:
    contents: read
    id-token: write
    pull-requests: write
    checks: write  # Required for dorny/test-reporter@v1
```

**Alternatives Considered**:
- Single test job for all test types: Rejected because contract tests would wait for E2E setup
- E2E tests in parallel with contract tests: Rejected because E2E is slower and less reliable

**Implementation References**:
- `.github/workflows/deploy-weight-service.yml` (complete pattern)
- `.github/workflows/deploy-sleep-service.yml` (refined pattern)
- Common resolutions: `.specify/memory/common-resolutions.md` (GitHub Actions section)

---

### 8. Test Package Versions & Dependencies

**Decision**: Use standardized package versions across all service test projects

**Rationale**:
- Consistency across microservices simplifies maintenance
- Proven versions from Weight/Activity/Sleep services
- Compatible with .NET 9.0 target framework

**Package Manifest**:
```xml
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="FluentAssertions" Version="8.4.0" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="AutoFixture" Version="4.18.1" />
<PackageReference Include="coverlet.collector" Version="6.0.4" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

**Note**: Do NOT include `coverlet.msbuild` package. Use only `coverlet.collector` for CI/CD compatibility.

**Alternatives Considered**:
- Latest package versions: Rejected to maintain consistency with existing services
- Different test frameworks: Rejected because xUnit is project standard

**Implementation References**:
- `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests/Biotrackr.Sleep.Svc.IntegrationTests.csproj`
- Decision record: `docs/decision-records/2025-10-28-integration-test-project-structure.md`

---

## Summary of Key Decisions

1. **Test Structure**: Contract/ and E2E/ namespaces in IntegrationTests project
2. **Service Registration**: Remove duplicate FitbitService registration (keep only AddHttpClient)
3. **Coverage Exclusion**: Use [ExcludeFromCodeCoverage] attribute on Program.cs
4. **Cosmos Connection**: ConnectionMode.Gateway for E2E tests
5. **Test Isolation**: ClearContainerAsync() at start of each E2E test
6. **Type Safety**: Use FoodDocument instead of dynamic in E2E queries
7. **Workflow Jobs**: Separate contract (parallel) and E2E (sequential) test jobs
8. **Package Versions**: Standardized versions matching Sleep Service

## Implementation Confidence

**High Confidence** - All decisions based on:
- Established patterns from 3 existing services (Weight, Activity, Sleep)
- Documented decision records
- Common resolutions for known issues
- Proven workflow configurations

No significant unknowns remain. Ready for implementation planning (Phase 1).
