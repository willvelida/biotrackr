# Feature Specification: Food Service Test Coverage and Integration Tests

**Feature Branch**: `009-food-svc-tests`  
**Created**: November 3, 2025  
**Status**: Draft  
**Input**: User description: "Extend the unit tests for Biotrackr.Food.Svc so that it meets the code coverage requirements and add Integration tests so that it can be run within the deploy-food-service.yml GitHub Action workflow"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Complete Unit Test Coverage for Missing Components (Priority: P1)

As a developer, I need comprehensive unit tests for all Food Service components to ensure business logic correctness and meet the 70% code coverage threshold.

**Why this priority**: Current unit tests exist for FoodWorker, services, and repository but may have gaps in edge cases and error scenarios. Achieving 70% coverage is foundational for maintaining code quality and preventing regressions.

**Independent Test**: Can be fully tested by running `dotnet test` with coverage collection and verifying that overall line coverage reaches at least 70% across all Food Service components.

**Acceptance Scenarios**:

1. **Given** the existing unit test suite, **When** code coverage is measured, **Then** it should reach at least 70% line coverage for the entire Biotrackr.Food.Svc project
2. **Given** Program.cs contains only DI registration and configuration, **When** coverage exclusions are configured, **Then** Program.cs should be excluded from coverage metrics via [ExcludeFromCodeCoverage] attribute following the established pattern
3. **Given** FoodWorker has comprehensive tests, **When** all edge cases are covered, **Then** test coverage should include cancellation scenarios, empty response handling, and exception paths
4. **Given** service tests exist, **When** reviewing test completeness, **Then** edge cases like malformed JSON, null responses, and network errors should be covered
5. **Given** repository tests exist, **When** testing error scenarios, **Then** Cosmos DB exceptions (rate limiting, network failures, duplicate IDs) should be properly handled

---

### User Story 2 - Integration Test Project Creation (Priority: P2)

As a developer, I need a properly structured integration test project following established patterns so that I can verify the Food Service behaves correctly with realistic dependencies.

**Why this priority**: Integration tests validate that components work together correctly and that external dependencies (Cosmos DB, Fitbit API) are properly integrated. Following the Weight Service and Sleep Service patterns ensures consistency across the codebase.

**Independent Test**: Can be fully tested by creating the project structure, running `dotnet build`, and verifying the project compiles successfully with all required dependencies.

**Acceptance Scenarios**:

1. **Given** the Food Service solution exists, **When** the integration test project is created, **Then** it includes all required testing packages (xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0)
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
2. **Given** services have defined lifetimes, **When** ServiceRegistrationTests verify lifetimes, **Then** Singleton services (CosmosClient, SecretClient) return the same instance, Scoped services (CosmosRepository, FoodService) return the same instance within a scope, and Transient services (FitbitService via AddHttpClient) return different instances
3. **Given** the application can start, **When** testing application bootstrapping, **Then** the host builds successfully without runtime exceptions
4. **Given** configuration is required, **When** testing with in-memory configuration, **Then** all required configuration values (keyvaulturl, managedidentityclientid, cosmosdbendpoint, applicationinsightsconnectionstring) are accessible
5. **Given** duplicate service registrations are a known issue, **When** verifying FitbitService registration, **Then** it should only be registered via AddHttpClient (no duplicate AddScoped registration) following service lifetime guidelines

---

### User Story 4 - E2E Integration Tests with Cosmos DB (Priority: P3)

As a developer, I need end-to-end integration tests that verify data persistence and retrieval with a real Cosmos DB instance so that I can ensure the complete workflow functions correctly.

**Why this priority**: E2E tests validate the full workflow including external dependencies. While important, they are slower and require Cosmos DB Emulator setup, making them lower priority than unit and contract tests.

**Independent Test**: Can be tested by running E2E tests with Cosmos DB Emulator using `dotnet test --filter "FullyQualifiedName~E2E"` and verifying they create, read, and cleanup test data successfully.

**Acceptance Scenarios**:

1. **Given** a test Cosmos DB container is available, **When** CosmosRepositoryTests create a food document, **Then** it persists with the correct structure, partition key (documentType), and can be retrieved by ID
2. **Given** the FoodService orchestrates data operations, **When** FoodServiceTests verify MapAndSaveDocument, **Then** it correctly transforms FoodResponse into FoodDocument and saves it to Cosmos DB
3. **Given** the FoodWorker executes its workflow, **When** FoodWorkerTests run end-to-end, **Then** it retrieves food data from mocked Fitbit service, maps it to documents, and saves to Cosmos DB
4. **Given** E2E tests create test data, **When** each test completes, **Then** the test container is cleaned up to ensure test isolation (no leftover data affects subsequent tests) using ClearContainerAsync pattern
5. **Given** Cosmos DB connection configuration, **When** tests use Gateway mode (ConnectionMode.Gateway), **Then** they work reliably with the local Cosmos DB Emulator without SSL negotiation failures
6. **Given** E2E tests query documents, **When** using FluentAssertions on query results, **Then** they use strongly-typed models (FoodDocument) instead of dynamic types to avoid RuntimeBinderException in CI/CD

---

### User Story 5 - GitHub Actions Workflow Integration (Priority: P3)

As a developer, I need integration tests to run automatically in the GitHub Actions CI/CD pipeline so that test failures are caught before deployment.

**Why this priority**: Automated testing in CI/CD prevents regressions from reaching production. This is important but depends on having working tests first (P1-P2 priorities).

**Independent Test**: Can be tested by triggering the workflow on a pull request and verifying all test jobs (unit, contract, E2E) execute successfully and report results.

**Acceptance Scenarios**:

1. **Given** the deploy-food-service.yml workflow exists, **When** contract tests are configured, **Then** they run in parallel with unit tests without requiring Cosmos DB Emulator
2. **Given** E2E tests require Cosmos DB, **When** the workflow is configured, **Then** the E2E test job includes Cosmos DB Emulator service (mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest) matching the Weight Service pattern
3. **Given** tests complete in the workflow, **When** coverage reports are generated, **Then** they are uploaded as artifacts and published using dorny/test-reporter@v1 with checks: write permission
4. **Given** test failures occur, **When** the workflow runs, **Then** detailed error messages and logs are available in the GitHub Actions summary
5. **Given** the workflow targets .NET 9.0, **When** the integration test project is created, **Then** it targets net9.0 framework matching the DOTNET_VERSION environment variable in deploy-food-service.yml

---

### Edge Cases

- What happens when Fitbit API returns empty food logs for a date?
  - FoodService should handle empty responses gracefully and log appropriately without throwing exceptions
- What happens when Cosmos DB is unavailable during E2E tests?
  - Tests should fail fast with clear error messages about connection issues
- What happens when multiple food documents have the same date?
  - E2E tests must use ClearContainerAsync before each test to ensure isolation and prevent test interference
- What happens when food documents contain null or missing nutritional values?
  - Service should handle optional fields gracefully and not throw NullReferenceException
- What happens when test coverage is calculated with Program.cs included?
  - Program.cs should have [ExcludeFromCodeCoverage] attribute to prevent it from affecting coverage metrics

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Unit tests MUST achieve at least 70% line coverage for Biotrackr.Food.Svc project (excluding Program.cs)
- **FR-002**: Integration test project MUST follow the established folder structure (Contract/, E2E/, Fixtures/, Collections/, Helpers/)
- **FR-003**: Contract tests MUST verify all service registrations (Singleton: CosmosClient, SecretClient; Scoped: ICosmosRepository, IFoodService; Transient: IFitbitService)
- **FR-004**: Contract tests MUST verify service lifetimes using scope-based testing patterns from established services
- **FR-005**: E2E tests MUST use ConnectionMode.Gateway for Cosmos DB Emulator compatibility
- **FR-006**: E2E tests MUST use strongly-typed models (FoodDocument) instead of dynamic types when querying Cosmos DB
- **FR-007**: E2E tests MUST implement ClearContainerAsync method to ensure test isolation
- **FR-008**: Program.cs MUST have [ExcludeFromCodeCoverage] attribute to exclude it from coverage metrics
- **FR-009**: Integration test project MUST target net9.0 framework to match deploy-food-service.yml DOTNET_VERSION
- **FR-010**: Integration test project MUST include required test packages: xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0
- **FR-011**: deploy-food-service.yml workflow MUST include separate jobs for contract tests and E2E tests with appropriate test filters
- **FR-012**: E2E test job MUST include Cosmos DB Emulator service using mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest image
- **FR-013**: Workflow MUST have checks: write permission for test reporter action
- **FR-014**: All tests MUST use appsettings.Test.json for configuration with test-specific connection strings
- **FR-015**: FitbitService MUST only be registered via AddHttpClient (remove any duplicate AddScoped registration)

### Key Entities

- **FoodDocument**: Represents persisted food data in Cosmos DB with properties like id, date, documentType, userId, goals, summary, foods (collection of LoggedFood entries)
- **FoodResponse**: Fitbit API response containing food log data for a specific date
- **TestDataGenerator**: Helper class to generate realistic test data for FoodDocument and FoodResponse entities
- **IntegrationTestFixture**: Configures Cosmos DB Emulator connection for E2E tests with Gateway mode
- **ContractTestFixture**: Configures in-memory services for contract tests without external dependencies
- **IntegrationTestCollection**: xUnit collection fixture for sharing E2E test context across tests
- **ContractTestCollection**: xUnit collection fixture for sharing contract test context across tests

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Unit test coverage reaches at least 70% line coverage for Biotrackr.Food.Svc project (excluding Program.cs)
- **SC-002**: All contract tests execute in under 5 seconds total without external dependencies
- **SC-003**: E2E tests successfully create, read, and cleanup test data from Cosmos DB Emulator
- **SC-004**: GitHub Actions workflow executes all test types (unit, contract, E2E) and reports results without failures
- **SC-005**: Test suite includes at least 4 comprehensive FoodWorker tests covering success, cancellation, exception, and empty response scenarios
- **SC-006**: Integration test project structure matches established patterns (Weight, Activity, Sleep services) for consistency
- **SC-007**: Zero RuntimeBinderException errors occur in CI/CD when running E2E tests (validates strongly-typed model usage)
- **SC-008**: All E2E tests demonstrate proper isolation (no test data leakage between test runs)

## Dependencies & Assumptions

### Dependencies

- .NET 9.0 SDK must be installed
- Cosmos DB Emulator (mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest) required for E2E tests
- GitHub Actions workflow must have required secrets configured (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID, AZURE_RG_NAME_DEV)
- Existing unit test project (Biotrackr.Food.Svc.UnitTests) must remain functional

### Assumptions

- Follow established patterns from Weight Service (003-weight-svc-integration-tests), Activity Service (005-activity-svc-tests), and Sleep Service (007-sleep-svc-tests)
- Program.cs should be excluded from coverage using [ExcludeFromCodeCoverage] attribute (not coverlet.msbuild configuration)
- Service lifetime pattern: Singleton for Azure SDK clients, Scoped for application services, Transient for HttpClient-based services
- Contract tests run in parallel with unit tests; E2E tests run after contract tests complete
- Test isolation is critical—use ClearContainerAsync pattern for E2E tests
- FoodService already has duplicate registration issue that needs fixing (AddScoped + AddHttpClient)
- deploy-food-service.yml workflow already exists and needs test job additions

## Scope

### In Scope

- Extending unit tests to achieve 70% code coverage
- Creating integration test project with Contract and E2E tests
- Updating deploy-food-service.yml workflow to run integration tests
- Fixing duplicate FitbitService registration in Program.cs
- Adding [ExcludeFromCodeCoverage] attribute to Program.cs
- Creating test fixtures, collections, and helpers following established patterns
- Implementing ClearContainerAsync for E2E test isolation
- Using strongly-typed models in E2E tests to avoid RuntimeBinderException

### Out of Scope

- Modifying existing Food Service business logic or API endpoints
- Adding new features to Food Service
- Performance testing or load testing
- Refactoring existing unit tests (unless required for coverage goals)
- Testing non-Cosmos DB data persistence mechanisms
- Testing actual Fitbit API integration (E2E tests use mocked Fitbit service)
- Deploying to production environments (only dev environment workflow updates)

## Notes

- Follow decision records in docs/decision-records/ for guidance on:
  - Service lifetime registration (2025-10-28-service-lifetime-registration.md)
  - Integration test project structure (2025-10-28-integration-test-project-structure.md)
  - Program entry point coverage exclusion (2025-10-28-program-entry-point-coverage-exclusion.md)
- Refer to .specify/memory/common-resolutions.md for known issues and solutions:
  - RuntimeBinderException with dynamic types and FluentAssertions
  - E2E test isolation with ClearContainerAsync
  - GitHub Actions workflow permissions and test filters
  - Service lifetime registration patterns
- FoodDocument structure includes Goals and Summary entities with nutritional values (calories, carbs, protein, fat, fiber, sodium)
- LoggedFood entities include detailed information about each food item logged during the day
- Unit type information is captured in Unit entities with id, name, and plural naming conventions
