---
description: "Task list for Weight Service Integration Tests implementation"
---

# Tasks: Weight Service Integration Tests

**Input**: Design documents from `/specs/003-weight-svc-integration-tests/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Integration tests ARE the feature. This project creates comprehensive integration test coverage for Biotrackr.Weight.Svc.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

All paths are relative to repository root: `C:\Users\velidawill\Documents\OpenSource\biotrackr\`

---

## Phase 1: Setup (Project Infrastructure)

**Purpose**: Create integration test project structure with required dependencies and configuration

- [X] T001 Create integration test project at src/Biotrackr.Weight.Svc.IntegrationTests/ using `dotnet new xunit`
- [X] T002 Add project reference to Biotrackr.Weight.Svc project in src/Biotrackr.Weight.Svc.IntegrationTests/Biotrackr.Weight.Svc.IntegrationTests.csproj
- [X] T003 [P] Install xUnit 2.9.3 package in src/Biotrackr.Weight.Svc.IntegrationTests/Biotrackr.Weight.Svc.IntegrationTests.csproj
- [X] T004 [P] Install FluentAssertions 8.4.0 package in src/Biotrackr.Weight.Svc.IntegrationTests/Biotrackr.Weight.Svc.IntegrationTests.csproj
- [X] T005 [P] Install Moq 4.20.72 package in src/Biotrackr.Weight.Svc.IntegrationTests/Biotrackr.Weight.Svc.IntegrationTests.csproj
- [X] T006 [P] Install AutoFixture 4.18.1 package in src/Biotrackr.Weight.Svc.IntegrationTests/Biotrackr.Weight.Svc.IntegrationTests.csproj
- [X] T007 [P] Install Microsoft.AspNetCore.Mvc.Testing 9.0.10 package in src/Biotrackr.Weight.Svc.IntegrationTests/Biotrackr.Weight.Svc.IntegrationTests.csproj
- [X] T008 [P] Install Microsoft.Azure.Cosmos 3.52.0 package in src/Biotrackr.Weight.Svc.IntegrationTests/Biotrackr.Weight.Svc.IntegrationTests.csproj
- [X] T009 [P] Install Azure.Identity 1.14.1 package in src/Biotrackr.Weight.Svc.IntegrationTests/Biotrackr.Weight.Svc.IntegrationTests.csproj
- [X] T010 [P] Install coverlet.collector 6.0.4 package in src/Biotrackr.Weight.Svc.IntegrationTests/Biotrackr.Weight.Svc.IntegrationTests.csproj
- [X] T011 Create Contract/ folder in src/Biotrackr.Weight.Svc.IntegrationTests/
- [X] T012 Create E2E/ folder in src/Biotrackr.Weight.Svc.IntegrationTests/
- [X] T013 Create Fixtures/ folder in src/Biotrackr.Weight.Svc.IntegrationTests/
- [X] T014 Create Collections/ folder in src/Biotrackr.Weight.Svc.IntegrationTests/
- [X] T015 Create Helpers/ folder in src/Biotrackr.Weight.Svc.IntegrationTests/
- [X] T016 Create appsettings.Test.json configuration file in src/Biotrackr.Weight.Svc.IntegrationTests/ with Cosmos DB Emulator connection settings
- [X] T017 Add integration test project to src/Biotrackr.Weight.Svc/Biotrackr.Weight.Svc.IntegrationTests solution file

---

## Phase 2: Foundational (Test Infrastructure)

**Purpose**: Core test fixtures and helpers that ALL integration tests depend on

**⚠️ CRITICAL**: No test implementation can begin until this phase is complete

- [X] T018 Create MockHttpMessageHandler class in src/Biotrackr.Weight.Svc.IntegrationTests/Helpers/MockHttpMessageHandler.cs for mocking HTTP responses
- [X] T019 Create TestDataBuilder class in src/Biotrackr.Weight.Svc.IntegrationTests/Helpers/TestDataBuilder.cs with AutoFixture for test data generation
- [X] T020 Create ContractTestFixture class in src/Biotrackr.Weight.Svc.IntegrationTests/Fixtures/ContractTestFixture.cs for DI container testing
- [X] T021 Create IntegrationTestFixture class implementing IAsyncLifetime in src/Biotrackr.Weight.Svc.IntegrationTests/Fixtures/IntegrationTestFixture.cs for Cosmos DB Emulator integration
- [X] T022 Implement InitializeAsync method in IntegrationTestFixture to connect to Cosmos DB Emulator and create test database/container
- [X] T023 Implement DisposeAsync method in IntegrationTestFixture to cleanup test database/container
- [X] T024 Create ContractTestCollection class in src/Biotrackr.Weight.Svc.IntegrationTests/Collections/ContractTestCollection.cs with xUnit CollectionDefinition attribute
- [X] T025 Create IntegrationTestCollection class in src/Biotrackr.Weight.Svc.IntegrationTests/Collections/IntegrationTestCollection.cs with xUnit CollectionDefinition attribute
- [X] T026 Add helper methods to TestDataBuilder for building WeightResponse objects with configurable count
- [X] T027 Add helper methods to TestDataBuilder for building successful and error Fitbit API HTTP responses
- [X] T028 Add mock SecretClient configuration to IntegrationTestFixture to return test access tokens

**Checkpoint**: Foundation ready - test implementation can now begin in parallel

---

## Phase 3: User Story 1 - Integration Test Project Creation (Priority: P1) 🎯 MVP

**Goal**: Create and verify basic integration test project structure with proper configuration

**Independent Test**: Run `dotnet test` on the integration test project and verify it builds successfully with test discovery working

### Contract Tests for User Story 1

> **NOTE: These tests verify DI configuration and service registration without external dependencies**

- [X] T029 [P] [US1] Create ProgramStartupTests class in src/Biotrackr.Weight.Svc.IntegrationTests/Contract/ProgramStartupTests.cs using ContractTestCollection
- [X] T030 [P] [US1] Implement Application_Builds_Service_Provider_Successfully test in ProgramStartupTests
- [X] T031 [P] [US1] Implement All_Required_Services_Are_Registered test in ProgramStartupTests to verify ICosmosRepository, IWeightService, IFitbitService resolve correctly
- [X] T032 [P] [US1] Implement Settings_Are_Bound_From_Configuration test in ProgramStartupTests to verify Options pattern configuration

### Implementation for User Story 1

- [X] T033 [US1] Configure ContractTestFixture ServiceProvider to build minimal DI container with production service registrations
- [X] T034 [US1] Add test configuration to ContractTestFixture with Biotrackr section (DatabaseName, ContainerName)
- [X] T035 [US1] Verify all contract tests pass and execute in under 2 seconds
- [X] T036 [US1] Document test project structure and setup in README.md at src/Biotrackr.Weight.Svc.IntegrationTests/

**Checkpoint**: At this point, contract tests verify DI configuration is correct and project builds successfully

---

## Phase 4: User Story 2 - Azure Cosmos DB Integration Testing (Priority: P2)

**Goal**: Verify data persistence operations work correctly with Cosmos DB Emulator

**Independent Test**: Create test data, save through repository, verify persistence in test container

### Contract Tests for User Story 2

- [ ] T037 [P] [US2] Create ServiceRegistrationTests class in src/Biotrackr.Weight.Svc.IntegrationTests/Contract/ServiceRegistrationTests.cs using ContractTestCollection
- [ ] T038 [P] [US2] Implement CosmosRepository_Is_Registered_As_Scoped test in ServiceRegistrationTests
- [ ] T039 [P] [US2] Implement WeightService_Is_Registered_As_Scoped test in ServiceRegistrationTests
- [ ] T040 [P] [US2] Implement FitbitService_Is_Registered_As_Scoped test in ServiceRegistrationTests

### E2E Tests for User Story 2

- [ ] T041 [P] [US2] Create WeightServiceTests class in src/Biotrackr.Weight.Svc.IntegrationTests/E2E/WeightServiceTests.cs using IntegrationTestCollection
- [ ] T042 [P] [US2] Implement MapAndSaveDocument_Saves_Weight_Document_To_Cosmos test in WeightServiceTests
- [ ] T043 [P] [US2] Implement MapAndSaveDocument_Creates_Unique_Document_Ids test in WeightServiceTests
- [ ] T044 [P] [US2] Create CosmosRepositoryTests class in src/Biotrackr.Weight.Svc.IntegrationTests/E2E/CosmosRepositoryTests.cs using IntegrationTestCollection
- [ ] T045 [P] [US2] Implement CreateDocument_Persists_To_Cosmos_With_Correct_PartitionKey test in CosmosRepositoryTests
- [ ] T046 [P] [US2] Implement CreateDocument_Handles_Duplicate_Id_Gracefully test in CosmosRepositoryTests

### Implementation for User Story 2

- [ ] T047 [US2] Configure IntegrationTestFixture to connect to Cosmos DB Emulator at https://localhost:8081 with emulator key
- [ ] T048 [US2] Implement database and container creation in IntegrationTestFixture.InitializeAsync using unique test database names
- [ ] T049 [US2] Implement container cleanup in IntegrationTestFixture.DisposeAsync to delete test database
- [ ] T050 [US2] Add helper methods to IntegrationTestFixture for querying documents from test container
- [ ] T051 [US2] Verify all User Story 2 tests pass with Cosmos DB Emulator running locally

**Checkpoint**: At this point, User Story 2 should verify data persistence works correctly with Cosmos DB

---

## Phase 5: User Story 4 - Background Worker Integration Testing (Priority: P2)

**Goal**: Verify WeightWorker orchestrates complete end-to-end workflow correctly

**Independent Test**: Execute worker and verify it retrieves data from Fitbit, transforms it, and saves to Cosmos DB

**Note**: User Story 4 implemented before User Story 3 because worker testing covers the complete workflow

### E2E Tests for User Story 4

- [ ] T052 [P] [US4] Create WeightWorkerTests class in src/Biotrackr.Weight.Svc.IntegrationTests/E2E/WeightWorkerTests.cs using IntegrationTestCollection
- [ ] T053 [P] [US4] Implement Worker_Successfully_Syncs_Weight_Data_From_Fitbit_To_Cosmos test in WeightWorkerTests
- [ ] T054 [P] [US4] Implement Worker_Handles_Empty_Weight_Response_Gracefully test in WeightWorkerTests
- [ ] T055 [P] [US4] Implement Worker_Returns_Error_Code_When_Fitbit_API_Fails test in WeightWorkerTests
- [ ] T056 [P] [US4] Implement Worker_Respects_Cancellation_Token test in WeightWorkerTests

### Implementation for User Story 4

- [ ] T057 [US4] Add BuildWorker helper method to WeightWorkerTests that creates WeightWorker with mocked dependencies
- [ ] T058 [US4] Configure MockHttpMessageHandler in IntegrationTestFixture to support dynamic response configuration
- [ ] T059 [US4] Add TestDataBuilder methods for creating WeightResponse with configurable number of weight entries
- [ ] T060 [US4] Add TestDataBuilder methods for creating successful and error Fitbit API responses
- [ ] T061 [US4] Implement mock SecretClient setup in IntegrationTestFixture to return test access token
- [ ] T062 [US4] Verify all User Story 4 tests pass and complete end-to-end workflow validation

**Checkpoint**: At this point, User Story 4 should verify complete worker workflow from Fitbit to Cosmos DB

---

## Phase 6: User Story 3 - Fitbit API Integration Testing (Priority: P3)

**Goal**: Verify external API communication works correctly with various response scenarios

**Independent Test**: Set up mock HTTP responses and verify service handles various API response scenarios correctly

### E2E Tests for User Story 3

- [ ] T063 [P] [US3] Create FitbitServiceTests class in src/Biotrackr.Weight.Svc.IntegrationTests/E2E/FitbitServiceTests.cs using IntegrationTestCollection
- [ ] T064 [P] [US3] Implement GetWeightLogsAsync_Returns_Parsed_Weight_Data test in FitbitServiceTests for successful API response
- [ ] T065 [P] [US3] Implement GetWeightLogsAsync_Throws_Exception_On_API_Error test in FitbitServiceTests for error response handling
- [ ] T066 [P] [US3] Implement GetWeightLogsAsync_Includes_Access_Token_In_Headers test in FitbitServiceTests to verify authentication

### Contract Tests for User Story 3

- [ ] T067 [P] [US3] Create ConfigurationTests class in src/Biotrackr.Weight.Svc.IntegrationTests/Contract/ConfigurationTests.cs using ContractTestCollection
- [ ] T068 [P] [US3] Implement HttpClient_Is_Configured_With_Base_Address test in ConfigurationTests
- [ ] T069 [P] [US3] Implement SecretClient_Configuration_Loads_Successfully test in ConfigurationTests

### Implementation for User Story 3

- [ ] T070 [US3] Add BuildFitbitService helper method to FitbitServiceTests that creates FitbitService with MockHttpMessageHandler
- [ ] T071 [US3] Enhance MockHttpMessageHandler to capture request details (headers, URL, method)
- [ ] T072 [US3] Add TestDataBuilder methods for creating Fitbit API response JSON with various weight data scenarios
- [ ] T073 [US3] Add TestDataBuilder methods for creating error responses (401, 429, 500, timeout)
- [ ] T074 [US3] Verify all User Story 3 tests pass and cover success, error, and authentication scenarios

**Checkpoint**: All user stories should now be independently functional with comprehensive test coverage

---

## Phase 7: GitHub Actions Integration (CI/CD)

**Purpose**: Integrate integration tests into existing GitHub Actions workflow

- [ ] T075 Read existing workflow file at .github/workflows/deploy-weight-service.yml to understand current structure
- [ ] T076 Add run-contract-tests job to .github/workflows/deploy-weight-service.yml that runs after unit tests job
- [ ] T077 Configure run-contract-tests job to execute contract tests using dotnet test with filter "FullyQualifiedName~Contract"
- [ ] T078 Set contract tests job timeout to 5 minutes (target: <2 seconds execution)
- [ ] T079 Add run-e2e-tests job to .github/workflows/deploy-weight-service.yml that runs after run-contract-tests job
- [ ] T080 Configure Cosmos DB Emulator service container in run-e2e-tests job using mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
- [ ] T081 Add Cosmos DB Emulator health check and certificate trust steps to run-e2e-tests job
- [ ] T082 Configure run-e2e-tests job to execute E2E tests using dotnet test with filter "FullyQualifiedName~E2E"
- [ ] T083 Set E2E tests job timeout to 10 minutes (target: <30 seconds execution)
- [ ] T084 Add needs dependency in run-e2e-tests job to ensure contract tests pass before E2E tests run
- [ ] T085 Verify both test jobs run successfully in GitHub Actions workflow

---

## Phase 8: Coverage and Documentation (Polish)

**Purpose**: Add code coverage reporting and finalize documentation

- [ ] T086 Configure coverlet.collector in src/Biotrackr.Weight.Svc.IntegrationTests/Biotrackr.Weight.Svc.IntegrationTests.csproj with coverage settings
- [ ] T087 Add coverage exclusions for Program.cs entry point in coverlet configuration
- [ ] T088 Create coverage report generation script using ReportGenerator at src/Biotrackr.Weight.Svc/scripts/generate-coverage-report.ps1
- [ ] T089 Run coverage report generation and verify 80% minimum coverage of service layer components
- [ ] T090 [P] Create comprehensive README.md at src/Biotrackr.Weight.Svc.IntegrationTests/README.md with quickstart instructions
- [ ] T091 [P] Document test organization and patterns in README.md
- [ ] T092 [P] Document Cosmos DB Emulator setup for local development in README.md
- [ ] T093 [P] Document parallel test execution capabilities in README.md
- [ ] T094 Run full test suite 100 consecutive times to verify reliability (SC-003)
- [ ] T095 Verify all 8 success criteria from spec.md are met
- [ ] T096 Update specs/003-weight-svc-integration-tests/quickstart.md with any lessons learned during implementation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all test implementation
- **User Story 1 (Phase 3)**: Depends on Foundational phase - Can proceed immediately after Phase 2
- **User Story 2 (Phase 4)**: Depends on Foundational phase - Can proceed immediately after Phase 2
- **User Story 4 (Phase 5)**: Depends on Foundational phase - Can proceed immediately after Phase 2
- **User Story 3 (Phase 6)**: Depends on Foundational phase - Can proceed immediately after Phase 2
- **GitHub Actions (Phase 7)**: Depends on User Stories 1, 2, 4 completion - Need contract and E2E tests implemented
- **Coverage (Phase 8)**: Depends on all test implementation phases - Final polish phase

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories - Creates contract tests for DI configuration
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Independent of other stories - Tests Cosmos DB persistence
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - Independent of other stories - Tests complete workflow
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Independent of other stories - Tests Fitbit API integration

### Within Each User Story

- Contract tests and E2E tests within a story can run in parallel (marked [P])
- Implementation tasks follow test creation
- Story complete before moving to next priority

### Parallel Opportunities

- **Phase 1 (Setup)**: Tasks T003-T010 (package installations) can run in parallel
- **Phase 2 (Foundational)**: Tasks T018-T019 (MockHttpMessageHandler + TestDataBuilder) can run in parallel, Tasks T024-T025 (collection definitions) can run in parallel
- **Phase 3 (US1 Contract Tests)**: Tasks T029-T032 can run in parallel
- **Phase 4 (US2 Contract Tests)**: Tasks T037-T040 can run in parallel
- **Phase 4 (US2 E2E Tests)**: Tasks T041-T043 and T044-T046 can run in parallel
- **Phase 5 (US4 E2E Tests)**: Tasks T052-T056 can run in parallel
- **Phase 6 (US3 E2E Tests)**: Tasks T063-T066 can run in parallel
- **Phase 6 (US3 Contract Tests)**: Tasks T067-T069 can run in parallel
- **Phase 8 (Documentation)**: Tasks T090-T093 can run in parallel
- **User Stories**: After Phase 2, all user stories (Phase 3-6) can be worked on in parallel by different team members

---

## Parallel Example: User Story 2 (Cosmos DB Testing)

```bash
# Contract tests (can run in parallel)
git checkout -b us2-contract-tests
# Implement T037, T038, T039, T040 simultaneously

# E2E tests (can run in parallel)
git checkout -b us2-e2e-tests
# Implement T041-T043 and T044-T046 simultaneously

# Merge both branches when complete
```

---

## Implementation Strategy

### MVP Definition (Minimum Viable Product)

**MVP = User Story 1 ONLY (Contract Tests + Project Setup)**

This provides:
- ✅ Integration test project structure
- ✅ Contract tests verifying DI configuration
- ✅ Fast feedback on service registration issues (<2 seconds)
- ✅ Foundation for all subsequent testing

### Incremental Delivery Plan

1. **Sprint 1**: MVP (User Story 1) - Deliver contract tests and project structure
2. **Sprint 2**: User Story 2 + User Story 4 - Add Cosmos DB persistence testing and worker workflow testing
3. **Sprint 3**: User Story 3 + GitHub Actions Integration - Complete Fitbit API testing and CI/CD integration
4. **Sprint 4**: Coverage & Polish - Add coverage reporting and documentation

### Validation Checkpoints

- After Phase 2: Verify all fixtures and helpers work correctly
- After Phase 3: Verify contract tests pass in <2 seconds
- After Phase 4: Verify E2E tests work with Cosmos DB Emulator locally
- After Phase 5: Verify complete workflow tests pass
- After Phase 6: Verify all 25+ integration tests pass
- After Phase 7: Verify tests run successfully in GitHub Actions
- After Phase 8: Verify 80% code coverage and 100-run reliability

---

## Task Statistics

- **Total Tasks**: 96
- **Phase 1 (Setup)**: 17 tasks
- **Phase 2 (Foundational)**: 11 tasks
- **Phase 3 (User Story 1 - P1)**: 8 tasks (4 contract tests + 4 implementation)
- **Phase 4 (User Story 2 - P2)**: 15 tasks (4 contract tests + 6 E2E tests + 5 implementation)
- **Phase 5 (User Story 4 - P2)**: 11 tasks (4 E2E tests + 7 implementation)
- **Phase 6 (User Story 3 - P3)**: 12 tasks (3 E2E tests + 3 contract tests + 6 implementation)
- **Phase 7 (GitHub Actions)**: 11 tasks
- **Phase 8 (Coverage & Documentation)**: 11 tasks

**Parallel Opportunities Identified**: 45+ tasks can run in parallel across different phases

**Independent Test Criteria**:
- User Story 1: Run `dotnet test --filter "FullyQualifiedName~Contract"` and verify all contract tests pass in <2 seconds
- User Story 2: Run `dotnet test --filter "FullyQualifiedName~WeightService|CosmosRepository"` and verify data persistence works
- User Story 4: Run `dotnet test --filter "FullyQualifiedName~WeightWorker"` and verify complete workflow executes successfully
- User Story 3: Run `dotnet test --filter "FullyQualifiedName~FitbitService"` and verify HTTP mocking works correctly

**Suggested MVP Scope**: Phase 1 + Phase 2 + Phase 3 (User Story 1) = 36 tasks

This MVP provides complete contract test coverage and fast feedback loop for DI configuration issues.
