# Quickstart Guide: Weight Service Integration Tests

**Date**: October 28, 2025  
**Feature**: Integration test infrastructure for Biotrackr.Weight.Svc

## Overview

This guide provides everything developers need to run, write, and maintain integration tests for the Weight Service. Integration tests verify end-to-end workflows and service integration points using real Cosmos DB operations (via Emulator) and mocked external dependencies.

---

## Prerequisites

### Required Software
- .NET 9.0 SDK
- Docker (for Cosmos DB Emulator locally)
- Git
- Visual Studio 2022 / VS Code / Rider (optional, for IDE support)

### Optional Tools
- Azure Cosmos DB Emulator (for local development)
- ReportGenerator (installed automatically by test scripts)

---

## Quick Start

### 1. Clone and Navigate to Project
```bash
git clone https://github.com/willvelida/biotrackr.git
cd biotrackr/src/Biotrackr.Weight.Svc
```

### 2. Run All Tests (Fastest)
```bash
dotnet test
```

This runs unit tests, contract tests, and E2E tests sequentially.

### 3. Run Only Integration Tests
```bash
dotnet test --filter "FullyQualifiedName~Biotrackr.Weight.Svc.IntegrationTests"
```

### 4. Run Contract Tests Only (Fast Feedback)
```bash
dotnet test --filter "FullyQualifiedName~Contract"
```

### 5. Run E2E Tests Only
```bash
dotnet test --filter "FullyQualifiedName~E2E"
```

---

## Running Tests Locally

### Option 1: Using GitHub Actions Services (Recommended for CI Parity)

This matches exactly how tests run in GitHub Actions.

**Start Cosmos DB Emulator**:
```bash
docker run -d --name cosmos-emulator \
  -p 8081:8081 -p 10251:10251 -p 10252:10252 -p 10253:10253 -p 10254:10254 \
  -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10 \
  -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=false \
  -e AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE=127.0.0.1 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

**Wait for Emulator to Start** (1-2 minutes):
```bash
# Linux/Mac
until curl -k https://localhost:8081/_explorer/emulator.pem > /dev/null 2>&1; do 
  echo "Waiting for Cosmos DB Emulator..."; 
  sleep 10; 
done

# Windows (PowerShell)
while (!(Test-NetConnection -ComputerName localhost -Port 8081 -InformationLevel Quiet)) {
  Write-Host "Waiting for Cosmos DB Emulator..."
  Start-Sleep -Seconds 10
}
```

**Download and Trust Certificate** (Linux/Mac only):
```bash
curl -k https://localhost:8081/_explorer/emulator.pem > emulatorcert.crt
sudo cp emulatorcert.crt /usr/local/share/ca-certificates/
sudo update-ca-certificates
```

**Run E2E Tests**:
```bash
cd Biotrackr.Weight.Svc.IntegrationTests
dotnet test --filter "FullyQualifiedName~E2E"
```

**Cleanup**:
```bash
docker stop cosmos-emulator
docker rm cosmos-emulator
```

### Option 2: Using Installed Azure Cosmos DB Emulator (Windows Only)

Download and install from: https://aka.ms/cosmosdb-emulator

The emulator automatically starts on Windows. Then run tests:
```bash
dotnet test --filter "FullyQualifiedName~E2E"
```

---

## Project Structure

```
Biotrackr.Weight.Svc.IntegrationTests/
├── Contract/                          # Fast tests, no external deps
│   ├── ProgramStartupTests.cs
│   └── ServiceRegistrationTests.cs
├── E2E/                               # Full workflow tests
│   ├── WeightWorkerTests.cs
│   ├── WeightServiceTests.cs
│   ├── FitbitServiceTests.cs
│   └── CosmosRepositoryTests.cs
├── Fixtures/                          # Test infrastructure
│   ├── ContractTestFixture.cs
│   └── IntegrationTestFixture.cs
├── Collections/                       # xUnit test collections
│   ├── ContractTestCollection.cs
│   └── IntegrationTestCollection.cs
├── Helpers/                           # Test utilities
│   ├── TestDataBuilder.cs
│   ├── MockHttpMessageHandler.cs
│   └── MockSecretClientFactory.cs
├── appsettings.Test.json
└── Biotrackr.Weight.Svc.IntegrationTests.csproj
```

---

## Writing New Tests

### Contract Test Example

Contract tests verify DI configuration without external dependencies:

```csharp
[Collection("Contract Tests")]
public class MyServiceRegistrationTests
{
    private readonly ContractTestFixture _fixture;
    
    public MyServiceRegistrationTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public void MyService_Is_Registered()
    {
        // Arrange
        var serviceProvider = _fixture.ServiceProvider;
        
        // Act
        var service = serviceProvider.GetService<IMyService>();
        
        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<MyService>();
    }
}
```

**Guidelines**:
- Place in `Contract/` folder
- Use `[Collection("Contract Tests")]` attribute
- No external dependencies (Cosmos DB, HTTP, Key Vault)
- Focus on DI configuration and service registration
- Keep tests fast (<100ms each)

### E2E Test Example

E2E tests verify complete workflows with Cosmos DB Emulator:

```csharp
[Collection("Integration Tests")]
public class MyEndToEndTests
{
    private readonly IntegrationTestFixture _fixture;
    
    public MyEndToEndTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task My_Workflow_Saves_Data_To_Cosmos()
    {
        // Arrange
        var document = TestDataBuilder.BuildWeightDocument();
        var repository = new CosmosRepository(
            _fixture.CosmosClient, 
            Options.Create(new Settings 
            { 
                DatabaseName = _fixture.DatabaseName,
                ContainerName = _fixture.ContainerName
            }),
            Mock.Of<ILogger<CosmosRepository>>());
        
        // Act
        await repository.CreateWeightDocument(document);
        
        // Assert - Query Cosmos DB directly
        var response = await _fixture.Container.ReadItemAsync<WeightDocument>(
            document.Id,
            new PartitionKey(document.DocumentType));
        
        response.Resource.Should().BeEquivalentTo(document);
    }
}
```

**Guidelines**:
- Place in `E2E/` folder
- Use `[Collection("Integration Tests")]` attribute
- Use `IntegrationTestFixture` for Cosmos DB access
- Generate unique test data (avoid conflicts)
- Clean up test data after execution (or use unique database)
- Use `TestDataBuilder` for consistent test data
- Mock external HTTP/Key Vault calls

---

## Test Data Best Practices

### Use TestDataBuilder
```csharp
// Good: Reusable, maintainable
var weight = TestDataBuilder.BuildWeight(DateTime.Now);
var response = TestDataBuilder.BuildWeightResponse(count: 7);
var document = TestDataBuilder.BuildWeightDocument();

// Avoid: Manual construction
var weight = new Weight 
{ 
    Date = "2025-10-28", 
    Weight = 75.5, 
    Bmi = 23.4 
};
```

### Generate Unique IDs
```csharp
// Good: No conflicts
var documentId = Guid.NewGuid().ToString();
var document = TestDataBuilder.BuildWeightDocument(id: documentId);

// Avoid: Hard-coded IDs
var document = new WeightDocument { Id = "test-123" }; // Conflicts!
```

### Use AutoFixture for Complex Objects
```csharp
var fixture = new Fixture();
var settings = fixture.Create<Settings>();
```

---

## Mocking External Dependencies

### Mock HTTP Responses (Fitbit API)
```csharp
_fixture.MockHttpMessageHandler.SetResponse(request =>
{
    // Verify request
    request.RequestUri.Should().Contain("fitbit.com");
    
    // Return mocked response
    return new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(TestDataBuilder.BuildWeightResponse()),
            Encoding.UTF8,
            "application/json")
    };
});
```

### Mock Key Vault (SecretClient)
```csharp
_fixture.MockSecretClient
    .Setup(x => x.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
    .ReturnsAsync(SecretModelFactory.KeyVaultSecret(
        new SecretProperties("AccessToken"), 
        "test-access-token"));

// Verify it was called
_fixture.MockSecretClient.Verify(
    x => x.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()),
    Times.Once);
```

---

## Debugging Tests

### Visual Studio
1. Right-click test method → Debug Test
2. Set breakpoints in test or production code
3. Inspect variables and step through execution

### VS Code
1. Open test file
2. Click "Debug Test" code lens above test method
3. Or use Debug panel with .NET Core Test configuration

### Command Line
```bash
# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run single test
dotnet test --filter "FullyQualifiedName~MyTestClass.MyTestMethod"

# Debug mode (attach debugger)
dotnet test --logger "console;verbosity=detailed" -- --debug
```

---

## Troubleshooting

### Cosmos DB Emulator Not Starting
**Problem**: Tests fail with connection errors

**Solutions**:
1. Check if emulator is running: `docker ps` or check Windows services
2. Verify port 8081 is accessible: `curl -k https://localhost:8081`
3. Wait longer for startup (2-3 minutes on first run)
4. Check logs: `docker logs cosmos-emulator`

### Certificate Trust Issues (Linux/Mac)
**Problem**: SSL/TLS errors connecting to emulator

**Solutions**:
1. Download certificate: `curl -k https://localhost:8081/_explorer/emulator.pem > cert.crt`
2. Trust certificate: `sudo cp cert.crt /usr/local/share/ca-certificates/ && sudo update-ca-certificates`
3. Restart terminal/shell

### Tests Timeout
**Problem**: Tests hang or timeout

**Solutions**:
1. Check Cosmos DB Emulator is responsive
2. Increase test timeout: `[Fact(Timeout = 30000)]`
3. Check for deadlocks in async code (use `ConfigureAwait(false)`)
4. Verify test isolation (unique database/container names)

### Test Data Conflicts
**Problem**: Tests fail intermittently due to duplicate IDs

**Solutions**:
1. Always generate unique IDs: `Guid.NewGuid().ToString()`
2. Use unique database per test run: `DatabaseName = $"test-{Guid.NewGuid():N}"`
3. Clean up test data in `DisposeAsync()`

### Slow Test Execution
**Problem**: Tests take >30 seconds

**Solutions**:
1. Run contract tests separately (should be <2s)
2. Check Cosmos DB Emulator performance
3. Use shared fixtures (already implemented)
4. Verify parallel execution enabled (xUnit default)

---

## CI/CD Integration

Tests automatically run in GitHub Actions:

```yaml
# .github/workflows/deploy-weight-service.yml

run-unit-tests:
  # Runs first, fastest feedback
  
run-contract-tests:
  needs: run-unit-tests
  uses: ./.github/workflows/template-dotnet-run-contract-tests.yml
  
run-e2e-tests:
  needs: run-contract-tests
  uses: ./.github/workflows/template-dotnet-run-e2e-tests.yml
  # Includes Cosmos DB Emulator service container
```

**Workflow Execution Order**:
1. Unit Tests (30s) ✅
2. Contract Tests (2s) ✅
3. E2E Tests (28s) ✅
4. Build & Deploy 🚀

---

## Coverage Reports

### Generate Coverage Locally
```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  "-reports:./TestResults/**/coverage.cobertura.xml" \
  "-targetdir:./CoverageReport" \
  "-reporttypes:Html"

# Open report
open ./CoverageReport/index.html  # Mac
start ./CoverageReport/index.html  # Windows
xdg-open ./CoverageReport/index.html  # Linux
```

### View Coverage in GitHub Actions
Coverage reports automatically upload to GitHub Actions artifacts:
1. Go to PR → Checks → Run Details
2. Scroll to Artifacts section
3. Download coverage-report artifact
4. Open index.html

### Coverage Targets
- **CosmosRepository**: 100%
- **WeightService**: 100%
- **FitbitService**: 90%
- **WeightWorker**: 85%
- **Overall**: ≥80%

---

## Performance Benchmarks

### Expected Timings
- Contract Tests: <2 seconds (5-10 tests)
- E2E Tests: <28 seconds (10-15 tests)
- Total Integration Tests: <30 seconds
- Full Test Suite (unit + integration): <60 seconds

### Optimization Tips
1. Use shared fixtures for expensive setup
2. Run contract tests first (fast feedback)
3. Parallelize test classes (xUnit default)
4. Keep Cosmos DB Emulator running between test runs locally
5. Use `[Theory]` for parameterized tests instead of multiple `[Fact]` methods

---

## Best Practices Summary

✅ **Do**:
- Use `TestDataBuilder` for test data
- Generate unique IDs (`Guid.NewGuid()`)
- Mock external dependencies (HTTP, Key Vault)
- Write descriptive test names
- Use FluentAssertions for readable assertions
- Clean up test data in `DisposeAsync()`
- Keep contract tests fast (<100ms each)
- Use shared fixtures for expensive setup

❌ **Don't**:
- Hard-code test data IDs
- Make actual external API calls
- Share state between tests
- Skip test cleanup
- Write slow contract tests
- Mix contract and E2E tests in same class
- Use `Thread.Sleep` for timing (use async/await)

---

## Getting Help

- **Documentation**: See `/specs/003-weight-svc-integration-tests/`
- **Examples**: Review existing tests in `Biotrackr.Weight.Api.IntegrationTests`
- **Issues**: Open GitHub issue with `area: testing` label
- **Questions**: Ask in team channel or PR comments

---

## Next Steps

1. ✅ Run existing tests to verify setup
2. ✅ Review test examples in Contract/ and E2E/
3. ✅ Write your first test using this guide
4. ✅ Generate coverage report
5. ✅ Push changes and verify CI/CD pipeline

Happy Testing! 🧪
