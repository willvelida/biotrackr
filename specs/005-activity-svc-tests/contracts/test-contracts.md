# Test Contracts: Activity Service Integration Tests

**Feature**: 005-activity-svc-tests  
**Date**: 2025-10-31

## Overview

This document defines the contracts (interfaces and expectations) for Activity Service integration tests. Since this is a background service (not an API), contracts focus on test interfaces, fixture behaviors, and test execution expectations rather than HTTP endpoints.

---

## Test Fixture Contracts

### IAsyncLifetime Contract

All test fixtures MUST implement xUnit's IAsyncLifetime interface:

```csharp
public interface IAsyncLifetime
{
    Task InitializeAsync();
    Task DisposeAsync();
}
```

**Contract Requirements**:
- `InitializeAsync` MUST complete before any tests execute
- `DisposeAsync` MUST execute after all tests complete
- Both methods MUST be idempotent
- Exceptions in `InitializeAsync` MUST fail all tests in collection
- Exceptions in `DisposeAsync` MUST NOT fail tests but should be logged

---

### ContractTestFixture Contract

```csharp
public class ContractTestFixture : IntegrationTestFixture
{
    protected override bool InitializeDatabase => false;
}
```

**Contract Guarantees**:
- MUST provide IServiceProvider with all services registered
- MUST NOT initialize Cosmos DB connection
- MUST use in-memory IConfiguration
- InitializeAsync MUST complete in <1 second
- MUST support parallel test execution

**Required Configuration Values**:
```csharp
{
    "keyvaulturl": "https://test-vault.vault.azure.net/",
    "managedidentityclientid": "00000000-0000-0000-0000-000000000000",
    "cosmosdbendpoint": "https://localhost:8081",
    "applicationinsightsconnectionstring": "InstrumentationKey=test-key"
}
```

**Service Resolution Contract**:
All registered services MUST be resolvable:
- `CosmosClient` (Singleton)
- `SecretClient` (Singleton)
- `ICosmosRepository` (Scoped)
- `IActivityService` (Scoped)
- `IFitbitService` (Transient)

---

### IntegrationTestFixture Contract

```csharp
public class IntegrationTestFixture : IAsyncLifetime
{
    protected virtual bool InitializeDatabase => true;
    
    public CosmosClient CosmosClient { get; private set; }
    public Database Database { get; private set; }
    public Container Container { get; private set; }
    public IServiceProvider ServiceProvider { get; private set; }
}
```

**Contract Guarantees**:
- MUST initialize Cosmos DB Emulator connection with Gateway mode
- MUST create test database and container
- MUST configure ServerCertificateCustomValidationCallback for Emulator
- MUST delete test database in DisposeAsync
- InitializeAsync MUST complete in <10 seconds
- Container MUST use partition key "/documentType"

**Cosmos DB Configuration Contract**:
```csharp
new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway, // REQUIRED
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    },
    HttpClientFactory = () => new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    })
}
```

**Database Schema Contract**:
- Database ID: "BiotrackrTestDb"
- Container ID: "ActivityTestContainer"
- Partition Key Path: "/documentType"
- Throughput: Default (400 RU/s)

---

## Test Execution Contracts

### Contract Test Execution

**Performance Contract**:
- Entire contract test suite MUST execute in <5 seconds
- Individual contract tests MUST execute in <500ms
- Contract tests MUST run in parallel

**Isolation Contract**:
- Contract tests MUST NOT make network calls
- Contract tests MUST NOT access Cosmos DB
- Contract tests MUST NOT have side effects
- Contract tests MUST be deterministic

**xUnit Filter Contract**:
```bash
dotnet test --filter "FullyQualifiedName~Contract"
```

Must execute only tests in `Contract/` namespace.

---

### E2E Test Execution

**Performance Contract**:
- Entire E2E test suite MUST execute in <30 seconds
- Individual E2E tests MUST execute in <5 seconds
- E2E tests MAY run sequentially within collection

**Isolation Contract**:
- Each E2E test MUST call `ClearContainerAsync()` at start
- Tests MUST NOT depend on execution order
- Tests MUST clean up created resources
- Tests MUST be repeatable without external state

**xUnit Filter Contract**:
```bash
dotnet test --filter "FullyQualifiedName~E2E"
```

Must execute only tests in `E2E/` namespace.

**Container Cleanup Contract**:
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

MUST delete ALL documents before test execution.

---

## Service Lifetime Contracts

### Singleton Services

**Contract**: Same instance across all scopes and requests

**Services**:
- `CosmosClient`
- `SecretClient`

**Test Verification**:
```csharp
var instance1 = serviceProvider.GetService<CosmosClient>();
var instance2 = serviceProvider.GetService<CosmosClient>();
instance1.Should().BeSameAs(instance2);
```

---

### Scoped Services

**Contract**: Same instance within a scope, different across scopes

**Services**:
- `ICosmosRepository`
- `IActivityService`

**Test Verification**:
```csharp
using (var scope1 = serviceProvider.CreateScope())
{
    var svc1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
    var svc2 = scope1.ServiceProvider.GetService<ICosmosRepository>();
    svc1.Should().BeSameAs(svc2); // Same within scope
}

using (var scope2 = serviceProvider.CreateScope())
{
    var svc3 = scope2.ServiceProvider.GetService<ICosmosRepository>();
    // svc3 different from svc1/svc2 (new scope)
}
```

---

### Transient Services

**Contract**: New instance on every request

**Services**:
- `IFitbitService` (via AddHttpClient)

**Test Verification**:
```csharp
var instance1 = scope.ServiceProvider.GetService<IFitbitService>();
var instance2 = scope.ServiceProvider.GetService<IFitbitService>();
instance1.Should().NotBeSameAs(instance2); // Always different
```

---

## Test Data Contracts

### ActivityDocument Contract

**Required Fields**:
```csharp
{
    "id": "string (non-empty)",
    "userId": "string (non-empty)",
    "date": "string (yyyy-MM-dd format)",
    "documentType": "activity",
    "summary": { /* Summary object */ },
    "goals": { /* Goals object */ },
    "activities": [ /* Activity array */ ]
}
```

**Validation Rules**:
- `id` MUST be unique per document
- `date` MUST match regex: `^\d{4}-\d{2}-\d{2}$`
- `documentType` MUST equal "activity" (partition key value)
- All fields MUST be non-null

---

### TestDataGenerator Contract

**GenerateActivityDocument() Method**:

**Returns**: Valid ActivityDocument with all required fields

**Guarantees**:
- Unique ID per invocation
- Valid date format (yyyy-MM-dd)
- documentType = "activity"
- Non-null nested objects
- Suitable for Cosmos DB persistence

**GenerateActivityResponse() Method**:

**Returns**: Valid ActivityResponse (Fitbit API format)

**Guarantees**:
- Valid summary with steps, calories, distances
- Valid goals with daily targets
- Non-empty activities array
- Suitable for service transformation testing

---

## GitHub Actions Workflow Contracts

### Unit Test Job

**Contract**:
```yaml
run-unit-tests:
  working-directory: ./src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.UnitTests
  test-filter: none (runs all unit tests)
```

**Expectations**:
- Executes in <5 seconds
- Requires no external services
- Coverage report generated
- Runs in parallel with contract tests

---

### Contract Test Job

**Contract**:
```yaml
run-contract-tests:
  working-directory: ./src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.IntegrationTests
  test-filter: 'FullyQualifiedName~Contract'
  needs: [env-setup]
```

**Expectations**:
- Executes in <5 seconds
- Requires no external services (no Cosmos DB)
- Runs in parallel with unit tests
- Tests service registration only

---

### E2E Test Job

**Contract**:
```yaml
run-e2e-tests:
  working-directory: ./src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.IntegrationTests
  test-filter: 'FullyQualifiedName~E2E'
  needs: [env-setup, run-contract-tests]
  services:
    cosmos:
      image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

**Expectations**:
- Executes in <30 seconds
- Requires Cosmos DB Emulator service
- Runs after contract tests complete
- Tests full integration workflow

---

### Test Reporter Contract

**Contract**:
```yaml
permissions:
  checks: write  # REQUIRED for dorny/test-reporter@v1
```

**Expectations**:
- Test results published to PR checks
- Coverage reports uploaded as artifacts
- Detailed failure messages in PR comments

---

## Coverage Contracts

### Overall Coverage Target

**Contract**: ≥70% line coverage for Biotrackr.Activity.Svc project

**Exclusions**:
```xml
<PropertyGroup>
    <ExcludeByFile>**/Program.cs</ExcludeByFile>
</PropertyGroup>
```

**Measurement**: Coverlet with Cobertura format

---

### Component Coverage Targets

| Component | Target | Priority |
|-----------|--------|----------|
| ActivityWorker | ≥85% | P1 (critical orchestration) |
| ActivityService | ≥80% | P1 (core business logic) |
| CosmosRepository | ≥80% | P1 (data access) |
| FitbitService | ≥75% | P2 (external integration) |
| Models | ≥60% | P3 (mostly DTOs) |

---

## Error Handling Contracts

### Test Failure Behavior

**Unit/Contract Test Failures**:
- Workflow MUST fail immediately
- Error details MUST appear in PR checks
- No deployment MUST occur

**E2E Test Failures**:
- Workflow MUST fail immediately
- Cosmos DB logs MUST be available
- Retry NOT attempted (deterministic failures only)

**Flaky Test Policy**:
- Flaky tests MUST be removed (not skipped)
- CI MUST maintain high signal-to-noise ratio
- No disabled/skipped tests accumulate

---

## Assertion Contracts

### FluentAssertions Usage

All assertions MUST use FluentAssertions for clarity:

```csharp
// ❌ DON'T
Assert.Equal(expected, actual);
Assert.True(condition);

// ✅ DO
actual.Should().Be(expected);
condition.Should().BeTrue();
```

**Benefits**:
- Clear error messages
- Better readability
- Consistent patterns

---

## Mock Behavior Contracts

### Moq Setup Requirements

**Strict Mocks**:
- Use `MockBehavior.Strict` when behavior MUST be explicit
- Verify all expected calls execute

**Loose Mocks** (default):
- Allow unexpected calls
- Focus tests on specific behaviors

**Verification**:
```csharp
_mockService.Verify(x => x.Method(), Times.Once);
_mockService.VerifyNoOtherCalls(); // When strict behavior needed
```

---

## Summary

These contracts ensure:
1. ✅ Consistent test fixture behavior across services
2. ✅ Predictable test execution performance
3. ✅ Proper test isolation and independence
4. ✅ Clear service lifetime verification
5. ✅ Reliable CI/CD integration
6. ✅ Comprehensive coverage tracking

All implementations MUST adhere to these contracts to maintain test reliability and consistency with existing patterns (Weight Service).
