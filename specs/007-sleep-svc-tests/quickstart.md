# Quickstart: Sleep Service Test Coverage Implementation

**Feature**: 007-sleep-svc-tests  
**Date**: October 31, 2025

## Prerequisites

Before starting, ensure you have:

- [ ] .NET 9.0 SDK installed
- [ ] Docker Desktop installed and running (for Cosmos DB Emulator)
- [ ] Git repository cloned with branch `007-sleep-svc-tests` checked out
- [ ] Visual Studio Code or Visual Studio 2022
- [ ] Access to existing Sleep Service source code at `src/Biotrackr.Sleep.Svc/`

---

## Quick Setup (10 minutes)

### 1. Create Integration Test Project

```powershell
cd src\Biotrackr.Sleep.Svc
dotnet new xunit -n Biotrackr.Sleep.Svc.IntegrationTests
dotnet sln add Biotrackr.Sleep.Svc.IntegrationTests
```

### 2. Add Required NuGet Packages

```powershell
cd Biotrackr.Sleep.Svc.IntegrationTests
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.0
dotnet add package FluentAssertions --version 8.4.0
dotnet add package xUnit --version 2.9.3
dotnet add package Moq --version 4.20.72
dotnet add package AutoFixture --version 4.18.1
dotnet add package coverlet.collector --version 6.0.4
dotnet add reference ..\Biotrackr.Sleep.Svc\Biotrackr.Sleep.Svc.csproj
```

### 3. Create Directory Structure

```powershell
New-Item -ItemType Directory -Path Contract, E2E, Fixtures, Collections, Helpers
```

### 4. Create appsettings.Test.json

```powershell
@"
{
  `"keyvaulturl`": `"https://localhost:8081`",
  `"managedidentityclientid`": `"test-client-id`",
  `"cosmosdbendpoint`": `"https://localhost:8081`",
  `"applicationinsightsconnectionstring`": `"InstrumentationKey=test-key`",
  `"Biotrackr`": {
    `"DatabaseName`": `"BiotrackrTestDb`",
    `"ContainerName`": `"SleepTestContainer`"
  }
}
"@ | Out-File -FilePath appsettings.Test.json -Encoding UTF8
```

### 5. Start Cosmos DB Emulator

```powershell
# Navigate to repo root
cd ..\..\..\
.\cosmos-emulator.ps1
```

---

## Phase 1: Complete Unit Tests (30 minutes)

### Step 1: Create SleepWorkerShould.cs

**Location**: `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.UnitTests/WorkerTests/SleepWorkerShould.cs`

```powershell
New-Item -ItemType Directory -Path src\Biotrackr.Sleep.Svc\Biotrackr.Sleep.Svc.UnitTests\WorkerTests -Force
```

**Template**:
```csharp
using Biotrackr.Sleep.Svc.Services.Interfaces;
using Biotrackr.Sleep.Svc.Worker;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Biotrackr.Sleep.Svc.UnitTests.WorkerTests
{
    public class SleepWorkerShould
    {
        private readonly Mock<IFitbitService> _mockFitbitService;
        private readonly Mock<ISleepService> _mockSleepService;
        private readonly Mock<ILogger<SleepWorker>> _mockLogger;
        private readonly Mock<IHostApplicationLifetime> _mockAppLifetime;
        private readonly SleepWorker _sleepWorker;

        public SleepWorkerShould()
        {
            _mockFitbitService = new Mock<IFitbitService>();
            _mockSleepService = new Mock<ISleepService>();
            _mockLogger = new Mock<ILogger<SleepWorker>>();
            _mockAppLifetime = new Mock<IHostApplicationLifetime>();
            _sleepWorker = new SleepWorker(
                _mockFitbitService.Object,
                _mockSleepService.Object,
                _mockLogger.Object,
                _mockAppLifetime.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturn0_WhenSuccessful()
        {
            // Arrange
            var sleepResponse = new SleepResponse();
            _mockFitbitService.Setup(x => x.GetSleepResponse(It.IsAny<string>()))
                .ReturnsAsync(sleepResponse);

            // Act
            var result = await _sleepWorker.StartAsync(CancellationToken.None);

            // Assert
            result.Should().Be(0);
            _mockAppLifetime.Verify(x => x.StopApplication(), Times.Once);
        }

        // Add more tests...
    }
}
```

### Step 2: Add [ExcludeFromCodeCoverage] to Program.cs

**Location**: `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc/Program.cs`

Add at the top:
```csharp
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        // Existing code...
    }
}
```

### Step 3: Fix Duplicate Service Registration

**Location**: `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc/Program.cs`

Remove this line:
```csharp
services.AddScoped<IFitbitService, FitbitService>(); // DELETE THIS
```

Keep only:
```csharp
services.AddHttpClient<IFitbitService, FitbitService>()
    .AddStandardResilienceHandler();
```

### Step 4: Run Unit Tests with Coverage

```powershell
cd src\Biotrackr.Sleep.Svc
dotnet test Biotrackr.Sleep.Svc.UnitTests --collect:"XPlat Code Coverage"
```

**Expected**: ≥70% coverage

---

## Phase 2: Create Integration Test Fixtures (20 minutes)

### ContractTestFixture.cs

**Location**: `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests/Fixtures/ContractTestFixture.cs`

**Template**:
```csharp
public class ContractTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }

    public ContractTestFixture()
    {
        var services = new ServiceCollection();
        
        // Add in-memory configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Register services (without database initialization)
        // ... service registration code ...
        
        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        (ServiceProvider as IDisposable)?.Dispose();
    }
}
```

### IntegrationTestFixture.cs

**Location**: `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests/Fixtures/IntegrationTestFixture.cs`

**Template**:
```csharp
public class IntegrationTestFixture : IAsyncLifetime
{
    public CosmosClient CosmosClient { get; private set; }
    public Container Container { get; private set; }
    
    private const string DatabaseName = "BiotrackrTestDb";
    private const string ContainerName = "SleepTestContainer";

    public async Task InitializeAsync()
    {
        // Initialize Cosmos DB connection
        CosmosClient = new CosmosClient(
            "https://localhost:8081",
            "C2y6yDjf5/R+ob0N8A7Cgv...", // Emulator key
            new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                HttpClientFactory = () => new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                })
            });

        // Create database and container
        await CosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
        var database = CosmosClient.GetDatabase(DatabaseName);
        await database.CreateContainerIfNotExistsAsync(ContainerName, "/documentType");
        Container = database.GetContainer(ContainerName);
    }

    public async Task DisposeAsync()
    {
        if (CosmosClient != null)
        {
            await CosmosClient.GetDatabase(DatabaseName).DeleteAsync();
            CosmosClient.Dispose();
        }
    }
}
```

---

## Phase 3: Write Contract Tests (15 minutes)

### ProgramStartupTests.cs

**Location**: `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests/Contract/ProgramStartupTests.cs`

**Quick Test**:
```csharp
[Collection("SleepServiceContractTests")]
public class ProgramStartupTests
{
    private readonly ContractTestFixture _fixture;

    public ProgramStartupTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Application_ShouldResolveAllServices()
    {
        // Arrange & Act
        var cosmosClient = _fixture.ServiceProvider.GetService<CosmosClient>();
        var secretClient = _fixture.ServiceProvider.GetService<SecretClient>();
        var repository = _fixture.ServiceProvider.GetService<ICosmosRepository>();
        var sleepService = _fixture.ServiceProvider.GetService<ISleepService>();
        var fitbitService = _fixture.ServiceProvider.GetService<IFitbitService>();

        // Assert
        cosmosClient.Should().NotBeNull();
        secretClient.Should().NotBeNull();
        repository.Should().NotBeNull();
        sleepService.Should().NotBeNull();
        fitbitService.Should().NotBeNull();
    }
}
```

---

## Phase 4: Write E2E Tests (20 minutes)

### CosmosRepositoryTests.cs

**Location**: `src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests/E2E/CosmosRepositoryTests.cs`

**Quick Test**:
```csharp
[Collection("SleepServiceIntegrationTests")]
public class CosmosRepositoryTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ICosmosRepository _repository;

    public CosmosRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        // Initialize repository with test fixture
    }

    public async Task InitializeAsync()
    {
        await ClearContainerAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateSleepDocument_ShouldPersistToDatabase()
    {
        // Arrange
        var sleepDocument = new SleepDocument
        {
            Id = Guid.NewGuid().ToString(),
            Date = "2025-10-31",
            DocumentType = "Sleep",
            Sleep = new SleepResponse()
        };

        // Act
        await _repository.CreateSleepDocument(sleepDocument);

        // Assert
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
            .WithParameter("@id", sleepDocument.Id);
        var iterator = _fixture.Container.GetItemQueryIterator<SleepDocument>(query);
        var results = await iterator.ReadNextAsync();
        
        results.Should().HaveCount(1);
        results.First().Date.Should().Be("2025-10-31");
    }

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
}
```

---

## Phase 5: Update GitHub Workflow (10 minutes)

### Update deploy-sleep-service.yml

Add after `run-unit-tests` job:

```yaml
run-contract-tests:
  name: Run Contract Tests
  needs: env-setup
  uses: willvelida/biotrackr/.github/workflows/template-dotnet-run-contract-tests.yml@main
  with:
    dotnet-version: ${{ needs.env-setup.outputs.dotnet-version }}
    working-directory: ./src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests
    coverage-path: ${{ needs.env-setup.outputs.coverage-path }}
    test-filter: 'FullyQualifiedName~Contract'

run-e2e-tests:
  name: Run E2E Tests
  needs: [env-setup, run-contract-tests]
  uses: willvelida/biotrackr/.github/workflows/template-dotnet-run-e2e-tests.yml@main
  with:
    dotnet-version: ${{ needs.env-setup.outputs.dotnet-version }}
    working-directory: ./src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.IntegrationTests
    coverage-path: ${{ needs.env-setup.outputs.coverage-path }}
    test-filter: 'FullyQualifiedName~E2E'
```

---

## Verification Checklist

After completing all phases:

- [ ] Unit tests achieve ≥70% coverage
- [ ] All unit tests pass (`dotnet test Biotrackr.Sleep.Svc.UnitTests`)
- [ ] Contract tests pass (`dotnet test --filter "FullyQualifiedName~Contract"`)
- [ ] E2E tests pass (`dotnet test --filter "FullyQualifiedName~E2E"`)
- [ ] GitHub workflow runs successfully on pull request
- [ ] Coverage reports uploaded to GitHub Actions
- [ ] Test results published in PR comments

---

## Common Issues & Solutions

### Issue: Cosmos DB Emulator Not Starting

**Solution**: Ensure Docker Desktop is running and restart emulator:
```powershell
docker ps
.\cosmos-emulator.ps1
```

### Issue: SSL Negotiation Failed

**Solution**: Ensure using Gateway mode in CosmosClient options:
```csharp
ConnectionMode = ConnectionMode.Gateway
```

### Issue: Tests Find Each Other's Data

**Solution**: Ensure `ClearContainerAsync()` called in `InitializeAsync()`:
```csharp
public async Task InitializeAsync()
{
    await ClearContainerAsync();
}
```

### Issue: Workflow Fails with "Target Framework Not Found"

**Solution**: Verify test project targets `net9.0`:
```xml
<TargetFramework>net9.0</TargetFramework>
```

---

## Next Steps

1. Run `/speckit.plan` to generate detailed implementation plan
2. Follow plan to implement all tests systematically
3. Monitor coverage metrics to ensure ≥70% threshold
4. Review test results in GitHub Actions
5. Update spec status to "Implemented" when complete

---

## References

- [Full Specification](./spec.md)
- [Research Document](./research.md)
- [Data Model](./data-model.md)
- [Common Resolutions](.specify/memory/common-resolutions.md)
