# Quickstart Guide: Enhanced Test Coverage for Activity API

**Target Audience**: Developers implementing the enhanced testing strategy  
**Prerequisites**: .NET 9.0 SDK, Azure CLI, Visual Studio Code or similar IDE  
**Estimated Time**: 30-45 minutes for initial setup

## Overview

This guide walks you through implementing the enhanced test coverage for the Biotrackr Activity API, including:
- Extending unit tests to ≥80% coverage
- Creating a new integration test project with Contract and E2E tests
- Following proven patterns from Weight.Api implementation
- Setting up GitHub Actions workflows for automated testing

## Phase 1: Extend Unit Test Coverage (Priority P1)

### Step 1: Analyze Current Coverage

1. **Run existing tests with coverage**:
   ```bash
   cd src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.UnitTests
   dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
   ```

2. **Generate coverage report**:
   ```bash
   dotnet tool install -g dotnet-reportgenerator-globaltool
   reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./coveragereport" -reporttypes:"Html;TextSummary"
   ```

3. **Open coverage report** in browser: `./coveragereport/index.html`
4. **Review Summary.txt** for quick overview: `cat ./coveragereport/Summary.txt`

### Step 2: Identify Coverage Gaps

Current coverage gaps to address based on existing code:

1. **Configuration/Settings.cs** - Add comprehensive property tests
2. **Extensions/EndpointRouteBuilderExtensions.cs** - Test endpoint registration logic
3. **Models/FitbitEntities/** - Add tests for Activity, ActivityResponse, Distance, Goals, HeartRateZone, Summary
4. **Error handling paths** in ActivityHandlers
5. **Edge cases** in pagination (null tokens, negative values, corruption)
6. **Null/missing data scenarios** for Fitbit entities

### Step 3: Add Missing Unit Tests

#### 1. Configuration Tests

```bash
mkdir -p src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.UnitTests/ConfigurationTests
```

Create `SettingsShould.cs`:
```csharp
using FluentAssertions;
using Biotrackr.Activity.Api.Configuration;
using Xunit;

namespace Biotrackr.Activity.Api.UnitTests.ConfigurationTests;

public class SettingsShould
{
    [Fact]
    public void HaveValidDatabaseNameProperty()
    {
        // Arrange
        var settings = new Settings { DatabaseName = "test-db" };
        
        // Act & Assert
        settings.DatabaseName.Should().Be("test-db");
    }
    
    [Fact]
    public void HaveValidContainerNameProperty()
    {
        // Arrange
        var settings = new Settings { ContainerName = "test-container" };
        
        // Act & Assert
        settings.ContainerName.Should().Be("test-container");
    }
}
```

#### 2. Extension Tests

```bash
mkdir -p src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.UnitTests/ExtensionTests
```

Create `EndpointRouteBuilderExtensionsShould.cs`:
```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Biotrackr.Activity.Api.Extensions;
using Xunit;

namespace Biotrackr.Activity.Api.UnitTests.ExtensionTests;

public class EndpointRouteBuilderExtensionsShould
{
    [Fact]
    public void RegisterActivityEndpoints_ShouldNotThrow()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        
        // Act
        Action act = () => app.RegisterActivityEndpoints();
        
        // Assert
        act.Should().NotThrow();
    }
}
```

#### 3. Fitbit Entity Model Tests

```bash
mkdir -p src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.UnitTests/ModelTests/FitbitEntityTests
```

Create comprehensive tests for each Fitbit entity (Activity, ActivityResponse, Distance, Goals, HeartRateZone, Summary).

Example: `ActivityShould.cs`:
```csharp
using AutoFixture;
using FluentAssertions;
using Biotrackr.Activity.Api.Models.FitbitEntities;
using Xunit;

namespace Biotrackr.Activity.Api.UnitTests.ModelTests.FitbitEntityTests;

public class ActivityShould
{
    private readonly IFixture _fixture;
    
    public ActivityShould()
    {
        _fixture = new Fixture();
    }
    
    [Fact]
    public void BeConstructableWithValidProperties()
    {
        // Arrange & Act
        var activity = _fixture.Create<Activity>();
        
        // Assert
        activity.Should().NotBeNull();
        activity.activityId.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public void HandleNullDistanceGracefully()
    {
        // Arrange & Act
        var activity = new Activity { distance = null };
        
        // Assert
        activity.distance.Should().BeNull();
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AllowZeroOrNegativeDuration(int duration)
    {
        // Arrange & Act
        var activity = new Activity { duration = duration };
        
        // Assert
        activity.duration.Should().Be(duration);
    }
}
```

### Step 4: Enhance Existing Tests

Add error handling and edge case tests to existing test classes:

**ActivityHandlersShould.cs enhancements**:
```csharp
[Fact]
public async Task GetActivitiesByDateRange_ShouldReturnBadRequest_WhenStartDateAfterEndDate()
{
    // Test date validation logic
}

[Fact]
public async Task GetActivities_ShouldHandleNullContinuationToken()
{
    // Test null continuation token handling
}

[Theory]
[InlineData(0)]
[InlineData(-1)]
[InlineData(101)]
public async Task GetActivities_ShouldValidateMaxItemCount(int maxItemCount)
{
    // Test pagination validation
}
```

**PaginationRequestShould.cs enhancements**:
```csharp
[Fact]
public void HandleNullContinuationToken()
{
    // Test null continuation token
}

[Fact]
public void HandleEmptyContinuationToken()
{
    // Test empty continuation token
}

[Theory]
[InlineData("corrupted-token")]
[InlineData("!@#$%^&*()")]
public void HandleMalformedContinuationToken(string token)
{
    // Test malformed tokens
}
```

### Step 5: Verify Coverage Target

1. **Run all tests with coverage**:
   ```bash
   dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
   ```

2. **Generate and review report**:
   ```bash
   reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./coveragereport" -reporttypes:"Html;TextSummary"
   cat ./coveragereport/Summary.txt
   ```

3. **Verify coverage ≥80%** - if not, identify and test remaining gaps

## Phase 2: Create Integration Test Project (Priority P2)

### Step 1: Create Integration Test Project

1. **Create project directory**:
   ```bash
   mkdir src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.IntegrationTests
   cd src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.IntegrationTests
   ```

2. **Initialize xUnit project**:
   ```bash
   dotnet new xunit
   ```

3. **Add required packages**:
   ```bash
   dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.0
   dotnet add package FluentAssertions --version 8.4.0
   dotnet add package Microsoft.Extensions.Configuration --version 9.0.0
   dotnet add package Azure.Identity --version 1.15.0
   dotnet add package Microsoft.Azure.Cosmos --version 3.50.0
   ```

4. **Add project reference**:
   ```bash
   dotnet add reference ../Biotrackr.Activity.Api/Biotrackr.Activity.Api.csproj
   ```

5. **Add to solution**:
   ```bash
   cd ../
   dotnet sln add Biotrackr.Activity.Api.IntegrationTests/Biotrackr.Activity.Api.IntegrationTests.csproj
   ```

### Step 2: Create Test Infrastructure

#### 1. Create Test Configuration

Create `appsettings.Test.json`:
```json
{
  "Biotrackr": {
    "DatabaseName": "biotrackr-test",
    "ContainerName": "activity-test"
  },
  "azureappconfigendpoint": "http://localhost:5000",
  "cosmosdbendpoint": "https://test-cosmos.documents.azure.com:443/",
  "managedidentityclientid": "00000000-0000-0000-0000-000000000000"
}
```

#### 2. Create Fixtures Directory Structure

```bash
mkdir -p src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.IntegrationTests/Fixtures
mkdir -p src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.IntegrationTests/Collections
mkdir -p src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.IntegrationTests/Contract
mkdir -p src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.IntegrationTests/E2E
mkdir -p src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.IntegrationTests/Helpers
```

#### 3. Create Base Integration Test Fixture

Create `Fixtures/IntegrationTestFixture.cs`:
```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biotrackr.Activity.Api.IntegrationTests.Fixtures;

public class IntegrationTestFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; private set; }
    public HttpClient Client { get; private set; }
    protected virtual bool InitializeDatabase => true;
    
    public async Task InitializeAsync()
    {
        Factory = new ActivityApiWebApplicationFactory();
        Client = Factory.CreateClient();
        
        if (InitializeDatabase)
        {
            // Initialize test database
            var cosmosClient = Factory.Services.GetRequiredService<CosmosClient>();
            var database = await cosmosClient.CreateDatabaseIfNotExistsAsync("biotrackr-test");
            var container = await database.Database.CreateContainerIfNotExistsAsync(
                "activity-test",
                "/documentType"
            );
        }
    }
    
    public async Task DisposeAsync()
    {
        if (InitializeDatabase)
        {
            // Cleanup test data
            var cosmosClient = Factory.Services.GetRequiredService<CosmosClient>();
            var database = cosmosClient.GetDatabase("biotrackr-test");
            await database.DeleteAsync();
        }
        
        Client?.Dispose();
        await Factory.DisposeAsync();
    }
}
```

#### 4. Create Contract Test Fixture

Create `Fixtures/ContractTestFixture.cs`:
```csharp
namespace Biotrackr.Activity.Api.IntegrationTests.Fixtures;

/// <summary>
/// Lightweight fixture for contract/smoke tests
/// Skips database initialization to allow quick API startup verification
/// </summary>
public class ContractTestFixture : IntegrationTestFixture
{
    protected override bool InitializeDatabase => false;
}
```

#### 5. Create WebApplicationFactory

Create `Fixtures/ActivityApiWebApplicationFactory.cs`:
```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Biotrackr.Activity.Api.IntegrationTests.Fixtures;

public class ActivityApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false);
        });
    }
}
```

#### 6. Create Test Collection

Create `Collections/ContractTestCollection.cs`:
```csharp
using Biotrackr.Activity.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Biotrackr.Activity.Api.IntegrationTests.Collections;

[CollectionDefinition(nameof(ContractTestCollection))]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
    // This class is just a marker for xUnit to know about the collection
}
```

### Step 3: Create Contract Tests

#### 1. Program Startup Tests

Create `Contract/ProgramStartupTests.cs`:
```csharp
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Biotrackr.Activity.Api.Configuration;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using Biotrackr.Activity.Api.IntegrationTests.Fixtures;
using Biotrackr.Activity.Api.IntegrationTests.Collections;
using Xunit;

namespace Biotrackr.Activity.Api.IntegrationTests.Contract;

[Collection(nameof(ContractTestCollection))]
public class ProgramStartupTests
{
    private readonly ContractTestFixture _fixture;
    
    public ProgramStartupTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public void Application_Should_Start_Successfully()
    {
        // Arrange & Act
        var factory = _fixture.Factory;
        using var client = factory.CreateClient();
        
        // Assert
        client.Should().NotBeNull();
        client.BaseAddress.Should().NotBeNull();
    }
    
    [Fact]
    public void CosmosClient_Should_Be_Registered_As_Singleton()
    {
        // Arrange
        var services = _fixture.Factory.Services;
        
        // Act
        var client1 = services.GetService<CosmosClient>();
        var client2 = services.GetService<CosmosClient>();
        
        // Assert
        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.Should().BeSameAs(client2);
    }
    
    [Fact]
    public void CosmosRepository_Should_Be_Registered_As_Transient()
    {
        // Arrange
        var services = _fixture.Factory.Services;
        
        // Act
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();
        var repository1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
        var repository2 = scope2.ServiceProvider.GetService<ICosmosRepository>();
        
        // Assert
        repository1.Should().NotBeNull();
        repository2.Should().NotBeNull();
        repository1.Should().NotBeSameAs(repository2);
    }
    
    [Fact]
    public void Settings_Should_Be_Configured()
    {
        // Arrange
        var services = _fixture.Factory.Services;
        
        // Act
        var settings = services.GetService<IOptions<Settings>>();
        
        // Assert
        settings.Should().NotBeNull();
        settings!.Value.Should().NotBeNull();
        settings.Value.DatabaseName.Should().Be("biotrackr-test");
        settings.Value.ContainerName.Should().Be("activity-test");
    }
    
    [Fact]
    public void HealthChecks_Should_Be_Registered()
    {
        // Arrange
        var services = _fixture.Factory.Services;
        
        // Act
        var healthCheckService = services.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        
        // Assert
        healthCheckService.Should().NotBeNull();
    }
}
```

#### 2. API Smoke Tests

Create `Contract/ApiSmokeTests.cs`:
```csharp
using FluentAssertions;
using Biotrackr.Activity.Api.IntegrationTests.Fixtures;
using Biotrackr.Activity.Api.IntegrationTests.Collections;
using Xunit;

namespace Biotrackr.Activity.Api.IntegrationTests.Contract;

[Collection(nameof(ContractTestCollection))]
public class ApiSmokeTests
{
    private readonly ContractTestFixture _fixture;
    
    public ApiSmokeTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task HealthCheck_Should_Return_Healthy()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/health");
        
        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
    }
    
    [Fact]
    public async Task Swagger_Should_Be_Available()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/swagger/v1/swagger.json");
        
        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
```

### Step 4: Run Integration Tests

1. **Run contract tests** (fast, no database):
   ```bash
   dotnet test --filter "FullyQualifiedName~Contract"
   ```

2. **Verify execution time** - should be <1 minute

3. **Run all integration tests**:
   ```bash
   dotnet test
   ```

## Phase 3: GitHub Actions Integration (Priority P3)

### Update Workflow

Add test steps to `.github/workflows/deploy-activity-api.yml`:

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Run Unit Tests
        run: |
          dotnet test src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.UnitTests \
            --collect:"XPlat Code Coverage" \
            --results-directory ./TestResults
      
      - name: Run Integration Tests
        run: |
          dotnet test src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.IntegrationTests
      
      - name: Generate Coverage Report
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator \
            -reports:"./TestResults/**/coverage.cobertura.xml" \
            -targetdir:"./coveragereport" \
            -reporttypes:"TextSummary"
          cat ./coveragereport/Summary.txt
```

## Troubleshooting

### Unit Test Issues
- **Low coverage**: Check Summary.txt for uncovered methods
- **Flaky tests**: Ensure tests are independent and don't share state
- **Slow tests**: Check for unnecessary async operations

### Integration Test Issues
- **Cosmos DB connection failures**: Verify test configuration in appsettings.Test.json
- **Fixture initialization errors**: Check IAsyncLifetime implementation
- **Collection conflicts**: Ensure proper [Collection] attributes

## Next Steps

1. ✅ Extend unit tests to ≥80% coverage
2. ✅ Create integration test project with Contract and E2E tests
3. ⏭️ Implement E2E tests for activity endpoints
4. ⏭️ Add CI/CD workflow integration
5. ⏭️ Document test maintenance procedures

## Reference

- Weight.Api patterns: `src/Biotrackr.Weight.Api/Biotrackr.Weight.Api.IntegrationTests/`
- Decision records: `docs/decision-records/`
- Test execution contracts: `specs/004-activity-api-tests/contracts/`
