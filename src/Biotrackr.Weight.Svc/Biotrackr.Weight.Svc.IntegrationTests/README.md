# Biotrackr Weight Service Integration Tests

Integration test suite for the Biotrackr Weight Service that verifies end-to-end workflows and service integration points.

## Overview

This project contains comprehensive integration tests for `Biotrackr.Weight.Svc`, organized into two categories:

- **Contract Tests**: Fast tests (<2s) that verify DI configuration and service registration without external dependencies
- **E2E Tests**: Full integration tests that use Cosmos DB Emulator to verify complete workflows

## Project Structure

```
Biotrackr.Weight.Svc.IntegrationTests/
├── Contract/                          # Contract tests (no external dependencies)
│   ├── ProgramStartupTests.cs         # DI configuration verification
│   └── ServiceRegistrationTests.cs    # Service registration tests
├── E2E/                               # End-to-end tests (with Cosmos DB Emulator)
│   ├── WeightWorkerTests.cs           # Complete workflow tests
│   ├── WeightServiceTests.cs          # Service integration tests
│   ├── FitbitServiceTests.cs          # API integration tests
│   └── CosmosRepositoryTests.cs       # Database integration tests
├── Fixtures/                          # Test infrastructure
│   ├── ContractTestFixture.cs         # Contract test base fixture
│   └── IntegrationTestFixture.cs      # E2E test base fixture
├── Collections/                       # xUnit test collections
│   ├── ContractTestCollection.cs      # Contract test collection
│   └── IntegrationTestCollection.cs   # E2E test collection
├── Helpers/                           # Test utilities
│   ├── TestDataBuilder.cs             # Test data creation
│   └── MockHttpMessageHandler.cs      # HTTP mocking
└── appsettings.Test.json              # Test configuration
```

## Prerequisites

- .NET 9.0 SDK or later
- Docker (for running Cosmos DB Emulator locally)

## Running Tests

### All Tests
```powershell
dotnet test
```

### Contract Tests Only (Fast - <2 seconds)
```powershell
dotnet test --filter "FullyQualifiedName~Contract"
```

### E2E Tests Only
```powershell
dotnet test --filter "FullyQualifiedName~E2E"
```

## Running Tests Locally with Cosmos DB Emulator

### Start Cosmos DB Emulator

Windows (PowerShell):
```powershell
docker run -d --name cosmos-emulator `
  -p 8081:8081 -p 10251:10251 -p 10252:10252 -p 10253:10253 -p 10254:10254 `
  -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10 `
  -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=false `
  -e AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE=127.0.0.1 `
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

### Wait for Emulator to Start (1-2 minutes)

Windows (PowerShell):
```powershell
while (!(Test-NetConnection -ComputerName localhost -Port 8081 -InformationLevel Quiet)) {
  Write-Host "Waiting for Cosmos DB Emulator..."
  Start-Sleep -Seconds 10
}
Write-Host "Cosmos DB Emulator is ready!"
```

### Run E2E Tests
```powershell
dotnet test --filter "FullyQualifiedName~E2E"
```

### Stop Cosmos DB Emulator
```powershell
docker stop cosmos-emulator
docker rm cosmos-emulator
```

## Test Categories

### Contract Tests
- Verify service provider builds successfully
- Verify all required services are registered
- Verify configuration binding
- Verify service lifetimes (scoped, singleton, transient)
- Execute in <2 seconds total

### E2E Tests
- Complete workflow testing through WeightWorker
- Data persistence verification with Cosmos DB
- HTTP API integration with mocked responses
- Error handling and edge cases
- Execute in <30 seconds total

## Configuration

Test configuration is in `appsettings.Test.json`:
```json
{
  "Biotrackr": {
    "DatabaseName": "biotrackr-weight-test",
    "ContainerName": "weights"
  },
  "CosmosDb": {
    "Endpoint": "https://localhost:8081",
    "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  }
}
```

## GitHub Actions

Integration tests run automatically in CI/CD via two separate jobs:
1. **run-contract-tests**: Runs after unit tests, verifies DI configuration
2. **run-e2e-tests**: Runs after contract tests, uses Cosmos DB Emulator service container

## Test Patterns

### Using Contract Test Fixture
```csharp
[Collection("Contract Tests")]
public class MyContractTests
{
    private readonly ContractTestFixture _fixture;

    public MyContractTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void My_Contract_Test()
    {
        // Use _fixture.ServiceProvider for DI testing
        var service = _fixture.ServiceProvider.GetService<IMyService>();
        service.Should().NotBeNull();
    }
}
```

### Using Integration Test Fixture
```csharp
[Collection("Integration Tests")]
public class MyE2ETests
{
    private readonly IntegrationTestFixture _fixture;

    public MyE2ETests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task My_E2E_Test()
    {
        // Use _fixture.Container for Cosmos DB operations
        // Use _fixture.MockHttpMessageHandler for HTTP mocking
        
        // Setup mock HTTP response
        _fixture.MockHttpMessageHandler.SetResponse(
            TestDataBuilder.BuildSuccessfulFitbitResponse());
        
        // Perform test operations
        var service = _fixture.ServiceProvider.GetService<IWeightService>();
        await service.MapAndSaveDocument("2025-10-28", TestDataBuilder.BuildWeight());
        
        // Verify in Cosmos DB
        var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
            .WithParameter("@date", "2025-10-28");
        var iterator = _fixture.Container.GetItemQueryIterator<WeightDocument>(query);
        var documents = await iterator.ReadNextAsync();
        
        documents.Should().HaveCount(1);
    }
}
```

## Coverage

Run tests with coverage:
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

Generate HTML coverage report:
```powershell
reportgenerator `
  -reports:"**\coverage.cobertura.xml" `
  -targetdir:"coveragereport" `
  -reporttypes:Html
```

Target coverage: **80% minimum** for service layer components.

## Troubleshooting

### Cosmos DB Emulator Connection Issues
- Ensure emulator is running: `docker ps`
- Check emulator logs: `docker logs cosmos-emulator`
- Verify port 8081 is accessible: `Test-NetConnection -ComputerName localhost -Port 8081`

### Certificate Issues (Linux/Mac)
Download and trust the emulator certificate:
```bash
curl -k https://localhost:8081/_explorer/emulator.pem > emulatorcert.crt
sudo cp emulatorcert.crt /usr/local/share/ca-certificates/
sudo update-ca-certificates
```

### Test Failures
- Ensure all dependencies are restored: `dotnet restore`
- Clean and rebuild: `dotnet clean && dotnet build`
- Check test output for detailed error messages

## Performance Targets

- Contract tests: <2 seconds total
- All integration tests: <30 seconds total  
- Test reliability: 100 consecutive runs without environmental failures

## References

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Azure Cosmos DB Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)
- [Biotrackr Project README](../../../README.md)
