# Research: Sleep Service Test Coverage and Integration Tests

**Feature**: 007-sleep-svc-tests  
**Created**: October 31, 2025  
**Purpose**: Document findings from investigating test patterns, coverage requirements, and integration test architecture

---

## Current State Analysis

### Existing Unit Tests

**Location**: `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.UnitTests/`

**Coverage**:
- ✅ `ServiceTests/SleepServiceShould.cs` - 2 tests (MapAndSaveDocument success/failure)
- ✅ `ServiceTests/FitbitServiceShould.cs` - 5 tests (GetSleepResponse various scenarios)
- ✅ `RepositoryTests/CosmosRepositoryShould.cs` - 2 tests (CreateSleepDocument success/failure)
- ❌ `WorkerTests/SleepWorkerShould.cs` - **MISSING** (no tests for SleepWorker)

**Total Tests**: 9 unit tests
**Estimated Coverage**: ~40-50% (missing SleepWorker, edge cases)

**Gaps Identified**:
1. No tests for `SleepWorker` class (constructor, ExecuteAsync, error handling, cancellation)
2. Limited edge case coverage (empty responses, malformed JSON, null handling)
3. Program.cs not excluded from coverage (needs [ExcludeFromCodeCoverage] attribute)
4. No integration tests (contract or E2E)

---

### Code Structure Analysis

**SleepWorker** (`Worker/SleepWorker.cs`):
```csharp
protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
{
    try
    {
        _logger.LogInformation($"{nameof(SleepWorker)} executed at: {DateTime.Now}");
        var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        _logger.LogInformation($"Getting sleep data for {date}");
        var sleepResponse = await _fitbitService.GetSleepResponse(date);
        _logger.LogInformation($"Mapping and saving document for {date}");
        await _sleepService.MapAndSaveDocument(date, sleepResponse);
        return 0;
    }
    catch (Exception ex)
    {
        _logger.LogError($"Exception thrown in {nameof(SleepWorker)}: {ex.Message}");
        return 1;
    }
    finally
    {
        _appLifetime.StopApplication();
    }
}
```

**Testing Requirements**:
- Constructor injection verification
- ExecuteAsync success path (returns 0)
- ExecuteAsync exception path (returns 1)
- Cancellation token handling
- Logging verification (Information and Error levels)
- IHostApplicationLifetime.StopApplication called in finally block

---

### Program.cs Service Registration

**Current Registration Pattern**:
```csharp
// Singleton services (expensive to create, thread-safe)
services.AddSingleton(new SecretClient(...));
services.AddSingleton(new CosmosClient(...));

// Scoped services (one per request/execution scope)
services.AddScoped<ICosmosRepository, CosmosRepository>();
services.AddScoped<IFitbitService, FitbitService>(); // DUPLICATE!
services.AddScoped<ISleepService, SleepService>();

// HttpClient-based services (transient via AddHttpClient)
services.AddHttpClient<IFitbitService, FitbitService>()
    .AddStandardResilienceHandler();

// Hosted service
services.AddHostedService<SleepWorker>();
```

**Issue Found**: Duplicate registration for `IFitbitService`
- First registered as Scoped
- Then registered via AddHttpClient (overrides to Transient)
- **Resolution**: Remove `AddScoped<IFitbitService, FitbitService>()` line

**Reference**: [Decision Record: Service Lifetime Registration](../../docs/decision-records/2025-10-28-service-lifetime-registration.md)

---

## Reference Implementation Analysis

### Weight Service Integration Tests (003-weight-svc-integration-tests)

**Structure**:
```
Biotrackr.Weight.Svc.IntegrationTests/
├── Contract/
│   ├── ProgramStartupTests.cs
│   └── ServiceRegistrationTests.cs
├── E2E/
│   ├── CosmosRepositoryTests.cs
│   └── WeightServiceTests.cs
├── Fixtures/
│   ├── ContractTestFixture.cs
│   └── IntegrationTestFixture.cs
├── Collections/
│   ├── ContractTestCollection.cs
│   └── IntegrationTestCollection.cs
└── Helpers/
    └── TestDataGenerator.cs
```

**Key Patterns**:
1. **Separate Fixtures**: ContractTestFixture (no DB) vs IntegrationTestFixture (with DB)
2. **xUnit Collections**: Group tests that share fixtures
3. **Test Filters**: Contract tests use `FullyQualifiedName~Contract`, E2E use `FullyQualifiedName~E2E`
4. **Cleanup Pattern**: `ClearContainerAsync()` called before each E2E test
5. **Gateway Mode**: `ConnectionMode.Gateway` for Cosmos DB Emulator compatibility

---

### Activity Service Integration Tests (005-activity-svc-tests)

**Implemented**: October 31, 2025

**Learnings**:
1. Test organization matches Weight Service pattern
2. Program.cs uses [ExcludeFromCodeCoverage] attribute (not coverlet.msbuild config)
3. Workflow includes three test jobs: unit, contract, E2E
4. E2E tests require Cosmos DB Emulator service in GitHub Actions
5. Test isolation critical - cleanup before each test prevents cross-test contamination

**Workflow Pattern**:
```yaml
run-unit-tests:
  # Fast, no external dependencies, runs in parallel
  
run-contract-tests:
  # Fast, no external dependencies, runs in parallel with unit tests
  test-filter: 'FullyQualifiedName~Contract'
  
run-e2e-tests:
  # Slower, requires Cosmos DB Emulator, runs after contract tests
  needs: [env-setup, run-contract-tests]
  test-filter: 'FullyQualifiedName~E2E'
  services:
    cosmos:
      image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

---

## Testing Frameworks & Tools

### xUnit 2.9.3

**Chosen For**:
- Built-in parallelization support
- Collection fixtures for shared test context
- Theory/InlineData for parameterized tests
- Excellent .NET Core integration

**Usage**:
```csharp
[Collection("SleepServiceIntegrationTests")]
public class CosmosRepositoryTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    
    public CosmosRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    public async Task InitializeAsync()
    {
        await ClearContainerAsync();
    }
    
    public Task DisposeAsync() => Task.CompletedTask;
}
```

---

### FluentAssertions 8.4.0

**Benefits**:
- Readable assertion syntax
- Better error messages
- Chainable assertions

**Example**:
```csharp
result.Should().NotBeNull();
result.Should().BeOfType<SleepDocument>();
sleepDocument.Date.Should().Be("2025-10-31");
sleepDocument.DocumentType.Should().Be("Sleep");
```

---

### Moq 4.20.72

**Usage**:
- Mock dependencies (IFitbitService, ISleepService, ICosmosRepository)
- Verify method calls
- Setup return values and exceptions

**Example**:
```csharp
_mockFitbitService.Setup(x => x.GetSleepResponse(It.IsAny<string>()))
    .ReturnsAsync(sleepResponse);

_mockFitbitService.Verify(x => x.GetSleepResponse(date), Times.Once);
```

---

### AutoFixture 4.18.1

**Purpose**: Generate test data quickly

**Example**:
```csharp
var fixture = new Fixture();
var sleepResponse = fixture.Create<SleepResponse>();
var sleepDocument = fixture.Create<SleepDocument>();
```

---

### Coverlet 6.0.4

**Configuration**:
- Use `coverlet.collector` package (not `coverlet.msbuild`)
- Exclude Program.cs via [ExcludeFromCodeCoverage] attribute
- Generate Cobertura format for GitHub Actions

**Command**:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Cosmos DB Emulator Considerations

### Connection Mode Issue

**Problem**: Direct mode (TCP+SSL via rntbd://) fails with SSL negotiation errors in Cosmos DB Emulator

**Solution**: Force Gateway mode (HTTPS only)
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

**Reference**: [Common Resolutions: E2E Test Issues](.specify/memory/common-resolutions.md#issue-e2e-tests-fail-with-ssl-negotiation-failed-on-direct-connection-mode)

---

### Test Isolation Pattern

**Problem**: Tests share same Cosmos DB container, finding each other's data

**Solution**: Clean container before each test
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

## GitHub Actions Workflow Research

### Reusable Templates

**Available Templates**:
- `template-dotnet-run-unit-tests.yml` - Runs unit tests with coverage
- `template-dotnet-run-contract-tests.yml` - Runs contract tests (fast, no DB)
- `template-dotnet-run-e2e-tests.yml` - Runs E2E tests with Cosmos DB Emulator

**Required Parameters**:
- `dotnet-version`: .NET SDK version (9.0.x)
- `working-directory`: Path to test project (e.g., `./src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.UnitTests`)
- `coverage-path`: Path for coverage artifacts (e.g., `${{ github.workspace }}/coverage`)
- `test-filter`: xUnit filter expression (for contract/E2E tests)

---

### Common Workflow Issues

**Issue 1**: Test Reporter Failing - Missing `checks: write` permission
```yaml
permissions:
  contents: read
  id-token: write
  pull-requests: write
  checks: write  # Required for dorny/test-reporter@v1
```

**Issue 2**: Wrong working-directory - Using solution path instead of test project path
```yaml
# ❌ Wrong
working-directory: ./src/Biotrackr.Sleep.Svc

# ✅ Correct
working-directory: ./src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.UnitTests
```

**Issue 3**: Target framework mismatch - Workflow uses .NET 9.0 but test project targets different version
```xml
<!-- Test project must target net9.0 -->
<TargetFramework>net9.0</TargetFramework>
```

**Reference**: [Common Resolutions: GitHub Actions Workflow Issues](.specify/memory/common-resolutions.md#github-actions-workflow-issues)

---

## Code Coverage Requirements

### Industry Standards

- **Good**: 70-80% line coverage
- **Excellent**: 80-90% line coverage
- **Comprehensive**: 90%+ line coverage

### Biotrackr Standards

- **Minimum**: 70% line coverage (matches Activity Service, Weight Service)
- **Exclusions**: Program.cs (DI registration only)
- **Focus**: Business logic, error handling, edge cases

---

## Recommendations

### Phase 1: Unit Test Completion (Priority: P1)

1. Create `SleepWorkerShould.cs` with tests for:
   - Constructor injection
   - ExecuteAsync success (returns 0)
   - ExecuteAsync exception (returns 1)
   - Cancellation token handling
   - Logging verification

2. Add [ExcludeFromCodeCoverage] attribute to Program.cs

3. Fix duplicate FitbitService registration in Program.cs

4. Verify 70% coverage threshold met

### Phase 2: Integration Test Project (Priority: P2)

1. Create `Biotrackr.Sleep.Svc.IntegrationTests` project
2. Add NuGet packages (xUnit, FluentAssertions, Moq, AutoFixture, coverlet)
3. Create folder structure (Contract/, E2E/, Fixtures/, Collections/, Helpers/)
4. Create test fixtures (ContractTestFixture, IntegrationTestFixture)
5. Create xUnit collections

### Phase 3: Contract Tests (Priority: P2)

1. Implement ProgramStartupTests
2. Implement ServiceRegistrationTests
3. Verify service lifetimes (Singleton, Scoped, Transient)

### Phase 4: E2E Tests (Priority: P3)

1. Implement CosmosRepositoryTests
2. Implement SleepServiceTests
3. Implement SleepWorkerTests
4. Add ClearContainerAsync cleanup pattern

### Phase 5: GitHub Actions Integration (Priority: P3)

1. Update deploy-sleep-service.yml workflow
2. Add contract test job (parallel with unit tests)
3. Add E2E test job (after contract tests, with Cosmos DB Emulator)
4. Add test result publishing
5. Add coverage artifact upload

---

## References

- [Weight Service Integration Tests Spec](../003-weight-svc-integration-tests/spec.md)
- [Activity Service Tests Spec](../005-activity-svc-tests/spec.md)
- [Decision Record: Integration Test Project Structure](../../docs/decision-records/2025-10-28-integration-test-project-structure.md)
- [Decision Record: Service Lifetime Registration](../../docs/decision-records/2025-10-28-service-lifetime-registration.md)
- [Common Resolutions](.specify/memory/common-resolutions.md)
- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/VSTestIntegration.md)
