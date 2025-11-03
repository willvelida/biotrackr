# Quickstart: Food Service Test Coverage and Integration Tests

**Feature**: 009-food-svc-tests  
**Date**: November 3, 2025  
**Status**: Complete

## Overview

This guide provides step-by-step instructions for running tests for the Food Service, including unit tests, contract tests, and end-to-end (E2E) tests with Cosmos DB Emulator.

---

## Prerequisites

### Required Software

- **.NET 9.0 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker Desktop**: [Download](https://www.docker.com/products/docker-desktop) (for Cosmos DB Emulator)
- **PowerShell 7+**: [Download](https://github.com/PowerShell/PowerShell/releases) (Windows, macOS, Linux)
- **Git**: [Download](https://git-scm.com/downloads)

### Verify Installation

```powershell
# Check .NET version
dotnet --version  # Should show 9.0.x

# Check Docker
docker --version

# Check PowerShell
$PSVersionTable.PSVersion  # Should show 7.x or higher
```

---

## Quick Start (All Tests)

### 1. Clone Repository

```powershell
git clone https://github.com/willvelida/biotrackr.git
cd biotrackr
git checkout 009-food-svc-tests
```

### 2. Start Cosmos DB Emulator

```powershell
# From repository root
.\cosmos-emulator.ps1

# Wait for "Cosmos DB Emulator is ready" message (takes 1-2 minutes)
```

### 3. Run All Tests

```powershell
# Navigate to Food Service directory
cd src/Biotrackr.Food.Svc

# Run unit tests
dotnet test Biotrackr.Food.Svc.UnitTests/

# Run contract tests (fast, no DB required)
dotnet test Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~Contract"

# Run E2E tests (requires Cosmos DB Emulator)
dotnet test Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~E2E"
```

### 4. View Coverage Report

```powershell
# Run tests with coverage
dotnet test Biotrackr.Food.Svc.UnitTests/ --collect:"XPlat Code Coverage"

# Coverage report location: TestResults/{guid}/coverage.cobertura.xml
# View in VS Code with Coverage Gutters extension or upload to codecov.io
```

---

## Unit Tests Only

Unit tests are fast and require no external dependencies.

```powershell
cd src/Biotrackr.Food.Svc

# Run all unit tests
dotnet test Biotrackr.Food.Svc.UnitTests/

# Run specific test class
dotnet test Biotrackr.Food.Svc.UnitTests/ --filter "FullyQualifiedName~FoodWorkerShould"

# Run tests with detailed output
dotnet test Biotrackr.Food.Svc.UnitTests/ --logger "console;verbosity=detailed"

# Run tests with coverage
dotnet test Biotrackr.Food.Svc.UnitTests/ --collect:"XPlat Code Coverage" /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

**Expected Results**:
- ✅ All tests pass (100% success rate)
- ⏱️ Total duration: <5 seconds
- 📊 Coverage: ≥70% (excluding Program.cs)

---

## Contract Tests Only

Contract tests verify dependency injection and service registration without external dependencies.

```powershell
cd src/Biotrackr.Food.Svc

# Run contract tests only
dotnet test Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~Contract"

# Run specific contract test
dotnet test Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~ServiceRegistrationTests"

# Run with detailed output
dotnet test Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~Contract" --logger "console;verbosity=detailed"
```

**Expected Results**:
- ✅ All contract tests pass
- ⏱️ Total duration: <5 seconds
- 🚫 No external dependencies required

**Common Tests**:
- `ProgramStartupTests`: Validates application can build host
- `ServiceRegistrationTests`: Validates service lifetimes (Singleton, Scoped, Transient)

---

## E2E Tests Only

E2E tests require Cosmos DB Emulator and validate full workflows.

### Step 1: Start Cosmos DB Emulator

```powershell
# From repository root
.\cosmos-emulator.ps1

# Verify emulator is running
docker ps | Select-String "cosmosdb"
```

**Expected Output**:
```
CONTAINER ID   IMAGE                                                    STATUS
abc123def456   mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator   Up 2 minutes
```

### Step 2: Run E2E Tests

```powershell
cd src/Biotrackr.Food.Svc

# Run all E2E tests
dotnet test Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~E2E"

# Run specific E2E test class
dotnet test Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~FoodServiceTests&FullyQualifiedName~E2E"

# Run with detailed output
dotnet test Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~E2E" --logger "console;verbosity=detailed"
```

**Expected Results**:
- ✅ All E2E tests pass
- ⏱️ Total duration: <30 seconds
- 🗄️ Cosmos DB Emulator required

**Common Tests**:
- `CosmosRepositoryTests`: Validates document CRUD operations
- `FoodServiceTests`: Validates service orchestration with DB
- `FoodWorkerTests`: Validates complete worker workflow

### Step 3: Stop Cosmos DB Emulator

```powershell
docker stop $(docker ps -q --filter ancestor=mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator)
```

---

## Running Tests in Visual Studio

### Unit Tests

1. Open `Biotrackr.Food.Svc.sln` in Visual Studio
2. Open **Test Explorer** (Test → Test Explorer)
3. Click **Run All** or right-click specific test

### Contract Tests

1. In Test Explorer, use filter: `FullyQualifiedName~Contract`
2. Click **Run All** (filtered)

### E2E Tests

1. Start Cosmos DB Emulator: `.\cosmos-emulator.ps1`
2. In Test Explorer, use filter: `FullyQualifiedName~E2E`
3. Click **Run All** (filtered)

---

## Running Tests in VS Code

### Prerequisites

Install extensions:
- C# Dev Kit
- .NET Core Test Explorer

### Unit Tests

1. Open command palette (Ctrl+Shift+P)
2. Run: ".NET: Run All Tests"
3. Or click ▶️ next to test methods in editor

### Contract/E2E Tests

1. Open Testing view (Ctrl+Shift+T)
2. Filter tests by name (e.g., "Contract" or "E2E")
3. Click ▶️ next to filtered tests

---

## Troubleshooting

### Issue: Cosmos DB Emulator won't start

**Symptoms**: `docker: Error response from daemon: Ports are not available`

**Solution**:
```powershell
# Check if port 8081 is in use
netstat -ano | findstr :8081

# Stop any process using port 8081
# Or use different port in docker-compose.cosmos.yml
```

---

### Issue: E2E tests fail with "SSL negotiation failed"

**Symptoms**: `Microsoft.Azure.Documents.GoneException: SSL negotiation failed`

**Solution**: Verify IntegrationTestFixture uses `ConnectionMode.Gateway`:
```csharp
new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway, // Required for emulator
    // ...
}
```

---

### Issue: E2E tests find more documents than expected

**Symptoms**: `Expected documents to contain 1 item(s), but found 3`

**Solution**: Ensure `ClearContainerAsync()` is called at start of each test:
```csharp
[Fact]
public async Task MyTest()
{
    // Arrange - Clear container for test isolation
    await ClearContainerAsync();
    
    // Act & Assert
    // ...
}
```

---

### Issue: Coverage shows <70% including Program.cs

**Symptoms**: Coverage report includes Program.cs in metrics

**Solution**: Verify Program.cs has `[ExcludeFromCodeCoverage]` attribute:
```csharp
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        // ...
    }
}
```

---

### Issue: Tests fail with "RuntimeBinderException"

**Symptoms**: `'Newtonsoft.Json.Linq.JObject' does not contain a definition for 'Should'`

**Solution**: Use strongly-typed models instead of `dynamic`:
```csharp
// ❌ Wrong
var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);
savedDoc.id.Should().Be(expected); // RuntimeBinderException!

// ✅ Correct
var iterator = _fixture.Container.GetItemQueryIterator<FoodDocument>(query);
savedDoc.Id.Should().Be(expected); // Works!
```

---

## CI/CD Workflow

Tests run automatically in GitHub Actions when changes are pushed to the `009-food-svc-tests` branch.

### View Workflow Results

1. Go to [Actions tab](https://github.com/willvelida/biotrackr/actions)
2. Click on "Deploy Food Service" workflow
3. View test results for each job:
   - ✅ Run Unit Tests
   - ✅ Run Contract Tests
   - ✅ Run E2E Tests

### Workflow Execution Order

```
├─ Unit Tests (parallel)
├─ Contract Tests (parallel with unit tests)
└─ E2E Tests (after contract tests) → Build Container → Deploy
```

### Test Filters Used

| Job | Filter | Duration |
|-----|--------|----------|
| Unit Tests | None (all tests in UnitTests project) | <5s |
| Contract Tests | `FullyQualifiedName~Contract` | <5s |
| E2E Tests | `FullyQualifiedName~E2E` | <30s |

---

## Performance Expectations

| Test Type | Count | Duration | Dependencies |
|-----------|-------|----------|--------------|
| Unit Tests | ~30-40 | <5s | None |
| Contract Tests | ~7-10 | <5s | None |
| E2E Tests | ~10-15 | <30s | Cosmos DB Emulator |
| **Total** | **~50-65** | **<40s** | Docker |

---

## Next Steps

### Local Development

1. Make changes to Food Service code
2. Run unit tests: `dotnet test Biotrackr.Food.Svc.UnitTests/`
3. Run contract tests: `dotnet test Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~Contract"`
4. If DB changes, run E2E tests: `dotnet test Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~E2E"`
5. Check coverage: `dotnet test --collect:"XPlat Code Coverage"`
6. Commit and push changes

### Adding New Tests

1. **Unit Test**: Add to `Biotrackr.Food.Svc.UnitTests/{Component}Tests/`
2. **Contract Test**: Add to `Biotrackr.Food.Svc.IntegrationTests/Contract/`
3. **E2E Test**: Add to `Biotrackr.Food.Svc.IntegrationTests/E2E/`

Follow naming convention: `{MethodName}_Should_{ExpectedBehavior}`

---

## Useful Commands Reference

```powershell
# Start Cosmos DB Emulator
.\cosmos-emulator.ps1

# Stop Cosmos DB Emulator
docker stop $(docker ps -q --filter ancestor=mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator)

# Run all tests
dotnet test src/Biotrackr.Food.Svc/

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~FoodWorkerShould"

# Run tests by trait (if defined)
dotnet test --filter "Category=Unit"

# List all tests without running
dotnet test --list-tests

# Build solution
dotnet build src/Biotrackr.Food.Svc/

# Clean solution
dotnet clean src/Biotrackr.Food.Svc/
```

---

## References

- [.NET Testing Documentation](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [xUnit Documentation](https://xunit.net/)
- [Cosmos DB Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)

---

## Support

For issues or questions:
1. Check common resolutions: `.specify/memory/common-resolutions.md`
2. Review decision records: `docs/decision-records/`
3. Open GitHub issue: [New Issue](https://github.com/willvelida/biotrackr/issues/new)
