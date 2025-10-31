# Research & Analysis: Activity Service Test Coverage and Integration Tests

**Feature**: 005-activity-svc-tests  
**Date**: 2025-10-31  
**Status**: Complete

## Overview

This document consolidates research findings for implementing comprehensive test coverage for the Biotrackr Activity Service. All technical decisions have been resolved through reference to existing patterns and decision records.

---

## Research Areas

### 1. Test Organization Pattern (Contract vs E2E)

**Decision**: Use separate Contract and E2E test namespaces with dedicated fixtures

**Rationale**:
- Contract tests validate service registration and application startup without external dependencies
- E2E tests verify full workflow with Cosmos DB Emulator
- Separation enables parallel execution of fast tests (unit + contract) while E2E tests run sequentially
- Pattern proven successful in Weight Service implementation (003-weight-svc-integration-tests)

**Alternatives Considered**:
- Single test suite with all tests requiring Cosmos DB: Rejected due to slower execution and unnecessary DB overhead for contract tests
- No integration tests, only unit tests: Rejected as insufficient validation of service integration and external dependencies
- Using HttpClient-based tests for all scenarios: Rejected as Activity Service is a background worker, not an API

**References**:
- Decision Record: 2025-10-28-integration-test-project-structure.md
- Decision Record: 2025-10-28-contract-test-architecture.md
- Weight Service Integration Tests Spec (003-weight-svc-integration-tests)

---

### 2. Test Fixture Architecture

**Decision**: Implement separate ContractTestFixture and IntegrationTestFixture classes

**Rationale**:
- ContractTestFixture extends base fixture with `InitializeDatabase => false` for fast startup
- IntegrationTestFixture includes full Cosmos DB Emulator initialization for E2E tests
- Clear separation of concerns makes fixture choice obvious (Contract/ vs E2E/ folder structure)
- Reduces test execution time by avoiding unnecessary database initialization for contract tests
- Pattern successfully implemented in Weight API tests (decision record 2025-10-28-contract-test-architecture.md)

**Implementation Pattern**:
```csharp
// Base fixture with virtual property
public class IntegrationTestFixture : IAsyncLifetime
{
    protected virtual bool InitializeDatabase => true;
    // ... database initialization logic checks this property
}

// Lightweight contract fixture
public class ContractTestFixture : IntegrationTestFixture
{
    protected override bool InitializeDatabase => false;
}
```

**Alternatives Considered**:
- Single fixture with conditional initialization flag passed in constructor: Rejected as less explicit and harder to understand intent
- All tests use full IntegrationTestFixture: Rejected due to unnecessary overhead for contract tests
- Mock database for contract tests: Rejected as contract tests shouldn't need database at all

**References**:
- Decision Record: 2025-10-28-contract-test-architecture.md
- Weight API IntegrationTestFixture implementation

---

### 3. Cosmos DB Connection Mode for Tests

**Decision**: Use ConnectionMode.Gateway for all Cosmos DB Emulator tests

**Rationale**:
- Direct mode (default) uses TCP+SSL (rntbd://) which fails SSL negotiation with Emulator's self-signed certificates
- Gateway mode uses HTTPS (port 8081) which respects ServerCertificateCustomValidationCallback
- Slightly higher latency (~2-3ms) is acceptable for test scenarios
- Proven reliable in Weight Service integration tests

**Implementation**:
```csharp
new CosmosClient(endpoint, key, new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway, // Required for Emulator
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    },
    HttpClientFactory = () => new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    })
});
```

**Alternatives Considered**:
- Direct mode with system-level certificate trust: Rejected as requires manual certificate installation and admin privileges
- Mocking all Cosmos DB operations: Rejected as defeats purpose of integration testing

**References**:
- Common Resolutions: E2E Tests Fail with "SSL negotiation failed"
- Weight Service E2E test configuration

---

### 4. Test Isolation Strategy

**Decision**: Implement ClearContainerAsync() method called at test start to ensure isolation

**Rationale**:
- xUnit Collection Fixtures share database instance across tests in collection
- Tests querying by date find each other's documents when not cleaned up
- Clearing before each test ensures predictable test state
- Proven effective in Weight Service E2E tests

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
    await ClearContainerAsync(); // Ensure clean state
    // ... test logic
}
```

**Alternatives Considered**:
- Unique test data per test: Rejected as doesn't prevent accumulation over time
- Cleanup after test in finally block: Rejected as failure during test leaves orphaned data
- Separate collections per test: Rejected as significantly slower and more resource intensive

**References**:
- Common Resolutions: E2E Tests Find More Documents Than Expected
- Weight Service E2E test cleanup patterns

---

### 5. Flaky Test Handling Policy

**Decision**: Remove flaky tests entirely from test suite

**Rationale**:
- CI environment limitations (Cosmos DB Emulator timeout) cause non-deterministic failures
- Skipping tests (xUnit Skip attribute) leaves test code in place but reduces coverage metrics
- Removing flaky tests maintains high signal-to-noise ratio in CI results
- Differs from Weight API pattern (which used Skip attribute) based on user preference

**Implementation**:
When a test consistently fails in CI but passes locally:
1. Verify the test logic is correct
2. Confirm failure is due to environment constraints, not code issues
3. Remove the test entirely rather than skip it
4. Document the decision in test commit message

**Alternatives Considered**:
- Skip attribute (Weight API pattern): User explicitly chose removal instead
- Increase timeouts and add retries: Rejected as masks underlying issues and slows all tests
- Mock external dependencies for flaky tests: Rejected as changes test intent

**References**:
- Decision Record: 2025-10-28-flaky-test-handling.md (for context, but different policy chosen)
- Clarification session 2025-10-31 (user choice: Option C - Remove flaky tests)

---

### 6. Service Lifetime Registration Patterns

**Decision**: Follow established service lifetime guidelines without duplicate registrations

**Rationale**:
- Azure SDK clients (CosmosClient, SecretClient): Singleton (expensive to create, thread-safe)
- Application services (Repositories, Services): Scoped (one instance per execution scope)
- HttpClient-based services (FitbitService): Transient (managed by HttpClientFactory)
- AddHttpClient<TInterface, TImplementation>() handles registration automatically - no duplicate AddScoped needed

**Testing Implications**:
- Contract tests verify service lifetimes using separate scope instances
- FitbitService should return different instances on each resolution (transient)
- CosmosRepository should return same instance within a scope (scoped)
- CosmosClient should return same instance across all scopes (singleton)

**Alternatives Considered**:
- All services as scoped: Rejected as incorrect for expensive SDK clients and HttpClient services
- Duplicate registrations for clarity: Rejected as second registration overrides first, causing confusion

**References**:
- Decision Record: 2025-10-28-service-lifetime-registration.md
- Common Resolutions: Duplicate Service Registrations with AddHttpClient

---

### 7. Coverage Exclusion for Program.cs

**Decision**: Exclude Program.cs from coverage metrics via coverlet configuration

**Rationale**:
- Program.cs contains only DI registration and configuration setup
- Entry points are better tested through integration/contract tests
- Attempting to unit test Program.cs leads to brittle tests that duplicate integration test coverage
- Industry best practice for application entry points

**Implementation**:
```xml
<PropertyGroup>
    <ExcludeByFile>**/Program.cs</ExcludeByFile>
</PropertyGroup>
```

**Alternatives Considered**:
- Include Program.cs in coverage: Rejected as impractical to unit test and duplicates integration test coverage
- Extract logic from Program.cs: Rejected as unnecessary abstraction for simple DI setup

**References**:
- Decision Record: 2025-10-28-program-entry-point-coverage-exclusion.md
- Weight Service coverage configuration

---

### 8. GitHub Actions Workflow Integration

**Decision**: Add three separate test jobs - unit, contract, E2E - with appropriate dependencies and Cosmos DB Emulator service

**Rationale**:
- Unit and contract tests can run in parallel (both fast, no external dependencies)
- E2E tests run after contract tests and require Cosmos DB Emulator service
- Separate jobs provide clear test execution visibility and allow for targeted reruns
- Test filters enable running specific test suites: FullyQualifiedName~Contract, FullyQualifiedName~E2E

**Workflow Configuration**:
```yaml
run-unit-tests:
  # Existing job, no changes needed

run-contract-tests:
  needs: env-setup
  test-filter: 'FullyQualifiedName~Contract'
  working-directory: ./src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.IntegrationTests

run-e2e-tests:
  needs: [env-setup, run-contract-tests]
  test-filter: 'FullyQualifiedName~E2E'
  working-directory: ./src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.IntegrationTests
  services:
    cosmos:
      image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

**Critical Corrections from Common Resolutions**:
- Use test project directory, NOT solution directory for working-directory
- Add `checks: write` permission for dorny/test-reporter@v1
- Verify DOTNET_VERSION (9.0.x) matches test project TargetFramework (net9.0)

**Alternatives Considered**:
- Single test job for all test types: Rejected as slower (sequential execution) and less clear
- E2E tests in parallel with unit tests: Rejected as Cosmos DB Emulator adds overhead

**References**:
- Common Resolutions: GitHub Actions Workflow Issues
- deploy-weight-service.yml (reference implementation)

---

## Best Practices Summary

### Unit Test Best Practices
1. Follow existing naming conventions: MethodName_Should_ExpectedBehavior
2. Use Moq for mocking dependencies, FluentAssertions for assertions
3. Test edge cases: cancellation, empty responses, exceptions
4. Aim for 70% overall coverage, 85%+ for critical components
5. Keep tests fast (<5 seconds total execution)

### Integration Test Best Practices
1. Separate Contract tests (no DB) from E2E tests (with DB)
2. Use appropriate fixtures: ContractTestFixture vs IntegrationTestFixture
3. Clean test container before each E2E test for isolation
4. Use Gateway mode for Cosmos DB Emulator connections
5. Mock external APIs (Fitbit) to avoid network dependencies
6. Keep contract tests fast (<5 seconds), E2E tests reasonable (<30 seconds)

### CI/CD Best Practices
1. Run fast tests (unit, contract) in parallel
2. Run slow tests (E2E) after fast tests succeed
3. Use correct working-directory paths (test project, not solution)
4. Add required permissions (checks: write for test reporter)
5. Verify .NET version consistency across workflow and projects
6. Remove flaky tests rather than accumulating disabled tests

---

## Technical Unknowns Resolved

All technical context items from the plan have been resolved:

- ✅ Language/Version: C# / .NET 9.0
- ✅ Testing Framework: xUnit 2.9.3 with coverlet
- ✅ Test Organization: Contract vs E2E with separate fixtures
- ✅ Cosmos DB Configuration: Gateway mode for Emulator
- ✅ Test Isolation: ClearContainerAsync pattern
- ✅ Flaky Test Policy: Remove entirely
- ✅ Service Lifetimes: Singleton/Scoped/Transient guidelines
- ✅ Coverage Exclusions: Program.cs excluded
- ✅ Workflow Integration: Three separate jobs with filters

No further research required - ready for Phase 1 (Design & Contracts).
