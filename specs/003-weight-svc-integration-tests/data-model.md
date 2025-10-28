# Data Model: Weight Service Integration Tests

**Date**: October 28, 2025  
**Feature**: Integration test infrastructure for Biotrackr.Weight.Svc

## Overview

This document defines the test data models, fixtures, and test infrastructure entities used in the Weight Service integration tests. These models represent test versions of production entities and test-specific infrastructure components.

---

## Production Entities (Under Test)

### WeightDocument

**Purpose**: Cosmos DB document representing a weight measurement

**Properties**:
| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| Id | string | Unique document identifier (GUID) | Required, must be unique |
| Date | string | Measurement date (yyyy-MM-dd) | Required, valid date format |
| Weight | Weight | Weight measurement details | Required, valid Weight object |
| DocumentType | string | Document type identifier | Required, must be "Weight" |

**Partition Key**: `DocumentType`

**State Transitions**: None (immutable once created)

**Relationships**: 
- Contains one Weight entity
- Stored in Cosmos DB container with partition key "Weight"

**Example**:
```json
{
  "id": "a1b2c3d4-e5f6-4a5b-8c7d-9e0f1a2b3c4d",
  "date": "2025-10-28",
  "weight": {
    "date": "2025-10-28",
    "weight": 75.5,
    "bmi": 23.4,
    "fat": 18.2
  },
  "documentType": "Weight"
}
```

---

### Weight (Entity)

**Purpose**: Weight measurement data from Fitbit API

**Properties**:
| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| Date | string | Measurement date | Required, yyyy-MM-dd format |
| Weight | double | Weight in kilograms | Required, > 0 |
| Bmi | double | Body Mass Index | Optional, >= 0 |
| Fat | double | Body fat percentage | Optional, 0-100 |

**Validation Rules**:
- Date must be valid and not in the future
- Weight must be positive number
- BMI must be non-negative if provided
- Fat percentage must be 0-100 if provided

**Example**:
```json
{
  "date": "2025-10-28",
  "weight": 75.5,
  "bmi": 23.4,
  "fat": 18.2
}
```

---

### WeightResponse

**Purpose**: Fitbit API response containing weight measurements

**Properties**:
| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| Weight | List<Weight> | Array of weight measurements | Required, can be empty |

**Validation Rules**:
- Weight array must not be null (can be empty)
- All Weight objects must be valid

**Example**:
```json
{
  "weight": [
    {
      "date": "2025-10-28",
      "weight": 75.5,
      "bmi": 23.4,
      "fat": 18.2
    },
    {
      "date": "2025-10-27",
      "weight": 75.3,
      "bmi": 23.3,
      "fat": 18.1
    }
  ]
}
```

---

## Test Infrastructure Entities

### ContractTestFixture

**Purpose**: Lightweight fixture for contract tests (no external dependencies)

**Properties**:
| Property | Type | Description |
|----------|------|-------------|
| ServiceProvider | IServiceProvider | DI container for service verification |

**Lifecycle**:
- **Initialize**: Build minimal service collection with required services
- **Dispose**: Dispose service provider

**Responsibilities**:
- Configure minimal DI container
- Provide mock implementations for external dependencies
- Verify service registrations

**Example Setup**:
```csharp
public class ContractTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    
    public ContractTestFixture()
    {
        var services = new ServiceCollection();
        
        // Add minimal configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["cosmosdbendpoint"] = "https://localhost:8081",
                ["Biotrackr:DatabaseName"] = "test",
                ["Biotrackr:ContainerName"] = "test"
            })
            .Build();
            
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<Settings>()
            .Configure<IConfiguration>((settings, config) => 
                config.GetSection("Biotrackr").Bind(settings));
        
        // Add mocks for external dependencies
        var mockSecretClient = new Mock<SecretClient>();
        services.AddSingleton(mockSecretClient.Object);
        
        var mockCosmosClient = new Mock<CosmosClient>();
        services.AddSingleton(mockCosmosClient.Object);
        
        // Add actual services under test
        services.AddScoped<ICosmosRepository, CosmosRepository>();
        services.AddScoped<IWeightService, WeightService>();
        services.AddScoped<IFitbitService, FitbitService>();
        
        ServiceProvider = services.BuildServiceProvider();
    }
    
    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
```

---

### IntegrationTestFixture

**Purpose**: Full fixture for E2E tests with Cosmos DB Emulator

**Properties**:
| Property | Type | Description |
|----------|------|-------------|
| CosmosClient | CosmosClient | Connected Cosmos DB client |
| Database | Database | Test database instance |
| Container | Container | Test container instance |
| DatabaseName | string | Test database name (unique per run) |
| ContainerName | string | Test container name |
| MockSecretClient | Mock<SecretClient> | Mocked Key Vault client |
| MockHttpMessageHandler | MockHttpMessageHandler | Mocked HTTP handler for Fitbit API |

**Lifecycle**:
- **InitializeAsync**: Connect to emulator, create database/container
- **DisposeAsync**: Delete database, dispose clients

**Responsibilities**:
- Connect to Cosmos DB Emulator
- Create unique test database/container
- Provide mocked external dependencies
- Clean up resources after tests

**Example Setup**:
```csharp
public class IntegrationTestFixture : IAsyncLifetime
{
    public CosmosClient CosmosClient { get; private set; }
    public Database Database { get; private set; }
    public Container Container { get; private set; }
    public string DatabaseName { get; private set; }
    public string ContainerName { get; } = "weight-test";
    public Mock<SecretClient> MockSecretClient { get; private set; }
    public MockHttpMessageHandler MockHttpMessageHandler { get; private set; }
    
    public async Task InitializeAsync()
    {
        // Generate unique database name to avoid conflicts
        DatabaseName = $"biotrackr-test-{Guid.NewGuid():N}";
        
        // Connect to Cosmos DB Emulator
        var endpoint = "https://localhost:8081";
        var key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        
        var options = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };
        
        CosmosClient = new CosmosClient(endpoint, key, options);
        
        // Create database and container
        Database = await CosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
        Container = await Database.CreateContainerIfNotExistsAsync(
            ContainerName, 
            "/documentType");
        
        // Setup mocked dependencies
        MockSecretClient = new Mock<SecretClient>();
        MockSecretClient
            .Setup(x => x.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecretModelFactory.KeyVaultSecret(
                new SecretProperties("AccessToken"), 
                "test-fitbit-access-token"));
        
        MockHttpMessageHandler = new MockHttpMessageHandler(
            TestDataBuilder.BuildSuccessfulFitbitResponse);
    }
    
    public async Task DisposeAsync()
    {
        if (Database != null)
        {
            await Database.DeleteAsync();
        }
        
        CosmosClient?.Dispose();
    }
}
```

---

### MockHttpMessageHandler

**Purpose**: Custom HTTP message handler for mocking Fitbit API responses

**Properties**:
| Property | Type | Description |
|----------|------|-------------|
| ResponseHandler | Func<HttpRequestMessage, HttpResponseMessage> | Function to generate responses |

**Responsibilities**:
- Intercept HTTP requests
- Return predefined responses based on request
- Enable testing of different API scenarios (success, error, timeout)

**Example**:
```csharp
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseHandler;
    
    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseHandler)
    {
        _responseHandler = responseHandler ?? throw new ArgumentNullException(nameof(responseHandler));
    }
    
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_responseHandler(request));
    }
}
```

---

### TestDataBuilder

**Purpose**: Centralized test data creation helper

**Methods**:
| Method | Return Type | Description |
|--------|-------------|-------------|
| BuildWeightResponse | WeightResponse | Creates WeightResponse with N weight entries |
| BuildWeight | Weight | Creates single Weight entity |
| BuildWeightDocument | WeightDocument | Creates complete WeightDocument |
| BuildSuccessfulFitbitResponse | HttpResponseMessage | Creates successful Fitbit API response |
| BuildErrorFitbitResponse | HttpResponseMessage | Creates error Fitbit API response |

**Example**:
```csharp
public static class TestDataBuilder
{
    public static WeightResponse BuildWeightResponse(int count = 7, DateTime? startDate = null)
    {
        var start = startDate ?? DateTime.Now;
        
        return new WeightResponse
        {
            Weight = Enumerable.Range(0, count)
                .Select(i => BuildWeight(start.AddDays(-i)))
                .ToList()
        };
    }
    
    public static Weight BuildWeight(DateTime date)
    {
        return new Weight
        {
            Date = date.ToString("yyyy-MM-dd"),
            Weight = 75.0 + Random.Shared.NextDouble() * 5.0,
            Bmi = 22.0 + Random.Shared.NextDouble() * 3.0,
            Fat = 15.0 + Random.Shared.NextDouble() * 5.0
        };
    }
    
    public static WeightDocument BuildWeightDocument(string? id = null, DateTime? date = null)
    {
        var measurementDate = date ?? DateTime.Now;
        
        return new WeightDocument
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Date = measurementDate.ToString("yyyy-MM-dd"),
            Weight = BuildWeight(measurementDate),
            DocumentType = "Weight"
        };
    }
    
    public static HttpResponseMessage BuildSuccessfulFitbitResponse(HttpRequestMessage request)
    {
        var response = BuildWeightResponse();
        var json = JsonSerializer.Serialize(response);
        
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
    
    public static HttpResponseMessage BuildErrorFitbitResponse(HttpRequestMessage request)
    {
        return new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\": \"Invalid access token\"}")
        };
    }
}
```

---

## Test Collections

### ContractTestCollection

**Purpose**: xUnit collection for contract tests

**Configuration**:
```csharp
[CollectionDefinition("Contract Tests")]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
    // This class has no code, and is never created
    // Its purpose is to be the place to apply [CollectionDefinition]
}
```

**Usage**:
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

### IntegrationTestCollection

**Purpose**: xUnit collection for E2E tests

**Configuration**:
```csharp
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // Collection definition with shared fixture
}
```

**Usage**:
```csharp
[Collection("Integration Tests")]
public class WeightWorkerTests
{
    private readonly IntegrationTestFixture _fixture;
    
    public WeightWorkerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
}
```

---

## Test Configuration

### appsettings.Test.json

**Purpose**: Test-specific configuration values

**Content**:
```json
{
  "cosmosdbendpoint": "https://localhost:8081",
  "Biotrackr": {
    "CosmosDb": {
      "AccountKey": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
    },
    "DatabaseName": "biotrackr-test",
    "ContainerName": "weight-test"
  },
  "azureappconfigendpoint": "",
  "managedidentityclientid": "",
  "keyvaulturl": ""
}
```

---

## Data Flow

### Contract Test Data Flow

```
Test → ContractTestFixture → ServiceProvider → Service (with mocks) → Assertions
```

1. Test requests service from fixture
2. Fixture provides service with mocked dependencies
3. Service executes logic (no external calls)
4. Test asserts service configuration/behavior

### E2E Test Data Flow

```
Test → IntegrationTestFixture → WeightWorker
  → FitbitService (MockHttpMessageHandler) → Mock API Response
  → WeightService → CosmosRepository → Cosmos DB Emulator
  → Test queries Emulator to verify results
```

1. Test sets up scenario using fixture
2. Worker executes complete workflow
3. Mock HTTP handler provides Fitbit API responses
4. Service saves data to Cosmos DB Emulator
5. Test queries emulator to verify data persisted correctly

---

## Summary

This data model provides:
- **Production Entities**: WeightDocument, Weight, WeightResponse (under test)
- **Test Fixtures**: ContractTestFixture (lightweight), IntegrationTestFixture (full infrastructure)
- **Test Helpers**: MockHttpMessageHandler, TestDataBuilder
- **Test Collections**: ContractTestCollection, IntegrationTestCollection
- **Configuration**: appsettings.Test.json

All entities support the specified test scenarios and enable isolated, repeatable integration testing.
