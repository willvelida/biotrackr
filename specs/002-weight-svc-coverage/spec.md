# Feature Specification: Weight Service Unit Test Coverage Improvement

**Feature Branch**: `002-weight-svc-coverage`  
**Created**: 2025-10-28  
**Status**: Draft  
**Input**: User description: "improve the unit test code coverage in the Biotrackr.Weight.Svc so that it meets the 70% code coverage requirement"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - WeightWorker Test Coverage (Priority: P1)

As a developer, I need comprehensive unit tests for the WeightWorker background service to ensure the weight synchronization process works correctly and handles errors appropriately.

**Why this priority**: WeightWorker is currently at 0% coverage and is the core orchestration component that coordinates weight data fetching and storage. This is the most critical gap preventing us from meeting the 70% coverage requirement.

**Independent Test**: Can be fully tested by creating unit tests that mock dependencies (IFitbitService, IWeightService, ILogger, IHostApplicationLifetime) and verify the ExecuteAsync workflow under various scenarios including success, failure, and edge cases.

**Acceptance Scenarios**:

1. **Given** the WeightWorker executes successfully, **When** it fetches weight logs for the past 7 days, **Then** it should process each weight entry and save it to the repository
2. **Given** the WeightWorker encounters an exception during execution, **When** the exception is thrown, **Then** it should log the error, return exit code 1, and stop the application
3. **Given** the WeightWorker processes multiple weight entries, **When** all entries are saved successfully, **Then** it should return exit code 0 and stop the application
4. **Given** the WeightWorker is instantiated, **When** dependencies are provided, **Then** all dependencies should be properly assigned to private fields

---

### User Story 2 - Program.cs Entry Point Coverage (Priority: P2)

As a developer, I need to understand that Program.cs is typically excluded from unit test coverage since it's the application entry point and better suited for integration testing.

**Why this priority**: While Program.cs shows 0% coverage, it's standard practice to exclude application entry points from unit test coverage metrics since they primarily wire up dependency injection and configuration, which is better tested through integration tests.

**Independent Test**: Can be documented through decision records or configuration changes to exclude Program.cs from coverage calculations, aligning with industry best practices.

**Acceptance Scenarios**:

1. **Given** Program.cs contains only DI registration and configuration, **When** coverage is calculated, **Then** Program.cs should be excluded from coverage metrics via coverlet configuration
2. **Given** the coverage configuration excludes Program.cs, **When** tests run, **Then** the effective coverage percentage should reflect only testable business logic

---

### User Story 3 - Additional Service Edge Cases (Priority: P3)

As a developer, I need to ensure existing service tests cover additional edge cases to maximize code path coverage and improve overall test quality.

**Why this priority**: Services already have good test coverage (FitbitService, WeightService, CosmosRepository are at or near 100%), but adding edge case tests can push overall coverage closer to the 70% target and improve robustness.

**Independent Test**: Can be tested by adding specific edge case tests to existing test classes without modifying production code.

**Acceptance Scenarios**:

1. **Given** FitbitService receives malformed JSON from the API, **When** deserializing the response, **Then** it should handle the JsonException appropriately
2. **Given** WeightService receives null or invalid date formats, **When** mapping documents, **Then** it should handle the error gracefully
3. **Given** CosmosRepository encounters a rate limiting exception, **When** creating a document, **Then** it should propagate the exception with appropriate logging

---

### Edge Cases

- What happens when the WeightWorker encounters a cancellation token during execution?
- How does the system handle empty weight logs returned from the Fitbit API?
- What occurs when IHostApplicationLifetime.StopApplication is called multiple times?
- How does the WeightWorker behave when the date range contains no data?
- What happens if dependencies are null when WeightWorker is constructed?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Test suite MUST include comprehensive unit tests for WeightWorker class covering constructor, ExecuteAsync success path, and exception handling
- **FR-002**: Test suite MUST achieve at least 70% overall code coverage for the Biotrackr.Weight.Svc project
- **FR-003**: Tests MUST verify WeightWorker correctly orchestrates calls to IFitbitService.GetWeightLogs and IWeightService.MapAndSaveDocument
- **FR-004**: Tests MUST verify WeightWorker logs appropriate messages at information and error levels
- **FR-005**: Tests MUST verify WeightWorker returns correct exit codes (0 for success, 1 for failure)
- **FR-006**: Tests MUST verify WeightWorker calls IHostApplicationLifetime.StopApplication in the finally block
- **FR-007**: Tests MUST verify WeightWorker processes all weight entries in the response collection
- **FR-008**: Coverage configuration MUST exclude Program.cs from coverage calculations to align with industry best practices for entry point testing
- **FR-009**: Tests MUST use appropriate mocking frameworks (Moq) and assertion libraries (FluentAssertions) consistent with existing test patterns
- **FR-010**: Tests MUST follow the existing test structure and naming conventions used in the project (e.g., MethodName_Should_ExpectedBehavior)

### Key Entities

- **WeightWorker**: Background service that orchestrates weight data synchronization; requires testing of dependency injection, async execution flow, error handling, and application lifecycle management
- **Test Fixtures**: Mock implementations of IFitbitService, IWeightService, ILogger<WeightWorker>, and IHostApplicationLifetime needed to isolate WeightWorker behavior
- **Coverage Report**: XML-based coverage report (Cobertura format) that tracks line and branch coverage metrics across the codebase

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Code coverage for Biotrackr.Weight.Svc project reaches at least 70% line coverage as measured by coverlet
- **SC-002**: WeightWorker class achieves at least 85% line coverage with comprehensive unit tests
- **SC-003**: All WeightWorker tests execute successfully in under 1 second total
- **SC-004**: Test suite continues to maintain 100% pass rate (no failing tests)
- **SC-005**: WeightWorker tests cover at least 4 distinct scenarios: constructor initialization, successful execution, exception handling, and application shutdown
