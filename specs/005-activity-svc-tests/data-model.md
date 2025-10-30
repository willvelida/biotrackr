# Data Model: Activity Service Test Coverage and Integration Tests

**Feature**: 005-activity-svc-tests  
**Date**: 2025-10-31  
**Status**: Complete

## Overview

This document defines the data models and entities involved in testing the Activity Service. Since this is a testing feature, the focus is on test data structures, fixtures, and test entity patterns rather than production domain models.

---

## Test Infrastructure Entities

### 1. ContractTestFixture

**Purpose**: Lightweight test fixture for contract tests that validates service registration without external dependencies.

**Properties**:
- `InitializeDatabase`: bool (override = false) - Disables database initialization
- `ServiceProvider`: IServiceProvider - DI container for service resolution
- `Configuration`: IConfiguration - In-memory test configuration

**Lifecycle**:
- InitializeAsync: Creates host with in-memory configuration
- DisposeAsync: Disposes host and service provider

**Usage Pattern**:
```csharp
[Collection(nameof(ContractTestCollection))]
public class ProgramStartupTests
{
    private readonly ContractTestFixture _fixture;
    
    public ProgramStartupTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }
}
```

**Relationships**:
- Inherits from: IntegrationTestFixture (base)
- Used by: Contract test classes via ContractTestCollection

---

### 2. IntegrationTestFixture

**Purpose**: Full test infrastructure for E2E tests that manages Cosmos DB Emulator connection, database/container creation, and cleanup.

**Properties**:
- `InitializeDatabase`: bool (virtual = true) - Enables database initialization
- `CosmosClient`: CosmosClient - Cosmos DB client (Gateway mode)
- `Database`: Database - Test database instance
- `Container`: Container - Test container instance
- `ServiceProvider`: IServiceProvider - DI container for service resolution
- `Configuration`: IConfiguration - Test configuration with Cosmos DB settings

**Configuration Values**:
- `cosmosdbendpoint`: "https://localhost:8081" (Emulator endpoint)
- `cosmosdbaccountkey`: Emulator master key
- `keyvaulturl`: Mock value for tests
- `managedidentityclientid`: Mock value for tests
- `databaseId`: "BiotrackrTestDb"
- `containerId`: "ActivityTestContainer"
- `partitionKey`: "/documentType"

**Lifecycle**:
1. InitializeAsync:
   - Create CosmosClient with Gateway mode
   - Create test database (if not exists)
   - Create test container with partition key /documentType
   - Initialize service provider with test dependencies

2. DisposeAsync:
   - Delete test database
   - Dispose CosmosClient
   - Dispose service provider

**Usage Pattern**:
```csharp
[Collection(nameof(IntegrationTestCollection))]
public class CosmosRepositoryTests
{
    private readonly IntegrationTestFixture _fixture;
    
    public CosmosRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Test()
    {
        await ClearContainerAsync(); // Ensure isolation
        // Use _fixture.Container for operations
    }
}
```

**Relationships**:
- Base class for: ContractTestFixture
- Used by: E2E test classes via IntegrationTestCollection
- Manages: CosmosClient, Database, Container instances

---

### 3. xUnit Collection Definitions

#### ContractTestCollection

**Purpose**: Groups contract tests to share ContractTestFixture instance across all tests.

**Definition**:
```csharp
[CollectionDefinition(nameof(ContractTestCollection))]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
}
```

**Characteristics**:
- Tests run in parallel within collection
- No database overhead
- Fast execution (<5 seconds total)

---

#### IntegrationTestCollection

**Purpose**: Groups E2E tests to share IntegrationTestFixture and Cosmos DB connection.

**Definition**:
```csharp
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
```

**Characteristics**:
- Tests share Cosmos DB instance
- Each test must clean container for isolation
- Slower execution due to database operations

---

## Test Data Entities

### 4. TestDataGenerator

**Purpose**: Helper class to generate consistent test data for Activity Service entities.

**Methods**:

#### GenerateActivityDocument()
Returns: ActivityDocument with test data

**Properties**:
- `id`: Guid.NewGuid().ToString()
- `userId`: "test-user-123"
- `date`: DateTime.UtcNow.ToString("yyyy-MM-dd")
- `documentType`: "activity"
- `summary`: Mock Summary object
- `goals`: Mock Goals object
- `activities`: Array of mock Activity objects

**Validation Rules**:
- All required fields must be non-null
- Date format: yyyy-MM-dd
- documentType: Must be "activity" for partition key

#### GenerateActivityResponse()
Returns: ActivityResponse (from Fitbit API format)

**Properties**:
- `summary`: Summary with steps, calories, distances
- `goals`: Goals with daily targets
- `activities`: List of Activity entries

**Usage**: Mock Fitbit API responses in tests

---

### 5. Activity Domain Entities (from production code)

#### ActivityDocument

**Purpose**: Cosmos DB document structure for persisted activity data.

**Properties**:
- `id`: string (unique document identifier)
- `userId`: string (partition-compatible user identifier)
- `date`: string (yyyy-MM-dd format)
- `documentType`: string ("activity" - partition key value)
- `summary`: Summary object
- `goals`: Goals object
- `activities`: Activity[] array

**Partition Key**: `/documentType` (value = "activity")

**Validation Rules**:
- `id` must be unique
- `date` must be valid yyyy-MM-dd format
- `documentType` must be "activity"
- All required nested objects must be present

**Relationships**:
- Contains: Summary, Goals, Activity[] entities
- Stored in: Cosmos DB container
- Created by: ActivityService.MapAndSaveDocument

---

#### ActivityResponse

**Purpose**: Deserialized response from Fitbit API.

**Properties**:
- `summary`: Summary object
- `goals`: Goals object
- `activities`: Activity[] array

**Source**: Fitbit API via FitbitService
**Consumed by**: ActivityService for transformation to ActivityDocument

---

## Test Isolation Patterns

### ClearContainerAsync Method

**Purpose**: Ensures test isolation by removing all documents from test container before test execution.

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

**When to Use**:
- At the start of every E2E test
- Before any Cosmos DB write operations
- To prevent test interference from previous runs

**Performance**:
- Minimal overhead for empty containers
- Scales with number of leftover documents
- Prevents flaky tests due to shared state

---

## Configuration Models

### Test Configuration

**appsettings.Test.json Structure**:
```json
{
  "cosmosdbendpoint": "https://localhost:8081",
  "cosmosdbaccountkey": "C2y6yDjf5/R+ob0N8A7Cgv...", // Emulator key
  "keyvaulturl": "https://test-vault.vault.azure.net/",
  "managedidentityclientid": "00000000-0000-0000-0000-000000000000",
  "databaseId": "BiotrackrTestDb",
  "containerId": "ActivityTestContainer",
  "applicationinsightsconnectionstring": "InstrumentationKey=test-key"
}
```

**Usage**: Loaded by fixtures to configure test environment

---

## Entity Relationships Diagram

```
┌─────────────────────────────┐
│   ContractTestFixture       │
│  (no database init)         │
└──────────┬──────────────────┘
           │ inherits from
           ▼
┌─────────────────────────────┐
│  IntegrationTestFixture     │
│  (with database init)       │
└──────────┬──────────────────┘
           │ manages
           ▼
┌─────────────────────────────┐
│     CosmosClient            │
│   (Gateway mode)            │
└──────────┬──────────────────┘
           │ contains
           ▼
┌─────────────────────────────┐
│    Database & Container     │
│   (test instances)          │
└──────────┬──────────────────┘
           │ stores
           ▼
┌─────────────────────────────┐
│    ActivityDocument         │
│  (test data)                │
└─────────────────────────────┘

┌─────────────────────────────┐
│   TestDataGenerator         │
│  (helper utility)           │
└──────────┬──────────────────┘
           │ generates
           ├─────────────────────────┐
           ▼                         ▼
┌──────────────────┐    ┌────────────────────┐
│ ActivityDocument │    │ ActivityResponse   │
│  (for E2E tests) │    │ (mock API data)    │
└──────────────────┘    └────────────────────┘
```

---

## Data Flow: E2E Test Execution

1. **Test Setup**:
   - IntegrationTestFixture.InitializeAsync() creates Cosmos DB infrastructure
   - ClearContainerAsync() ensures clean container state

2. **Test Execution**:
   - TestDataGenerator creates ActivityResponse (mock Fitbit data)
   - FitbitService (mocked) returns ActivityResponse
   - ActivityService.MapAndSaveDocument transforms to ActivityDocument
   - CosmosRepository persists ActivityDocument to Container

3. **Test Verification**:
   - Query Container for saved documents
   - Assert document properties match expected values
   - Verify correct partition key usage

4. **Test Cleanup**:
   - Implicit via IntegrationTestFixture disposal
   - Database deletion in DisposeAsync

---

## Validation Rules Summary

### Test Fixture Validation
- ContractTestFixture MUST NOT initialize database
- IntegrationTestFixture MUST use Gateway connection mode
- All fixtures MUST implement IAsyncLifetime
- Fixtures MUST properly dispose resources

### Test Data Validation
- ActivityDocument.documentType MUST be "activity"
- Date fields MUST use yyyy-MM-dd format
- Generated test data MUST include all required fields
- Partition key MUST match schema (/documentType)

### Test Isolation Validation
- E2E tests MUST call ClearContainerAsync before operations
- Contract tests MUST NOT access Cosmos DB
- Tests MUST NOT depend on execution order
- Shared fixtures MUST maintain thread-safe state

---

## Notes

- This data model focuses on test infrastructure rather than production domain models
- Production models (ActivityDocument, ActivityResponse) are already defined in the service code
- Test fixtures follow Weight API patterns (decision record 2025-10-28-contract-test-architecture.md)
- Partition key strategy (/documentType) matches production schema
- Gateway mode is required for Cosmos DB Emulator SSL compatibility
