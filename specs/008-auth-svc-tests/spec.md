# Feature Specification: Auth Service Test Coverage and Integration Tests

**Feature Branch**: `008-auth-svc-tests`  
**Created**: November 3, 2025  
**Status**: Draft  
**Input**: User description: "Extend the unit tests for Biotrackr.Auth.Svc so that they meet the code coverage requirements and add Integration tests that can be run within the GitHub Action workflow"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Complete Unit Test Coverage for Missing Components (Priority: P1)

As a developer, I need comprehensive unit tests for all Auth Service components to ensure token refresh logic correctness and meet the 70% code coverage threshold.

**Why this priority**: Current unit tests exist for AuthWorker, RefreshTokenService, and RefreshTokenResponse but may have gaps in edge cases, error scenarios, and exception paths. Achieving 70% coverage is foundational for maintaining code quality and preventing authentication failures in production.

**Independent Test**: Can be fully tested by running `dotnet test` with coverage collection and verifying that overall line coverage reaches at least 70% across all Auth Service components.

**Acceptance Scenarios**:

1. **Given** the existing unit test suite, **When** code coverage is measured, **Then** it should reach at least 70% line coverage for the entire Biotrackr.Auth.Svc project
2. **Given** Program.cs contains only DI registration and configuration, **When** coverage exclusions are configured, **Then** Program.cs should be excluded from coverage metrics using [ExcludeFromCodeCoverage] attribute
3. **Given** AuthWorker has comprehensive tests, **When** all edge cases are covered, **Then** test coverage should include cancellation scenarios, token refresh failures, and secret save failures
4. **Given** RefreshTokenService tests exist, **When** reviewing test completeness, **Then** edge cases like missing secrets, HTTP errors, malformed JSON, and Key Vault failures should be covered
5. **Given** error handling is critical for authentication, **When** testing exception paths, **Then** all catch blocks and error logging should be thoroughly tested

---

### User Story 2 - Integration Test Project Creation (Priority: P2)

As a developer, I need a properly structured integration test project following established patterns so that I can verify the Auth Service behaves correctly with realistic dependencies.

**Why this priority**: Integration tests validate that components work together correctly and that external dependencies (Azure Key Vault, Fitbit API) are properly integrated. Following the Weight Service pattern ensures consistency across the codebase.

**Independent Test**: Can be fully tested by creating the project structure, running `dotnet build`, and verifying the project compiles successfully with all required dependencies.

**Acceptance Scenarios**:

1. **Given** the Auth Service solution exists, **When** the integration test project is created, **Then** it includes all required testing packages (xUnit, FluentAssertions, Moq, AutoFixture, coverlet.collector, Microsoft.AspNetCore.Mvc.Testing)
2. **Given** the integration test project structure is defined, **When** reviewing the folder layout, **Then** Contract/, E2E/, Fixtures/, Collections/, and Helpers/ folders exist matching the Weight Service pattern
3. **Given** the test project is configured, **When** tests are discovered, **Then** the test runner recognizes all test classes and methods across both Contract and E2E namespaces
4. **Given** integration tests require configuration, **When** appsettings.Test.json is created, **Then** it contains test-specific connection strings and settings for mocked Key Vault access

---

### User Story 3 - Contract Integration Tests (Priority: P2)

As a developer, I need contract integration tests that verify dependency injection and service registration without external dependencies so that I can run fast tests in parallel with unit tests.

**Why this priority**: Contract tests validate the "contract" of the application—that services are properly registered and the application can start. These tests are fast and can run in parallel with unit tests without requiring Azure Key Vault or network access.

**Independent Test**: Can be tested by running contract tests in isolation using `dotnet test --filter "FullyQualifiedName~Contract"` and verifying they execute quickly (under 5 seconds total) without external dependencies.

**Acceptance Scenarios**:

1. **Given** the application uses dependency injection, **When** ProgramStartupTests verify service registration, **Then** all required services (SecretClient, RefreshTokenService, HttpClient) can be resolved from the service provider
2. **Given** services have defined lifetimes, **When** ServiceRegistrationTests verify lifetimes, **Then** Singleton services (SecretClient) return the same instance, Scoped services (RefreshTokenService without HttpClient) return the same instance within a scope, and Transient services (RefreshTokenService via AddHttpClient) return different instances
3. **Given** the application can start, **When** testing application bootstrapping, **Then** the host builds successfully without runtime exceptions
4. **Given** configuration is required, **When** testing with in-memory configuration, **Then** all required configuration values (keyvaulturl, managedidentityclientid, applicationinsightsconnectionstring) are accessible

---

### User Story 4 - E2E Integration Tests with Mocked Dependencies (Priority: P3)

As a developer, I need end-to-end integration tests that verify the complete token refresh workflow with mocked external dependencies so that I can ensure the workflow functions correctly without requiring actual Azure services.

**Why this priority**: E2E tests validate the full workflow including service interactions. While important, they are lower priority than unit and contract tests since the Auth Service is simpler than data-driven services and doesn't require Cosmos DB.

**Independent Test**: Can be tested by running E2E tests using `dotnet test --filter "FullyQualifiedName~E2E"` and verifying they execute the complete workflow with mocked SecretClient and HttpClient.

**Acceptance Scenarios**:

1. **Given** a mocked SecretClient is configured, **When** RefreshTokenServiceTests retrieve secrets, **Then** they correctly fetch RefreshToken and FitbitCredentials values and handle missing secret scenarios
2. **Given** a mocked HttpClient is configured, **When** RefreshTokenServiceTests call Fitbit API, **Then** they send correct POST requests with proper authorization headers and parse successful responses
3. **Given** the RefreshTokenService saves tokens, **When** tests verify SaveTokens method, **Then** both RefreshToken and AccessToken are saved to SecretClient with correct secret names
4. **Given** the AuthWorker orchestrates the workflow, **When** tests verify end-to-end execution, **Then** it retrieves tokens from Fitbit, saves them to Key Vault, logs appropriately, and stops the application
5. **Given** error scenarios occur, **When** tests verify exception handling, **Then** appropriate exceptions are thrown and logged, and the application returns exit code 1

---

### User Story 5 - GitHub Actions Workflow Integration (Priority: P3)

As a developer, I need integration tests to run automatically in the GitHub Actions CI/CD pipeline so that test failures are caught before deployment.

**Why this priority**: Automated testing in CI/CD prevents authentication failures from reaching production. This is important but depends on having working tests first (P1-P2 priorities).

**Independent Test**: Can be tested by triggering the workflow on a pull request and verifying all test jobs (unit, contract, E2E) execute successfully and report results.

**Acceptance Scenarios**:

1. **Given** the deploy-auth-service.yml workflow exists, **When** contract tests are configured, **Then** they run in parallel with unit tests without requiring external services
2. **Given** E2E tests use mocked dependencies, **When** the workflow is configured, **Then** E2E tests run without requiring Azure Key Vault or Cosmos DB Emulator
3. **Given** tests complete in the workflow, **When** coverage reports are generated, **Then** they are uploaded as artifacts and published using dorny/test-reporter@v1 with checks: write permission
4. **Given** test failures occur, **When** the workflow runs, **Then** detailed error messages and logs are available in the GitHub Actions summary
5. **Given** the workflow uses reusable templates, **When** test jobs are defined, **Then** they use correct working-directory paths (e.g., ./src/Biotrackr.Auth.Svc/Biotrackr.Auth.Svc.UnitTests for unit tests, not the solution directory)

---

### Edge Cases

- What happens when AuthWorker is cancelled mid-execution via CancellationToken?
- How does the system handle empty or null responses from the Fitbit token endpoint?
- What occurs when SecretClient.GetSecretAsync returns null for RefreshToken or FitbitCredentials?
- How does the system behave when Fitbit API returns HTTP 401 Unauthorized?
- What happens if Fitbit API returns HTTP 429 Too Many Requests?
- How does the system handle network timeouts when calling the Fitbit API?
- What occurs when SecretClient.SetSecretAsync fails during SaveTokens?
- How does the service handle malformed JSON responses from Fitbit API?
- What happens when RefreshTokenResponse deserialization fails?
- How does the system behave when IHostApplicationLifetime.StopApplication is called multiple times?
- What occurs when the managed identity cannot authenticate to Key Vault?
- How does the system handle scenarios where only one of two secrets (RefreshToken or AccessToken) fails to save?

## Requirements *(mandatory)*

### Functional Requirements - Unit Tests

- **FR-001**: Test suite MUST achieve at least 70% overall line coverage for Biotrackr.Auth.Svc project; if threshold not immediately reachable, prioritize RefreshTokenService tests (core token refresh logic) over AuthWorker tests (orchestration wrapper)
- **FR-002**: Coverage configuration MUST exclude Program.cs from coverage calculations using [ExcludeFromCodeCoverage] attribute on the Program class
- **FR-003**: Tests MUST cover AuthWorker edge cases including cancellation, RefreshTokens failure, SaveTokens failure, and exception handling
- **FR-004**: Tests MUST verify AuthWorker correctly orchestrates calls to IRefreshTokenService.RefreshTokens and IRefreshTokenService.SaveTokens
- **FR-005**: Tests MUST verify AuthWorker logs appropriate messages at information and error levels
- **FR-006**: Tests MUST verify AuthWorker returns correct exit codes (0 for success, 1 for failure) from ExecuteAsync
- **FR-007**: Tests MUST verify AuthWorker calls IHostApplicationLifetime.StopApplication in the finally block
- **FR-008**: Tests MUST verify RefreshTokenService correctly retrieves secrets from SecretClient
- **FR-009**: Tests MUST verify RefreshTokenService handles missing secrets by throwing NullReferenceException with descriptive message
- **FR-010**: Tests MUST verify RefreshTokenService constructs correct HTTP POST request to Fitbit API with authorization header
- **FR-011**: Tests MUST verify RefreshTokenService handles HTTP error responses (4xx, 5xx) appropriately
- **FR-012**: Tests MUST verify RefreshTokenService correctly deserializes successful JSON responses into RefreshTokenResponse
- **FR-013**: Tests MUST verify RefreshTokenService handles malformed JSON by throwing appropriate exceptions
- **FR-014**: Tests MUST verify SaveTokens calls SecretClient.SetSecretAsync for both RefreshToken and AccessToken
- **FR-015**: Tests MUST verify SaveTokens handles SecretClient failures and propagates exceptions
- **FR-016**: Tests MUST use appropriate mocking frameworks (Moq) and assertion libraries (FluentAssertions) consistent with existing test patterns
- **FR-017**: Tests MUST follow existing naming conventions (e.g., RefreshTokensSuccessfullyWhenValidSecretsAndHttpResponseProvided)

### Functional Requirements - Integration Tests Structure

- **FR-018**: Integration test project MUST be named Biotrackr.Auth.Svc.IntegrationTests and target .NET 9.0
- **FR-019**: Integration test project MUST include packages: xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0
- **FR-020**: Integration test project MUST organize tests into Contract/ and E2E/ namespaces matching Weight Service pattern
- **FR-021**: Integration test project MUST include Fixtures/ folder with separate ContractTestFixture (no external service initialization) and IntegrationTestFixture (with mocked services) following Weight API pattern
- **FR-022**: Integration test project MUST include Collections/ folder for xUnit collection definitions (ContractTestCollection, IntegrationTestCollection)
- **FR-023**: Integration test project MUST include Helpers/ folder for test utility code (TestDataGenerator, MockHttpMessageHandler helper, etc.)
- **FR-024**: Integration test project MUST include appsettings.Test.json with test configuration values

### Functional Requirements - Contract Tests

- **FR-025**: Contract tests MUST verify all services can be resolved from the dependency injection container
- **FR-026**: Contract tests MUST verify service lifetimes: Singleton (SecretClient), Transient (IRefreshTokenService registered via AddHttpClient)
- **FR-027**: Contract tests MUST verify HttpClient registration for RefreshTokenService includes AddStandardResilienceHandler
- **FR-028**: Contract tests MUST verify the application host builds successfully without runtime exceptions
- **FR-029**: Contract tests MUST run without external dependencies (no Key Vault, no network calls)
- **FR-030**: Contract tests MUST execute quickly (entire suite under 5 seconds)
- **FR-031**: Contract tests MUST use in-memory configuration to provide required settings
- **FR-032**: Contract tests MUST use ContractTestFixture with external service initialization disabled for faster execution
- **FR-033**: Contract tests MUST verify duplicate service registration pattern is avoided (only AddHttpClient registration for RefreshTokenService, not both AddScoped and AddHttpClient)

### Functional Requirements - E2E Tests

- **FR-034**: E2E tests MUST use mocked SecretClient to simulate Azure Key Vault operations
- **FR-035**: E2E tests MUST use mocked HttpMessageHandler to simulate Fitbit API responses
- **FR-036**: E2E tests MUST verify RefreshTokenService.RefreshTokens retrieves secrets, calls Fitbit API, and returns parsed RefreshTokenResponse
- **FR-037**: E2E tests MUST verify RefreshTokenService.SaveTokens persists both tokens to SecretClient
- **FR-038**: E2E tests MUST verify AuthWorker end-to-end workflow orchestrates RefreshTokens, SaveTokens, logging, and application shutdown
- **FR-039**: E2E tests MUST verify error scenarios including missing secrets, HTTP errors, JSON deserialization failures, and SecretClient save failures
- **FR-040**: E2E tests MUST use xUnit Collection Fixtures to share test infrastructure across tests
- **FR-041**: E2E tests MUST use IntegrationTestFixture with mocked service initialization enabled; no cleanup methods required as fixtures use stateless mocks configured per-test

### Functional Requirements - GitHub Actions Workflow

- **FR-042**: Workflow MUST include separate jobs for unit tests, contract tests, and E2E tests
- **FR-043**: Workflow MUST run contract tests in parallel with unit tests (both are fast and require no external services)
- **FR-044**: Workflow MUST run E2E tests after contract tests complete
- **FR-045**: Workflow MUST NOT require Cosmos DB Emulator (Auth Service does not use Cosmos DB)
- **FR-046**: Workflow MUST publish test results using dorny/test-reporter@v1 with checks: write permission
- **FR-047**: Workflow MUST upload coverage reports as artifacts
- **FR-048**: Workflow MUST use correct working-directory paths for test projects (not solution directories)
- **FR-049**: Workflow MUST use test filters: FullyQualifiedName~Contract for contract tests, FullyQualifiedName~E2E for E2E tests
- **FR-050**: Workflow MUST target .NET 9.0 (DOTNET_VERSION: 9.0.x) matching test project configuration
- **FR-051**: Workflow MUST include coverage-path environment variable for consistent artifact management

### Key Entities

- **AuthWorker**: Background service that orchestrates token refresh workflow; requires testing of dependency injection, async execution flow, error handling, and application lifecycle management; runs once and stops the application in the finally block
- **RefreshTokenService**: Service layer that manages Fitbit token refresh by retrieving stored refresh token from Key Vault, calling Fitbit OAuth2 endpoint, and saving new tokens; requires comprehensive error handling testing
- **SecretClient**: Azure Key Vault client for secure storage of tokens; mocked in integration tests to avoid external dependencies
- **RefreshTokenResponse**: Data model representing Fitbit OAuth2 token response; includes AccessToken, RefreshToken, ExpiresIn, Scope, TokenType, UserType properties
- **ContractTestFixture**: Lightweight test fixture for contract tests that does not initialize external services; extends base fixture with InitializeServices => false property
- **IntegrationTestFixture**: Test infrastructure for E2E tests that manages mocked SecretClient and HttpClient setup; base fixture with InitializeServices => true property
- **Coverage Report**: XML-based coverage report (Cobertura format) tracking line and branch coverage metrics

## Clarifications

### Session 2025-11-03

- Q: After T002 removes duplicate AddScoped registration, what lifetime should ServiceRegistrationTests verify for IRefreshTokenService? → A: Test only Transient lifetime (AddHttpClient pattern) since duplicate registration will be removed
- Q: How should E2E tests handle test isolation between tests sharing IntegrationTestFixture (no Cosmos DB)? → A: No cleanup needed - IntegrationTestFixture uses stateless mocks configured per-test via Setup calls
- Q: If 70% coverage threshold isn't immediately reachable, which components should be prioritized? → A: Prioritize RefreshTokenService tests first (core token refresh logic most critical)
- Q: How should developers manage test secrets/credentials for local development? → A: Hardcode sample/fake values directly in TestDataGenerator helper class (no real secrets needed since all dependencies mocked)

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Overall code coverage for Biotrackr.Auth.Svc project reaches at least 70% line coverage as measured by coverlet
- **SC-002**: Unit test suite executes in under 5 seconds total with 100% pass rate
- **SC-003**: Contract integration tests execute in under 5 seconds total without requiring external dependencies
- **SC-004**: E2E integration tests execute in under 10 seconds total with fully mocked dependencies
- **SC-005**: All test jobs in GitHub Actions workflow complete successfully within 5 minutes
- **SC-006**: Test coverage reports are successfully uploaded as artifacts and published in pull request comments
- **SC-007**: Integration test project structure matches established Weight Service pattern with 100% consistency
- **SC-008**: AuthWorker tests cover at least 4 distinct scenarios: constructor initialization, successful execution, exception handling, and cancellation
- **SC-009**: RefreshTokenService tests cover at least 8 distinct scenarios: successful refresh, missing secrets, HTTP errors, JSON deserialization failures, successful save, save failures, proper HTTP request construction, and authorization header validation
- **SC-010**: Integration tests achieve at least 80% coverage of integration points (worker-to-service, service-to-external-dependencies)

## Assumptions *(optional)*

- Azure Key Vault access is mocked for all tests (no actual Key Vault access required)
- Fitbit API responses are mocked in all integration tests to avoid external API dependencies
- Test data (sample Fitbit tokens, credentials) is hardcoded in TestDataGenerator helper class with fake/sample values since all external dependencies are mocked
- Program.cs is intentionally excluded from coverage as it contains only startup/DI configuration
- Test execution environment has .NET 9.0 SDK available
- Existing unit test patterns and conventions from RefreshTokenServiceShould.cs and AuthWorkerShould.cs are maintained
- The Weight Service integration test structure (003-weight-svc-integration-tests) serves as the authoritative pattern
- Developers have appropriate test data (sample Fitbit tokens, credentials) for local test execution
- Auth Service does not require Cosmos DB, simplifying E2E test setup compared to other services
- HttpClient factory pattern with AddStandardResilienceHandler is already configured in Program.cs
- Service lifetime for RefreshTokenService follows pattern: only AddHttpClient registration (not duplicate AddScoped + AddHttpClient)

## Dependencies *(optional)*

- Existing GitHub Actions reusable workflow templates (template-dotnet-run-unit-tests.yml, template-dotnet-run-contract-tests.yml, template-dotnet-run-e2e-tests.yml)
- Weight Service integration test project as reference implementation
- .NET 9.0 SDK in GitHub Actions runners
- dorny/test-reporter@v1 GitHub Action for test result publishing
- Moq library for mocking SecretClient and HttpMessageHandler
- AutoFixture for generating test data (RefreshTokenResponse objects)

## Out of Scope *(optional)*

- Performance testing or load testing of the Auth Service
- Integration tests with actual Azure Key Vault (mocks used instead)
- Integration tests with actual Fitbit API (mocks used instead)
- Security penetration testing or token encryption/storage security testing
- Token expiration and refresh timing logic (assumes tokens are refreshed on schedule by external scheduler)
- Multi-tenant or multi-user token management (Auth Service manages single user's tokens)
- Cosmos DB integration (Auth Service does not use database)
- Cross-service integration testing (testing interactions between multiple microservices)
- Infrastructure-as-code testing for Bicep templates
- Monitoring and observability testing (Application Insights configuration)
- Rate limiting or retry policy effectiveness testing beyond unit tests with mocks
- OAuth2 grant type variations (only refresh_token grant type is tested)

## References *(optional)*

- [Decision Record: Integration Test Project Structure](../../docs/decision-records/2025-10-28-integration-test-project-structure.md)
- [Decision Record: Contract Test Architecture](../../docs/decision-records/2025-10-28-contract-test-architecture.md)
- [Decision Record: Service Lifetime Registration](../../docs/decision-records/2025-10-28-service-lifetime-registration.md)
- [Decision Record: Program Entry Point Coverage Exclusion](../../docs/decision-records/2025-10-28-program-entry-point-coverage-exclusion.md)
- [Common Resolutions: Service Lifetime & Dependency Injection](.specify/memory/common-resolutions.md)
- [Weight Service Integration Tests Spec](../003-weight-svc-integration-tests/spec.md)
- [Activity Service Tests Spec](../005-activity-svc-tests/spec.md)
- [GitHub Workflow Templates Documentation](../../docs/github-workflow-templates.md)
- [Fitbit OAuth2 Documentation](https://dev.fitbit.com/build/reference/web-api/developer-guide/authorization/)
