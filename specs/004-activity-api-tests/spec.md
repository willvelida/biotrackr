# Feature Specification: Enhanced Test Coverage for Activity API

**Feature Branch**: `004-activity-api-tests`  
**Created**: 2025-10-29  
**Status**: Draft  
**Input**: User description: "Increase the unit test code coverage in the Biotrackr.Activity.Api and implement integration tests in the same way that we did for the Biotrackr.Weight.Api"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Comprehensive Unit Test Coverage (Priority: P1)

Development teams need comprehensive unit test coverage for the Activity API to ensure code quality and reliability. The current unit test coverage needs to be increased to meet the constitutional requirement of 80% coverage across all code components.

**Why this priority**: This is the foundation for code quality assurance and is mandated by the project constitution. Without adequate unit test coverage, the codebase becomes fragile and error-prone.

**Independent Test**: Can be fully tested by running unit tests with coverage reporting and verifying that coverage meets or exceeds 80% threshold across all code components.

**Acceptance Scenarios**:

1. **Given** the Activity API codebase, **When** unit tests are executed with coverage analysis, **Then** overall code coverage should be ≥80%
2. **Given** any code component (handlers, repositories, models, extensions, configuration), **When** unit tests are run, **Then** each component should have comprehensive test coverage including edge cases
3. **Given** exception scenarios and error conditions, **When** unit tests are executed, **Then** all error handling paths should be tested and verified

---

### User Story 2 - Integration Test Implementation (Priority: P2)

Development teams need integration tests that verify the Activity API works correctly with external dependencies (Cosmos DB, Azure services) and can be executed in automated CI/CD pipelines, following the same patterns established in the Weight API integration tests.

**Why this priority**: Integration tests ensure that components work together correctly and catch issues that unit tests might miss. They are essential for confident deployments and consistency across microservices.

**Independent Test**: Can be fully tested by running integration tests in a controlled environment and verifying they pass consistently in GitHub Actions workflows.

**Acceptance Scenarios**:

1. **Given** the Activity API deployed in a test environment, **When** integration tests are executed, **Then** all API endpoints should function correctly with real dependencies
2. **Given** a GitHub Actions workflow, **When** the workflow is triggered, **Then** integration tests should run automatically and report results
3. **Given** contract tests (smoke tests, startup tests), **When** executed, **Then** they should verify service registration, configuration, and basic application startup without database dependencies
4. **Given** E2E tests with database dependencies, **When** executed, **Then** they should verify full endpoint functionality with real Cosmos DB interactions

---

### User Story 3 - CI/CD Test Automation (Priority: P3)

Development teams need automated test execution in GitHub Actions workflows to ensure continuous quality assurance and prevent regressions from reaching production, with consistent patterns across all microservices.

**Why this priority**: Automated testing in CI/CD pipelines ensures consistent quality gates and enables confident continuous deployment practices across all Biotrackr microservices.

**Independent Test**: Can be fully tested by triggering GitHub Actions workflows and verifying that all tests execute successfully with proper reporting and failure handling.

**Acceptance Scenarios**:

1. **Given** a pull request or code push, **When** GitHub Actions workflows are triggered, **Then** all unit and integration tests should execute automatically
2. **Given** test failures in the CI/CD pipeline, **When** the workflow completes, **Then** clear failure reports should be provided with actionable information
3. **Given** successful test execution, **When** the workflow completes, **Then** test coverage reports should be generated and made available
4. **Given** integration tests requiring infrastructure, **When** executed in CI/CD, **Then** they should use appropriate test doubles or containerized dependencies

---

### Edge Cases

- What happens when Cosmos DB is unavailable during integration tests?
- How does the system handle malformed date formats in various endpoints?
- What occurs when pagination parameters exceed reasonable limits (e.g., continuationToken corruption, negative page sizes)?
- How are null or empty responses from the repository handled in endpoint handlers?
- What happens when Azure App Configuration is unavailable during startup?
- How does the API handle invalid or missing activity data from Fitbit entities?
- What occurs when heart rate zone data is incomplete or malformed?
- How are distance measurements handled when units are missing or invalid?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Unit tests MUST achieve ≥80% code coverage across all Activity API components (handlers, repositories, models, extensions, configuration)
- **FR-002**: Unit tests MUST cover all public methods, properties, and constructors in the Activity API codebase
- **FR-003**: Unit tests MUST include comprehensive error handling scenarios for all exception types
- **FR-004**: Unit tests MUST validate all edge cases including boundary conditions, null inputs, and invalid data formats
- **FR-005**: Unit tests MUST cover all Fitbit entity models (Activity, ActivityResponse, Distance, Goals, HeartRateZone, Summary)
- **FR-006**: Unit tests MUST validate Settings configuration including DatabaseName, ContainerName properties
- **FR-007**: Unit tests MUST verify CosmosRepository operations with mocked Cosmos SDK calls
- **FR-008**: Unit tests MUST test ActivityHandlers endpoint logic with various request scenarios
- **FR-009**: Unit tests MUST validate PaginationRequest model validation and edge cases
- **FR-010**: Unit tests MUST verify EndpointRouteBuilderExtensions register endpoints correctly
- **FR-011**: Integration tests MUST be organized into Contract and E2E test categories following Weight API patterns
- **FR-012**: Contract tests MUST verify application startup, service registration, and configuration without database dependencies
- **FR-013**: Contract tests MUST include ProgramStartupTests verifying CosmosClient, ICosmosRepository, Settings, and HealthChecks registration
- **FR-014**: Contract tests MUST verify API smoke tests (health checks, swagger endpoints)
- **FR-015**: E2E integration tests MUST verify end-to-end functionality of all Activity API endpoints with real dependencies
- **FR-016**: Integration tests MUST validate API responses, status codes, and data integrity in realistic scenarios
- **FR-017**: Integration tests MUST verify database operations (read, write, query) work correctly with Cosmos DB
- **FR-018**: Integration tests MUST use test fixtures (ContractTestFixture, IntegrationTestFixture) following established patterns
- **FR-019**: Integration tests MUST use WebApplicationFactory with proper configuration overrides for test environment
- **FR-020**: Integration tests MUST use xUnit collection fixtures to manage test lifecycle and resource sharing
- **FR-021**: Test execution MUST complete within reasonable time limits (unit tests <5 minutes, integration tests <15 minutes)

### Key Entities *(include if feature involves data)*

- **Unit Test Suite**: Comprehensive collection of tests covering all code paths, edge cases, and error scenarios across handlers, repositories, models, extensions, and configuration
- **Contract Test Suite**: Lightweight integration tests that verify application startup, service registration, and basic API functionality without database dependencies
- **E2E Test Suite**: Full integration tests that verify API functionality with real dependencies including Cosmos DB
- **Integration Test Fixtures**: Shared test infrastructure (ContractTestFixture, IntegrationTestFixture, ActivityApiWebApplicationFactory) that manage application lifecycle and resource initialization
- **Coverage Report**: Detailed analysis of code coverage metrics showing tested and untested code paths
- **Test Collections**: xUnit collection fixtures for managing shared test context and proper test isolation

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Unit test coverage increases to ≥80% across all Activity API components
- **SC-002**: All unit tests execute successfully within 5 minutes in local and CI/CD environments
- **SC-003**: Integration tests (Contract + E2E) execute successfully within 15 minutes in GitHub Actions workflows
- **SC-004**: Contract tests execute independently without requiring Cosmos DB or external dependencies
- **SC-005**: E2E tests successfully interact with test Cosmos DB instance and verify data integrity
- **SC-006**: Zero test failures in CI/CD pipeline for valid code changes
- **SC-007**: Test coverage reports are generated and accessible for every build
- **SC-008**: Integration test structure mirrors Weight API patterns for consistency across microservices
- **SC-009**: All Fitbit entity models have comprehensive unit test coverage including edge cases
- **SC-010**: GitHub Actions workflows provide clear pass/fail status and diagnostic information within 30 seconds of completion

## Assumptions

- Azure test environment resources (Cosmos DB, App Configuration) are available for integration testing
- GitHub Actions has necessary permissions and secrets configured for Azure resource access
- Existing unit test framework (xUnit, FluentAssertions, Moq) will continue to be used
- Integration tests will use separate test databases to avoid impacting production data
- Test data cleanup will be handled automatically after integration test execution
- Current API endpoints and data models will remain stable during test implementation
- Weight API integration test patterns (fixtures, collections, WebApplicationFactory) are proven and should be replicated
- Cosmos DB Emulator or test instance is available for E2E integration tests
- Test configuration uses appsettings.Test.json to override production settings
- Service lifetime patterns established in Weight API (Singleton for CosmosClient, Transient for repositories) apply to Activity API
