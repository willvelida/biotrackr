# Data Model: Food Service Test Coverage and Integration Tests

**Feature**: 009-food-svc-tests  
**Date**: November 3, 2025  
**Status**: Complete

## Overview

This document defines the data entities used in Food Service tests, including domain models from the main service, test-specific entities, and fixture configurations.

## Domain Entities (from Food Service)

### FoodDocument

Primary entity stored in Cosmos DB representing a daily food log snapshot.

**Properties**:
- `Id` (string, nullable): Unique identifier for the document (format: `{userId}_{date}`)
- `Food` (FoodResponse, nullable): Complete food log data from Fitbit API
- `Date` (string, nullable): ISO 8601 date string (e.g., "2025-11-03")
- `DocumentType` (string, nullable): Partition key value (constant: "food")

**Validation Rules**:
- `Id` must be unique within partition
- `Date` must be valid ISO 8601 date
- `DocumentType` must be "food" for proper partitioning

**State Transitions**:
- Created: New food log document persisted from Fitbit API data
- Updated: Existing document replaced with updated food log data (same date)
- Deleted: Test cleanup removes documents (not production scenario)

**Relationships**:
- Contains one `FoodResponse` entity with complete daily food data
- Stored in `biotrackr-food` container with `/documentType` partition key

---

### FoodResponse

Fitbit API response structure containing complete food log for a date.

**Properties**:
- `foods` (List<Food>): Collection of food items logged during the day
- `goals` (Goals): Daily nutritional goals set by user
- `summary` (Summary): Daily nutritional summary (actual vs goals)

**Validation Rules**:
- `foods` list can be empty (no food logged)
- `goals` must have valid nutritional values (>= 0)
- `summary` must have valid nutritional values (>= 0)

**Relationships**:
- Contained within `FoodDocument`
- Contains collections of `Food`, `Goals`, and `Summary` entities

---

### Food (LoggedFood)

Individual food item logged by the user.

**Properties**:
- `loggedFood` (LoggedFood): Details about the logged food item
- `nutritionalValues` (NutritionalValues): Nutritional breakdown for the item

**LoggedFood Properties**:
- `accessLevel` (string): Access level for food item
- `amount` (int): Quantity of food consumed
- `brand` (string): Brand name of food
- `calories` (int): Calorie count
- `foodId` (int): Fitbit food identifier
- `locale` (string): Locale for food database
- `mealTypeId` (int): Meal category (breakfast, lunch, dinner, snack)
- `name` (string): Food name
- `unit` (Unit): Unit of measurement
- `units` (List<int>): Available unit IDs

**NutritionalValues Properties**:
- `calories` (double): Calories in the food item
- `carbs` (double): Carbohydrates in grams
- `fat` (double): Fat in grams
- `fiber` (double): Dietary fiber in grams
- `protein` (double): Protein in grams
- `sodium` (double): Sodium in milligrams

**Validation Rules**:
- All numeric nutritional values must be >= 0
- `amount` must be > 0
- `name` cannot be null or empty
- `mealTypeId` must be valid category (1-4: breakfast, lunch, dinner, snack)

---

### Goals

Daily nutritional goals set by the user.

**Properties**:
- `calories` (int): Daily calorie goal
- `estimatedCaloriesOut` (int): Estimated calories burned

**Validation Rules**:
- Values must be >= 0
- Typically `calories` range: 1200-3000 (reasonable daily goals)
- `estimatedCaloriesOut` calculated by Fitbit based on activity

---

### Summary

Daily nutritional summary comparing actual intake to goals.

**Properties**:
- `calories` (double): Total calories consumed
- `carbs` (double): Total carbohydrates in grams
- `fat` (double): Total fat in grams
- `fiber` (double): Total fiber in grams
- `protein` (double): Total protein in grams
- `sodium` (double): Total sodium in milligrams
- `water` (double): Total water in milliliters

**Validation Rules**:
- All values must be >= 0
- Values should sum to match individual food items
- `water` typically 0-5000 ml (reasonable daily intake)

**Relationships**:
- Aggregates nutritional data from all `Food` items in the day

---

### Unit

Unit of measurement for food quantities.

**Properties**:
- `id` (int): Unit identifier
- `name` (string): Unit name (singular, e.g., "cup", "oz")
- `plural` (string): Plural form of unit name (e.g., "cups", "oz")

**Validation Rules**:
- `id` must be unique
- `name` and `plural` cannot be null or empty

---

## Test-Specific Entities

### TestDataGenerator

Helper class to generate realistic test data for unit and integration tests.

**Purpose**: Provide consistent, valid test data that matches production data structure

**Methods**:
- `GenerateFoodDocument(string date, string userId)`: Creates valid FoodDocument with random food items
- `GenerateFoodResponse()`: Creates FoodResponse with 2-5 random food items
- `GenerateLoggedFood(string name, int calories)`: Creates individual food item with nutritional values
- `GenerateGoals(int calorieGoal)`: Creates Goals with specified calorie target
- `GenerateSummary(int calories, double carbs, double fat, double protein)`: Creates Summary with specified values

**Validation**:
- Generated data must pass all entity validation rules
- Nutritional values should be realistic (e.g., calories 50-1000 per item)
- Dates must be valid ISO 8601 format

---

### IntegrationTestFixture (E2E Tests)

Configures Cosmos DB Emulator connection for end-to-end tests.

**Properties**:
- `CosmosClient` (CosmosClient): Configured client with Gateway mode
- `Database` (Database): Reference to test database (`biotrackr-test`)
- `Container` (Container): Reference to test container (`food`)
- `ConnectionString` (string): Cosmos Emulator connection string
- `AccountKey` (string): Emulator account key

**Configuration**:
- `ConnectionMode`: Gateway (HTTPS port 8081)
- `SerializerOptions`: CamelCase property naming
- `HttpClientFactory`: Custom handler with certificate validation bypass
- Database: `biotrackr-test`
- Container: `food` with `/documentType` partition key

**Lifecycle**:
- Constructor: Initialize CosmosClient, create database/container if not exists
- Dispose: Cleanup resources (client disposal)
- Shared across all E2E tests in IntegrationTestCollection

**Validation Rules**:
- Emulator must be running before fixture initialization
- Database and container creation must succeed
- Connection string must be valid Cosmos DB format

---

### ContractTestFixture (Contract Tests)

Configures in-memory services for fast contract tests without external dependencies.

**Properties**:
- `ServiceProvider` (IServiceProvider): Configured DI container with test services
- `Configuration` (IConfiguration): In-memory configuration with test values

**Configuration**:
- In-memory configuration with required keys:
  - `keyvaulturl`: Mock value
  - `managedidentityclientid`: Mock value
  - `cosmosdbendpoint`: Mock value
  - `applicationinsightsconnectionstring`: Mock value
- Mock Azure SDK clients (CosmosClient, SecretClient)
- Real service registrations for DI validation

**Lifecycle**:
- Constructor: Build host with in-memory configuration
- Dispose: Cleanup service provider
- Shared across all contract tests in ContractTestCollection

**Validation Rules**:
- All required configuration keys must be present
- Service provider must build successfully
- All registered services must resolve without exceptions

---

### IntegrationTestCollection

xUnit collection definition for sharing IntegrationTestFixture across E2E tests.

**Purpose**: Ensure single Cosmos DB connection shared across all E2E tests in the collection

**Usage**:
```csharp
[Collection(nameof(IntegrationTestCollection))]
public class FoodServiceTests
{
    private readonly IntegrationTestFixture _fixture;
    
    public FoodServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
}
```

**Validation Rules**:
- Collection name must match across all E2E test classes
- Fixture must implement ICollectionFixture<IntegrationTestFixture>

---

### ContractTestCollection

xUnit collection definition for sharing ContractTestFixture across contract tests.

**Purpose**: Ensure single DI container shared across all contract tests in the collection

**Usage**:
```csharp
[Collection(nameof(ContractTestCollection))]
public class ServiceRegistrationTests
{
    private readonly ContractTestFixture _fixture;
    
    public ServiceRegistrationTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }
}
```

**Validation Rules**:
- Collection name must match across all contract test classes
- Fixture must implement ICollectionFixture<ContractTestFixture>

---

## Test Data Patterns

### Unit Test Data

**Mocked Dependencies**:
- IFitbitService: Returns mock FoodResponse with 1-3 food items
- ICosmosRepository: Returns mock Task.CompletedTask for async operations
- ILogger: Mock logger for verification

**Test Data Characteristics**:
- Minimal data required to test specific scenario
- Focus on edge cases (empty lists, null values, exceptions)
- Use AutoFixture for generating random valid data

---

### Contract Test Data

**In-Memory Configuration**:
```json
{
  "keyvaulturl": "https://test-keyvault.vault.azure.net/",
  "managedidentityclientid": "test-client-id",
  "cosmosdbendpoint": "https://test-cosmos.documents.azure.com:443/",
  "applicationinsightsconnectionstring": "InstrumentationKey=test-key"
}
```

**Mock Services**:
- SecretClient: Mock (not used in tests)
- CosmosClient: Mock (not used in tests)
- All other services: Real implementations for DI validation

---

### E2E Test Data

**Cosmos DB Test Container**:
- Database: `biotrackr-test`
- Container: `food`
- Partition Key: `/documentType`

**Test Documents**:
```json
{
  "id": "testuser_2025-11-03",
  "date": "2025-11-03",
  "documentType": "food",
  "food": {
    "foods": [
      {
        "loggedFood": {
          "name": "Oatmeal",
          "calories": 150,
          "amount": 1,
          "mealTypeId": 1
        },
        "nutritionalValues": {
          "calories": 150,
          "carbs": 27,
          "protein": 5,
          "fat": 3,
          "fiber": 4,
          "sodium": 5
        }
      }
    ],
    "goals": {
      "calories": 2000,
      "estimatedCaloriesOut": 2200
    },
    "summary": {
      "calories": 150,
      "carbs": 27,
      "protein": 5,
      "fat": 3,
      "fiber": 4,
      "sodium": 5,
      "water": 500
    }
  }
}
```

**Cleanup Pattern**:
- Call `ClearContainerAsync()` at start of each test
- Query all documents: `SELECT c.id, c.documentType FROM c`
- Delete each document using id and partition key
- Ensures test isolation (no data leakage between tests)

---

## Validation Summary

All entities follow these principles:
- **Type Safety**: Use strongly-typed models (never `dynamic` with FluentAssertions)
- **Null Safety**: Nullable reference types where appropriate
- **Validation**: Range checks for numeric values
- **Consistency**: Naming conventions match Fitbit API (camelCase)
- **Test Isolation**: ClearContainerAsync ensures clean state per test

## References

- Food Service Models: `src/Biotrackr.Food.Svc/Biotrackr.Food.Svc/Models/`
- Weight Service Test Patterns: `src/Biotrackr.Weight.Svc/Biotrackr.Weight.Svc.IntegrationTests/`
- Sleep Service Test Patterns: `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests/`
- Common Resolutions: `.specify/memory/common-resolutions.md`
