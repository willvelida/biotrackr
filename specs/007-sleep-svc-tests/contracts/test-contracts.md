# Test Contracts: Sleep Service Test Coverage

**Feature**: 007-sleep-svc-tests  
**Created**: October 31, 2025  
**Purpose**: Define testable contracts for unit, contract, and E2E tests

---

## Unit Test Contracts

### SleepWorkerShould.cs

**Purpose**: Validate SleepWorker orchestrates sleep data synchronization correctly

#### Test Contract: Constructor Initialization

```csharp
[Fact]
public void Constructor_ShouldInitialize_WithValidDependencies()
```

**Given**: Valid mocked dependencies (IFitbitService, ISleepService, ILogger, IHostApplicationLifetime)  
**When**: SleepWorker constructor is called  
**Then**: Instance created without exceptions, all dependencies injected

**Assertions**:
- Worker instance is not null
- No exceptions thrown during construction

---

#### Test Contract: Successful Execution

```csharp
[Fact]
public async Task ExecuteAsync_ShouldReturn0_WhenSuccessful()
```

**Given**: 
- Mocked IFitbitService returns valid SleepResponse
- Mocked ISleepService.MapAndSaveDocument succeeds
- Mocked logger ready to capture logs

**When**: ExecuteAsync is called with CancellationToken

**Then**: 
- Returns exit code 0 (success)
- IFitbitService.GetSleepResponse called once with yesterday's date (yyyy-MM-dd format)
- ISleepService.MapAndSaveDocument called once with date and response
- Information logs recorded (execution start, getting sleep data, mapping/saving)
- IHostApplicationLifetime.StopApplication called in finally block

**Assertions**:
```csharp
result.Should().Be(0);
_mockFitbitService.Verify(x => x.GetSleepResponse(It.IsAny<string>()), Times.Once);
_mockSleepService.Verify(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<SleepResponse>()), Times.Once);
_mockLogger.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.Contains("SleepWorker executed at"))));
_mockAppLifetime.Verify(x => x.StopApplication(), Times.Once);
```

---

#### Test Contract: Exception Handling

```csharp
[Fact]
public async Task ExecuteAsync_ShouldReturn1_WhenExceptionThrown()
```

**Given**: 
- Mocked IFitbitService throws Exception
- Mocked logger ready to capture error logs

**When**: ExecuteAsync is called

**Then**: 
- Returns exit code 1 (failure)
- Error logged with exception message
- IHostApplicationLifetime.StopApplication still called in finally block

**Assertions**:
```csharp
result.Should().Be(1);
_mockLogger.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("Exception thrown in SleepWorker"))));
_mockAppLifetime.Verify(x => x.StopApplication(), Times.Once);
```

---

#### Test Contract: Cancellation Token Handling

```csharp
[Fact]
public async Task ExecuteAsync_ShouldHandleCancellation_Gracefully()
```

**Given**: 
- CancellationToken that is already cancelled
- Mocked dependencies ready

**When**: ExecuteAsync is called with cancelled token

**Then**: 
- Operation completes without hanging
- IHostApplicationLifetime.StopApplication called
- Appropriate exit code returned (0 or 1 depending on implementation)

**Assertions**:
```csharp
Func<Task> act = async () => await _sleepWorker.StartAsync(cancellationToken);
await act.Should().NotThrowAsync();
_mockAppLifetime.Verify(x => x.StopApplication(), Times.Once);
```

---

### FitbitServiceShould.cs (Existing - Coverage Verification)

**Purpose**: Validate Fitbit API client behavior

**Existing Tests** (to verify coverage):
- ✅ GetSleepResponse_ShouldReturnActivityResponse_WhenSuccessful
- ✅ GetSleepResponse_ShouldHandleInvalidDateFormat
- ✅ GetSleepResponse_ShouldHandleInvalidAccessToken
- ✅ GetSleepResponse_ShouldHandleEmptyResponse
- ✅ GetSleepResponse_ShouldLogErrorAndThrow_WhenExceptionOccurs

**Additional Edge Cases** (if coverage gaps found):
- Null date parameter handling
- Network timeout scenarios
- Malformed JSON response handling

---

### SleepServiceShould.cs (Existing - Coverage Verification)

**Purpose**: Validate sleep data mapping and persistence orchestration

**Existing Tests** (to verify coverage):
- ✅ MapAndSaveDocument_ShouldMapAndSaveDocument
- ✅ MapAndSaveDocument_ShouldThrowExceptionWhenFails

**Additional Edge Cases** (if coverage gaps found):
- Null SleepResponse handling
- Empty date string handling
- Document ID uniqueness validation

---

### CosmosRepositoryShould.cs (Existing - Coverage Verification)

**Purpose**: Validate Cosmos DB data access layer

**Existing Tests** (to verify coverage):
- ✅ CreateSleepDocument_ShouldSucceed
- ✅ CreateSleepDocument_ShouldThrowExceptionWhenFails

**Additional Edge Cases** (if coverage gaps found):
- Duplicate document ID handling
- Rate limiting scenarios
- Network failure handling

---

## Contract Integration Test Contracts

### ProgramStartupTests.cs

**Purpose**: Validate application can start and all services are registered correctly

#### Test Contract: Service Resolution

```csharp
[Fact]
public void Application_ShouldResolveAllServices()
```

**Given**: 
- Service provider built with test configuration
- All services registered in Program.cs

**When**: Attempt to resolve each service type

**Then**: All services resolve successfully (not null)

**Services to Verify**:
```csharp
var cosmosClient = serviceProvider.GetService<CosmosClient>();
var secretClient = serviceProvider.GetService<SecretClient>();
var cosmosRepository = serviceProvider.GetService<ICosmosRepository>();
var sleepService = serviceProvider.GetService<ISleepService>();
var fitbitService = serviceProvider.GetService<IFitbitService>();
var sleepWorker = serviceProvider.GetService<IHostedService>();

cosmosClient.Should().NotBeNull();
secretClient.Should().NotBeNull();
cosmosRepository.Should().NotBeNull();
sleepService.Should().NotBeNull();
fitbitService.Should().NotBeNull();
sleepWorker.Should().NotBeNull();
```

---

#### Test Contract: Host Building

```csharp
[Fact]
public void Application_ShouldBuildHost_WithoutExceptions()
```

**Given**: Host builder with test configuration

**When**: Build host

**Then**: No exceptions thrown during build process

**Assertions**:
```csharp
Func<IHost> act = () => hostBuilder.Build();
act.Should().NotThrow();
```

---

### ServiceRegistrationTests.cs

**Purpose**: Validate service lifetimes are configured correctly

#### Test Contract: Singleton Services

```csharp
[Fact]
public void SingletonServices_ShouldReturnSameInstance()
```

**Given**: Service provider with singleton registrations

**When**: Resolve CosmosClient and SecretClient multiple times

**Then**: Same instance returned each time

**Assertions**:
```csharp
var cosmosClient1 = serviceProvider.GetService<CosmosClient>();
var cosmosClient2 = serviceProvider.GetService<CosmosClient>();
cosmosClient1.Should().BeSameAs(cosmosClient2);

var secretClient1 = serviceProvider.GetService<SecretClient>();
var secretClient2 = serviceProvider.GetService<SecretClient>();
secretClient1.Should().BeSameAs(secretClient2);
```

---

#### Test Contract: Scoped Services

```csharp
[Fact]
public void ScopedServices_ShouldReturnSameInstance_WithinScope()
```

**Given**: Service provider with scoped registrations

**When**: Resolve services within same scope and different scopes

**Then**: Same instance within scope, different instances across scopes

**Assertions**:
```csharp
using (var scope1 = serviceProvider.CreateScope())
{
    var repository1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
    var repository2 = scope1.ServiceProvider.GetService<ICosmosRepository>();
    repository1.Should().BeSameAs(repository2);
}

using (var scope2 = serviceProvider.CreateScope())
{
    var repository3 = scope2.ServiceProvider.GetService<ICosmosRepository>();
    repository3.Should().NotBeSameAs(repository1); // Different scope
}
```

---

#### Test Contract: Transient Services

```csharp
[Fact]
public void TransientServices_ShouldReturnDifferentInstances()
```

**Given**: Service provider with IFitbitService registered via AddHttpClient (transient)

**When**: Resolve IFitbitService multiple times

**Then**: Different instance returned each time

**Assertions**:
```csharp
var fitbitService1 = serviceProvider.GetService<IFitbitService>();
var fitbitService2 = serviceProvider.GetService<IFitbitService>();
fitbitService1.Should().NotBeSameAs(fitbitService2);
```

---

## E2E Integration Test Contracts

### CosmosRepositoryTests.cs

**Purpose**: Validate repository operations with real Cosmos DB

#### Test Contract: Document Creation

```csharp
[Fact]
public async Task CreateSleepDocument_ShouldPersistToDatabase()
```

**Given**: 
- Real Cosmos DB container initialized
- Container cleared via ClearContainerAsync()
- Valid SleepDocument with unique ID

**When**: Call CreateSleepDocument

**Then**: 
- Document persists in Cosmos DB
- Can be retrieved by ID
- Contains correct data

**Assertions**:
```csharp
await repository.CreateSleepDocument(sleepDocument);

var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
    .WithParameter("@id", sleepDocument.Id);
var iterator = container.GetItemQueryIterator<SleepDocument>(query);
var results = await iterator.ReadNextAsync();

results.Should().ContainSingle();
results.First().Date.Should().Be(sleepDocument.Date);
results.First().DocumentType.Should().Be("Sleep");
```

---

#### Test Contract: Partition Key Handling

```csharp
[Fact]
public async Task CreateSleepDocument_ShouldUseCorrectPartitionKey()
```

**Given**: SleepDocument with DocumentType = "Sleep"

**When**: Create document in Cosmos DB

**Then**: Document stored with correct partition key (/documentType)

**Assertions**:
```csharp
await repository.CreateSleepDocument(sleepDocument);

var item = await container.ReadItemAsync<SleepDocument>(
    sleepDocument.Id, 
    new PartitionKey("Sleep"));
    
item.Resource.Should().NotBeNull();
```

---

### SleepServiceTests.cs

**Purpose**: Validate service layer with real Cosmos DB

#### Test Contract: End-to-End Mapping

```csharp
[Fact]
public async Task MapAndSaveDocument_ShouldTransformAndPersist()
```

**Given**: 
- Real Cosmos DB container
- Container cleared
- Date string and SleepResponse

**When**: Call MapAndSaveDocument

**Then**: 
- Document created in Cosmos DB
- ID is unique GUID
- Date matches input
- Sleep data mapped correctly
- DocumentType = "Sleep"

**Assertions**:
```csharp
await sleepService.MapAndSaveDocument(date, sleepResponse);

var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
    .WithParameter("@date", date);
var iterator = container.GetItemQueryIterator<SleepDocument>(query);
var results = await iterator.ReadNextAsync();

results.Should().ContainSingle();
results.First().Sleep.Should().BeEquivalentTo(sleepResponse);
Guid.TryParse(results.First().Id, out _).Should().BeTrue();
```

---

### SleepWorkerTests.cs

**Purpose**: Validate complete workflow with mocked Fitbit and real Cosmos DB

#### Test Contract: Complete Workflow

```csharp
[Fact]
public async Task ExecuteAsync_ShouldCompleteFullWorkflow()
```

**Given**: 
- Mocked IFitbitService returns SleepResponse
- Real Cosmos DB container
- Container cleared
- SleepWorker with real services (except Fitbit)

**When**: Execute worker

**Then**: 
- Data fetched from mocked Fitbit service
- Data mapped and saved to Cosmos DB
- Document retrievable from database
- Exit code 0 returned

**Assertions**:
```csharp
var result = await sleepWorker.StartAsync(CancellationToken.None);

result.Should().Be(0);

var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
    .WithParameter("@date", expectedDate);
var iterator = container.GetItemQueryIterator<SleepDocument>(query);
var results = await iterator.ReadNextAsync();

results.Should().ContainSingle();
mockFitbitService.Verify(x => x.GetSleepResponse(It.IsAny<string>()), Times.Once);
```

---

## Test Isolation Contract

### ClearContainerAsync Implementation

**Purpose**: Ensure clean state for each E2E test

**Contract**:
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

**Usage**: Called in `IAsyncLifetime.InitializeAsync()` before each test

**Guarantees**:
- Container is empty before test starts
- No data leakage between tests
- Test isolation maintained

---

## Coverage Requirements

### Unit Tests
- **Target**: ≥70% line coverage for Biotrackr.Sleep.Svc project
- **Exclusions**: Program.cs (via [ExcludeFromCodeCoverage] attribute)
- **Focus**: Business logic, error handling, edge cases

### Contract Tests
- **Target**: 100% service registration coverage
- **Focus**: All services resolvable, correct lifetimes

### E2E Tests
- **Target**: ≥80% integration point coverage
- **Focus**: Service-to-repository interactions, worker orchestration

---

## Test Execution Contracts

### Performance Contracts

**Unit Tests**:
- Total execution time: <5 seconds
- Parallelization: Enabled
- External dependencies: None (all mocked)

**Contract Tests**:
- Total execution time: <5 seconds
- Parallelization: Enabled
- External dependencies: None (in-memory configuration)

**E2E Tests**:
- Total execution time: <30 seconds
- Parallelization: Disabled (share Cosmos DB connection)
- External dependencies: Cosmos DB Emulator

### Reliability Contracts

**All Tests**:
- Pass rate: 100% (no flaky tests)
- Idempotent: Same result on repeated runs
- Isolated: No shared state between tests
- Deterministic: No random failures

**E2E Tests**:
- Cleanup: ClearContainerAsync() called before each test
- Isolation: No test affects another test's results
- Resilience: Retry logic for Cosmos DB connection issues

---

## GitHub Actions Workflow Contracts

### Test Job Definitions

**Unit Test Job**:
```yaml
run-unit-tests:
  working-directory: ./src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.UnitTests
  test-filter: none (run all unit tests)
  requires-cosmos-db: false
  runs-in-parallel: true (with contract tests)
```

**Contract Test Job**:
```yaml
run-contract-tests:
  working-directory: ./src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests
  test-filter: 'FullyQualifiedName~Contract'
  requires-cosmos-db: false
  runs-in-parallel: true (with unit tests)
```

**E2E Test Job**:
```yaml
run-e2e-tests:
  working-directory: ./src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests
  test-filter: 'FullyQualifiedName~E2E'
  requires-cosmos-db: true (Cosmos DB Emulator service)
  runs-in-parallel: false (runs after contract tests)
```

### Coverage Report Contract

**Format**: Cobertura XML  
**Upload**: As GitHub Actions artifact  
**Publish**: Via dorny/test-reporter@v1  
**Retention**: 30 days  
**Location**: Pull request comments

---

## References

- [Feature Specification](../spec.md)
- [Implementation Plan](../plan.md)
- [Data Model](../data-model.md)
- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [FluentAssertions Documentation](https://fluentassertions.com/introduction)
- [Moq Documentation](https://github.com/moq/moq4/wiki/Quickstart)
