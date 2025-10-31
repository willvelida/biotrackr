# Data Model: Enhanced Test Coverage for Sleep API

**Feature**: 006-sleep-api-tests  
**Date**: 2025-10-31  
**Status**: Complete

## Overview

This document defines the test-related entities and structures for the Sleep API test coverage expansion. Since this is a testing feature, the "data model" focuses on test fixtures, test data structures, and testing infrastructure rather than domain entities.

## Test Infrastructure Entities

### ContractTestFixture

**Purpose**: Manages application lifecycle for Contract tests (no database dependencies)

**Attributes**:
- `Factory` (ContractTestWebApplicationFactory): Test application factory
- `Client` (HttpClient): Test HTTP client for API calls
- Implements: `IAsyncLifetime` for setup/teardown

**Lifecycle**:
- InitializeAsync: Creates factory and client
- DisposeAsync: Cleans up resources

**Usage Pattern**:
```csharp
[Collection("Contract Tests")]
public class ProgramStartupTests
{
    private readonly ContractTestFixture _fixture;
    
    public ProgramStartupTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }
}
```

---

### IntegrationTestFixture

**Purpose**: Manages application lifecycle and Cosmos DB Emulator connection for E2E tests

**Attributes**:
- `Factory` (SleepApiWebApplicationFactory): Test application factory with database
- `Client` (HttpClient): Test HTTP client for API calls
- `CosmosClient` (CosmosClient): Direct database client for test verification
- `Database` (Database): Cosmos DB database instance
- `Container` (Container): Cosmos DB container instance
- `CosmosDbEndpoint` (string): Emulator endpoint URL
- `CosmosDbAccountKey` (string): Emulator account key
- Implements: `IAsyncLifetime` for setup/teardown

**Lifecycle**:
- InitializeAsync: 
  - Creates Cosmos DB client with Gateway mode
  - Creates/verifies database and container exist
  - Creates application factory with overridden configuration
  - Creates HTTP client
- DisposeAsync: Cleans up all resources

**Validation Rules**:
- CosmosClient MUST use `ConnectionMode.Gateway` (not Direct)
- MUST include `ServerCertificateCustomValidationCallback = true` for Emulator
- Container MUST be verified/created before tests run

**Usage Pattern**:
```csharp
[Collection("E2E Tests")]
public class SleepEndpointTests
{
    private readonly IntegrationTestFixture _fixture;
    
    public SleepEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    private async Task ClearContainerAsync()
    {
        // Clear all documents for test isolation
    }
}
```

---

### ContractTestWebApplicationFactory

**Purpose**: WebApplicationFactory for Contract tests with minimal configuration overrides

**Attributes**:
- Inherits: `WebApplicationFactory<Program>`

**Configuration Overrides**:
- Overrides health check dependencies (if needed)
- Overrides logging for test output
- Does NOT configure database (Contract tests don't use it)

**Example**:
```csharp
public class ContractTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Minimal overrides for contract tests
            services.AddLogging(logging => logging.AddXUnit(/* output */));
        });
    }
}
```

---

### SleepApiWebApplicationFactory

**Purpose**: WebApplicationFactory for E2E tests with full Cosmos DB Emulator integration

**Attributes**:
- Inherits: `WebApplicationFactory<Program>`
- `CosmosDbEndpoint` (string): Injected from fixture
- `CosmosDbAccountKey` (string): Injected from fixture

**Configuration Overrides**:
- Replaces CosmosClient with test instance (Gateway mode, certificate validation disabled)
- Overrides Settings with test database/container names
- Configures logging for test output
- Overrides any Azure Key Vault dependencies (not needed in tests)

**Example**:
```csharp
public class SleepApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public string CosmosDbEndpoint { get; set; }
    public string CosmosDbAccountKey { get; set; }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove production CosmosClient
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CosmosClient));
            if (descriptor != null) services.Remove(descriptor);
            
            // Add test CosmosClient with Gateway mode
            services.AddSingleton<CosmosClient>(sp =>
            {
                return new CosmosClient(CosmosDbEndpoint, CosmosDbAccountKey, new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway,
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
            
            // Override Settings for test
            services.Configure<Settings>(opts =>
            {
                opts.DatabaseName = "BiotrackrTestDb";
                opts.ContainerName = "SleepTestContainer";
            });
        });
    }
}
```

---

## Test Data Structures

### Test Collections

**Contract Test Collection**:
```csharp
[CollectionDefinition("Contract Tests")]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
    // This class has no code, and is never created.
    // Its purpose is simply to be the place to apply [CollectionDefinition] 
    // and all the ICollectionFixture<> interfaces.
}
```

**E2E Test Collection**:
```csharp
[CollectionDefinition("E2E Tests")]
public class E2ETestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
```

---

### Test Helper Methods

**Container Cleanup** (for test isolation):
```csharp
/// <summary>
/// Clears all documents from the test container to ensure test isolation.
/// MUST be called at the start of each E2E test method.
/// </summary>
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

## Domain Entities (From Sleep API)

These entities are tested but not modified by this feature.

### SleepDocument

**Purpose**: Main document stored in Cosmos DB

**Attributes**:
- `Id` (string): Unique identifier
- `Sleep` (Sleep): Sleep data from Fitbit
- `Date` (string): ISO date string
- `DocumentType` (string): Partition key ("Sleep")

**Validation Rules**:
- Id MUST be unique
- Date MUST be valid ISO format (YYYY-MM-DD)
- DocumentType MUST equal "Sleep"

---

### Sleep (Fitbit Entity)

**Purpose**: Sleep session data from Fitbit API

**Attributes**:
- `DateOfSleep` (string): Sleep date
- `Duration` (long): Total sleep duration in milliseconds
- `Efficiency` (int): Sleep efficiency percentage
- `IsMainSleep` (bool): Primary sleep session flag
- `Levels` (Levels): Sleep stage breakdown
- `LogId` (long): Fitbit log identifier
- `LogType` (string): Log type (auto/manual)
- `MinutesAfterWakeup` (int): Minutes awake after sleep
- `MinutesAsleep` (int): Total minutes asleep
- `MinutesAwake` (int): Total minutes awake
- `MinutesToFallAsleep` (int): Minutes to fall asleep
- `StartTime` (string): Sleep start timestamp
- `TimeInBed` (int): Total time in bed (minutes)
- `Type` (string): Sleep type (stages/classic)

---

### Levels

**Purpose**: Sleep stage data breakdown

**Attributes**:
- `Data` (List<SleepData>): Detailed sleep stage intervals
- `ShortData` (List<SleepData>): Short awakening intervals
- `Summary` (Summary): Aggregated stage summaries

---

### SleepData

**Purpose**: Individual sleep stage interval

**Attributes**:
- `DateTime` (string): Interval start time
- `Level` (string): Sleep stage (deep/light/rem/wake)
- `Seconds` (int): Interval duration

---

### Summary

**Purpose**: Aggregated sleep stage totals

**Attributes**:
- `Deep` (Stages): Deep sleep summary
- `Light` (Stages): Light sleep summary
- `Rem` (Stages): REM sleep summary
- `Wake` (Stages): Wake time summary

---

### Stages

**Purpose**: Summary statistics for a sleep stage

**Attributes**:
- `Count` (int): Number of stage intervals
- `Minutes` (int): Total minutes in stage
- `ThirtyDayAvgMinutes` (int): 30-day average

---

### PaginationRequest

**Purpose**: Query parameters for paginated endpoints

**Attributes**:
- `PageNumber` (int?): Optional page number (default: 1)
- `PageSize` (int?): Optional page size (default: 20)

**Validation Rules**:
- PageNumber MUST be ≥1 if provided
- PageSize MUST be ≥1 and ≤100 if provided

---

### PaginationResponse<T>

**Purpose**: Paginated response wrapper

**Attributes**:
- `Items` (List<T>): Page items
- `TotalCount` (int): Total items across all pages
- `PageNumber` (int): Current page number
- `PageSize` (int): Items per page
- `TotalPages` (int): Calculated total pages

---

### Settings

**Purpose**: Application configuration

**Attributes**:
- `DatabaseName` (string): Cosmos DB database name
- `ContainerName` (string): Cosmos DB container name

**Validation Rules**:
- Both properties MUST be non-null and non-empty
- Values loaded from Azure App Configuration in production
- Overridden with test values in integration tests

---

## Test Coverage Entities

### Coverage Report Structure

Generated by `dotnet test --collect:"XPlat Code Coverage"`:

**Files**:
- `TestResults/{guid}/coverage.cobertura.xml`: Cobertura format coverage report

**Key Metrics**:
- Line coverage percentage
- Branch coverage percentage
- Coverage by file/class/method

**Thresholds**:
- Overall: ≥80% line coverage
- Per-component: No hard threshold, but gaps should be justified

---

## Relationships

```
ContractTestFixture
  └── ContractTestWebApplicationFactory
      └── Program (Sleep API)

IntegrationTestFixture
  ├── SleepApiWebApplicationFactory
  │   └── Program (Sleep API)
  ├── CosmosClient (Gateway mode)
  ├── Database
  └── Container
      └── SleepDocument[]
          └── Sleep
              └── Levels
                  ├── Data (SleepData[])
                  └── Summary
                      └── Stages
```

---

## State Transitions

### Test Fixture Lifecycle

```
[Not Created]
    ↓
InitializeAsync() called
    ↓
[Initialized] - Tests can run
    ↓
DisposeAsync() called
    ↓
[Disposed] - Fixture cleaned up
```

### E2E Test Execution Flow

```
Test Method Start
    ↓
ClearContainerAsync() - Clean state
    ↓
Arrange - Create test data
    ↓
Act - Call API endpoint
    ↓
Assert - Verify results
    ↓
(Implicit) - Fixture persists for next test
```

---

## Design Patterns Applied

1. **Test Fixture Pattern**: Shared expensive resources (app factory, database) across tests
2. **Collection Fixture Pattern**: xUnit collections share fixtures within test collections
3. **Factory Pattern**: WebApplicationFactory creates configured test applications
4. **Builder Pattern**: CosmosClientOptions built with specific test configuration
5. **Template Method Pattern**: WebApplicationFactory.ConfigureWebHost override points
6. **Singleton Pattern**: One fixture instance per collection
7. **Repository Pattern**: ICosmosRepository abstraction (tested, not modified)

---

## References

- Sleep API Models: `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api/Models/`
- WebApplicationFactory: `Microsoft.AspNetCore.Mvc.Testing`
- xUnit Collection Fixtures: https://xunit.net/docs/shared-context
- Cosmos DB Client: `Microsoft.Azure.Cosmos`
