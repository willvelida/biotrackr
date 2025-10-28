# Feature Specification: Weight Service Integration Tests

**Feature Branch**: `003-weight-svc-integration-tests`  
**Created**: October 28, 2025  
**Status**: Draft  
**Input**: User description: "Create integration tests for Biotrackr.Weight.Svc"

## Clarifications

### Session 2025-10-28

- Q: How should integration tests be integrated into the existing GitHub Actions workflow? → A: Add two separate jobs: contract tests run after unit tests, then E2E tests run after contract tests
- Q: Which database testing approach should be used for the Weight Service integration tests? → A: Use Azure Cosmos DB Emulator via GitHub Actions services (matching existing Weight API pattern)
- Q: How should Fitbit API integration be tested? → A: Mock all Fitbit API responses using HttpMessageHandler for predictable, fast tests
- Q: How should Azure Key Vault integration be handled in tests? → A: Mock SecretClient to return test access tokens without connecting to actual Key Vault
- Q: Should integration tests focus on specific components or comprehensive end-to-end scenarios? → A: Focus on end-to-end workflow tests through WeightWorker that exercise all components together

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Integration Test Project Creation (Priority: P1)

As a developer, I need a properly configured integration test project so that I can verify the Weight Service behaves correctly in realistic scenarios with external dependencies.

**Why this priority**: Without the basic test infrastructure, no integration testing can be performed. This is foundational for all subsequent testing efforts.

**Independent Test**: Can be fully tested by running `dotnet test` on the new project and verifying it builds successfully and test discovery works.

**Acceptance Scenarios**:

1. **Given** the Weight Service solution exists, **When** the integration test project is created, **Then** it includes all required testing packages and project references
2. **Given** the integration test project is configured, **When** tests are discovered, **Then** the test runner recognizes all test classes and methods
3. **Given** the test project structure follows the established pattern, **When** reviewing the folder layout, **Then** Contract/, E2E/, Fixtures/, Collections/, and Helpers/ folders exist

---

### User Story 2 - Azure Cosmos DB Integration Testing (Priority: P2)

As a developer, I need to verify data persistence operations work correctly so that I can ensure weight data is properly saved to and retrieved from Cosmos DB.

**Why this priority**: The Weight Service's primary responsibility is data persistence. Verifying this functionality is critical to service reliability.

**Independent Test**: Can be tested by creating test data, saving it through the repository, and verifying it persists correctly in the test container.

**Acceptance Scenarios**:

1. **Given** a test Cosmos DB container is running, **When** a weight document is created, **Then** it persists with the correct structure and partition key
2. **Given** weight documents exist in the container, **When** the service attempts to create a duplicate document with the same ID, **Then** appropriate error handling occurs
3. **Given** the service starts up, **When** connecting to Cosmos DB, **Then** the connection succeeds and the container is accessible

---

### User Story 3 - Fitbit API Integration Testing (Priority: P3)

As a developer, I need to verify external API communication works correctly so that I can ensure the service retrieves weight data from Fitbit successfully.

**Why this priority**: External API integration is important but can be tested with mocked responses in most scenarios. Full integration testing is valuable but lower priority than core data operations.

**Independent Test**: Can be tested by setting up mock HTTP responses and verifying the service handles various API response scenarios correctly.

**Acceptance Scenarios**:

1. **Given** the Fitbit API returns valid weight data, **When** the service requests weight logs for a date range, **Then** the data is correctly deserialized into weight entities
2. **Given** the Fitbit API is unavailable or returns an error, **When** the service attempts to retrieve weight logs, **Then** appropriate error handling and logging occurs
3. **Given** the service requires an access token, **When** authenticating with Fitbit API, **Then** the mocked SecretClient provides the test token and it is correctly included in the request headers

---

### User Story 4 - Background Worker Integration Testing (Priority: P2)

As a developer, I need to verify the hosted service executes its workflow correctly so that I can ensure the end-to-end weight data sync process works as designed.

**Why this priority**: The worker orchestrates the entire service workflow. Testing it ensures all components work together correctly.

**Independent Test**: Can be tested by executing the worker and verifying it retrieves data from Fitbit, transforms it, and saves it to Cosmos DB in the correct sequence.

**Acceptance Scenarios**:

1. **Given** the WeightWorker starts execution, **When** it runs its workflow, **Then** it retrieves weight logs for the past 7 days from Fitbit
2. **Given** weight data is retrieved from Fitbit, **When** the worker processes each weight entry, **Then** it creates and saves a corresponding WeightDocument to Cosmos DB
3. **Given** the worker completes its execution, **When** all data is processed, **Then** the application gracefully shuts down
4. **Given** an exception occurs during worker execution, **When** processing fails, **Then** the error is logged and the application returns a non-zero exit code

---

### Edge Cases

- What happens when Cosmos DB connection fails during startup?
- How does the system handle network timeouts when calling the Fitbit API?
- What occurs when the mocked SecretClient fails to return an access token?
- How does the service behave when Fitbit returns an empty weight array?
- What happens if the worker is cancelled via CancellationToken before completion?
- How does the system handle invalid date formats in weight data?
- What occurs when attempting to save a weight document with missing required fields?
- How does the service handle concurrent execution requests (should be prevented)?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide isolated integration tests that can run independently without affecting production data
- **FR-002**: System MUST use Azure Cosmos DB Emulator via GitHub Actions services for E2E test database, matching the existing Weight API integration test pattern
- **FR-003**: System MUST verify CosmosRepository correctly creates weight documents with proper partition keys and serialization
- **FR-004**: System MUST verify WeightService correctly maps weight entities to WeightDocument format before persistence
- **FR-005**: System MUST verify FitbitService correctly retrieves and deserializes weight data from external API using mocked HTTP responses via HttpMessageHandler
- **FR-006**: System MUST verify WeightWorker orchestrates the complete end-to-end workflow from data retrieval through FitbitService to persistence via WeightService and CosmosRepository
- **FR-007**: System MUST test error handling paths including API failures, database errors, and authentication issues
- **FR-008**: System MUST verify proper cleanup and disposal of test resources after test execution, including Cosmos DB Emulator database and container cleanup
- **FR-009**: System MUST follow the established integration test project structure with Contract/, E2E/, Fixtures/, Collections/, and Helpers/ folders
- **FR-010**: System MUST support both contract tests (no external dependencies) and E2E tests (with test containers)
- **FR-011**: System MUST verify service dependency injection configuration loads correctly
- **FR-012**: System MUST test HTTP communication patterns using mocked responses for success, error, and timeout scenarios
- **FR-013**: System MUST use mocked SecretClient to provide test access tokens for Fitbit API authentication without requiring actual Key Vault connectivity
- **FR-014**: System MUST test the complete date range query functionality for weight log retrieval
- **FR-015**: System MUST verify OpenTelemetry instrumentation does not interfere with test execution
- **FR-016**: System MUST integrate into GitHub Actions workflow as two separate jobs: contract tests execute after unit tests, and E2E tests execute after contract tests
- **FR-017**: System MUST allow contract tests and E2E tests to be executed independently for targeted test runs
- **FR-018**: System MUST prioritize end-to-end workflow testing over isolated component testing, with integration tests primarily exercising complete scenarios through the WeightWorker entry point

### Key Entities *(include if feature involves data)*

- **WeightDocument**: Test entity representing persisted weight data with id, date, weight object, and document type
- **Weight Entity**: Test entity representing weight measurement data from Fitbit including date, weight value, and metadata
- **WeightResponse**: Test entity representing the API response structure containing an array of weight entities
- **Cosmos DB Emulator Service**: GitHub Actions service container running the Azure Cosmos DB Emulator for E2E test database operations
- **Test Fixture**: Infrastructure component managing test database lifecycle and shared test context
- **Mock HTTP Handler**: Test infrastructure simulating Fitbit API responses for controlled testing scenarios

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Integration test project achieves at least 80% code coverage of service layer components (WeightService, FitbitService, WeightWorker)
- **SC-002**: All integration tests complete execution in under 30 seconds on a standard development machine
- **SC-003**: Test suite can run 100 times consecutively without failures due to environmental issues or test data conflicts
- **SC-004**: Every external dependency (Cosmos DB, Fitbit API, Key Vault) has both successful and failure scenario tests
- **SC-005**: Test project builds successfully on any machine with .NET 9.0 SDK without additional setup requirements
- **SC-006**: Contract tests complete in under 2 seconds, providing rapid feedback on service configuration
- **SC-007**: E2E tests successfully connect to and use the Cosmos DB Emulator in GitHub Actions without manual intervention
- **SC-008**: Zero false-positive test failures occur when external services are properly available
