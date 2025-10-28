# Test Contracts: Weight Service Unit Test Coverage Improvement

**Feature**: 002-weight-svc-coverage  
**Date**: 2025-10-28  
**Status**: Complete

## Overview

This document defines the test contracts for WeightWorker unit tests. Since this feature adds tests without modifying APIs or service interfaces, the "contracts" here refer to test method signatures and mock behavior contracts.

## Test Class Contract

### WeightWorkerShould

**Namespace**: `Biotrackr.Weight.Svc.UnitTests.WorkerTests`

**Dependencies** (Constructor Injected as Mocks):
```csharp
Mock<IFitbitService> _fitbitServiceMock;
Mock<IWeightService> _weightServiceMock;
Mock<ILogger<WeightWorker>> _loggerMock;
Mock<IHostApplicationLifetime> _appLifetimeMock;
```

## Test Method Contracts

### 1. Constructor_Should_InitializeAllDependencies

**Purpose**: Verify proper dependency injection

**Signature**:
```csharp
[Fact]
public void Constructor_Should_InitializeAllDependencies()
```

**Preconditions**: None

**Test Actions**:
1. Create WeightWorker with all mock dependencies
2. Verify instance created successfully

**Expected Outcomes**:
- WeightWorker instance is not null
- No exceptions thrown

**Mock Setups**: None required

**Verifications**: Instance creation succeeds

---

### 2. ExecuteAsync_Should_FetchAndSaveWeightLogs_Successfully

**Purpose**: Verify happy path execution

**Signature**:
```csharp
[Fact]
public async Task ExecuteAsync_Should_FetchAndSaveWeightLogs_Successfully()
```

**Preconditions**: 
- GetWeightLogs returns valid WeightResponse with 2 entries

**Test Actions**:
1. Setup GetWeightLogs to return mock data
2. Setup MapAndSaveDocument to complete successfully
3. Call ExecuteAsync
4. Verify return value, method calls, and logging

**Expected Outcomes**:
- ExecuteAsync returns 0 (success)
- GetWeightLogs called once with date range (7 days ago to today)
- MapAndSaveDocument called twice (once per weight entry)
- Information log written
- StopApplication called once

**Mock Setups**:
```csharp
_fitbitServiceMock
    .Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(weightResponseWithTwoEntries);

_weightServiceMock
    .Setup(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<Weight>()))
    .Returns(Task.CompletedTask);

_appLifetimeMock
    .Setup(x => x.StopApplication());
```

**Verifications**:
```csharp
result.Should().Be(0);
_fitbitServiceMock.Verify(x => x.GetWeightLogs(startDate, endDate), Times.Once);
_weightServiceMock.Verify(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<Weight>()), Times.Exactly(2));
_loggerMock.VerifyLog(logger => logger.LogInformation(...));
_appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
```

---

### 3. ExecuteAsync_Should_HandleMultipleWeightEntries

**Purpose**: Verify iteration and correct parameter passing

**Signature**:
```csharp
[Fact]
public async Task ExecuteAsync_Should_HandleMultipleWeightEntries()
```

**Preconditions**:
- GetWeightLogs returns WeightResponse with 3 entries with specific dates

**Test Actions**:
1. Setup GetWeightLogs with 3 weight entries
2. Call ExecuteAsync
3. Verify MapAndSaveDocument called with correct date for each entry

**Expected Outcomes**:
- MapAndSaveDocument called 3 times
- Each call receives the correct date matching the weight entry

**Mock Setups**:
```csharp
_fitbitServiceMock
    .Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(weightResponseWithThreeEntries);
```

**Verifications**:
```csharp
_weightServiceMock.Verify(x => x.MapAndSaveDocument(weight1.Date, weight1), Times.Once);
_weightServiceMock.Verify(x => x.MapAndSaveDocument(weight2.Date, weight2), Times.Once);
_weightServiceMock.Verify(x => x.MapAndSaveDocument(weight3.Date, weight3), Times.Once);
```

---

### 4. ExecuteAsync_Should_HandleEmptyWeightLogs

**Purpose**: Verify behavior when no weight data returned

**Signature**:
```csharp
[Fact]
public async Task ExecuteAsync_Should_HandleEmptyWeightLogs()
```

**Preconditions**:
- GetWeightLogs returns empty WeightResponse

**Test Actions**:
1. Setup GetWeightLogs to return empty list
2. Call ExecuteAsync

**Expected Outcomes**:
- ExecuteAsync returns 0 (success, not an error condition)
- MapAndSaveDocument never called
- StopApplication still called

**Mock Setups**:
```csharp
_fitbitServiceMock
    .Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(new WeightResponse { Weight = new List<Weight>() });
```

**Verifications**:
```csharp
result.Should().Be(0);
_weightServiceMock.Verify(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<Weight>()), Times.Never);
_appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
```

---

### 5. ExecuteAsync_Should_LogErrorAndReturnOne_WhenGetWeightLogsThrows

**Purpose**: Verify error handling when fetching data fails

**Signature**:
```csharp
[Fact]
public async Task ExecuteAsync_Should_LogErrorAndReturnOne_WhenGetWeightLogsThrows()
```

**Preconditions**:
- GetWeightLogs throws exception

**Test Actions**:
1. Setup GetWeightLogs to throw exception
2. Call ExecuteAsync

**Expected Outcomes**:
- ExecuteAsync returns 1 (failure)
- Error logged with exception message
- StopApplication still called (finally block)

**Mock Setups**:
```csharp
_fitbitServiceMock
    .Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
    .ThrowsAsync(new Exception("Test exception"));
```

**Verifications**:
```csharp
result.Should().Be(1);
_loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in {nameof(WeightWorker)}: Test exception"));
_appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
```

---

### 6. ExecuteAsync_Should_LogErrorAndReturnOne_WhenMapAndSaveDocumentThrows

**Purpose**: Verify error handling when saving data fails

**Signature**:
```csharp
[Fact]
public async Task ExecuteAsync_Should_LogErrorAndReturnOne_WhenMapAndSaveDocumentThrows()
```

**Preconditions**:
- GetWeightLogs returns valid data
- MapAndSaveDocument throws exception

**Test Actions**:
1. Setup GetWeightLogs to return data
2. Setup MapAndSaveDocument to throw exception
3. Call ExecuteAsync

**Expected Outcomes**:
- ExecuteAsync returns 1 (failure)
- Error logged
- StopApplication called

**Mock Setups**:
```csharp
_fitbitServiceMock
    .Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(weightResponseWithOneEntry);

_weightServiceMock
    .Setup(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<Weight>()))
    .ThrowsAsync(new Exception("Save failed"));
```

**Verifications**:
```csharp
result.Should().Be(1);
_loggerMock.VerifyLog(logger => logger.LogError(...));
_appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
```

## Mock Behavior Contracts

### IFitbitService Mock Contract

**Method**: `GetWeightLogs(string startDate, string endDate)`

**Expected Call Pattern**:
- Called once per ExecuteAsync execution
- Parameters: startDate = 7 days ago, endDate = today (yyyy-MM-dd format)

**Return Values**:
- Success: WeightResponse with 0+ Weight entries
- Failure: Throws exception

### IWeightService Mock Contract

**Method**: `MapAndSaveDocument(string date, Weight weight)`

**Expected Call Pattern**:
- Called once per Weight entry in response
- Parameters: date from Weight.Date, weight object

**Return Values**:
- Success: Completed Task
- Failure: Throws exception

### IHostApplicationLifetime Mock Contract

**Method**: `StopApplication()`

**Expected Call Pattern**:
- Called exactly once per ExecuteAsync execution
- Always called regardless of success/failure (finally block)

**Return Values**: void

### ILogger<WeightWorker> Mock Contract

**Expected Log Calls**:

**Information Level**:
- Message: `"WeightWorker executed at: {DateTime.Now}"`
- Called: On successful execution start

**Error Level**:
- Message: `"Exception thrown in WeightWorker: {exception.Message}"`
- Called: When exception caught

## Test Execution Contract

**Performance Requirements**:
- All 6 tests must complete in <1 second total
- Tests must be independent (no shared state)
- Tests must be deterministic (same result every run)

**Coverage Requirements**:
- Tests must achieve ≥85% line coverage for WeightWorker class
- Tests must cover all public methods
- Tests must cover success and failure paths

## No External API Contracts Modified

This feature does NOT modify:
- IFitbitService interface
- IWeightService interface
- IHostApplicationLifetime interface (from Microsoft.Extensions.Hosting)
- Any REST API endpoints
- Any database schemas

All contracts listed above are for test verification purposes only.
