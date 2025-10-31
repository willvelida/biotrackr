# Quickstart: Sleep API Test Coverage Implementation

**Feature**: 006-sleep-api-tests  
**Date**: 2025-10-31

## Prerequisites

Before starting, ensure you have:

- [ ] .NET 9.0 SDK installed
- [ ] Docker Desktop installed and running (for Cosmos DB Emulator)
- [ ] Git repository cloned with branch `006-sleep-api-tests` checked out
- [ ] Visual Studio Code or Visual Studio 2022
- [ ] Access to existing Sleep API source code at `src/Biotrackr.Sleep.Api/`

## Quick Setup (5 minutes)

### 1. Create Integration Test Project

```bash
cd src/Biotrackr.Sleep.Api
dotnet new xunit -n Biotrackr.Sleep.Api.IntegrationTests
dotnet sln add Biotrackr.Sleep.Api.IntegrationTests
```

### 2. Add Required NuGet Packages

```bash
cd Biotrackr.Sleep.Api.IntegrationTests
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.0
dotnet add package FluentAssertions --version 8.4.0
dotnet add package xunit --version 2.9.3
dotnet add package coverlet.collector --version 6.0.4
dotnet add reference ../Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.csproj
```

### 3. Start Cosmos DB Emulator

From repository root:

```bash
docker-compose -f docker-compose.cosmos.yml up -d
```

Wait ~60 seconds for emulator to initialize, then verify:

```bash
curl -k https://localhost:8081/_explorer/index.html
```

### 4. Run Existing Unit Tests with Coverage

```bash
cd src/Biotrackr.Sleep.Api
dotnet test --collect:"XPlat Code Coverage" --results-directory ../../TestResults
```

Check coverage report at `TestResults/.../coverage.cobertura.xml`

---

## Development Workflow

### Running Tests Locally

**All tests**:
```bash
dotnet test src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.sln
```

**Unit tests only**:
```bash
dotnet test src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests
```

**Contract tests only**:
```bash
dotnet test src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests --filter "FullyQualifiedName~Contract"
```

**E2E tests only** (requires Cosmos DB Emulator):
```bash
dotnet test src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests --filter "FullyQualifiedName~E2E"
```

**With coverage**:
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

---

## Project Structure Creation

### Step 1: Create Integration Test Directories

```bash
cd src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests
mkdir Contract E2E Fixtures WebApplicationFactories
```

### Step 2: Create Fixture Classes

Create `Fixtures/ContractTestFixture.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.Fixtures;

public class ContractTestFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>();
        Client = Factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        await Factory?.DisposeAsync()!;
    }
}
```

Create `Fixtures/IntegrationTestFixture.cs` (see data-model.md for full implementation)

### Step 3: Create Test Collections

Create `Contract/ContractTestCollection.cs`:

```csharp
using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.Contract;

[CollectionDefinition("Contract Tests")]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
}
```

Create `E2E/E2ETestCollection.cs`:

```csharp
using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.E2E;

[CollectionDefinition("E2E Tests")]
public class E2ETestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
```

### Step 4: Add Program.cs Attribute

In `Biotrackr.Sleep.Api/Program.cs`, add at the top:

```csharp
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        // existing code...
    }
}
```

---

## Writing Your First Tests

### Contract Test Example

Create `Contract/ProgramStartupTests.cs`:

```csharp
using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.Contract;

[Collection("Contract Tests")]
public class ProgramStartupTests
{
    private readonly ContractTestFixture _fixture;

    public ProgramStartupTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Application_Should_StartSuccessfully()
    {
        // Assert
        _fixture.Factory.Should().NotBeNull();
        _fixture.Client.Should().NotBeNull();
    }

    [Fact]
    public void Services_Should_BeRegistered()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Assert
        services.GetService<ICosmosRepository>().Should().NotBeNull();
    }
}
```

### E2E Test Example

Create `E2E/HealthCheckTests.cs`:

```csharp
using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.E2E;

[Collection("E2E Tests")]
public class HealthCheckTests
{
    private readonly IntegrationTestFixture _fixture;

    public HealthCheckTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Liveness_Check_Should_ReturnHealthy()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/healthz/liveness");

        // Assert
        response.Should().Be200Ok();
    }
}
```

---

## Common Tasks

### Task: Analyze Current Coverage

1. Run tests with coverage:
   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```

2. View report (install reportgenerator):
   ```bash
   dotnet tool install -g dotnet-reportgenerator-globaltool
   reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:TestResults/html -reporttypes:Html
   ```

3. Open `TestResults/html/index.html` in browser

### Task: Add Unit Test for Uncovered Code

1. Identify uncovered method/class from coverage report
2. Create test class with naming: `{ClassName}Should.cs`
3. Write test methods with naming: `{MethodName}_Should_{ExpectedBehavior}_When_{Condition}`
4. Use FluentAssertions for readable assertions
5. Use Moq for mocking dependencies

Example:

```csharp
[Fact]
public void Constructor_Should_InitializeProperties_When_ValidValuesProvided()
{
    // Arrange
    var settings = new Settings
    {
        DatabaseName = "TestDb",
        ContainerName = "TestContainer"
    };

    // Assert
    settings.DatabaseName.Should().Be("TestDb");
    settings.ContainerName.Should().Be("TestContainer");
}
```

### Task: Add E2E Test for Endpoint

1. Create test class in `E2E/` namespace
2. Add `[Collection("E2E Tests")]` attribute
3. Inject `IntegrationTestFixture` in constructor
4. Add `ClearContainerAsync()` helper method
5. Call cleanup in test setup/arrange phase
6. Seed test data as needed
7. Call API endpoint via `_fixture.Client`
8. Assert response status and content

Example:

```csharp
[Fact]
public async Task GetSleepByDate_Should_ReturnNotFound_When_NoDataExists()
{
    // Arrange
    await ClearContainerAsync();
    var date = "2025-10-31";

    // Act
    var response = await _fixture.Client.GetAsync($"/{date}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

---

## Troubleshooting

### Issue: Tests Can't Connect to Cosmos DB Emulator

**Symptoms**: `TransportException: SSL negotiation failed` or connection timeout

**Solution**: 
1. Verify emulator is running: `docker ps | grep cosmos`
2. Check Gateway connection mode is set in `SleepApiWebApplicationFactory`
3. Verify certificate validation callback is disabled

### Issue: E2E Tests Find Extra Documents

**Symptoms**: `Expected 1 item but found 3`

**Solution**:
1. Ensure `ClearContainerAsync()` is called in every E2E test
2. Verify cleanup method queries all documents and partition keys correctly

### Issue: Coverage Reports Show Program.cs

**Symptoms**: Coverage percentage lower than expected, Program.cs visible in reports

**Solution**:
1. Add `[ExcludeFromCodeCoverage]` attribute to Program class
2. Remove any `<ExcludeByFile>` entries from .csproj
3. Remove `coverlet.msbuild` package if present

### Issue: Test Isolation Failures

**Symptoms**: Tests pass individually but fail when run together

**Solution**:
1. Ensure each test clears container at start
2. Avoid shared static/mutable state
3. Verify fixtures are collection-scoped, not global

---

## Performance Optimization Tips

1. **Parallel Execution**: Run contract tests parallel with unit tests
2. **Test Ordering**: Fast tests first (unit > contract > E2E)
3. **Shared Fixtures**: Reuse expensive resources (app factory, database)
4. **Selective Cleanup**: Only clear container, don't recreate database/container
5. **Test Filters**: Run targeted test subsets during development

---

## CI/CD Integration

### GitHub Actions Workflow

The feature includes workflow updates for:
- Unit test execution (parallel with contract tests)
- Contract test execution (fast, no Cosmos DB)
- E2E test execution (with Cosmos DB Emulator)
- Coverage report generation and upload
- Test result reporting via dorny/test-reporter

See `.github/workflows/deploy-sleep-api.yml` for full configuration.

---

## Next Steps

After completing initial setup:

1. [ ] Analyze current coverage gaps
2. [ ] Add missing unit tests for models
3. [ ] Create Contract test suite (3-5 tests)
4. [ ] Create E2E test suite (6-10 tests)
5. [ ] Verify ≥80% coverage achieved
6. [ ] Update GitHub Actions workflow
7. [ ] Create pull request

---

## References

- **Decision Records**: `docs/decision-records/2025-10-28-*.md`
- **Common Resolutions**: `.specify/memory/common-resolutions.md`
- **Weight API Tests**: `specs/001-weight-api-tests/` (reference implementation)
- **Activity API Tests**: `specs/004-activity-api-tests/` (reference implementation)
- **Test Contracts**: `specs/006-sleep-api-tests/contracts/test-contracts.md`
- **Data Model**: `specs/006-sleep-api-tests/data-model.md`

---

## Estimated Time

| Task | Estimated Duration |
|------|-------------------|
| Project setup | 15 minutes |
| Coverage analysis | 30 minutes |
| Unit test expansion | 2-4 hours |
| Contract tests | 1-2 hours |
| E2E tests | 2-3 hours |
| CI/CD integration | 1 hour |
| **Total** | **7-11 hours** |
