# Tasks: Activity Service Test Coverage and Integration Tests

**Input**: Design documents from `/specs/005-activity-svc-tests/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: This is a testing feature - all tasks focus on expanding and creating tests.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Environment preparation and tooling setup

- [x] T001 Verify Cosmos DB Emulator is available via docker-compose.cosmos.yml
- [x] T002 Verify .NET 9.0 SDK is installed and existing unit tests run successfully
- [x] T003 [P] Generate baseline coverage report to establish current coverage percentage
- [x] T004 [P] Install coverage reporting tools (dotnet-reportgenerator-globaltool)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Integration test project structure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Create Biotrackr.Activity.Svc.IntegrationTests project targeting .NET 9.0 in src/Biotrackr.Activity.Svc/
- [x] T006 Add integration test project to Biotrackr.Activity.Svc.sln
- [x] T007 [P] Add NuGet package xUnit 2.9.3 to integration test project
- [x] T008 [P] Add NuGet package FluentAssertions 8.4.0 to integration test project
- [x] T009 [P] Add NuGet package Moq 4.20.72 to integration test project
- [x] T010 [P] Add NuGet package AutoFixture 4.18.1 to integration test project
- [x] T011 [P] Add NuGet package coverlet.collector 6.0.4 to integration test project
- [x] T012 [P] Add NuGet package Microsoft.AspNetCore.Mvc.Testing 9.0.0 to integration test project
- [x] T013 [P] Add NuGet package Microsoft.Azure.Cosmos to integration test project
- [x] T014 Add project reference to Biotrackr.Activity.Svc from integration test project
- [x] T015 [P] Create folder structure: Contract/, E2E/, Fixtures/, Collections/, Helpers/ in integration test project
- [x] T016 [P] Create appsettings.Test.json with Cosmos DB Emulator connection strings in integration test project root
- [x] T017 Verify integration test project builds successfully with dotnet build

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Complete Unit Test Coverage for Missing Components (Priority: P1) 🎯 MVP

**Goal**: Expand existing unit tests to reach 70% overall coverage and improve edge case handling

**Independent Test**: Run `dotnet test /p:CollectCoverage=true` and verify line coverage ≥70%

### Implementation for User Story 1

- [x] T018 [P] [US1] Configure Program.cs coverage exclusion in Biotrackr.Activity.Svc.UnitTests/Biotrackr.Activity.Svc.UnitTests.csproj
- [x] T019 [P] [US1] Add ActivityWorker cancellation test in Biotrackr.Activity.Svc.UnitTests/WorkerTests/ActivityWorkerShould.cs
- [x] T020 [P] [US1] Add ActivityWorker empty response handling test in Biotrackr.Activity.Svc.UnitTests/WorkerTests/ActivityWorkerShould.cs
- [x] T021 [P] [US1] Add ActivityWorker exception path test in Biotrackr.Activity.Svc.UnitTests/WorkerTests/ActivityWorkerShould.cs
- [x] T022 [P] [US1] Add ActivityService malformed JSON handling test in Biotrackr.Activity.Svc.UnitTests/ServiceTests/ (if not exists)
- [x] T023 [P] [US1] Add ActivityService null response handling test in Biotrackr.Activity.Svc.UnitTests/ServiceTests/ (if not exists)
- [x] T024 [P] [US1] Add CosmosRepository rate limiting exception test in Biotrackr.Activity.Svc.UnitTests/RepositoryTests/ (if not exists)
- [x] T025 [P] [US1] Add CosmosRepository duplicate ID exception test in Biotrackr.Activity.Svc.UnitTests/RepositoryTests/ (if not exists)
- [x] T026 [P] [US1] Add CosmosRepository network failure exception test in Biotrackr.Activity.Svc.UnitTests/RepositoryTests/ (if not exists)
- [x] T027 [US1] Run dotnet test with coverage and verify 70% threshold met
- [x] T028 [US1] Generate HTML coverage report and review uncovered lines
- [x] T029 [US1] Add additional edge case tests for any remaining coverage gaps identified in report

**Checkpoint**: Unit test coverage reaches 100% (exceeds 70% target) and all tests pass in <5 seconds

---

## Phase 4: User Story 2 - Integration Test Project Creation (Priority: P2)

**Goal**: Create properly structured integration test project with all infrastructure

**Independent Test**: Run `dotnet build` on integration test project and verify successful compilation

**Note**: This phase builds on Phase 2 (Foundational) by adding test fixtures and configuration

### Implementation for User Story 2

- [x] T030 [P] [US2] Create IntegrationTestFixture base class in src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.IntegrationTests/Fixtures/IntegrationTestFixture.cs
- [x] T031 [P] [US2] Implement IAsyncLifetime.InitializeAsync in IntegrationTestFixture with Cosmos DB Gateway mode configuration
- [x] T032 [P] [US2] Implement IAsyncLifetime.DisposeAsync in IntegrationTestFixture with database cleanup
- [x] T033 [P] [US2] Create ContractTestFixture class extending IntegrationTestFixture in Fixtures/ContractTestFixture.cs
- [x] T034 [P] [US2] Override InitializeDatabase property to false in ContractTestFixture
- [x] T035 [P] [US2] Create ContractTestCollection xUnit collection definition in Collections/ContractTestCollection.cs
- [x] T036 [P] [US2] Create IntegrationTestCollection xUnit collection definition in Collections/IntegrationTestCollection.cs
- [x] T037 [P] [US2] Create TestDataGenerator helper class in Helpers/TestDataGenerator.cs with activity data generation methods
- [x] T038 [US2] Add in-memory configuration setup to ContractTestFixture with required keys (keyvaulturl, managedidentityclientid, cosmosdbendpoint, applicationinsightsconnectionstring)
- [x] T039 [US2] Add service provider initialization to ContractTestFixture
- [x] T040 [US2] Add Cosmos DB client, database, and container properties to IntegrationTestFixture
- [x] T041 [US2] Verify fixtures compile and integration test project structure is complete

**Checkpoint**: Integration test project structure is complete and ready for test implementation

---

## Phase 5: User Story 3 - Contract Integration Tests (Priority: P2)

**Goal**: Implement fast contract tests that verify service registration and application startup

**Independent Test**: Run `dotnet test --filter "FullyQualifiedName~Contract"` and verify all pass in <5 seconds

### Implementation for User Story 3

- [x] T042 [P] [US3] Create ProgramStartupTests class in Contract/ProgramStartupTests.cs with ContractTestCollection attribute
- [x] T043 [P] [US3] Implement test: ApplicationHost_Should_BuildSuccessfully in ProgramStartupTests
- [x] T044 [P] [US3] Implement test: ServiceProvider_Should_ResolveAllRequiredServices in ProgramStartupTests
- [x] T045 [P] [US3] Implement test: Configuration_Should_ContainAllRequiredKeys in ProgramStartupTests
- [x] T046 [P] [US3] Create ServiceRegistrationTests class in Contract/ServiceRegistrationTests.cs with ContractTestCollection attribute
- [x] T047 [P] [US3] Implement test: CosmosClient_Should_BeRegisteredAsSingleton in ServiceRegistrationTests
- [x] T048 [P] [US3] Implement test: SecretClient_Should_BeRegisteredAsSingleton in ServiceRegistrationTests
- [x] T049 [P] [US3] Implement test: CosmosRepository_Should_BeRegisteredAsScoped in ServiceRegistrationTests
- [x] T050 [P] [US3] Implement test: ActivityService_Should_BeRegisteredAsScoped in ServiceRegistrationTests
- [x] T051 [P] [US3] Implement test: FitbitService_Should_BeRegisteredAsTransient in ServiceRegistrationTests
- [x] T052 [US3] Run contract tests with filter and verify execution time <5 seconds
- [x] T053 [US3] Verify contract tests pass without requiring Cosmos DB Emulator

**Checkpoint**: All contract tests pass quickly without external dependencies

---

## Phase 6: User Story 4 - E2E Integration Tests with Cosmos DB (Priority: P3)

**Goal**: Implement end-to-end tests that verify data persistence and full workflow with Cosmos DB

**Independent Test**: Run `dotnet test --filter "FullyQualifiedName~E2E"` and verify all pass in <30 seconds

### Implementation for User Story 4

- [ ] T054 [P] [US4] Create CosmosRepositoryTests class in E2E/CosmosRepositoryTests.cs with IntegrationTestCollection attribute
- [x] T055 [P] [US4] Implement ClearContainerAsync helper method in CosmosRepositoryTests for test isolation
- [x] T056 [P] [US4] Implement test: CreateItemAsync_Should_PersistActivityDocument in CosmosRepositoryTests
- [x] T057 [P] [US4] Implement test: CreateItemAsync_Should_UseCorrectPartitionKey in CosmosRepositoryTests
- [x] T058 [P] [US4] Implement test: GetItemAsync_Should_RetrieveActivityDocumentById in CosmosRepositoryTests
- [x] T059 [P] [US4] Implement test: GetItemsAsync_Should_QueryActivityDocumentsByDate in CosmosRepositoryTests
- [x] T060 [P] [US4] Create ActivityServiceTests class in E2E/ActivityServiceTests.cs with IntegrationTestCollection attribute
- [x] T061 [P] [US4] Implement ClearContainerAsync helper method in ActivityServiceTests
- [x] T062 [P] [US4] Implement test: MapAndSaveDocument_Should_TransformAndPersistData in ActivityServiceTests
- [x] T063 [P] [US4] Implement test: MapAndSaveDocument_Should_HandleMultipleDocuments in ActivityServiceTests
- [x] T064 [P] [US4] Create ActivityWorkerTests class in E2E/ActivityWorkerTests.cs with IntegrationTestCollection attribute
- [x] T065 [P] [US4] Implement ClearContainerAsync helper method in ActivityWorkerTests
- [x] T066 [P] [US4] Setup mocked IFitbitService for ActivityWorkerTests
- [x] T067 [P] [US4] Implement test: ExecuteAsync_Should_CompleteEndToEndWorkflow in ActivityWorkerTests
- [x] T068 [P] [US4] Implement test: ExecuteAsync_Should_SaveActivityDocumentsToCosmosDB in ActivityWorkerTests
- [x] T069 [US4] Start Cosmos DB Emulator and verify it's running on localhost:8081
- [x] T070 [US4] Run E2E tests with filter and verify all pass in <30 seconds
- [x] T071 [US4] Verify test isolation by running E2E tests multiple times
- [x] T072 [US4] Verify E2E tests clean up all test data after execution

**Checkpoint**: All E2E tests pass with real Cosmos DB Emulator and demonstrate proper isolation

---

## Phase 7: User Story 5 - GitHub Actions Workflow Integration (Priority: P3)

**Goal**: Configure CI/CD pipeline to run all test suites automatically

**Independent Test**: Trigger workflow on pull request and verify all test jobs pass

### Implementation for User Story 5

- [x] T073 [US5] Review existing deploy-activity-service.yml workflow structure in .github/workflows/
- [x] T074 [P] [US5] Add checks: write permission to workflow permissions section
- [x] T075 [P] [US5] Update unit test job working-directory to ./src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.UnitTests
- [x] T076 [US5] Create contract test job using template-dotnet-run-contract-tests.yml reusable workflow
- [x] T077 [US5] Configure contract test job with test-filter: 'FullyQualifiedName~Contract'
- [x] T078 [US5] Configure contract test job with working-directory: ./src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.IntegrationTests
- [x] T079 [US5] Set contract test job to run in parallel with unit tests (needs: env-setup)
- [x] T080 [US5] Create E2E test job using template-dotnet-run-e2e-tests.yml reusable workflow
- [x] T081 [US5] Add Cosmos DB Emulator service to E2E test job using mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
- [x] T082 [US5] Configure E2E test job with test-filter: 'FullyQualifiedName~E2E'
- [x] T083 [US5] Configure E2E test job with working-directory: ./src/Biotrackr.Activity.Svc/Biotrackr.Activity.Svc.IntegrationTests
- [x] T084 [US5] Set E2E test job to run after contract tests complete (needs: [env-setup, run-contract-tests])
- [x] T085 [US5] Verify DOTNET_VERSION environment variable is set to 9.0.x matching test project target
- [ ] T086 [US5] Test workflow locally using act or by pushing to feature branch
- [ ] T087 [US5] Create pull request and verify all test jobs execute successfully
- [ ] T088 [US5] Verify test results are published via dorny/test-reporter@v1
- [ ] T089 [US5] Verify coverage reports are uploaded as artifacts
- [ ] T090 [US5] Verify workflow completes in <10 minutes total

**Checkpoint**: CI/CD pipeline executes all tests automatically with proper reporting

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and final validation

- [ ] T091 [P] Update README.md with instructions for running integration tests locally
- [ ] T092 [P] Document Cosmos DB Emulator setup requirements in project documentation
- [ ] T093 [P] Add troubleshooting section to quickstart.md based on common issues
- [ ] T094 Validate all tests pass on clean clone of repository
- [ ] T095 Run quickstart.md validation to ensure developer experience is smooth
- [ ] T096 Review and update .github/copilot-instructions.md if needed
- [ ] T097 Generate final coverage report and verify all success criteria met

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Can start immediately (expands existing tests) - RECOMMENDED FIRST
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) completion
- **User Story 3 (Phase 5)**: Depends on User Story 2 completion (needs fixtures)
- **User Story 4 (Phase 6)**: Depends on User Story 2 completion (needs fixtures)
- **User Story 5 (Phase 7)**: Depends on User Stories 1, 3, and 4 completion (needs working tests)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Independent - No dependencies on other stories (works with existing unit tests)
- **User Story 2 (P2)**: Depends on Foundational phase - Creates infrastructure for US3 and US4
- **User Story 3 (P2)**: Depends on US2 (needs ContractTestFixture) - Independent from US4
- **User Story 4 (P3)**: Depends on US2 (needs IntegrationTestFixture) - Independent from US3
- **User Story 5 (P3)**: Depends on US1, US3, US4 (needs all tests working)

### Within Each User Story

**User Story 1 (Unit Tests)**:
- T018 (coverage exclusion) before T027 (coverage verification)
- T019-T026 (test additions) can all run in parallel [P]
- T027-T029 (verification) run sequentially after tests added

**User Story 2 (Project Creation)**:
- T030-T037 all parallelizable [P] (different files)
- T038-T041 sequential (depends on fixtures being created)

**User Story 3 (Contract Tests)**:
- T042-T051 all parallelizable [P] (different test methods)
- T052-T053 sequential (verification)

**User Story 4 (E2E Tests)**:
- T054-T068 all parallelizable [P] (different test methods/classes)
- T069-T072 sequential (execution and verification)

**User Story 5 (Workflow)**:
- T074-T075 can run in parallel with T076-T084
- T086-T090 sequential (execution and verification)

### Parallel Opportunities

**Phase 1 (Setup)**: T003 and T004 can run in parallel

**Phase 2 (Foundational)**: T007-T013 (NuGet packages) all run in parallel, T015-T016 run in parallel

**Phase 3 (US1)**: T019-T026 (test implementations) all run in parallel

**Phase 4 (US2)**: T030-T037 (fixture/helper classes) all run in parallel

**Phase 5 (US3)**: T042-T051 (contract tests) all run in parallel

**Phase 6 (US4)**: T054-T068 (E2E tests) all run in parallel

**Phase 7 (US5)**: T074-T075 and T076-T084 can overlap

**Phase 8 (Polish)**: T091-T093 (documentation) all run in parallel

---

## Parallel Example: User Story 1 (Unit Test Expansion)

```bash
# All edge case tests can be written in parallel
# Assign to different developers or work on in parallel branches

# Developer 1: Worker tests
git checkout -b us1-worker-tests
# Implement T019, T020, T021

# Developer 2: Service tests  
git checkout -b us1-service-tests
# Implement T022, T023

# Developer 3: Repository tests
git checkout -b us1-repository-tests
# Implement T024, T025, T026

# Then merge all branches and run T027-T029 for verification
```

## Parallel Example: User Story 3 (Contract Tests)

```bash
# All contract tests can be written in parallel

# Developer 1: Program startup tests
git checkout -b us3-program-tests
# Implement T042-T045

# Developer 2: Service registration tests
git checkout -b us3-registration-tests
# Implement T046-T051

# Then merge and run T052-T053 for verification
```

---

## Recommended MVP Scope

**Minimum Viable Product** includes:
- ✅ Phase 1: Setup
- ✅ Phase 2: Foundational
- ✅ Phase 3: User Story 1 (Unit Test Coverage - P1)

This MVP delivers immediate value by achieving 70% code coverage and improving test quality without requiring integration test infrastructure.

**Extended MVP** (recommended):
- ✅ All of Minimum MVP
- ✅ Phase 4: User Story 2 (Integration Test Project - P2)
- ✅ Phase 5: User Story 3 (Contract Tests - P2)

This provides a complete testing foundation with fast contract tests that validate service configuration.

**Full Implementation**:
- All phases including E2E tests (US4) and CI/CD integration (US5)

---

## Implementation Strategy

1. **Start with MVP**: Implement Phase 1-3 (User Story 1) first to achieve 70% coverage goal
2. **Build infrastructure**: Add Phase 4 (User Story 2) to establish test project structure
3. **Add contract tests**: Implement Phase 5 (User Story 3) for fast service validation
4. **Add E2E tests**: Implement Phase 6 (User Story 4) for comprehensive integration testing
5. **Automate in CI/CD**: Implement Phase 7 (User Story 5) to catch regressions
6. **Polish**: Complete Phase 8 for documentation and developer experience

Each phase delivers independently testable value and can be merged incrementally.

---

## Validation Checklist

After completing all tasks, verify:

- [ ] Unit test coverage ≥70% (dotnet test /p:CollectCoverage=true)
- [ ] All unit tests pass in <5 seconds
- [ ] Contract tests pass in <5 seconds without Cosmos DB
- [ ] E2E tests pass in <30 seconds with Cosmos DB Emulator
- [ ] GitHub Actions workflow completes in <10 minutes
- [ ] Test results published in pull request
- [ ] Coverage reports uploaded as artifacts
- [ ] Test isolation verified (no cross-test contamination)
- [ ] All fixtures implement IAsyncLifetime correctly
- [ ] Service lifetimes verified (Singleton/Scoped/Transient)
- [ ] Gateway mode configured for Cosmos DB Emulator
- [ ] Program.cs excluded from coverage
- [ ] No flaky tests present
- [ ] Documentation updated
- [ ] Quickstart guide validated
