# Quick Start Guide: Activity Service Test Coverage and Integration Tests

**Feature**: 005-activity-svc-tests  
**Date**: 2025-10-31

## Prerequisites

Before starting development, ensure you have:

- ✅ .NET 9.0 SDK installed
- ✅ Docker Desktop running (for Cosmos DB Emulator)
- ✅ VS Code with C# extension
- ✅ Git repository cloned and on branch `005-activity-svc-tests`
- ✅ Familiarity with xUnit, Moq, and FluentAssertions

## Quick Setup (5 minutes)

### 1. Start Cosmos DB Emulator

```powershell
# From repository root
docker-compose -f docker-compose.cosmos.yml up -d

# Verify emulator is running
docker ps | Select-String cosmos
```

**Expected Output**: Container running on ports 8081 (HTTPS), 10251-10254

### 2. Verify Existing Tests Run

```powershell
# Navigate to service directory
cd src/Biotrackr.Activity.Svc

# Run existing unit tests
dotnet test Biotrackr.Activity.Svc.UnitTests/Biotrackr.Activity.Svc.UnitTests.csproj
```

**Expected**: All tests pass in <5 seconds

### 3. Check Current Coverage

```powershell
# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# View coverage summary (look for line coverage %)
```

**Baseline**: Note current coverage to track improvement toward 70%

---

## Development Workflow

### Phase 1: Expand Unit Test Coverage (P1 - Start Here)

**Goal**: Reach 70% overall coverage

#### 1.1 Identify Coverage Gaps

```powershell
# Generate detailed coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./coverage/

# Open coverage report (install ReportGenerator if needed)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:./coverage/coverage.cobertura.xml -targetdir:./coverage/html
```

Open `./coverage/html/index.html` in browser to see line-by-line coverage.

#### 1.2 Add Edge Case Tests

Focus areas (from spec):
- ✅ ActivityWorker cancellation scenarios
- ✅ Empty response handling
- ✅ Exception paths
- ✅ Service error scenarios (malformed JSON, null responses)
- ✅ Repository error scenarios (rate limiting, duplicates)

**Example Test Template**:
```csharp
[Fact]
public async Task ExecuteAsync_Should_HandleCancellation_WhenTokenIsCancelled()
{
    // Arrange
    var cts = new CancellationTokenSource();
    cts.Cancel();
    
    // Act
    var exitCode = await InvokeExecuteAsync(cts.Token);
    
    // Assert
    exitCode.Should().Be(1);
    _loggerMock.Verify(/* verify cancellation logged */);
}
```

#### 1.3 Configure Coverage Exclusions

Add to `Biotrackr.Activity.Svc.UnitTests.csproj`:

```xml
<PropertyGroup>
    <ExcludeByFile>**/Program.cs</ExcludeByFile>
</PropertyGroup>
```

**Rationale**: Program.cs tested via integration tests (decision record 2025-10-28)

#### 1.4 Verify Coverage Target

```powershell
dotnet test /p:CollectCoverage=true

# Look for: "Line coverage: 70.0%" or higher
```

---

### Phase 2: Create Integration Test Project (P2)

**Goal**: Establish test infrastructure

#### 2.1 Create Project

```powershell
cd src/Biotrackr.Activity.Svc

# Create integration test project
dotnet new xunit -n Biotrackr.Activity.Svc.IntegrationTests

# Add to solution
dotnet sln add Biotrackr.Activity.Svc.IntegrationTests/Biotrackr.Activity.Svc.IntegrationTests.csproj
```

#### 2.2 Add NuGet Packages

```powershell
cd Biotrackr.Activity.Svc.IntegrationTests

dotnet add package xunit --version 2.9.3
dotnet add package FluentAssertions --version 8.4.0
dotnet add package Moq --version 4.20.72
dotnet add package AutoFixture --version 4.18.1
dotnet add package coverlet.collector --version 6.0.4
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.0
dotnet add package Microsoft.Azure.Cosmos --version 3.52.0
dotnet add package Azure.Identity --version 1.14.1

# Add project reference
dotnet add reference ../Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.csproj
```

#### 2.3 Create Directory Structure

```powershell
# Create folders
New-Item -ItemType Directory -Path Contract, E2E, Fixtures, Collections, Helpers

# Verify structure
tree /F
```

**Expected Structure**:
```
Biotrackr.Activity.Svc.IntegrationTests/
├── Contract/
├── E2E/
├── Fixtures/
├── Collections/
├── Helpers/
└── Biotrackr.Activity.Svc.IntegrationTests.csproj
```

#### 2.4 Create appsettings.Test.json

Create `appsettings.Test.json`:
```json
{
  "cosmosdbendpoint": "https://localhost:8081",
  "cosmosdbaccountkey": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
  "keyvaulturl": "https://test-vault.vault.azure.net/",
  "managedidentityclientid": "00000000-0000-0000-0000-000000000000",
  "databaseId": "BiotrackrTestDb",
  "containerId": "ActivityTestContainer",
  "applicationinsightsconnectionstring": "InstrumentationKey=test-key"
}
```

Update `.csproj`:
```xml
<ItemGroup>
  <None Update="appsettings.Test.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

### Phase 3: Implement Test Fixtures (P2)

#### 3.1 Create IntegrationTestFixture

Create `Fixtures/IntegrationTestFixture.cs`:

```csharp
public class IntegrationTestFixture : IAsyncLifetime
{
    protected virtual bool InitializeDatabase => true;
    
    public CosmosClient CosmosClient { get; private set; }
    public Database Database { get; private set; }
    public Container Container { get; private set; }
    
    public async Task InitializeAsync()
    {
        if (InitializeDatabase)
        {
            // Load configuration
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json")
                .Build();
            
            // Create Cosmos client (Gateway mode)
            CosmosClient = new CosmosClient(
                config["cosmosdbendpoint"],
                config["cosmosdbaccountkey"],
                new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway,
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    },
                    HttpClientFactory = () => new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    })
                });
            
            // Create database and container
            Database = await CosmosClient.CreateDatabaseIfNotExistsAsync(config["databaseId"]);
            Container = await Database.CreateContainerIfNotExistsAsync(
                config["containerId"], 
                "/documentType");
        }
    }
    
    public async Task DisposeAsync()
    {
        if (InitializeDatabase && Database != null)
        {
            await Database.DeleteAsync();
        }
        CosmosClient?.Dispose();
    }
}
```

#### 3.2 Create ContractTestFixture

Create `Fixtures/ContractTestFixture.cs`:

```csharp
public class ContractTestFixture : IntegrationTestFixture
{
    protected override bool InitializeDatabase => false;
    
    public IServiceProvider ServiceProvider { get; private set; }
    
    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"keyvaulturl", "https://test-vault.vault.azure.net/"},
                {"managedidentityclientid", "00000000-0000-0000-0000-000000000000"},
                {"cosmosdbendpoint", "https://localhost:8081"},
                {"applicationinsightsconnectionstring", "InstrumentationKey=test-key"}
            })
            .Build();
        
        // Register services (copy from Program.cs)
        // ...
        
        ServiceProvider = services.BuildServiceProvider();
    }
}
```

#### 3.3 Create Collection Definitions

Create `Collections/IntegrationTestCollection.cs`:
```csharp
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture> { }
```

Create `Collections/ContractTestCollection.cs`:
```csharp
[CollectionDefinition(nameof(ContractTestCollection))]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture> { }
```

---

### Phase 4: Write Contract Tests (P2)

#### 4.1 Create ServiceRegistrationTests

Create `Contract/ServiceRegistrationTests.cs`:

```csharp
[Collection(nameof(ContractTestCollection))]
public class ServiceRegistrationTests
{
    private readonly ContractTestFixture _fixture;
    
    public ServiceRegistrationTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public void AllRequiredServices_Should_BeRegistered()
    {
        // Arrange & Act
        var cosmosClient = _fixture.ServiceProvider.GetService<CosmosClient>();
        var secretClient = _fixture.ServiceProvider.GetService<SecretClient>();
        var repository = _fixture.ServiceProvider.GetService<ICosmosRepository>();
        var activityService = _fixture.ServiceProvider.GetService<IActivityService>();
        var fitbitService = _fixture.ServiceProvider.GetService<IFitbitService>();
        
        // Assert
        cosmosClient.Should().NotBeNull();
        secretClient.Should().NotBeNull();
        repository.Should().NotBeNull();
        activityService.Should().NotBeNull();
        fitbitService.Should().NotBeNull();
    }
    
    [Fact]
    public void FitbitService_Should_BeTransient()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        
        // Act
        var service1 = scope.ServiceProvider.GetService<IFitbitService>();
        var service2 = scope.ServiceProvider.GetService<IFitbitService>();
        
        // Assert
        service1.Should().NotBeSameAs(service2);
    }
}
```

#### 4.2 Run Contract Tests

```powershell
dotnet test --filter "FullyQualifiedName~Contract"
```

**Expected**: Tests pass in <5 seconds

---

### Phase 5: Write E2E Tests (P3)

#### 5.1 Create CosmosRepositoryTests

Create `E2E/CosmosRepositoryTests.cs`:

```csharp
[Collection(nameof(IntegrationTestCollection))]
public class CosmosRepositoryTests
{
    private readonly IntegrationTestFixture _fixture;
    
    public CosmosRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
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
    
    [Fact]
    public async Task CreateDocument_Should_PersistToCosmosDb()
    {
        // Arrange
        await ClearContainerAsync();
        var document = new ActivityDocument
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "test-user",
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            DocumentType = "activity"
            // ... other fields
        };
        
        // Act
        var response = await _fixture.Container.CreateItemAsync(
            document, 
            new PartitionKey("activity"));
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Resource.Id.Should().Be(document.Id);
    }
}
```

#### 5.2 Run E2E Tests

```powershell
# Ensure Cosmos DB Emulator is running
docker ps | Select-String cosmos

# Run E2E tests
dotnet test --filter "FullyQualifiedName~E2E"
```

**Expected**: Tests pass in <30 seconds

---

### Phase 6: Update GitHub Actions Workflow (P3)

Edit `.github/workflows/deploy-activity-service.yml`:

```yaml
run-contract-tests:
  name: Run Contract Tests
  needs: env-setup
  uses: willvelida/biotrackr/.github/workflows/template-dotnet-run-contract-tests.yml@main
  with:
    dotnet-version: ${{ needs.env-setup.outputs.dotnet-version }}
    working-directory: ./src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.IntegrationTests
    test-filter: 'FullyQualifiedName~Contract'
    coverage-path: ${{ needs.env-setup.outputs.coverage-path }}

run-e2e-tests:
  name: Run E2E Tests
  needs: [env-setup, run-contract-tests]
  uses: willvelida/biotrackr/.github/workflows/template-dotnet-run-e2e-tests.yml@main
  with:
    dotnet-version: ${{ needs.env-setup.outputs.dotnet-version }}
    working-directory: ./src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.IntegrationTests
    test-filter: 'FullyQualifiedName~E2E'
    coverage-path: ${{ needs.env-setup.outputs.coverage-path }}
```

**Critical**: Add `checks: write` permission if not present:

```yaml
permissions:
  contents: read
  id-token: write
  pull-requests: write
  checks: write  # Required for test reporter
```

---

## Common Commands Reference

### Running Tests Locally

```powershell
# All tests
dotnet test

# Unit tests only
dotnet test --filter "FullyQualifiedName!~Integration"

# Contract tests only
dotnet test --filter "FullyQualifiedName~Contract"

# E2E tests only
dotnet test --filter "FullyQualifiedName~E2E"

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Cosmos DB Emulator Management

```powershell
# Start
docker-compose -f docker-compose.cosmos.yml up -d

# Stop
docker-compose -f docker-compose.cosmos.yml down

# View logs
docker logs <container-id>

# Reset data
docker-compose -f docker-compose.cosmos.yml down -v
docker-compose -f docker-compose.cosmos.yml up -d
```

### Coverage Reports

```powershell
# Generate HTML report
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./coverage/
reportgenerator -reports:./coverage/coverage.cobertura.xml -targetdir:./coverage/html

# Open in browser
start ./coverage/html/index.html  # Windows
open ./coverage/html/index.html   # macOS
```

---

## Troubleshooting

### Issue: Cosmos DB Emulator SSL Errors

**Symptom**: `TransportException: SSL negotiation failed`

**Solution**: Ensure using Gateway mode:
```csharp
ConnectionMode = ConnectionMode.Gateway
```

**Reference**: Common Resolutions - E2E Tests Fail with SSL negotiation failed

---

### Issue: Tests Find More Documents Than Expected

**Symptom**: `Expected 1 item but found 3`

**Solution**: Add `ClearContainerAsync()` at start of each E2E test

**Reference**: Common Resolutions - E2E Tests Find More Documents Than Expected

---

### Issue: Workflow Can't Find Test Project

**Symptom**: `Project not found` in GitHub Actions

**Solution**: Use test project directory, not solution directory:
```yaml
working-directory: ./src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.IntegrationTests
```

**Reference**: Common Resolutions - Incorrect Working Directory for Reusable Workflow Templates

---

### Issue: Test Reporter Fails with Permissions Error

**Symptom**: `Publish Test Results` step fails in workflow

**Solution**: Add `checks: write` to workflow permissions

**Reference**: Common Resolutions - Test Reporter Action Failing with Permissions Error

---

## Verification Checklist

Before marking feature complete:

- [ ] Unit test coverage ≥70%
- [ ] Contract tests execute in <5 seconds
- [ ] E2E tests execute in <30 seconds
- [ ] All tests pass locally
- [ ] GitHub Actions workflow succeeds
- [ ] Coverage reports uploaded as artifacts
- [ ] Test results published to PR
- [ ] No flaky tests (remove if found)
- [ ] Code follows existing patterns (ActivityWorkerShould.cs)
- [ ] Decision records referenced in comments

---

## Next Steps

After completing this feature:
1. Apply same pattern to other services (Sleep, Food, Auth)
2. Review and update `.github/copilot-instructions.md`
3. Document lessons learned in `.specify/memory/common-resolutions.md`
4. Consider creating reusable test fixture base classes

---

## References

- [Specification](./spec.md)
- [Implementation Plan](./plan.md)
- [Research](./research.md)
- [Data Model](./data-model.md)
- [Test Contracts](./contracts/test-contracts.md)
- [Weight Service Tests](../003-weight-svc-integration-tests/spec.md) (reference pattern)
