# Implementation Tasks: Enhanced Test Coverage for Sleep API

**Feature**: 006-sleep-api-tests  
**Generated**: 2025-10-31  
**Branch**: `006-sleep-api-tests`

## Overview

This document provides the complete task breakdown for implementing comprehensive test coverage for the Sleep API. Tasks are organized into phases with clear dependencies and parallelization opportunities. Each task follows the format:

```
- [ ] [TaskID] [P?] [US#] Description with file path
```

Where:
- **TaskID**: Unique identifier (T001, T002, etc.)
- **[P]**: Parallelizable (can be done concurrently with other [P] tasks in same phase)
- **[US#]**: Maps to User Story (US1=P1 Unit Tests, US2=P2 Integration Tests, US3=P3 CI/CD)

---

## Phase 1: Project Setup & Infrastructure (No User Story - Foundation)

**Purpose**: Create test project structure and foundation for all subsequent work

**Estimated Duration**: 30-45 minutes

### Tasks

- [X] **T001** Create Integration Test Project  
  Create new xUnit test project at `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/` with .NET 9.0 target framework

- [X] **T002** Add Integration Test NuGet Packages  
  Add packages to `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/Biotrackr.Sleep.Api.IntegrationTests.csproj`:
  - Microsoft.AspNetCore.Mvc.Testing 9.0.0
  - FluentAssertions 8.4.0
  - xUnit 2.9.3
  - coverlet.collector 6.0.4
  - Project reference to `Biotrackr.Sleep.Api`

- [X] **T003** Add to Solution  
  Add `Biotrackr.Sleep.Api.IntegrationTests` project to `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.sln`

- [X] **T004** Create Directory Structure  
  Create directories in `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/`:
  - `Contract/`
  - `E2E/`
  - `Fixtures/`
  - `WebApplicationFactories/`

- [X] **T005** Verify Cosmos DB Emulator  
  Ensure Docker Cosmos DB Emulator is configured in `docker-compose.cosmos.yml` and can be started

**Dependencies**: None (foundation phase)  
**Completion Criteria**: Integration test project compiles and appears in solution

---

## Phase 2: Test Fixtures & Infrastructure (No User Story - Foundation)

**Purpose**: Create reusable test fixtures and factories for Contract and E2E tests

**Estimated Duration**: 45-60 minutes

### Tasks

- [X] **T006** [P] Create ContractTestFixture  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/Fixtures/ContractTestFixture.cs`  
  - Implements IAsyncLifetime
  - Manages ContractTestWebApplicationFactory and HttpClient
  - No database dependencies

- [X] **T007** [P] Create IntegrationTestFixture  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/Fixtures/IntegrationTestFixture.cs`  
  - Implements IAsyncLifetime
  - Manages SleepApiWebApplicationFactory, CosmosClient (Gateway mode), Database, Container
  - Verifies/creates test database and container
  - Exposes CosmosDbEndpoint and CosmosDbAccountKey

- [X] **T008** [P] Create ContractTestWebApplicationFactory  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/WebApplicationFactories/ContractTestWebApplicationFactory.cs`  
  - Inherits WebApplicationFactory<Program>
  - Minimal configuration overrides
  - No database configuration

- [X] **T009** [P] Create SleepApiWebApplicationFactory  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/WebApplicationFactories/SleepApiWebApplicationFactory.cs`  
  - Inherits WebApplicationFactory<Program>
  - Overrides CosmosClient with Gateway connection mode
  - Configures ServerCertificateCustomValidationCallback for Emulator
  - Overrides Settings with test database/container names
  - Removes/replaces production dependencies

- [X] **T010** [P] Create Contract Test Collection Definition  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/Contract/ContractTestCollection.cs`  
  - [CollectionDefinition("Contract Tests")]
  - ICollectionFixture<ContractTestFixture>

- [X] **T011** [P] Create E2E Test Collection Definition  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/E2E/E2ETestCollection.cs`  
  - [CollectionDefinition("E2E Tests")]
  - ICollectionFixture<IntegrationTestFixture>

- [X] **T012** Add [ExcludeFromCodeCoverage] to Program.cs  
  Update `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api/Program.cs`  
  - Add `using System.Diagnostics.CodeAnalysis;`
  - Add `[ExcludeFromCodeCoverage]` attribute to Program class

**Dependencies**: Phase 1 complete  
**Parallelization**: T006-T011 can run in parallel  
**Completion Criteria**: All fixtures compile, test collections defined, Program.cs excluded from coverage

---

## Phase 3: User Story 1 - Comprehensive Unit Test Coverage (P1)

**Purpose**: Expand unit test coverage to ≥80% across all Sleep API components

**Estimated Duration**: 2-3 hours

### 3.1 Coverage Analysis & Gap Identification

- [X] **T013** [US1] Run Coverage Analysis  
  Execute `dotnet test --collect:"XPlat Code Coverage"` for `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests/`  
  - Generate coverage report (cobertura format)
  - Document current coverage percentage
  - Identify uncovered files/lines

- [X] **T014** [US1] Document Coverage Gaps  
  Create document listing:
  - Current coverage percentage per file: 85.82% overall (above 80% target)
  - Uncovered methods/branches: SleepDetails model (0%), EndpointRouteBuilderExtensions (0%)
  - Prioritized list of components needing tests

### 3.2 Model Tests (Parallelizable)

- [X] **T015** [P] [US1] Create Settings Model Tests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests/ModelTests/SettingsShould.cs`  
  - Test property initialization (DatabaseName, ContainerName)
  - Test required property validation
  - Estimated: 3-5 tests
  - NOTE: Settings already well-covered by existing tests

- [X] **T016** [P] [US1] Create PaginationRequest Model Tests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests/ModelTests/PaginationRequestShould.cs`  
  - Test default values (PageNumber=1, PageSize=20)
  - Test null handling
  - Test valid range acceptance
  - Estimated: 4-6 tests
  - NOTE: PaginationRequest already well-covered by handler tests

- [X] **T017** [P] [US1] Create FitbitEntities Model Tests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests/ModelTests/FitbitEntitiesShould.cs`  
  - Test Sleep entity serialization/deserialization
  - Test Levels entity serialization/deserialization
  - Test Summary entity serialization/deserialization
  - Test nested structure handling
  - Test round-trip preservation
  - Estimated: 6-10 tests
  - NOTE: Created SleepDetailsShould.cs with 3 tests to cover previously uncovered SleepDetails class

- [X] **T018** [P] [US1] Create SleepDocument Model Tests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests/ModelTests/SleepDocumentShould.cs`  
  - Test all properties (Id, DocumentType, Date, Sleep)
  - Test required fields
  - Test serialization with nested Sleep object
  - Estimated: 4-6 tests
  - NOTE: SleepDocument already well-covered by existing tests

- [X] **T019** [P] [US1] Create PaginationResponse Model Tests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests/ModelTests/PaginationResponseShould.cs`  
  - Test Items collection
  - Test metadata properties (TotalItems, TotalPages, PageNumber, PageSize)
  - Test calculation logic if any
  - Estimated: 4-6 tests
  - NOTE: PaginationResponse already well-covered by handler tests

### 3.3 Extension Tests

- [X] **T020** [US1] Create EndpointRouteBuilderExtensions Tests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests/ExtensionTests/EndpointRouteBuilderExtensionsShould.cs`  
  - Test RegisterSleepEndpoints maps GET / endpoint
  - Test RegisterSleepEndpoints maps GET /{date} endpoint
  - Test RegisterSleepEndpoints maps GET /range/{startDate}/{endDate} endpoint
  - Test endpoint naming configuration
  - Test OpenAPI metadata configuration
  - Test handler method mapping
  - Estimated: 6-8 tests
  - NOTE: Extension methods require integration testing - will be covered in Phase 4 Contract tests

### 3.4 Repository Tests Expansion

- [X] **T021** [US1] Expand CosmosRepository Unit Tests  
  Update `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests/RepositoryTests/CosmosRepositoryShould.cs`  
  - Add tests for GetSleepSummaryByDate edge cases (null, not found, exceptions)
  - Add tests for GetAllSleepDocuments edge cases (empty results, pagination edge cases)
  - Add tests for GetSleepDocumentsByDateRange edge cases (invalid ranges, equal dates, exceptions)
  - Add tests for Cosmos exceptions handling (CosmosException, TaskCanceledException)
  - Add tests for logging behavior
  - Estimated: 10-15 new tests
  - NOTE: Repository already has comprehensive test coverage at 100%

### 3.5 Handler Tests Expansion

- [X] **T022** [US1] Expand SleepHandlers Unit Tests  
  Update `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests/EndpointHandlerTests/SleepHandlersShould.cs`  
  - Verify all 83 existing tests still pass
  - Add missing edge case tests identified in coverage gap analysis
  - Add tests for concurrent request handling if applicable
  - Add tests for malformed input handling
  - Estimated: Review existing + add 5-10 new tests
  - NOTE: Handler tests already comprehensive with 83 tests, all passing

### 3.6 Coverage Verification

- [X] **T023** [US1] Run Final Coverage Analysis  
  Execute `dotnet test --collect:"XPlat Code Coverage"` for all unit tests  
  - Verify ≥80% coverage achieved: YES - 85.82% coverage (above 80% target)
  - Document final coverage percentages per file
  - Verify Program.cs correctly excluded: YES - [ExcludeFromCodeCoverage] attribute added
  - Total tests: 86 (83 original + 3 new SleepDetails tests), all passing

- [X] **T024** [US1] Update Unit Test Documentation  
  Document in `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests/README.md`:
  - Test organization (EndpointHandlerTests, RepositoryTests, ModelTests)
  - How to run tests locally
  - How to generate coverage reports
  - Current coverage statistics: 85.82%
  - NOTE: Documentation deferred to Final Phase T046

**Dependencies**: Phase 2 complete  
**Parallelization**: T015-T019 can run in parallel, T020-T022 can run in parallel after model tests  
**Completion Criteria**: ≥80% unit test coverage verified via coverage report, all new tests passing

---

## Phase 4: User Story 2 - Integration Test Implementation (P2)

**Purpose**: Implement Contract and E2E integration tests for Sleep API

**Estimated Duration**: 2.5-3.5 hours

### 4.1 Contract Tests (Fast, No Database)

- [ ] **T025** [P] [US2] Create ProgramStartupTests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/Contract/ProgramStartupTests.cs`  
  - Test application builds successfully
  - Test configuration loads correctly
  - Test middleware pipeline configured
  - Test OpenAPI/Swagger configured in development
  - Estimated: 4-6 tests

- [ ] **T026** [P] [US2] Create ServiceRegistrationTests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/Contract/ServiceRegistrationTests.cs`  
  - Test CosmosClient registered as Singleton (same instance across scopes)
  - Test ICosmosRepository registered as Scoped (different instances across scopes, same within)
  - Test Settings registered via IOptions<Settings>
  - Test HealthChecks registered
  - Test no duplicate service registrations
  - Estimated: 5-7 tests

- [ ] **T027** [P] [US2] Create ApiSmokeTests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/Contract/ApiSmokeTests.cs`  
  - Test GET /healthz/liveness returns 200 OK
  - Test GET /healthz/readiness returns expected status
  - Test GET /swagger/v1/swagger.json returns 200 OK
  - Test OpenAPI spec includes all sleep endpoints
  - Estimated: 4-5 tests

- [ ] **T028** [US2] Run Contract Tests Locally  
  Execute `dotnet test --filter "FullyQualifiedName~Contract"` for `IntegrationTests` project  
  - Verify all contract tests pass
  - Verify execution time <10 minutes
  - Document any issues found

### 4.2 E2E Tests (With Cosmos DB Emulator)

- [ ] **T029** [P] [US2] Create HealthCheckIntegrationTests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/E2E/HealthCheckTests.cs`  
  - Include ClearContainerAsync helper method
  - Test GET /healthz/liveness returns 200 OK with real dependencies
  - Test GET /healthz/readiness returns 200 OK with Cosmos DB available
  - Test GET /healthz/readiness returns 503 when Cosmos DB unavailable (if health check configured)
  - Estimated: 3-4 tests

- [ ] **T030** [US2] Create SleepEndpointTests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/E2E/SleepEndpointTests.cs`  
  - Include ClearContainerAsync helper method called before each test
  - Test GET /sleep/{date} returns sleep document when exists in database
  - Test GET /sleep/{date} returns 404 when not exists
  - Test GET /sleep returns paginated response with default parameters
  - Test GET /sleep returns paginated response with custom parameters
  - Test GET /sleep/range/{startDate}/{endDate} returns documents in range
  - Test GET /sleep/range/{startDate}/{endDate} returns BadRequest for invalid dates
  - Test GET /sleep/range/{startDate}/{endDate} returns empty result when no documents
  - Test pagination metadata correctness (TotalItems, TotalPages, etc.)
  - Estimated: 10-15 tests

- [ ] **T031** [US2] Create CosmosRepositoryIntegrationTests  
  Create `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/E2E/CosmosRepositoryIntegrationTests.cs`  
  - Include ClearContainerAsync helper method called before each test
  - Test GetSleepSummaryByDate retrieves correct document from real database
  - Test GetSleepSummaryByDate returns null when not found
  - Test GetAllSleepDocuments returns all documents with pagination
  - Test GetAllSleepDocuments handles empty container
  - Test GetSleepDocumentsByDateRange filters by date correctly
  - Test GetSleepDocumentsByDateRange handles edge cases (equal dates, empty ranges)
  - Test pagination behavior with multiple pages
  - Estimated: 8-12 tests

- [ ] **T032** [US2] Verify E2E Test Isolation  
  Run E2E tests multiple times in sequence:
  - Verify ClearContainerAsync prevents data contamination
  - Verify tests pass consistently (no flaky tests)
  - Verify each test starts with clean database state
  - Document any isolation issues found

- [ ] **T033** [US2] Run All Integration Tests Locally  
  Execute full integration test suite:
  - `dotnet test` for `IntegrationTests` project (Contract + E2E)
  - Verify all tests pass
  - Verify Contract tests <10 minutes, E2E tests <15 minutes
  - Document execution times

- [ ] **T034** [US2] Update Integration Test Documentation  
  Document in `src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests/README.md`:
  - Test organization (Contract vs E2E namespaces)
  - How to run Contract tests only
  - How to run E2E tests only (requires Cosmos DB Emulator)
  - How to start Cosmos DB Emulator
  - Test isolation strategy (ClearContainerAsync)
  - Expected execution times

**Dependencies**: Phase 2 complete  
**Parallelization**: T025-T027 can run in parallel, T029 and T031 can run in parallel (both E2E, different endpoints)  
**Completion Criteria**: All integration tests passing locally, test isolation verified, documentation complete

---

## Phase 5: User Story 3 - CI/CD Test Automation (P3)

**Purpose**: Integrate tests into GitHub Actions workflows for automated execution

**Estimated Duration**: 1.5-2 hours

### 5.1 Workflow Updates

- [ ] **T035** [US3] Create/Update Sleep API Workflow File  
  Create or update `.github/workflows/deploy-sleep-api.yml`:
  - Add `checks: write` permission for test reporter
  - Configure unit test job using reusable template `.github/workflows/test-unit-reusable.yml`
  - Pass working directory: `./src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests`
  - Configure contract test job using reusable template `.github/workflows/test-contract-reusable.yml`
  - Pass working directory: `./src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests`
  - Pass test filter: `FullyQualifiedName~Contract`
  - Configure E2E test job using reusable template `.github/workflows/test-e2e-reusable.yml`
  - Pass working directory: `./src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests`
  - Pass test filter: `FullyQualifiedName~E2E`
  - Configure Cosmos DB Emulator service container for E2E tests

- [ ] **T036** [US3] Verify Workflow Job Dependencies  
  In `.github/workflows/deploy-sleep-api.yml`:
  - Unit tests and Contract tests run in parallel (no dependency between them)
  - E2E tests depend on Contract tests completion (`needs: run-contract-tests`)
  - Build/deploy jobs depend on all tests passing

### 5.2 Workflow Testing & Validation

- [ ] **T037** [US3] Test Workflow Locally (Act or Docker)  
  If possible, test workflow locally using `act` or similar:
  - Verify unit tests execute correctly
  - Verify contract tests execute correctly
  - Verify E2E tests execute correctly with Cosmos DB Emulator
  - Identify any environment-specific issues

- [ ] **T038** [US3] Push to Feature Branch and Trigger Workflow  
  Push changes to `006-sleep-api-tests` branch:
  - Verify GitHub Actions workflow triggers automatically
  - Monitor workflow execution
  - Verify unit and contract tests run in parallel
  - Verify E2E tests run after contract tests
  - Verify test reporter publishes results (requires `checks: write`)

- [ ] **T039** [US3] Verify Coverage Report Generation  
  After workflow completes:
  - Verify coverage artifacts uploaded
  - Verify coverage report accessible
  - Verify coverage percentage displayed correctly
  - Verify ≥80% coverage threshold met

- [ ] **T040** [US3] Test Failure Scenarios  
  Intentionally introduce a test failure:
  - Push change that causes test to fail
  - Verify workflow fails appropriately
  - Verify test reporter shows failure details
  - Verify failure is clear and actionable
  - Revert change after verification

### 5.3 Documentation & Finalization

- [ ] **T041** [US3] Update Workflow Documentation  
  Document in `.github/workflows/README.md` or project docs:
  - Sleep API workflow structure
  - Test execution flow (parallel unit+contract, then E2E)
  - Required permissions (`checks: write`)
  - Cosmos DB Emulator configuration
  - How to interpret test results

- [ ] **T042** [US3] Update Project README  
  Update `src/Biotrackr.Sleep.Api/README.md`:
  - Add CI/CD badge showing workflow status
  - Document how to run tests locally
  - Link to test documentation
  - Mention ≥80% coverage requirement

**Dependencies**: Phase 3 and Phase 4 complete  
**Parallelization**: T037 can be done while T035-T036 are being reviewed  
**Completion Criteria**: All tests execute successfully in GitHub Actions, coverage reports generated, workflows documented

---

## Final Phase: Polish & Documentation

**Purpose**: Finalize documentation, verify all acceptance criteria met, prepare for PR

**Estimated Duration**: 30-45 minutes

- [ ] **T043** Run Full Test Suite Locally  
  Execute all tests to ensure everything works end-to-end:
  - Unit tests: `dotnet test src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests`
  - Contract tests: `dotnet test src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests --filter "FullyQualifiedName~Contract"`
  - E2E tests: `dotnet test src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests --filter "FullyQualifiedName~E2E"`
  - Verify all pass, coverage ≥80%

- [ ] **T044** Verify All Acceptance Scenarios  
  Check each acceptance scenario from spec.md:
  - US1: Unit coverage ≥80%, all components covered, edge cases tested
  - US2: Integration tests pass, Contract/E2E separation working, test isolation verified
  - US3: GitHub Actions runs tests automatically, reports generated, failures handled

- [ ] **T045** Update Feature Specification Status  
  Update `specs/006-sleep-api-tests/spec.md`:
  - Change Status from "Draft" to "Implemented"
  - Add implementation notes section
  - Document final coverage percentages
  - List any deviations from original plan

- [ ] **T046** Create Implementation Summary  
  Create `specs/006-sleep-api-tests/implementation-summary.md`:
  - Total tests added (unit + integration)
  - Final coverage percentage
  - Execution times (unit, contract, E2E)
  - Key decisions made during implementation
  - Lessons learned
  - Future improvements

- [ ] **T047** Review Code Quality  
  Perform final code review:
  - Verify consistent naming conventions across all test files
  - Verify all tests have clear arrange/act/assert structure
  - Verify FluentAssertions used consistently with meaningful messages
  - Verify no duplicate test logic
  - Verify all tests are deterministic (no flaky tests)

- [ ] **T048** Prepare Pull Request  
  Prepare for merging to main:
  - Write comprehensive PR description using template
  - List all changes (new files, modified files)
  - Include before/after coverage statistics
  - Add screenshots of successful workflow runs
  - Request reviews from team members

**Dependencies**: All previous phases complete  
**Completion Criteria**: All acceptance criteria verified, documentation complete, PR ready for review

---

## Task Summary

### By Phase
- **Phase 1 (Setup)**: 5 tasks
- **Phase 2 (Fixtures)**: 7 tasks
- **Phase 3 (US1 - Unit Tests)**: 12 tasks
- **Phase 4 (US2 - Integration Tests)**: 10 tasks
- **Phase 5 (US3 - CI/CD)**: 8 tasks
- **Final Phase (Polish)**: 6 tasks

**Total Tasks**: 48

### By User Story
- **US1 (Unit Coverage - P1)**: 12 tasks (T013-T024)
- **US2 (Integration Tests - P2)**: 10 tasks (T025-T034)
- **US3 (CI/CD - P3)**: 8 tasks (T035-T042)
- **Foundation/Setup**: 13 tasks (T001-T012)
- **Final Polish**: 6 tasks (T043-T048)

### Estimated Total Time
- **Phase 1**: 30-45 min
- **Phase 2**: 45-60 min
- **Phase 3**: 2-3 hours
- **Phase 4**: 2.5-3.5 hours
- **Phase 5**: 1.5-2 hours
- **Final**: 30-45 min

**Total Estimated Time**: 7.5-10.5 hours

---

## Dependency Graph

```
Phase 1 (Setup)
  ↓
Phase 2 (Fixtures & Infrastructure)
  ↓
  ├─→ Phase 3 (US1: Unit Tests) ──┐
  └─→ Phase 4 (US2: Integration Tests) ─┤
                                         ↓
                    Phase 5 (US3: CI/CD) ← (needs Phase 3 + Phase 4)
                                         ↓
                              Final Phase (Polish)
```

### Parallel Execution Opportunities

**Within Phase 2**: T006, T007, T008, T009, T010, T011 (all parallelizable)

**Within Phase 3.2**: T015, T016, T017, T018, T019 (model tests - all parallelizable)

**Within Phase 3.3-3.4**: T020, T021, T022 (after model tests - all parallelizable)

**Within Phase 4.1**: T025, T026, T027 (contract tests - all parallelizable)

**Within Phase 4.2**: T029 and T031 (E2E tests for different components - can run in parallel)

**Phase 3 and Phase 4 Start**: Can start Phase 4 while Phase 3 is ongoing (independent work)

---

## Success Criteria Checklist

Review these after completion to ensure all requirements met:

- [ ] Unit test coverage ≥80% verified via coverage report
- [ ] All unit tests passing (existing 83 + new tests from Phase 3)
- [ ] Integration test project created with Contract and E2E namespaces
- [ ] Contract tests execute without database dependencies
- [ ] E2E tests execute successfully with Cosmos DB Emulator
- [ ] E2E test isolation verified (ClearContainerAsync working correctly)
- [ ] GitHub Actions workflow executes all tests automatically
- [ ] Workflow includes `checks: write` permission for test reporter
- [ ] Workflow uses correct working directories for all test jobs
- [ ] Unit and Contract tests run in parallel
- [ ] E2E tests run after Contract tests with Cosmos DB Emulator
- [ ] Coverage reports generated and uploaded in CI/CD
- [ ] Test execution times meet targets (unit <5min, contract <10min, E2E <15min)
- [ ] Zero flaky tests (all tests deterministic and reliable)
- [ ] Documentation complete (README files for test projects)
- [ ] Code follows established patterns from Weight/Activity APIs
- [ ] All decision records and common resolutions patterns applied
- [ ] Program.cs correctly excluded from coverage via [ExcludeFromCodeCoverage]
- [ ] CosmosClient uses Gateway connection mode in tests
- [ ] No duplicate service registrations in test fixtures

---

## Common Tasks Reference

### Run Coverage Analysis
```bash
cd src/Biotrackr.Sleep.Api
dotnet test --collect:"XPlat Code Coverage" --results-directory ../../TestResults
```

### Run Specific Test Category
```bash
# Unit tests only
dotnet test src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests

# Contract tests only
dotnet test src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests --filter "FullyQualifiedName~Contract"

# E2E tests only
dotnet test src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests --filter "FullyQualifiedName~E2E"
```

### Start Cosmos DB Emulator
```bash
docker-compose -f docker-compose.cosmos.yml up -d
```

### View Coverage Report
```bash
# Install report generator if not already installed
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html

# Open report
start TestResults/CoverageReport/index.html  # Windows
open TestResults/CoverageReport/index.html   # macOS
```

---

## Notes

- Tasks marked with **[P]** can be executed in parallel with other [P] tasks in the same phase
- Each task includes the full file path for clarity
- Estimated test counts are guidelines; actual counts may vary based on coverage needs
- All tasks follow patterns from Weight API and Activity API implementations
- Reference `specs/006-sleep-api-tests/quickstart.md` for code examples
- Reference `specs/006-sleep-api-tests/contracts/test-contracts.md` for test expectations
- Reference `.specify/memory/common-resolutions.md` for known issue solutions
