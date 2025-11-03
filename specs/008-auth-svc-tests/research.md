# Research: Auth Service Test Coverage and Integration Tests

**Feature**: 008-auth-svc-tests  
**Date**: November 3, 2025  
**Phase**: 0 - Outline & Research

## Research Questions Resolved

### 1. How to exclude Program.cs from code coverage in .NET 9.0?

**Decision**: Use `[ExcludeFromCodeCoverage]` attribute on the Program class

**Rationale**:
- The `[ExcludeFromCodeCoverage]` attribute is recognized by all coverage tools (coverlet.collector, coverlet.msbuild, dotCover, etc.)
- Works consistently in both local development and CI/CD environments
- Does not require runsettings files or additional configuration
- Already validated in Weight Service (002-weight-svc-coverage)

**Alternatives Considered**:
- **coverlet.msbuild with `<ExcludeByFile>`**: Works locally but not in CI/CD with coverlet.collector
- **runsettings file**: Adds unnecessary complexity and configuration burden
- **Excluding entire file in workflow**: Would require template modifications across all services

**Implementation**:
```csharp
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(...)
            .Build();
        
        host.Run();
    }
}
```

**Reference**: [Decision Record: Program Entry Point Coverage Exclusion](../../docs/decision-records/2025-10-28-program-entry-point-coverage-exclusion.md)

---

### 2. What is the correct service lifetime for HttpClient-based services?

**Decision**: Use **Transient** lifetime via `AddHttpClient<TInterface, TImplementation>()` without duplicate `AddScoped` registration

**Rationale**:
- HttpClientFactory manages connection pooling automatically - services don't need extended lifetimes
- HttpClient-based services are typically stateless and cheap to instantiate
- Transient lifetime prevents lifetime conflicts and follows Microsoft best practices
- Having duplicate registrations (AddScoped + AddHttpClient) causes confusion as the second overrides the first

**Alternatives Considered**:
- **Scoped + HttpClient**: Would circumvent HttpClientFactory's intended design and increase memory usage
- **Singleton**: Inappropriate for services that receive HttpClient instances from the factory
- **Dual registration**: Confusing and error-prone as one registration is silently ignored

**Implementation**:
```csharp
// Correct - single registration
services.AddHttpClient<IRefreshTokenService, RefreshTokenService>()
    .AddStandardResilienceHandler();

// Wrong - duplicate registration where second overrides first
services.AddScoped<IRefreshTokenService, RefreshTokenService>();  // Ignored!
services.AddHttpClient<IRefreshTokenService, RefreshTokenService>()
    .AddStandardResilienceHandler();
```

**Service Lifetime Guidelines**:
| Service Type | Lifetime | Reason |
|-------------|----------|--------|
| CosmosClient, SecretClient | Singleton | Expensive to create, thread-safe, manages connection pooling |
| Repositories, Services | Scoped | One instance per request/execution scope |
| HttpClient-based services | Transient | Managed by HttpClientFactory, lightweight |

**Reference**: [Decision Record: Service Lifetime Registration](../../docs/decision-records/2025-10-28-service-lifetime-registration.md)

---

### 3. How should Contract and E2E integration tests be organized?

**Decision**: Separate namespaces (Contract/ and E2E/) with distinct test fixtures and xUnit test filters

**Rationale**:
- Contract tests are fast (no external dependencies) and can run in parallel with unit tests
- E2E tests require mocked services but don't need Cosmos DB (Auth Service doesn't use database)
- xUnit Collection Fixtures enable efficient resource sharing while maintaining test isolation
- Test filters enable selective execution: `FullyQualifiedName~Contract` vs `FullyQualifiedName~E2E`

**Alternatives Considered**:
- **Single fixture with conditional initialization**: More complex, harder to maintain, loses separation of concerns
- **Separate test projects**: Unnecessary overhead for this service's scope
- **No fixtures**: Would duplicate setup code across tests, violating DRY principle

**Implementation Pattern**:
```
IntegrationTests/
├── Contract/                    # Fast tests, no external services
│   ├── ProgramStartupTests.cs
│   └── ServiceRegistrationTests.cs
├── E2E/                         # Full workflow tests with mocks
│   ├── RefreshTokenServiceTests.cs
│   └── AuthWorkerTests.cs
├── Fixtures/
│   ├── ContractTestFixture.cs   # Lightweight, no service initialization
│   └── IntegrationTestFixture.cs # Full setup with mocked SecretClient/HttpClient
└── Collections/
    ├── ContractTestCollection.cs
    └── IntegrationTestCollection.cs
```

**Reference**: 
- [Decision Record: Integration Test Project Structure](../../docs/decision-records/2025-10-28-integration-test-project-structure.md)
- [Decision Record: Contract Test Architecture](../../docs/decision-records/2025-10-28-contract-test-architecture.md)
- [Weight Service Integration Tests](../003-weight-svc-integration-tests/spec.md)

---

### 4. How to mock Azure Key Vault SecretClient in integration tests?

**Decision**: Use Moq to create mock SecretClient with setup for GetSecretAsync and SetSecretAsync

**Rationale**:
- Moq is already a project dependency and team is familiar with it
- SecretClient is not sealed and can be mocked directly
- Allows testing Key Vault integration without actual Azure resources
- Enables simulation of failure scenarios (missing secrets, save failures)

**Alternatives Considered**:
- **Azure Key Vault Emulator**: Does not exist (unlike Cosmos DB Emulator)
- **In-memory secret store**: Would require additional implementation and dependency injection changes
- **NSubstitute**: Would add another mocking library unnecessarily

**Implementation Example**:
```csharp
var mockSecretClient = new Mock<SecretClient>();

// Setup GetSecretAsync to return test secrets
mockSecretClient.Setup(client => client.GetSecretAsync("RefreshToken", null, default))
    .ReturnsAsync(Response.FromValue(
        SecretModelFactory.KeyVaultSecret(
            SecretModelFactory.SecretProperties(new Uri("https://vault.azure.net"), "RefreshToken"),
            "test-refresh-token"),
        Mock.Of<Response>()));

// Setup SetSecretAsync to verify save operations
mockSecretClient.Setup(client => client.SetSecretAsync("RefreshToken", It.IsAny<string>(), default))
    .ReturnsAsync(Response.FromValue(
        SecretModelFactory.KeyVaultSecret(
            SecretModelFactory.SecretProperties(new Uri("https://vault.azure.net"), "RefreshToken"),
            "new-refresh-token"),
        Mock.Of<Response>()));
```

---

### 5. How to mock HttpClient for Fitbit API calls?

**Decision**: Use Mock<HttpMessageHandler> with Protected().Setup() to intercept HTTP requests

**Rationale**:
- Standard pattern for mocking HttpClient in .NET tests
- Allows verification of request details (URL, headers, body)
- Enables simulation of various HTTP responses (success, errors, timeouts)
- Already used in existing RefreshTokenServiceShould.cs tests

**Alternatives Considered**:
- **WireMock.Net**: Overkill for simple HTTP mocking, adds external dependency
- **In-memory test server**: Not applicable (external Fitbit API, not internal service)
- **HttpClient factory with mock handler**: More complex than direct HttpMessageHandler mocking

**Implementation Example**:
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
        Content = new StringContent(JsonSerializer.Serialize(mockRefreshTokenResponse))
    });

var httpClient = new HttpClient(mockHttpMessageHandler.Object);
```

**Reference**: Existing implementation in `RefreshTokenServiceShould.cs` (lines 69-80)

---

### 6. Best practices for test isolation in xUnit Collection Fixtures?

**Decision**: 
- Use Collection Fixtures for shared expensive resources (fixtures)
- Tests within a collection run sequentially to avoid race conditions
- Clean up state between tests only if mutable state is shared
- For Auth Service: SecretClient and HttpClient are mocked fresh per test, so cleanup is minimal

**Rationale**:
- Collection Fixtures optimize resource usage while maintaining test independence
- Auth Service tests don't share mutable state (unlike Cosmos DB E2E tests that need cleanup)
- Sequential execution within collections prevents flaky tests from concurrent access

**Alternatives Considered**:
- **Class Fixtures**: Would duplicate fixture setup across test classes
- **No fixtures**: Would make tests slow and harder to maintain
- **Cleanup between tests**: Unnecessary for Auth Service since mocks are stateless

**Implementation Pattern**:
```csharp
[Collection("IntegrationTestCollection")]
public class RefreshTokenServiceTests
{
    private readonly IntegrationTestFixture _fixture;

    public RefreshTokenServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    // Tests use _fixture.MockSecretClient and _fixture.MockHttpClient
    // No cleanup needed - mocks are stateless
}
```

**Reference**: [Weight Service IntegrationTestCollection](../003-weight-svc-integration-tests/Collections/)

---

### 7. How to verify AuthWorker calls IHostApplicationLifetime.StopApplication?

**Decision**: Use TaskCompletionSource in mock setup to detect when StopApplication is called

**Rationale**:
- AuthWorker calls StopApplication in finally block to shut down the service after token refresh
- Tests need to wait for async execution to complete before verifying behavior
- TaskCompletionSource provides clean async coordination without Thread.Sleep

**Alternatives Considered**:
- **Thread.Sleep**: Brittle, timing-dependent, slow
- **Polling with timeout**: More complex than TaskCompletionSource
- **Not testing StopApplication**: Would leave critical application lifecycle untested

**Implementation Example** (already in AuthWorkerShould.cs):
```csharp
var completionSource = new TaskCompletionSource<bool>();

_mockAppLifeTime.Setup(l => l.StopApplication())
    .Callback(() => completionSource.SetResult(true));

await _sut.StartAsync(CancellationToken.None);
await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

_mockAppLifeTime.Verify(l => l.StopApplication(), Times.Once);
```

**Reference**: Existing implementation in `AuthWorkerShould.cs` (lines 26-47)

---

### 8. What edge cases need testing for AuthWorker and RefreshTokenService?

**Decision**: Comprehensive edge case coverage including:

**AuthWorker Edge Cases**:
1. ✅ Successful execution (already tested)
2. ⚠️ **MISSING**: RefreshTokens throws exception → logs error, returns exit code 1
3. ⚠️ **MISSING**: SaveTokens throws exception → logs error, returns exit code 1
4. ⚠️ **MISSING**: Cancellation via CancellationToken → gracefully stops
5. ✅ StopApplication called in finally block (already tested)

**RefreshTokenService Edge Cases**:
1. ✅ Successful token refresh (already tested)
2. ✅ Correct HTTP request construction (already tested)
3. ✅ Successful token save (already tested)
4. ⚠️ **MISSING**: GetSecretAsync returns null for RefreshToken → throws NullReferenceException
5. ⚠️ **MISSING**: GetSecretAsync returns null for FitbitCredentials → throws NullReferenceException
6. ⚠️ **MISSING**: Fitbit API returns HTTP 401 Unauthorized → throws HttpRequestException
7. ⚠️ **MISSING**: Fitbit API returns HTTP 429 Too Many Requests → throws HttpRequestException
8. ⚠️ **MISSING**: Fitbit API returns malformed JSON → throws JsonException
9. ⚠️ **MISSING**: Network timeout → throws TaskCanceledException
10. ⚠️ **MISSING**: SetSecretAsync fails for RefreshToken → throws RequestFailedException
11. ⚠️ **MISSING**: SetSecretAsync fails for AccessToken → throws RequestFailedException
12. ⚠️ **MISSING**: Partial save failure (RefreshToken saves but AccessToken fails)

**Rationale**: Edge case testing prevents production failures from unhandled exceptions and ensures proper error propagation for monitoring/alerting.

---

## Technology Stack Validation

### Testing Packages (Already in UnitTests project)
- ✅ xUnit 2.9.3 - Test framework
- ✅ FluentAssertions 8.4.0 - Readable assertions
- ✅ Moq 4.20.72 - Mocking framework
- ✅ AutoFixture 4.18.1 - Test data generation
- ✅ coverlet.collector 6.0.4 - Coverage collection

### Additional Packages for Integration Tests
- ➕ Microsoft.AspNetCore.Mvc.Testing 9.0.0 - WebApplicationFactory support (for consistency with pattern)
- ➕ Azure.Identity 1.14.1 - For SecretClient mocking types

### GitHub Actions Integration
- ✅ Reusable workflow templates exist: template-dotnet-run-unit-tests.yml, template-dotnet-run-contract-tests.yml, template-dotnet-run-e2e-tests.yml
- ✅ dorny/test-reporter@v1 for test result publishing
- ⚠️ Need to add `checks: write` permission to deploy-auth-service.yml
- ⚠️ Need to add contract and E2E test jobs to workflow

---

## Key Patterns Validated

### 1. Weight Service Integration Test Pattern
✅ **Confirmed applicable** to Auth Service:
- Contract/ and E2E/ namespace separation
- Separate ContractTestFixture and IntegrationTestFixture
- xUnit Collection Fixtures for resource sharing
- Test filters: `FullyQualifiedName~Contract` and `FullyQualifiedName~E2E`

**Differences for Auth Service**:
- No Cosmos DB required (Auth Service uses Key Vault only)
- E2E tests use mocked SecretClient instead of real Cosmos DB Emulator
- Simpler fixture setup (no database initialization, container creation, or cleanup)

### 2. Service Lifetime Pattern
✅ **Confirmed** current Program.cs has duplicate registration issue:
```csharp
services.AddScoped<IRefreshTokenService, RefreshTokenService>();  // First registration
services.AddHttpClient<IRefreshTokenService, RefreshTokenService>()
    .AddStandardResilienceHandler();  // Second registration (overrides first)
```

**Fix Required**: Remove duplicate `AddScoped` registration

### 3. Coverage Exclusion Pattern
✅ **Confirmed** Program.cs needs `[ExcludeFromCodeCoverage]` attribute

**Current State**: Program.cs has no coverage exclusion
**Required Change**: Add attribute to Program class

---

## Implementation Dependencies

### Prerequisites
1. ✅ .NET 9.0 SDK (already in project)
2. ✅ Existing unit test infrastructure (UnitTests project exists)
3. ✅ GitHub Actions workflow (deploy-auth-service.yml exists)
4. ✅ Reference implementation (Weight Service integration tests)

### New Dependencies
1. Create IntegrationTests project with NuGet packages
2. Add contract and E2E test jobs to deploy-auth-service.yml
3. Fix Program.cs duplicate service registration
4. Add Program.cs coverage exclusion attribute

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Mocking SecretClient incorrectly | Tests pass but prod fails | Follow Azure.Security.KeyVault.Secrets documentation for mock setup |
| HttpMessageHandler mocking brittle | Flaky tests | Use existing pattern from RefreshTokenServiceShould.cs |
| Test isolation issues | Flaky tests | Use Collection Fixtures with stateless mocks (no cleanup needed) |
| CI/CD test failures | Blocks deployments | Test locally with same filters before committing |
| Coverage not reaching 70% | Unmet requirement | Add tests systematically for each edge case identified |

---

## Success Metrics

- ✅ Research completed: All technical questions resolved
- ⏭️ Next Phase: Data Model & Contracts (Phase 1)

**Research Completion Checklist**:
- [x] Coverage exclusion strategy defined
- [x] Service lifetime pattern validated
- [x] Test organization structure confirmed
- [x] SecretClient mocking approach validated
- [x] HttpClient mocking approach validated
- [x] Test isolation strategy confirmed
- [x] AuthWorker testing pattern validated
- [x] Edge cases identified and categorized
- [x] Technology stack validated
- [x] Reference patterns confirmed applicable
- [x] Dependencies identified
- [x] Risks assessed with mitigations
