# Quickstart: Weight Service Unit Test Coverage Improvement

**Feature**: 002-weight-svc-coverage  
**Date**: 2025-10-28  
**Branch**: `002-weight-svc-coverage`

## Overview

This guide provides step-by-step instructions for implementing unit tests to improve Biotrackr.Weight.Svc coverage from 41.36% to 70%+.

## Prerequisites

- .NET 9.0 SDK installed
- Visual Studio Code or Visual Studio 2022
- Git configured
- Repository cloned locally

## Quick Start (5 minutes)

### 1. Verify Current State

```powershell
# Navigate to project
cd c:\Users\velidawill\Documents\OpenSource\biotrackr\src\Biotrackr.Weight.Svc

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Check current coverage (should be ~41%)
$coverageFile = Get-ChildItem -Recurse -Filter "coverage.cobertura.xml" | Select-Object -First 1 -ExpandProperty FullName
Select-String -Path $coverageFile -Pattern "line-rate" -Context 0,0 | Select-Object -First 1
```

### 2. Create Test File Structure

```powershell
# Create WorkerTests directory
New-Item -Path ".\Biotrackr.Weight.Svc.UnitTests\WorkerTests" -ItemType Directory -Force

# Create test file
New-Item -Path ".\Biotrackr.Weight.Svc.UnitTests\WorkerTests\WeightWorkerShould.cs" -ItemType File
```

### 3. Implement Tests

Copy the test implementation from `contracts/test-contracts.md` or implement the 6 test methods:

1. `Constructor_Should_InitializeAllDependencies`
2. `ExecuteAsync_Should_FetchAndSaveWeightLogs_Successfully`
3. `ExecuteAsync_Should_HandleMultipleWeightEntries`
4. `ExecuteAsync_Should_HandleEmptyWeightLogs`
5. `ExecuteAsync_Should_LogErrorAndReturnOne_WhenGetWeightLogsThrows`
6. `ExecuteAsync_Should_LogErrorAndReturnOne_WhenMapAndSaveDocumentThrows`

### 4. Exclude Program.cs from Coverage

Add to `Biotrackr.Weight.Svc/Program.cs` at the top:

```csharp
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
```

Or modify the implicit Program class at the bottom of the file.

### 5. Verify Coverage

```powershell
# Run tests again
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Check new coverage (should be ≥70%)
$coverageFile = Get-ChildItem -Recurse -Filter "coverage.cobertura.xml" | Select-Object -First 1 -ExpandProperty FullName
Select-String -Path $coverageFile -Pattern "line-rate" -Context 0,0 | Select-Object -First 1
```

### 6. Commit Changes

```powershell
cd c:\Users\velidawill\Documents\OpenSource\biotrackr
git add .
git commit -m "feat: add WeightWorker unit tests to achieve 70% coverage"
git push origin 002-weight-svc-coverage
```

## Detailed Implementation Guide

### Test File Template

```csharp
using AutoFixture;
using Biotrackr.Weight.Svc.Models.Entities;
using Biotrackr.Weight.Svc.Services.Interfaces;
using Biotrackr.Weight.Svc.Workers;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Biotrackr.Weight.Svc.UnitTests.WorkerTests
{
    public class WeightWorkerShould
    {
        private readonly Mock<IFitbitService> _fitbitServiceMock;
        private readonly Mock<IWeightService> _weightServiceMock;
        private readonly Mock<ILogger<WeightWorker>> _loggerMock;
        private readonly Mock<IHostApplicationLifetime> _appLifetimeMock;

        public WeightWorkerShould()
        {
            _fitbitServiceMock = new Mock<IFitbitService>();
            _weightServiceMock = new Mock<IWeightService>();
            _loggerMock = new Mock<ILogger<WeightWorker>>();
            _appLifetimeMock = new Mock<IHostApplicationLifetime>();
        }

        // Add test methods here
    }
}
```

### Test Method Pattern

```csharp
[Fact]
public async Task ExecuteAsync_Should_FetchAndSaveWeightLogs_Successfully()
{
    // Arrange
    var fixture = new Fixture();
    var weight1 = fixture.Create<Weight>();
    var weight2 = fixture.Create<Weight>();
    
    var weightResponse = new WeightResponse
    {
        Weight = new List<Weight> { weight1, weight2 }
    };

    _fitbitServiceMock
        .Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(weightResponse);

    _weightServiceMock
        .Setup(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<Weight>()))
        .Returns(Task.CompletedTask);

    var worker = new WeightWorker(
        _fitbitServiceMock.Object,
        _weightServiceMock.Object,
        _loggerMock.Object,
        _appLifetimeMock.Object
    );

    // Act
    var result = await worker.ExecuteAsync(CancellationToken.None);

    // Assert
    result.Should().Be(0);
    _fitbitServiceMock.Verify(
        x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()), 
        Times.Once
    );
    _weightServiceMock.Verify(
        x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<Weight>()), 
        Times.Exactly(2)
    );
    _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
}
```

## Verification Checklist

After implementation, verify:

- [ ] All 6 test methods implemented
- [ ] All tests pass (green)
- [ ] Coverage ≥70% for overall project
- [ ] WeightWorker coverage ≥85%
- [ ] Tests execute in <1 second
- [ ] Program.cs excluded from coverage
- [ ] No compilation warnings
- [ ] Following existing test naming conventions
- [ ] Using Moq for all mocks
- [ ] Using FluentAssertions for assertions
- [ ] Using AutoFixture for test data where appropriate

## Troubleshooting

### Issue: ExecuteAsync is protected

**Solution**: Call it directly - protected methods are accessible in tests:
```csharp
var result = await worker.ExecuteAsync(CancellationToken.None);
```

### Issue: Logger verification fails

**Solution**: Use the custom VerifyLog extension method:
```csharp
_loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in WeightWorker: {message}"));
```

If VerifyLog extension doesn't exist, verify log was called with:
```csharp
_loggerMock.Verify(
    x => x.Log(
        LogLevel.Error,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Exception thrown")),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
    Times.Once
);
```

### Issue: Coverage still below 70%

**Solution**: 
1. Verify Program.cs is excluded
2. Check that all 6 tests are running
3. Ensure tests are actually executing ExecuteAsync logic
4. Review coverage report for uncovered lines

### Issue: Tests take too long

**Solution**:
- Remove Task.Delay if present
- Use `Returns(Task.CompletedTask)` not `ReturnsAsync(null)`
- Avoid actual I/O operations
- Ensure all dependencies are mocked

## Commands Reference

```powershell
# Build
dotnet build

# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~WeightWorkerShould"

# Run single test method
dotnet test --filter "FullyQualifiedName~ExecuteAsync_Should_FetchAndSaveWeightLogs_Successfully"

# Watch mode (auto-run on changes)
dotnet watch test
```

## Next Steps

After completing this feature:

1. Create pull request from `002-weight-svc-coverage` branch
2. Review coverage report in PR
3. Address any code review feedback
4. Merge to main branch
5. Consider similar coverage improvements for other services

## Success Criteria

✅ Coverage: 41.36% → ≥70%  
✅ WeightWorker: 0% → ≥85%  
✅ All tests pass: 100% pass rate  
✅ Test execution: <1 second  
✅ No breaking changes to production code

## Resources

- [spec.md](spec.md) - Full feature specification
- [research.md](research.md) - Research findings and decisions
- [data-model.md](data-model.md) - Data model reference
- [contracts/test-contracts.md](contracts/test-contracts.md) - Detailed test contracts
- [plan.md](plan.md) - Implementation plan

## Support

For questions or issues:
1. Review existing tests in `ServiceTests/` and `RepositoryTests/` for patterns
2. Check [research.md](research.md) for detailed technical decisions
3. Refer to [contracts/test-contracts.md](contracts/test-contracts.md) for test specifications
