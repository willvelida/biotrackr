# Test Contracts: Food Service Test Coverage and Integration Tests

**Feature**: 009-food-svc-tests  
**Date**: November 3, 2025  
**Status**: Complete

## Overview

This document defines test contracts for the Food Service, organized by test type (Unit, Contract, E2E). Test contracts describe expected behavior, test boundaries, and verification patterns.

---

## Unit Test Contracts

### FoodWorker Tests

**Contract**: FoodWorker orchestrates the food data synchronization workflow

**Test Suite**: `FoodWorkerShould.cs`

| Test | Given | When | Then | Priority |
|------|-------|------|------|----------|
| Constructor_WithValidParameters_ShouldCreateInstance | Valid dependencies | FoodWorker is instantiated | Instance created without exceptions | P1 |
| ExecuteAsync_WhenFitbitReturnsData_ShouldMapAndSaveDocument | Fitbit returns food data | ExecuteAsync is called | MapAndSaveDocument called with correct data | P1 |
| ExecuteAsync_WhenCancellationRequested_ShouldStopGracefully | Cancellation token signaled | ExecuteAsync is running | Worker stops and returns exit code | P2 |
| ExecuteAsync_WhenExceptionOccurs_ShouldLogErrorAndContinue | Service throws exception | ExecuteAsync is called | Error logged, application lifetime stopped | P2 |

**Edge Cases**:
- Empty food response (no foods logged)
- Null response from Fitbit service
- Network timeout during API call
- Cosmos DB unavailable during save

**Mock Verification**:
- FitbitService.GetFoodAsync called once per execution
- FoodService.MapAndSaveDocument called when data retrieved
- Logger logs informational and error messages
- ApplicationLifetime.StopApplication called on exceptions

---

### FoodService Tests

**Contract**: FoodService transforms Fitbit API responses into Cosmos DB documents

**Test Suite**: `FoodServiceShould.cs`

| Test | Given | When | Then | Priority |
|------|-------|------|------|----------|
| MapAndSaveDocument_WithValidFoodResponse_ShouldSaveToCosmosDb | Valid FoodResponse | MapAndSaveDocument called | Document saved with correct structure | P1 |
| MapAndSaveDocument_WithEmptyFoodList_ShouldSaveWithEmptyFoods | FoodResponse with empty foods list | MapAndSaveDocument called | Document saved with empty foods array | P2 |
| MapAndSaveDocument_WhenRepositoryThrowsException_ShouldThrowException | Repository throws CosmosException | MapAndSaveDocument called | Exception propagated to caller | P2 |
| MapAndSaveDocument_ShouldSetCorrectDocumentType | Any FoodResponse | MapAndSaveDocument called | DocumentType set to "food" | P1 |

**Data Transformation Validation**:
- `Id` format: `{userId}_{date}`
- `Date` matches Fitbit API date
- `DocumentType` always "food"
- `Food` contains complete FoodResponse structure

**Mock Verification**:
- CosmosRepository.CreateItemAsync called with FoodDocument
- Logger logs operation details
- Correct partition key used ("/documentType")

---

### FitbitService Tests

**Contract**: FitbitService retrieves food data from Fitbit API

**Test Suite**: `FitbitServiceShould.cs`

| Test | Given | When | Then | Priority |
|------|-------|------|------|----------|
| GetFoodAsync_WithValidDate_ShouldReturnFoodResponse | Valid date string | GetFoodAsync called | Returns FoodResponse with data | P1 |
| GetFoodAsync_WhenApiReturnsError_ShouldThrowException | API returns 4xx/5xx error | GetFoodAsync called | HttpRequestException thrown | P2 |
| GetFoodAsync_WhenApiReturnsInvalidJson_ShouldThrowException | API returns malformed JSON | GetFoodAsync called | JsonException thrown | P2 |
| GetFoodAsync_WithNullDate_ShouldThrowArgumentNullException | Null date parameter | GetFoodAsync called | ArgumentNullException thrown | P3 |

**API Contract Validation**:
- Correct endpoint format: `/1/user/-/foods/log/date/{date}.json`
- Authorization header included
- Response deserialized to FoodResponse
- Resilience handler applied (retry logic)

---

### CosmosRepository Tests

**Contract**: CosmosRepository handles Cosmos DB operations for food documents

**Test Suite**: `CosmosRepositoryShould.cs`

| Test | Given | When | Then | Priority |
|------|-------|------|------|----------|
| CreateItemAsync_WithValidDocument_ShouldPersistToCosmosDb | Valid FoodDocument | CreateItemAsync called | Document persisted successfully | P1 |
| CreateItemAsync_WhenDuplicateId_ShouldThrowCosmosException | Document with duplicate ID | CreateItemAsync called | CosmosException with 409 status | P2 |
| CreateItemAsync_WhenRateLimitExceeded_ShouldThrowCosmosException | Too many requests | CreateItemAsync called | CosmosException with 429 status | P3 |
| CreateItemAsync_ShouldUseCorrectPartitionKey | Any FoodDocument | CreateItemAsync called | Partition key = document.DocumentType | P1 |

**Cosmos DB Validation**:
- Container name: Configured via Settings
- Partition key: `/documentType`
- Serialization: CamelCase property naming
- Error handling: Proper exception propagation

---

## Contract Test Contracts (Integration)

### ProgramStartup Tests

**Contract**: Application can start and build host successfully

**Test Suite**: `ProgramStartupTests.cs`

| Test | Given | When | Then | Priority |
|------|-------|------|------|----------|
| Application_ShouldBuildHostSuccessfully | Valid configuration | Host is built | Host created without exceptions | P1 |
| Configuration_ShouldHaveRequiredKeys | In-memory configuration | Configuration accessed | All required keys present | P1 |
| Configuration_ShouldBindToSettings | Configuration with values | Settings bound | Settings object populated correctly | P2 |

**Required Configuration Keys**:
- `keyvaulturl`
- `managedidentityclientid`
- `cosmosdbendpoint`
- `applicationinsightsconnectionstring`
- `Biotrackr:FitbitUserId`
- `Biotrackr:DatabaseName`
- `Biotrackr:ContainerName`
- `Biotrackr:PartitionKey`

---

### ServiceRegistration Tests

**Contract**: All services are properly registered with correct lifetimes

**Test Suite**: `ServiceRegistrationTests.cs`

| Test | Given | When | Then | Priority |
|------|-------|------|------|----------|
| CosmosClient_ShouldBeRegisteredAsSingleton | Service provider built | CosmosClient resolved twice | Same instance returned | P1 |
| SecretClient_ShouldBeRegisteredAsSingleton | Service provider built | SecretClient resolved twice | Same instance returned | P1 |
| CosmosRepository_ShouldBeRegisteredAsScoped | Service scope created | ICosmosRepository resolved twice in scope | Same instance within scope | P1 |
| FoodService_ShouldBeRegisteredAsScoped | Service scope created | IFoodService resolved twice in scope | Same instance within scope | P1 |
| FitbitService_ShouldBeRegisteredAsTransient | Service scope created | IFitbitService resolved twice | Different instances returned | P1 |
| FoodWorker_ShouldBeRegisteredAsHostedService | Service provider built | IHostedService resolved | FoodWorker instance returned | P1 |
| FitbitService_ShouldNotHaveDuplicateRegistration | Service descriptors inspected | Check for IFitbitService | Only one registration exists | P2 |

**Service Lifetime Expectations**:
| Service | Lifetime | Reason |
|---------|----------|--------|
| CosmosClient | Singleton | Expensive to create, thread-safe, manages pooling |
| SecretClient | Singleton | Expensive to create, thread-safe |
| ICosmosRepository | Scoped | One instance per execution scope |
| IFoodService | Scoped | One instance per execution scope |
| IFitbitService | Transient | HttpClient-based, managed by factory |
| FoodWorker | Singleton | Hosted service (one per application) |

---

## E2E Test Contracts (Integration)

### CosmosRepository E2E Tests

**Contract**: Repository correctly persists and retrieves documents from actual Cosmos DB

**Test Suite**: `CosmosRepositoryTests.cs` (E2E namespace)

| Test | Given | When | Then | Priority |
|------|-------|------|------|----------|
| CreateItemAsync_ShouldPersistFoodDocument | Valid FoodDocument | CreateItemAsync called | Document retrievable by ID | P1 |
| CreateItemAsync_ShouldSetCorrectPartitionKey | FoodDocument with documentType | Document created | Partition key matches documentType | P1 |
| CreateItemAsync_ShouldPersistComplexFoodStructure | FoodDocument with nested entities | Document created | All nested properties persisted correctly | P2 |

**Setup**: Call `ClearContainerAsync()` before each test

**Validation**:
- Query document by ID using `ReadItemAsync`
- Verify all properties match source document
- Check partition key used correctly
- Use strongly-typed `FoodDocument` (not `dynamic`)

---

### FoodService E2E Tests

**Contract**: Service orchestrates complete workflow with actual Cosmos DB

**Test Suite**: `FoodServiceTests.cs` (E2E namespace)

| Test | Given | When | Then | Priority |
|------|-------|------|------|----------|
| MapAndSaveDocument_ShouldPersistToCosmosDb | Valid FoodResponse | MapAndSaveDocument called | Document exists in Cosmos DB | P1 |
| MapAndSaveDocument_ShouldTransformDataCorrectly | FoodResponse with multiple foods | MapAndSaveDocument called | Saved document matches transformation | P1 |
| MapAndSaveDocument_WithEmptyFoodList_ShouldSaveSuccessfully | FoodResponse with empty foods | MapAndSaveDocument called | Document saved with empty array | P2 |

**Setup**: 
- Call `ClearContainerAsync()` before each test
- Mock FitbitService (don't call actual API)

**Validation**:
- Query Cosmos DB for saved document
- Verify transformation logic:
  - ID format correct
  - Date matches input
  - DocumentType = "food"
  - Food structure preserved
- Use strongly-typed `FoodDocument` in queries

---

### FoodWorker E2E Tests

**Contract**: Worker executes complete workflow end-to-end with all real dependencies

**Test Suite**: `FoodWorkerTests.cs` (E2E namespace)

| Test | Given | When | Then | Priority |
|------|-------|------|------|----------|
| ExecuteAsync_ShouldRetrieveAndSaveFoodData | Mocked Fitbit returns data | Worker executes | Document persisted to Cosmos DB | P1 |
| ExecuteAsync_WithMultipleFoods_ShouldSaveAllItems | FoodResponse with 5 foods | Worker executes | All foods present in saved document | P2 |
| ExecuteAsync_ShouldHandleCancellationGracefully | Cancellation token signaled | Worker executing | Stops without corrupting data | P2 |

**Setup**:
- Call `ClearContainerAsync()` before each test
- Mock IFitbitService (return test data)
- Use real CosmosRepository and FoodService

**Validation**:
- Query Cosmos DB for saved document
- Verify complete workflow:
  - Fitbit service called
  - Data transformed correctly
  - Document persisted
  - Proper logging occurred
- Use strongly-typed `FoodDocument` in queries

---

## Test Isolation Contract

**Pattern**: ClearContainerAsync Method

All E2E tests MUST call `ClearContainerAsync()` in the Arrange phase to ensure test isolation.

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

**Rationale**:
- xUnit Collection Fixtures share database across tests
- Tests querying by date can find documents from previous tests
- Cleanup ensures predictable state per test
- Prevents "expected 1 but found 3" errors

---

## Test Data Contract

### AutoFixture Configuration

Use AutoFixture for generating random valid test data in unit tests:

```csharp
private readonly Fixture _fixture = new Fixture();

var foodResponse = _fixture.Build<FoodResponse>()
    .With(f => f.foods, new List<Food> { _fixture.Create<Food>() })
    .Create();
```

### TestDataGenerator Contract

Helper class must provide methods for common test scenarios:

```csharp
public static class TestDataGenerator
{
    public static FoodDocument GenerateFoodDocument(string date, string userId);
    public static FoodResponse GenerateFoodResponse(int foodCount = 3);
    public static Food GenerateFood(string name, int calories);
    public static Goals GenerateGoals(int calorieGoal = 2000);
    public static Summary GenerateSummary(int calories, double carbs, double fat, double protein);
}
```

**Validation**:
- Generated data must pass all entity validation rules
- Dates must be valid ISO 8601 format
- Nutritional values must be realistic (50-1000 calories per food)
- IDs must be unique within test context

---

## GitHub Actions Workflow Contract

### Test Job Execution Order

```
Unit Tests (parallel) ──┐
                        ├──> Contract Tests ──> E2E Tests ──> Build Container
Unit Tests complete ────┘
```

**Contract Tests**:
- Run in parallel with unit tests
- No external dependencies required
- Filter: `FullyQualifiedName~Contract`
- Expected duration: <5 seconds

**E2E Tests**:
- Run after contract tests complete
- Require Cosmos DB Emulator service
- Filter: `FullyQualifiedName~E2E`
- Expected duration: <30 seconds

### Required Workflow Permissions

```yaml
permissions:
    contents: read
    id-token: write
    pull-requests: write
    checks: write  # Required for dorny/test-reporter@v1
```

### Cosmos DB Emulator Service Configuration

```yaml
services:
    cosmos-db:
        image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
        ports:
            - 8081:8081
            - 10251:10251
        environment:
            AZURE_COSMOS_EMULATOR_PARTITION_COUNT: 10
            AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE: false
```

---

## Coverage Contract

**Target**: ≥70% line coverage for Biotrackr.Food.Svc project

**Exclusions**:
- Program.cs (marked with `[ExcludeFromCodeCoverage]` attribute)
- Auto-generated code
- Model classes with only properties

**Measurement**:
- Tool: coverlet.collector (via `dotnet test --collect:"XPlat Code Coverage"`)
- Report format: Cobertura XML
- Published via: dorny/test-reporter@v1

**Success Criteria**:
- Overall coverage ≥70%
- No flaky tests (100% pass rate required)
- All tests complete within performance targets

---

## References

- Weight Service Tests: `src/Biotrackr.Weight.Svc/Biotrackr.Weight.Svc.IntegrationTests/`
- Sleep Service Tests: `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests/`
- Common Resolutions: `.specify/memory/common-resolutions.md`
- Decision Records: `docs/decision-records/2025-10-28-*.md`
