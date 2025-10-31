# Feature Specification: Sleep Service Test Coverage and Integration Tests

**Feature Branch**: `007-sleep-svc-tests`  
**Created**: October 31, 2025  
**Status**: Implemented  
**Input**: User description: "Extend the unit tests for Biotrackr.Sleep.Svc so that they meet the code coverage requirements and add Integration Tests that can be run within the GitHub Action workflow"

**Implementation Notes**:
- **Completion Date**: October 31, 2025
- **Final Test Count**: 23 tests total (13 unit + 5 contract + 5 E2E)
- **Final Coverage**: 97.69% line coverage (exceeds 70% requirement by 27.69%)
- **Test Execution Times**:
  - Unit tests: 1.9s (13 tests passed, 100% success rate)
  - Contract tests: 0.8s (5 tests passed, 100% success rate)
  - E2E tests: Verified in CI/CD (ARM64 local incompatibility with Cosmos DB Emulator)
- **Key Fixes Applied**:
  - Added `[ExcludeFromCodeCoverage]` attribute to Program.cs
  - Removed duplicate `IFitbitService` registration
  - Created 4 comprehensive SleepWorker tests covering all scenarios
  - Added 5 contract tests for DI validation
  - Added 5 E2E tests for full workflow validation
  - Fixed TestDataGenerator to match actual entity structure
  - Added appsettings.Test.json copy directive to .csproj
- **Pattern Consistency**: Follows Weight Service (003-weight-svc-integration-tests) and Activity Service (005-activity-svc-tests) patterns

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Complete Unit Test Coverage for Missing Components (Priority: P1)

As a developer, I need comprehensive unit tests for all Sleep Service components to ensure business logic correctness and meet the 70% code coverage threshold.

**Why this priority**: Current unit tests exist for services and repository but lack tests for the SleepWorker component and edge cases. Achieving 70% coverage is foundational for maintaining code quality and preventing regressions.

**Independent Test**: Can be fully tested by running `dotnet test` with coverage collection and verifying that overall line coverage reaches at least 70% across all Sleep Service components.

**Acceptance Scenarios**:

1. **Given** the existing unit test suite, **When** code coverage is measured, **Then** it should reach at least 70% line coverage for the entire Biotrackr.Sleep.Svc project
2. **Given** Program.cs contains only DI registration and configuration, **When** coverage exclusions are configured, **Then** Program.cs should be excluded from coverage metrics via [ExcludeFromCodeCoverage] attribute
3. **Given** SleepWorker requires testing, **When** creating unit tests, **Then** they should cover constructor initialization, successful execution, exception handling, and cancellation scenarios
4. **Given** service tests exist, **When** reviewing test completeness, **Then** edge cases like malformed JSON, null responses, and network errors should be covered
5. **Given** repository tests exist, **When** testing error scenarios, **Then** Cosmos DB exceptions (rate limiting, network failures, duplicate IDs) should be properly handled

---

### User Story 2 - Integration Test Project Creation (Priority: P2)

As a developer, I need a properly structured integration test project following established patterns so that I can verify the Sleep Service behaves correctly with realistic dependencies.

**Why this priority**: Integration tests validate that components work together correctly and that external dependencies (Cosmos DB, Fitbit API) are properly integrated. Following the Weight Service pattern ensures consistency across the codebase.

**Independent Test**: Can be fully tested by creating the project structure, running `dotnet build`, and verifying the project compiles successfully with all required dependencies.

**Acceptance Scenarios**:

1. **Given** the Sleep Service solution exists, **When** the integration test project is created, **Then** it includes all required testing packages (xUnit, FluentAssertions, Moq, AutoFixture, coverlet.collector, Microsoft.AspNetCore.Mvc.Testing)
2. **Given** the integration test project structure is defined, **When** reviewing the folder layout, **Then** Contract/, E2E/, Fixtures/, Collections/, and Helpers/ folders exist matching the Weight Service pattern
3. **Given** the test project is configured, **When** tests are discovered, **Then** the test runner recognizes all test classes and methods across both Contract and E2E namespaces
4. **Given** integration tests require configuration, **When** appsettings.Test.json is created, **Then** it contains test-specific connection strings and settings for local Cosmos DB Emulator

---

### User Story 3 - Contract Integration Tests (Priority: P2)

As a developer, I need contract integration tests that verify dependency injection and service registration without external dependencies so that I can run fast tests in parallel with unit tests.

**Why this priority**: Contract tests validate the "contract" of the application—that services are properly registered and the application can start. These tests are fast and can run in parallel with unit tests without requiring Cosmos DB Emulator.

**Independent Test**: Can be tested by running contract tests in isolation using `dotnet test --filter "FullyQualifiedName~Contract"` and verifying they execute quickly (under 5 seconds total) without external dependencies.

**Acceptance Scenarios**:

1. **Given** the application uses dependency injection, **When** ProgramStartupTests verify service registration, **Then** all required services (CosmosClient, SecretClient, repositories, services) can be resolved from the service provider
2. **Given** services have defined lifetimes, **When** ServiceRegistrationTests verify lifetimes, **Then** Singleton services (CosmosClient, SecretClient) return the same instance, Scoped services return the same instance within a scope, and Transient services (FitbitService) return different instances
3. **Given** the application can start, **When** testing application bootstrapping, **Then** the host builds successfully without runtime exceptions
4. **Given** configuration is required, **When** testing with in-memory configuration, **Then** all required configuration values (keyvaulturl, managedidentityclientid, cosmosdbendpoint, applicationinsightsconnectionstring) are accessible

---

### User Story 4 - E2E Integration Tests with Cosmos DB (Priority: P3)

As a developer, I need end-to-end integration tests that verify data persistence and retrieval with a real Cosmos DB instance so that I can ensure the complete workflow functions correctly.

**Why this priority**: E2E tests validate the full workflow including external dependencies. While important, they are slower and require Cosmos DB Emulator setup, making them lower priority than unit and contract tests.

**Independent Test**: Can be tested by running E2E tests with Cosmos DB Emulator using `dotnet test --filter "FullyQualifiedName~E2E"` and verifying they create, read, and cleanup test data successfully.

**Acceptance Scenarios**:

1. **Given** a test Cosmos DB container is available, **When** CosmosRepositoryTests create a sleep document, **Then** it persists with the correct structure, partition key (documentType), and can be retrieved by ID
2. **Given** the SleepService orchestrates data operations, **When** SleepServiceTests verify MapAndSaveDocument, **Then** it correctly transforms SleepResponse into SleepDocument and saves it to Cosmos DB
3. **Given** the SleepWorker executes its workflow, **When** SleepWorkerTests run end-to-end, **Then** it retrieves sleep data from mocked Fitbit service, maps it to documents, and saves to Cosmos DB
4. **Given** E2E tests create test data, **When** each test completes, **Then** the test container is cleaned up to ensure test isolation (no leftover data affects subsequent tests)
5. **Given** Cosmos DB connection configuration, **When** tests use Gateway mode (ConnectionMode.Gateway), **Then** they work reliably with the local Cosmos DB Emulator without SSL negotiation failures

---

### User Story 5 - GitHub Actions Workflow Integration (Priority: P3)

As a developer, I need integration tests to run automatically in the GitHub Actions CI/CD pipeline so that test failures are caught before deployment.

**Why this priority**: Automated testing in CI/CD prevents regressions from reaching production. This is important but depends on having working tests first (P1-P2 priorities).

**Independent Test**: Can be tested by triggering the workflow on a pull request and verifying all test jobs (unit, contract, E2E) execute successfully and report results.

**Acceptance Scenarios**:

1. **Given** the deploy-sleep-service.yml workflow exists, **When** contract tests are configured, **Then** they run in parallel with unit tests without requiring Cosmos DB Emulator
2. **Given** E2E tests require Cosmos DB, **When** the workflow is configured, **Then** the E2E test job includes Cosmos DB Emulator service (mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest) matching the Weight Service pattern
3. **Given** tests complete in the workflow, **When** coverage reports are generated, **Then** they are uploaded as artifacts and published using dorny/test-reporter@v1 with checks: write permission
4. **Given** test failures occur, **When** the workflow runs, **Then** detailed error messages and logs are available in the GitHub Actions summary
5. **Given** the workflow uses reusable templates, **When** test jobs are defined, **Then** they use correct working-directory paths (e.g., ./src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.UnitTests for unit tests, not the solution directory)

---

### Edge Cases

- What happens when SleepWorker is cancelled mid-execution via CancellationToken?
- How does the system handle empty sleep responses from Fitbit API?
- What occurs when Cosmos DB connection fails during E2E test setup?
- How does the system behave when attempting to create duplicate sleep documents with the same ID?
- What happens if the Fitbit API returns malformed JSON that fails deserialization?
- How does the system handle network timeouts when calling the Fitbit API?
- What occurs when the mocked SecretClient fails to return an access token?
- How does the service handle invalid date formats in sleep data?
- What happens when E2E tests run concurrently and try to access the same test container?
- How does the system behave when IHostApplicationLifetime.StopApplication is called multiple times?

## Requirements *(mandatory)*

### Functional Requirements - Unit Tests

- **FR-001**: Test suite MUST achieve at least 70% overall line coverage for Biotrackr.Sleep.Svc project
- **FR-002**: Coverage configuration MUST exclude Program.cs from coverage calculations using [ExcludeFromCodeCoverage] attribute
- **FR-003**: Tests MUST cover SleepWorker component including constructor, ExecuteAsync success path, exception handling, and cancellation
- **FR-004**: Tests MUST verify SleepWorker correctly orchestrates calls to IFitbitService.GetSleepResponse and ISleepService.MapAndSaveDocument
- **FR-005**: Tests MUST verify SleepWorker logs appropriate messages at information and error levels
- **FR-006**: Tests MUST verify SleepWorker returns correct exit codes (0 for success, 1 for failure)
- **FR-007**: Tests MUST verify SleepWorker calls IHostApplicationLifetime.StopApplication in the finally block
- **FR-008**: Tests MUST use appropriate mocking frameworks (Moq) and assertion libraries (FluentAssertions) consistent with existing test patterns
- **FR-009**: Tests MUST follow existing naming conventions (e.g., MethodName_Should_ExpectedBehavior or ExecuteAsync_ShouldReturn0_WhenSuccessful)

### Functional Requirements - Integration Tests Structure

- **FR-010**: Integration test project MUST be named Biotrackr.Sleep.Svc.IntegrationTests and target .NET 9.0
- **FR-011**: Integration test project MUST include packages: xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0
- **FR-012**: Integration test project MUST organize tests into Contract/ and E2E/ namespaces matching Weight Service pattern
- **FR-013**: Integration test project MUST include Fixtures/ folder with separate ContractTestFixture (no database initialization) and IntegrationTestFixture (with database initialization) following Weight Service pattern
- **FR-014**: Integration test project MUST include Collections/ folder for xUnit collection definitions (ContractTestCollection, IntegrationTestCollection)
- **FR-015**: Integration test project MUST include Helpers/ folder for test utility code (TestDataGenerator, etc.)
- **FR-016**: Integration test project MUST include appsettings.Test.json with Cosmos DB Emulator connection strings

### Functional Requirements - Contract Tests

- **FR-017**: Contract tests MUST verify all services can be resolved from the dependency injection container
- **FR-018**: Contract tests MUST verify service lifetimes: Singleton (CosmosClient, SecretClient), Scoped (repositories, services), Transient (FitbitService via AddHttpClient)
- **FR-019**: Contract tests MUST verify the application host builds successfully without runtime exceptions
- **FR-020**: Contract tests MUST run without external dependencies (no Cosmos DB, no Key Vault, no network calls)
- **FR-021**: Contract tests MUST execute quickly (entire suite under 5 seconds)
- **FR-022**: Contract tests MUST use in-memory configuration to provide required settings
- **FR-023**: Contract tests MUST use ContractTestFixture with database initialization disabled for faster execution

### Functional Requirements - E2E Tests

- **FR-024**: E2E tests MUST use Azure Cosmos DB Emulator for data persistence testing
- **FR-025**: E2E tests MUST configure CosmosClient with ConnectionMode.Gateway to avoid SSL negotiation issues with local emulator
- **FR-026**: E2E tests MUST create dedicated test database and container during fixture initialization
- **FR-027**: E2E tests MUST clean up test data after each test to ensure test isolation
- **FR-028**: E2E tests MUST verify CosmosRepository can create, read, and query sleep documents
- **FR-029**: E2E tests MUST verify SleepService.MapAndSaveDocument correctly transforms and persists data
- **FR-030**: E2E tests MUST verify SleepWorker end-to-end workflow with mocked Fitbit service and real Cosmos DB
- **FR-031**: E2E tests MUST use xUnit Collection Fixtures to share Cosmos DB connection across tests
- **FR-032**: E2E tests MUST handle Cosmos DB Emulator startup delays and connection retries
- **FR-033**: E2E tests MUST use IntegrationTestFixture with database initialization enabled
- **FR-034**: Flaky tests that consistently fail in CI but pass locally MUST be removed from the test suite rather than skipped or disabled

### Functional Requirements - GitHub Actions Workflow

- **FR-035**: Workflow MUST include separate jobs for unit tests, contract tests, and E2E tests
- **FR-036**: Workflow MUST run contract tests in parallel with unit tests (both are fast and require no external services)
- **FR-037**: Workflow MUST run E2E tests after contract tests complete (E2E tests require Cosmos DB Emulator)
- **FR-038**: Workflow MUST configure Cosmos DB Emulator service for E2E tests using mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
- **FR-039**: Workflow MUST publish test results using dorny/test-reporter@v1 with checks: write permission
- **FR-040**: Workflow MUST upload coverage reports as artifacts
- **FR-041**: Workflow MUST use correct working-directory paths for test projects (not solution directories)
- **FR-042**: Workflow MUST use test filters: FullyQualifiedName~Contract for contract tests, FullyQualifiedName~E2E for E2E tests
- **FR-043**: Workflow MUST target .NET 9.0 (DOTNET_VERSION: 9.0.x) matching test project configuration

### Key Entities

- **SleepWorker**: Background service that orchestrates sleep data synchronization; requires testing of dependency injection, async execution flow, error handling, and application lifecycle management
- **SleepService**: Service layer that transforms and saves sleep data; requires integration testing with Cosmos DB
- **CosmosRepository**: Data access layer for Cosmos DB operations; requires E2E testing with real Cosmos DB Emulator
- **FitbitService**: External API client for Fitbit; mocked in integration tests to avoid external dependencies
- **ContractTestFixture**: Lightweight test fixture for contract tests that does not initialize database; extends base fixture with InitializeDatabase => false property
- **IntegrationTestFixture**: Full test infrastructure for E2E tests that manages Cosmos DB Emulator connection, test database/container creation, and cleanup; base fixture with InitializeDatabase => true property
- **Test Container**: Dedicated Cosmos DB container for integration tests, cleaned between test runs to ensure isolation
- **Coverage Report**: XML-based coverage report (Cobertura format) tracking line and branch coverage metrics

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Overall code coverage for Biotrackr.Sleep.Svc project reaches at least 70% line coverage as measured by coverlet
- **SC-002**: Unit test suite executes in under 5 seconds total with 100% pass rate
- **SC-003**: Contract integration tests execute in under 5 seconds total without requiring external dependencies
- **SC-004**: E2E integration tests execute in under 30 seconds total with Cosmos DB Emulator
- **SC-005**: All test jobs in GitHub Actions workflow complete successfully within 10 minutes
- **SC-006**: Test coverage reports are successfully uploaded as artifacts and published in pull request comments
- **SC-007**: Integration test project structure matches established Weight Service pattern with 100% consistency
- **SC-008**: E2E tests demonstrate 100% test isolation (no test failures due to leftover data from previous tests)
- **SC-009**: SleepWorker tests cover at least 4 distinct scenarios: constructor initialization, successful execution, exception handling, and cancellation
- **SC-010**: Integration tests achieve at least 80% coverage of integration points (service-to-repository, worker-to-services)

## Assumptions *(optional)*

- Cosmos DB Emulator is available and functional in the GitHub Actions environment
- Test execution environment has sufficient resources to run Cosmos DB Emulator container
- Developers have local Cosmos DB Emulator installed for local integration test execution
- Existing unit test patterns and conventions from Sleep Service tests are maintained
- The Weight Service integration test structure (003-weight-svc-integration-tests) serves as the authoritative pattern
- Program.cs is intentionally excluded from coverage as it contains only startup/DI configuration
- Azure Key Vault integration is mocked for tests (no actual Key Vault access required)
- Fitbit API responses are mocked in all integration tests to avoid external API dependencies
- Test containers use partition key "documentType" consistent with production schema
- E2E tests run sequentially within a collection to share Cosmos DB connection efficiently

## Dependencies *(optional)*

- Azure Cosmos DB Emulator Docker image (mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest)
- Existing GitHub Actions reusable workflow templates (template-dotnet-run-unit-tests.yml, template-dotnet-run-contract-tests.yml, template-dotnet-run-e2e-tests.yml)
- Weight Service integration test project as reference implementation
- .NET 9.0 SDK in GitHub Actions runners
- dorny/test-reporter@v1 GitHub Action for test result publishing

## Out of Scope *(optional)*

- Performance testing or load testing of the Sleep Service
- Integration tests with actual Azure Key Vault (mocks used instead)
- Integration tests with actual Fitbit API (mocks used instead)
- UI/acceptance testing (not applicable for background service)
- Cross-service integration testing (testing interactions between multiple microservices)
- Database migration or schema evolution testing
- Security penetration testing or vulnerability scanning
- Infrastructure-as-code testing for Bicep templates
- Monitoring and observability testing (OpenTelemetry configuration)
- Rate limiting or retry policy effectiveness testing (covered by unit tests with mocks)

## References *(optional)*

- [Decision Record: Integration Test Project Structure](../../docs/decision-records/2025-10-28-integration-test-project-structure.md)
- [Decision Record: Contract Test Architecture](../../docs/decision-records/2025-10-28-contract-test-architecture.md)
- [Decision Record: Service Lifetime Registration](../../docs/decision-records/2025-10-28-service-lifetime-registration.md)
- [Decision Record: Program Entry Point Coverage Exclusion](../../docs/decision-records/2025-10-28-program-entry-point-coverage-exclusion.md)
- [Decision Record: Flaky Test Handling](../../docs/decision-records/2025-10-28-flaky-test-handling.md)
- [Common Resolutions: E2E Test Issues](.specify/memory/common-resolutions.md)
- [Weight Service Integration Tests Spec](../003-weight-svc-integration-tests/spec.md)
- [Activity Service Tests Spec](../005-activity-svc-tests/spec.md)
- [GitHub Workflow Templates Documentation](../../docs/github-workflow-templates.md)
- [Cosmos DB Emulator Setup Guide](../../docs/cosmos-emulator-setup.md)
