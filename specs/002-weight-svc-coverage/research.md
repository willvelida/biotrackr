# Research: Weight Service Unit Test Coverage Improvement

**Feature**: 002-weight-svc-coverage  
**Date**: 2025-10-28  
**Status**: Complete

## Overview

This document captures research findings for improving unit test coverage in Biotrackr.Weight.Svc from 41.36% to 70%+. Primary focus is creating comprehensive tests for WeightWorker (currently 0% coverage).

## Research Areas

### 1. Current Coverage Analysis

**Decision**: Focus testing efforts on WeightWorker class, exclude Program.cs from coverage metrics

**Rationale**:
- Current overall coverage: 41.36% (79/191 lines covered)
- WeightWorker: 0% coverage (largest gap)
- Program.cs: 0% coverage (application entry point)
- Services/Repositories: Already 100% coverage
- Models: Already 100% coverage

**Coverage Report Analysis**:
```
WeightWorker           - 0% (line-rate="0")
Program                - 0% (line-rate="0")
FitbitService         - 100% (line-rate="1")
WeightService         - 100% (line-rate="1")
CosmosRepository      - 100% (line-rate="1")
Models                - 100% (line-rate="1")
```

**Alternatives Considered**:
- Testing Program.cs: Rejected - Entry points are better suited for integration tests; standard practice to exclude from unit test coverage
- Rewriting existing tests: Rejected - Existing tests are comprehensive and follow good patterns

### 2. Testing BackgroundService in .NET

**Decision**: Use standard .NET testing patterns for BackgroundService with mock dependencies

**Rationale**:
- BackgroundService.ExecuteAsync is protected, call it directly in tests
- Mock all dependencies (IFitbitService, IWeightService, ILogger, IHostApplicationLifetime)
- Test return values (0 for success, 1 for failure) and side effects
- Verify finally block behavior (StopApplication call)

**Best Practices**:
```csharp
// Test structure
public class WeightWorkerShould
{
    private readonly Mock<IFitbitService> _fitbitServiceMock;
    private readonly Mock<IWeightService> _weightServiceMock;
    private readonly Mock<ILogger<WeightWorker>> _loggerMock;
    private readonly Mock<IHostApplicationLifetime> _appLifetimeMock;
    
    // Test protected ExecuteAsync directly
    var result = await worker.ExecuteAsync(CancellationToken.None);
    
    // Verify orchestration
    _fitbitServiceMock.Verify(x => x.GetWeightLogs(...), Times.Once);
    _weightServiceMock.Verify(x => x.MapAndSaveDocument(...), Times.Exactly(n));
    
    // Verify lifecycle
    _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
}
```

**Alternatives Considered**:
- Using BackgroundService.StartAsync: Rejected - ExecuteAsync is the method containing business logic
- Integration testing: Rejected - Unit tests provide faster feedback and better isolation

### 3. Mocking IHostApplicationLifetime

**Decision**: Mock IHostApplicationLifetime to verify StopApplication is called

**Rationale**:
- IHostApplicationLifetime is an interface, easily mockable with Moq
- Verify StopApplication() called exactly once in finally block
- Ensures worker properly terminates application after completion

**Implementation**:
```csharp
_appLifetimeMock = new Mock<IHostApplicationLifetime>();
_appLifetimeMock.Setup(x => x.StopApplication()).Verifiable();

// After test execution
_appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
```

**Alternatives Considered**:
- Not testing StopApplication: Rejected - Critical for proper application lifecycle management

### 4. Testing Async Foreach Loop

**Decision**: Mock GetWeightLogs to return collection, verify MapAndSaveDocument called for each item

**Rationale**:
- WeightWorker iterates over weightResponse.Weight collection
- Need to verify all items processed
- Use Times.Exactly(n) where n is count of items in mock response

**Implementation**:
```csharp
var weightResponse = new WeightResponse 
{ 
    Weight = new List<Weight> { weight1, weight2, weight3 } 
};

_fitbitServiceMock.Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(weightResponse);

// Verify called for each item
_weightServiceMock.Verify(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<Weight>()), 
    Times.Exactly(3));
```

**Alternatives Considered**:
- Only testing single item: Rejected - Doesn't verify iteration logic
- Testing with empty collection: Included as additional edge case test

### 5. Coverlet Configuration for Exclusions

**Decision**: Configure coverlet to exclude Program.cs via .csproj attributes or coverlet.runsettings

**Rationale**:
- Industry standard to exclude entry points from coverage
- Program.cs contains only DI wiring, better tested via integration tests
- Exclusion improves signal-to-noise ratio in coverage reports

**Implementation Options**:

Option A - ExcludeFromCodeCoverage attribute (preferred):
```csharp
// In Program.cs
[ExcludeFromCodeCoverage]
internal class Program { }
```

Option B - Coverlet configuration in .csproj:
```xml
<PropertyGroup>
    <ExcludeByFile>**/Program.cs</ExcludeByFile>
</PropertyGroup>
```

Option C - coverlet.runsettings file:
```xml
<Exclude>
    [*]Program
</Exclude>
```

**Decision**: Use Option A ([ExcludeFromCodeCoverage] attribute) for simplicity and explicitness

**Alternatives Considered**:
- Testing Program.cs: Rejected - Entry point testing is integration test concern
- Including in coverage: Rejected - Creates false coverage deficit

### 6. Test Scenarios to Cover

**Decision**: Implement 6 core test scenarios for WeightWorker

**Test Scenarios**:

1. **Constructor_Should_InitializeAllDependencies**
   - Verifies proper dependency injection
   - Ensures no null references

2. **ExecuteAsync_Should_FetchAndSaveWeightLogs_Successfully**
   - Happy path: GetWeightLogs returns data
   - MapAndSaveDocument called for each weight entry
   - Returns exit code 0
   - Logs information message
   - Calls StopApplication

3. **ExecuteAsync_Should_HandleMultipleWeightEntries**
   - Tests iteration over collection
   - Verifies correct date parameter passed to MapAndSaveDocument
   - Verifies all entries processed

4. **ExecuteAsync_Should_HandleEmptyWeightLogs**
   - GetWeightLogs returns empty collection
   - MapAndSaveDocument never called
   - Still returns exit code 0
   - Still calls StopApplication

5. **ExecuteAsync_Should_LogErrorAndReturnOne_WhenExceptionThrown**
   - GetWeightLogs throws exception
   - Error logged with exception message
   - Returns exit code 1
   - StopApplication still called (finally block)

6. **ExecuteAsync_Should_LogErrorAndReturnOne_WhenSaveFailsForWeight**
   - MapAndSaveDocument throws exception
   - Error logged
   - Returns exit code 1
   - StopApplication called

**Rationale**: These scenarios cover constructor, success path, iteration logic, empty response, and error handling, achieving comprehensive coverage of WeightWorker logic.

**Alternatives Considered**:
- Testing cancellation token: Deferred - Not critical for initial 70% coverage goal
- Testing date range calculation: Deferred - Date formatting is straightforward, lower priority

## Technology Stack Confirmation

- **.NET 9.0**: Confirmed from existing csproj files
- **xUnit 2.9.3**: Confirmed from existing test projects
- **Moq 4.20.72**: Confirmed, used extensively in existing tests
- **FluentAssertions 8.4.0**: Confirmed, used for readable assertions
- **AutoFixture 4.18.1**: Confirmed, used for test data generation
- **coverlet.collector 6.0.4**: Confirmed for coverage collection

## Implementation Notes

### Existing Test Patterns to Follow

1. **Test Class Structure**:
   - Name: `{ClassName}Should`
   - Constructor initializes all mocks
   - Each test method follows: `MethodName_Should_Behavior_When_Condition`

2. **Mocking Pattern**:
   ```csharp
   _mockDependency = new Mock<IDependency>();
   _mockDependency.Setup(x => x.Method(...)).ReturnsAsync(result);
   ```

3. **Assertion Style**:
   ```csharp
   await action.Should().NotThrowAsync();
   result.Should().Be(expectedValue);
   _mock.Verify(x => x.Method(...), Times.Once);
   ```

4. **Logger Verification**:
   ```csharp
   _loggerMock.VerifyLog(logger => logger.LogError($"Message: {details}"));
   ```

### Files to Create

1. `src/Biotrackr.Weight.Svc/Biotrackr.Weight.Svc.UnitTests/WorkerTests/WeightWorkerShould.cs`
   - New test class with 6 test methods
   - ~150-200 lines of code

### Files to Modify

1. `src/Biotrackr.Weight.Svc/Biotrackr.Weight.Svc/Program.cs`
   - Add `[ExcludeFromCodeCoverage]` attribute
   - Add using statement for System.Diagnostics.CodeAnalysis

## Success Metrics

- Overall coverage: 41.36% → ≥70%
- WeightWorker coverage: 0% → ≥85%
- Test execution time: <1 second for new tests
- All tests pass: 100% pass rate maintained

## References

- [Microsoft Docs: Unit testing BackgroundService](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers)
- [Moq Documentation](https://github.com/moq/moq4)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [xUnit Best Practices](https://xunit.net/docs/getting-started/netcore/cmdline)

## Conclusion

Research confirms the approach is straightforward: create comprehensive unit tests for WeightWorker using existing test patterns and exclude Program.cs from coverage. This will efficiently achieve the 70% coverage target without unnecessary complexity.
