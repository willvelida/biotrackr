# Biotrackr.Auth.Svc.IntegrationTests

Integration tests for the Biotrackr Auth Service, following the established Weight Service pattern.

## Project Structure

```
Biotrackr.Auth.Svc.IntegrationTests/
├── Contract/                    # Fast DI/startup validation tests
│   ├── ProgramStartupTests.cs   # Host builder verification
│   └── ServiceRegistrationTests.cs # DI lifetime verification
├── E2E/                         # End-to-end workflow tests
│   ├── RefreshTokenServiceTests.cs # Service integration tests
│   └── AuthWorkerTests.cs       # Worker E2E tests
├── Fixtures/                    # Test infrastructure
│   ├── ContractTestFixture.cs   # Lightweight fixture (no mocks)
│   └── IntegrationTestFixture.cs # Full fixture (mocked services)
├── Collections/                 # xUnit test collections
│   ├── ContractTestCollection.cs
│   └── IntegrationTestCollection.cs
├── Helpers/                     # Test utilities
│   ├── TestDataGenerator.cs     # Generate test data
│   └── MockHttpMessageHandlerBuilder.cs # Fluent HTTP mocking
└── appsettings.Test.json       # Test configuration
```

## Test Categories

### Contract Tests
- **Purpose**: Verify dependency injection and service registration
- **Speed**: <5 seconds
- **Dependencies**: None (in-memory configuration only)
- **Run Command**: `dotnet test --filter "FullyQualifiedName~Contract"`

Contract tests validate:
- All services can be resolved from DI container
- Service lifetimes are correct (Singleton, Transient)
- Application host builds successfully
- Configuration values are accessible

### E2E Tests
- **Purpose**: Verify complete token refresh workflow
- **Speed**: <10 seconds
- **Dependencies**: Mocked (SecretClient, HttpClient)
- **Run Command**: `dotnet test --filter "FullyQualifiedName~E2E"`

E2E tests validate:
- RefreshTokenService retrieves secrets and calls Fitbit API
- Tokens are saved to SecretClient
- AuthWorker orchestrates the complete workflow
- Error scenarios are handled correctly

## Running Tests

```powershell
# Run all integration tests
dotnet test

# Run only contract tests (fast)
dotnet test --filter "FullyQualifiedName~Contract"

# Run only E2E tests
dotnet test --filter "FullyQualifiedName~E2E"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
reportgenerator -reports:./TestResults/**/coverage.cobertura.xml -targetdir:./TestResults/CoverageReport -reporttypes:Html
```

## Test Isolation

**Contract Tests**: Use `ContractTestFixture` which provides in-memory configuration without external service initialization.

**E2E Tests**: Use `IntegrationTestFixture` which provides:
- Mocked `SecretClient` for Azure Key Vault operations
- Mocked `HttpMessageHandler` for Fitbit API calls
- Stateless mocks configured per-test via Setup calls
- **No cleanup needed** between tests (mocks are stateless)

## Test Data

All test data is generated using hardcoded sample values in `TestDataGenerator`:
- No real Azure Key Vault secrets required
- No real Fitbit API credentials needed
- All external dependencies are mocked

## Key Patterns

### Using ContractTestFixture

```csharp
[Collection("ContractTestCollection")]
public class MyContractTests
{
    private readonly ContractTestFixture _fixture;

    public MyContractTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void MyTest()
    {
        // Use _fixture.ServiceProvider to resolve services
        var service = _fixture.ServiceProvider.GetService<IMyService>();
        service.Should().NotBeNull();
    }
}
```

### Using IntegrationTestFixture

```csharp
[Collection("IntegrationTestCollection")]
public class MyE2ETests
{
    private readonly IntegrationTestFixture _fixture;

    public MyE2ETests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MyTest()
    {
        // Configure mocks per-test
        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("RefreshToken", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("RefreshToken", "test_token"), new Mock<Response>().Object));

        // Use MockHttpMessageHandlerBuilder for fluent HTTP mocking
        MockHttpMessageHandlerBuilder.For(_fixture.MockHttpMessageHandler)
            .WithStatusCode(HttpStatusCode.OK)
            .WithJsonContent(TestDataGenerator.CreateRefreshTokenResponse())
            .Build();

        // Execute test...
    }
}
```

## Coverage Goals

- **Overall**: ≥70% line coverage for Biotrackr.Auth.Svc
- **Unit Tests**: Primary coverage mechanism
- **Integration Tests**: Validate component interactions

## CI/CD Integration

Integration tests run in GitHub Actions workflow:
- **Contract Tests**: Run in parallel with unit tests (no external dependencies)
- **E2E Tests**: Run after contract tests (with mocked dependencies)
- **No Cosmos DB Emulator required** (Auth Service doesn't use Cosmos DB)

## References

- [Weight Service Integration Tests](../../../specs/003-weight-svc-integration-tests/) - Reference pattern
- [Decision Record: Integration Test Project Structure](../../../docs/decision-records/2025-10-28-integration-test-project-structure.md)
- [Decision Record: Service Lifetime Registration](../../../docs/decision-records/2025-10-28-service-lifetime-registration.md)
