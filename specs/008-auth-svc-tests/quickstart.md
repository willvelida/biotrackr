# Quickstart Guide: Auth Service Test Coverage and Integration Tests

**Feature**: 008-auth-svc-tests  
**Date**: November 3, 2025  
**For**: Developers implementing or maintaining Auth Service tests

## Overview

This guide provides quick reference for working with Auth Service tests, including setup, running tests, and common patterns.

---

## Prerequisites

- ✅ .NET 9.0 SDK installed
- ✅ Visual Studio 2022, VS Code, or Rider IDE
- ✅ Git client for version control
- ✅ PowerShell (for Windows) or Bash (for Linux/Mac)

---

## Project Structure

```
src/Biotrackr.Auth.Svc/
├── Biotrackr.Auth.Svc/                    # Production code
├── Biotrackr.Auth.Svc.UnitTests/          # Unit tests
└── Biotrackr.Auth.Svc.IntegrationTests/   # Integration tests (Contract + E2E)
```

---

## Quick Commands

### Run All Tests

```powershell
# From repository root
dotnet test src/Biotrackr.Auth.Svc/Biotrackr.Auth.Svc.sln
```

### Run Unit Tests Only

```powershell
dotnet test src/Biotrackr.Auth.Svc/Biotrackr.Auth.Svc.UnitTests
```

### Run Contract Tests Only

```powershell
dotnet test src/Biotrackr.Auth.Svc/Biotrackr.Auth.Svc.IntegrationTests --filter "FullyQualifiedName~Contract"
```

### Run E2E Tests Only

```powershell
dotnet test src/Biotrackr.Auth.Svc/Biotrackr.Auth.Svc.IntegrationTests --filter "FullyQualifiedName~E2E"
```

### Run Tests with Coverage

```powershell
# Run unit tests with coverage
dotnet test src/Biotrackr.Auth.Svc/Biotrackr.Auth.Svc.UnitTests `
  --collect:"XPlat Code Coverage" `
  --results-directory:./TestResults

# View coverage report (requires reportgenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:./TestResults/**/coverage.cobertura.xml `
  -targetdir:./TestResults/CoverageReport `
  -reporttypes:Html
```

---

## Test Organization

### Unit Tests (`Biotrackr.Auth.Svc.UnitTests`)

**Purpose**: Fast, isolated tests for individual components

**Structure**:
```
UnitTests/
├── WorkerTests/
│   └── AuthWorkerShould.cs        # Worker orchestration logic
├── ServiceTests/
│   └── RefreshTokenServiceShould.cs  # Token refresh service
└── ModelTests/
    └── RefreshTokenResponseShould.cs # Data models
```

**Naming Convention**: `{ClassName}Should.cs` → `{MethodName}_{ExpectedBehavior}` or `{MethodName}Successfully When{Condition}`

**Example**:
```csharp
public class AuthWorkerShould
{
    [Fact]
    public async Task RefreshAndSaveTokensSuccessfullyWhenExecuteAsyncIsCalled()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

---

### Contract Tests (`IntegrationTests/Contract/`)

**Purpose**: Verify dependency injection, service registration, and application startup

**Structure**:
```
Contract/
├── ProgramStartupTests.cs         # Host builder validation
└── ServiceRegistrationTests.cs    # DI lifetime verification
```

**Key Characteristics**:
- No external dependencies (no network, no database)
- Use in-memory configuration
- Fast execution (< 5 seconds total)
- Run in parallel with unit tests in CI/CD

**Example**:
```csharp
[Collection("ContractTestCollection")]
public class ServiceRegistrationTests
{
    private readonly ContractTestFixture _fixture;

    public ServiceRegistrationTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void AllRequiredServicesCanBeResolvedFromDI()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        
        // Act & Assert
        scope.ServiceProvider.GetService<SecretClient>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IRefreshTokenService>().Should().NotBeNull();
    }
}
```

---

### E2E Tests (`IntegrationTests/E2E/`)

**Purpose**: Verify complete workflows with mocked external dependencies

**Structure**:
```
E2E/
├── RefreshTokenServiceTests.cs    # Service integration tests
└── AuthWorkerTests.cs             # Worker E2E tests
```

**Key Characteristics**:
- Use mocked SecretClient (Azure Key Vault)
- Use mocked HttpClient (Fitbit API)
- Test complete workflows end-to-end
- Moderate execution time (< 10 seconds total)

**Example**:
```csharp
[Collection("IntegrationTestCollection")]
public class RefreshTokenServiceTests
{
    private readonly IntegrationTestFixture _fixture;

    public RefreshTokenServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RefreshesTokensEndToEndWithMockedDependencies()
    {
        // Arrange - customize fixture mocks for this test
        _fixture.MockSecretClient
            .Setup(c => c.GetSecretAsync("RefreshToken", null, default))
            .ReturnsAsync(/* mock response */);

        // Act
        var result = await _fixture.RefreshTokenService.RefreshTokens();

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
    }
}
```

---

## Common Patterns

### 1. Mocking SecretClient

```csharp
var mockSecretClient = new Mock<SecretClient>();

// Setup GetSecretAsync
mockSecretClient
    .Setup(client => client.GetSecretAsync("RefreshToken", null, default))
    .ReturnsAsync(Response.FromValue(
        SecretModelFactory.KeyVaultSecret(
            SecretModelFactory.SecretProperties(
                new Uri("https://test-vault.azure.net"),
                "RefreshToken"),
            "test-refresh-token-value"),
        Mock.Of<Response>()));

// Setup SetSecretAsync
mockSecretClient
    .Setup(client => client.SetSecretAsync("RefreshToken", It.IsAny<string>(), default))
    .ReturnsAsync(Response.FromValue(
        SecretModelFactory.KeyVaultSecret(
            SecretModelFactory.SecretProperties(
                new Uri("https://test-vault.azure.net"),
                "RefreshToken"),
            "new-token-value"),
        Mock.Of<Response>()));

// Verify calls
mockSecretClient.Verify(
    c => c.SetSecretAsync("RefreshToken", It.IsAny<string>(), default),
    Times.Once);
```

---

### 2. Mocking HttpClient for Fitbit API

```csharp
var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

mockHttpMessageHandler.Protected()
    .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
    .ReturnsAsync(new HttpResponseMessage
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent(JsonSerializer.Serialize(new RefreshTokenResponse
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            ExpiresIn = 28800,
            Scope = "activity heartrate profile",
            TokenType = "Bearer",
            UserId = "TEST_USER"
        }))
    });

var httpClient = new HttpClient(mockHttpMessageHandler.Object);
```

---

### 3. Verifying HTTP Request Details

```csharp
HttpRequestMessage? capturedRequest = null;

mockHttpMessageHandler.Protected()
    .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
    .Callback<HttpRequestMessage, CancellationToken>((request, _) => 
        capturedRequest = request)
    .ReturnsAsync(/* response */);

// Act
await service.RefreshTokens();

// Assert
capturedRequest.Should().NotBeNull();
capturedRequest!.Method.Should().Be(HttpMethod.Post);
capturedRequest.RequestUri!.AbsoluteUri.Should().Contain("api.fitbit.com/oauth2/token");
capturedRequest.Headers.Authorization!.Scheme.Should().Be("Basic");
```

---

### 4. Testing AuthWorker with TaskCompletionSource

```csharp
var completionSource = new TaskCompletionSource<bool>();

mockAppLifetime
    .Setup(l => l.StopApplication())
    .Callback(() => completionSource.SetResult(true));

await worker.StartAsync(CancellationToken.None);

// Wait for ExecuteAsync to complete
await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

await worker.StopAsync(CancellationToken.None);

// Verify behavior
mockAppLifetime.Verify(l => l.StopApplication(), Times.Once);
```

---

### 5. Using FluentAssertions

```csharp
// Object assertions
result.Should().NotBeNull();
result.AccessToken.Should().NotBeNullOrEmpty();
result.ExpiresIn.Should().BeGreaterThan(0);

// Collection assertions
capturedRequest.RequestUri!.Query.Should().Contain("grant_type=refresh_token");

// Exception assertions
await act.Should().ThrowAsync<HttpRequestException>()
    .WithMessage("*401*");

// Reference equality (for Singleton tests)
var service1 = serviceProvider.GetService<SecretClient>();
var service2 = serviceProvider.GetService<SecretClient>();
service1.Should().BeSameAs(service2);

// Reference inequality (for Transient tests)
var service1 = serviceProvider.GetService<IRefreshTokenService>();
var service2 = serviceProvider.GetService<IRefreshTokenService>();
service1.Should().NotBeSameAs(service2);
```

---

## Test Data Generation

### Using AutoFixture

```csharp
var fixture = new Fixture();
var mockRefreshTokenResponse = fixture.Create<RefreshTokenResponse>();
```

### Using Fixed Test Data

```csharp
var testResponse = new RefreshTokenResponse
{
    AccessToken = "test-access-token-12345",
    RefreshToken = "test-refresh-token-67890",
    ExpiresIn = 28800,
    Scope = "activity heartrate profile",
    TokenType = "Bearer",
    UserId = "TEST_USER"
};
```

### Using TestDataGenerator Helper (After Implementation)

```csharp
var response = TestDataGenerator.CreateValidRefreshTokenResponse();
var json = TestDataGenerator.CreateRefreshTokenResponseJson(response);
var secret = TestDataGenerator.CreateKeyVaultSecret("RefreshToken", "test-value");
```

---

## Troubleshooting

### Tests are slow

**Problem**: Tests take longer than expected  
**Solutions**:
- Ensure contract tests use ContractTestFixture (no external services)
- Verify E2E tests use mocked dependencies (not real HTTP calls)
- Check for Thread.Sleep or unnecessary delays
- Use TaskCompletionSource for async coordination instead of polling

---

### Coverage not reaching 70%

**Problem**: Code coverage below threshold  
**Solutions**:
- Check that Program.cs has `[ExcludeFromCodeCoverage]` attribute
- Review missing edge cases in test-scenarios.md
- Run coverage report: `dotnet test --collect:"XPlat Code Coverage"`
- Use reportgenerator to visualize uncovered lines

---

### Mocking SecretClient fails

**Problem**: SecretClient mock throws exceptions  
**Solutions**:
- Use `SecretModelFactory.KeyVaultSecret()` to create mock secrets
- Ensure Response.FromValue() wraps the secret properly
- Verify mock setup includes all required GetSecretAsync calls
- Check that secret names match exactly ("RefreshToken", "FitbitCredentials", "AccessToken")

---

### HttpClient mocking not working

**Problem**: HTTP calls fail or aren't intercepted  
**Solutions**:
- Use `Mock<HttpMessageHandler>` with `.Protected().Setup()`
- Verify "SendAsync" method name is exact (case-sensitive)
- Check that HttpClient receives mocked handler: `new HttpClient(mockHandler.Object)`
- Use `ItExpr.IsAny<HttpRequestMessage>()` for flexible matching

---

### Tests are flaky in CI/CD

**Problem**: Tests pass locally but fail in GitHub Actions  
**Solutions**:
- Avoid dynamic types with FluentAssertions (use strongly-typed models)
- Use TaskCompletionSource with timeouts instead of Thread.Sleep
- Ensure test isolation (no shared mutable state between tests)
- Verify Collection Fixtures are used correctly (tests in collection run sequentially)

---

## CI/CD Integration

### GitHub Actions Workflow

The deploy-auth-service.yml workflow includes:

```yaml
jobs:
  run-unit-tests:
    uses: ./.github/workflows/template-dotnet-run-unit-tests.yml
    with:
      working-directory: ./src/Biotrackr.Auth.Svc/Biotrackr.Auth.Svc.UnitTests

  run-contract-tests:
    uses: ./.github/workflows/template-dotnet-run-contract-tests.yml
    with:
      working-directory: ./src/Biotrackr.Auth.Svc/Biotrackr.Auth.Svc.IntegrationTests
      test-filter: 'FullyQualifiedName~Contract'

  run-e2e-tests:
    needs: run-contract-tests
    uses: ./.github/workflows/template-dotnet-run-e2e-tests.yml
    with:
      working-directory: ./src/Biotrackr.Auth.Svc/Biotrackr.Auth.Svc.IntegrationTests
      test-filter: 'FullyQualifiedName~E2E'
```

**Key Points**:
- Unit and contract tests run in parallel (both fast, no external deps)
- E2E tests run after contract tests complete
- No Cosmos DB Emulator required (Auth Service doesn't use database)
- Test results published via dorny/test-reporter@v1

---

## Code Coverage

### Viewing Coverage Locally

```powershell
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Install report generator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator `
  -reports:./TestResults/**/coverage.cobertura.xml `
  -targetdir:./TestResults/CoverageReport `
  -reporttypes:Html

# Open report in browser
start ./TestResults/CoverageReport/index.html
```

### Coverage Exclusions

Program.cs is excluded from coverage using:

```csharp
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        // ... startup code
    }
}
```

**Why excluded**: Program.cs contains only startup/DI configuration with no business logic to test.

---

## Next Steps

1. **Implement Missing Unit Tests**: See [test-scenarios.md](contracts/test-scenarios.md) for list of tests marked "⚠️ TO CREATE"
2. **Create Integration Test Project**: Follow structure in [data-model.md](data-model.md)
3. **Update GitHub Workflow**: Add contract and E2E test jobs to deploy-auth-service.yml
4. **Fix Program.cs Issues**: Add [ExcludeFromCodeCoverage] attribute and remove duplicate service registration

---

## References

- [Feature Specification](spec.md) - Complete requirements
- [Research](research.md) - Technical decisions and patterns
- [Data Model](data-model.md) - Test infrastructure entities
- [Test Scenarios](contracts/test-scenarios.md) - All test cases
- [Decision Records](../../docs/decision-records/) - Architecture decisions
- [Common Resolutions](../../.specify/memory/common-resolutions.md) - Known issues and solutions

---

## Support

For questions or issues:
1. Check [Common Resolutions](../../.specify/memory/common-resolutions.md) for known issues
2. Review [Decision Records](../../docs/decision-records/) for context
3. Consult Weight Service tests (003-weight-svc-integration-tests) as reference implementation
4. Create GitHub Issue with `testing` label for new problems
