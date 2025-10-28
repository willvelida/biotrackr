# Data Model: Weight Service Unit Test Coverage Improvement

**Feature**: 002-weight-svc-coverage  
**Date**: 2025-10-28  
**Status**: Complete

## Overview

This feature adds unit tests without modifying existing data models. All entities listed here are from the existing codebase and included for reference to understand test data requirements.

## Existing Entities

### WeightWorker

**Purpose**: Background service that orchestrates weight data synchronization from Fitbit to Cosmos DB

**Key Properties**:
- `_fitbitService: IFitbitService` - Service for fetching weight data from Fitbit API
- `_weightService: IWeightService` - Service for saving weight data to repository
- `_logger: ILogger<WeightWorker>` - Logger for diagnostic messages
- `_appLifetime: IHostApplicationLifetime` - Application lifetime manager

**Methods**:
- `ExecuteAsync(CancellationToken): Task<int>` - Main execution method, returns 0 for success, 1 for failure

**State Transitions**:
1. Initial → Executing (when ExecuteAsync called)
2. Executing → Fetching (calling GetWeightLogs)
3. Fetching → Processing (iterating over weight entries)
4. Processing → Saving (calling MapAndSaveDocument for each entry)
5. Saving → Completed (returns 0) OR Error (returns 1)
6. Completed/Error → Stopped (StopApplication called)

**Validation Rules**:
- Must have non-null dependencies
- Date range: startDate = 7 days ago, endDate = today
- Date format: "yyyy-MM-dd"

### Weight (Entity)

**Purpose**: Represents a single weight measurement from Fitbit

**Key Attributes**:
- `Bmi: double` - Body Mass Index
- `Date: string` - Date of measurement (yyyy-MM-dd)
- `Fat: double` - Body fat percentage
- `LogId: object` - Fitbit log identifier
- `Source: string` - Source of measurement (e.g., "API", "Aria")
- `Time: string` - Time of measurement (HH:mm:ss)
- `weight: double` - Weight value

**Validation Rules**:
- All fields populated by Fitbit API response
- Date must be valid date string

### WeightResponse

**Purpose**: Container for collection of weight measurements from Fitbit API

**Key Attributes**:
- `Weight: List<Weight>` - Collection of weight measurements

**Validation Rules**:
- List can be empty (valid scenario)
- List must not be null

### WeightDocument

**Purpose**: Document structure for storing weight data in Cosmos DB

**Key Attributes**:
- `Id: string` - Unique identifier (GUID)
- `Weight: Weight` - Weight measurement data
- `Date: string` - Date of measurement
- `DocumentType: string` - Always "Weight"

**Validation Rules**:
- Id must be unique GUID
- DocumentType must be "Weight"
- Weight must not be null

## Test Data Requirements

### Mock Data for WeightWorker Tests

**WeightResponse Mock**:
```csharp
// Happy path - multiple entries
var weightResponse = new WeightResponse
{
    Weight = new List<Weight>
    {
        new Weight 
        { 
            Date = "2023-10-01", 
            Bmi = 25.0, 
            Fat = 15.0, 
            weight = 80.5,
            LogId = 12345,
            Source = "Aria",
            Time = "08:00:00"
        },
        new Weight 
        { 
            Date = "2023-10-02", 
            Bmi = 24.8, 
            Fat = 14.8, 
            weight = 80.0,
            LogId = 12346,
            Source = "Aria",
            Time = "08:15:00"
        }
    }
};

// Edge case - empty response
var emptyResponse = new WeightResponse
{
    Weight = new List<Weight>()
};
```

**Date Parameters**:
- Format: "yyyy-MM-dd"
- Start date: 7 days prior to current date
- End date: Current date

### AutoFixture Usage

Following existing test patterns, use AutoFixture for generating Weight entities when specific values are not critical:

```csharp
var fixture = new Fixture();
var weight = fixture.Create<Weight>();
```

## Relationships

```
WeightWorker
    ├─depends on─> IFitbitService
    │                 └─returns─> WeightResponse
    │                                 └─contains─> List<Weight>
    │
    └─depends on─> IWeightService
                      └─accepts─> Weight
                      └─creates─> WeightDocument
```

## No Data Changes Required

This feature does NOT:
- Modify existing entity structures
- Add new entities
- Change validation rules
- Alter database schema
- Modify API contracts

All data models remain unchanged; tests will use existing models with mock implementations of dependencies.

## Notes

- All entities use JSON property name mapping for Fitbit API deserialization
- WeightDocument uses camelCase serialization for Cosmos DB
- Test data should respect existing entity structure but can use minimal/mock values where full data not needed
