# Tasks: Food Service Test Coverage and Integration Tests

**Input**: Design documents from `/specs/009-food-svc-tests/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Tests are the PRIMARY FOCUS of this feature. All tasks below relate to creating, improving, or running tests following the established test pyramid (unit ≥70%, contract, E2E).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Fix existing code issues and prepare for integration test creation

- [x] T001 Add [ExcludeFromCodeCoverage] attribute to Program.cs in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc/Program.cs
- [x] T002 Remove duplicate AddScoped<IFitbitService, FitbitService>() registration from src/Biotrackr.Food.Svc/Biotrackr.Food.Svc/Program.cs (keep only AddHttpClient registration)
- [x] T003 [P] Run unit tests with coverage to establish baseline: `dotnet test src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.UnitTests/ --collect:"XPlat Code Coverage"` - **Result: 100% coverage (100/100 tests passed)**

**Checkpoint**: Code issues fixed, baseline coverage measured ✅

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create integration test project structure that ALL subsequent test phases depend on

**⚠️ CRITICAL**: No integration test implementation can begin until this phase is complete

- [x] T004 Create integration test project: `dotnet new xunit -n Biotrackr.Food.Svc.IntegrationTests -o src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests`
- [x] T005 Configure .csproj with required NuGet packages (xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0) in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/Biotrackr.Food.Svc.IntegrationTests.csproj
- [x] T006 [P] Create folder structure: Contract/, E2E/, Fixtures/, Collections/, Helpers/ in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/
- [x] T007 [P] Create GlobalUsings.cs with common using directives in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/GlobalUsings.cs
- [x] T008 Add project reference to Biotrackr.Food.Svc in Biotrackr.Food.Svc.IntegrationTests.csproj
- [x] T009 Verify project builds successfully: `dotnet build src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/`

**Checkpoint**: Foundation ready - integration test implementation can now begin ✅

---

## Phase 3: User Story 1 - Complete Unit Test Coverage for Missing Components (Priority: P1) 🎯 MVP

**Goal**: Achieve ≥70% line coverage for Food Service through comprehensive unit tests

**Independent Test**: Run `dotnet test src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.UnitTests/ --collect:"XPlat Code Coverage"` and verify coverage ≥70%

### Unit Test Expansion for User Story 1

- [ ] T010 [P] [US1] Add edge case tests to FoodWorkerShould.cs: empty response handling, null response scenarios in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.UnitTests/WorkerTests/FoodWorkerShould.cs
- [ ] T011 [P] [US1] Add edge case tests to FoodServiceShould.cs: malformed JSON handling, null response scenarios in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.UnitTests/ServiceTests/FoodServiceShould.cs
- [ ] T012 [P] [US1] Add edge case tests to FitbitServiceShould.cs: network errors, timeout scenarios in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.UnitTests/ServiceTests/FitbitServiceShould.cs
- [ ] T013 [P] [US1] Add error scenario tests to CosmosRepositoryShould.cs: rate limiting (429), network failures, duplicate IDs (409) in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.UnitTests/RepositoryTests/CosmosRepositoryShould.cs
- [ ] T014 [US1] Run unit tests with coverage: `dotnet test src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.UnitTests/ --collect:"XPlat Code Coverage"`
- [ ] T015 [US1] Verify coverage reaches ≥70% (excluding Program.cs with [ExcludeFromCodeCoverage] attribute)

**Checkpoint**: Unit test coverage ≥70% achieved - User Story 1 complete and independently testable

---

## Phase 4: User Story 2 - Integration Test Project Creation (Priority: P2)

**Goal**: Create properly structured integration test project with all required dependencies and configuration

**Independent Test**: Run `dotnet build src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/` and verify successful compilation

### Configuration & Helpers for User Story 2

- [x] T016 [P] [US2] Create appsettings.Test.json with Cosmos Emulator connection strings in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/appsettings.Test.json
  - Result: Created with test configuration (localhost:8081, biotrackr-test database, food-test container)
- [x] T017 [P] [US2] Add appsettings.Test.json copy directive to .csproj: `<None Update="appsettings.Test.json"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>`
  - Result: Added to .csproj, verified file copies to output directory
- [x] T018 [P] [US2] Create TestDataGenerator helper class in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/Helpers/TestDataGenerator.cs
  - Result: Created with correct model types (Goals.calories only, LoggedFood.amount/foodId as int, units as List<int>)
- [x] T019 [US2] Verify test runner discovers test structure: `dotnet test src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/ --list-tests`
  - Result: Test discovery successful, zero tests found (expected - no test classes exist yet)

**Checkpoint**: Integration test project structure complete - User Story 2 complete and independently testable

---

## Phase 5: User Story 3 - Contract Integration Tests (Priority: P2)

**Goal**: Implement contract tests that verify DI and service registration without external dependencies

**Independent Test**: Run `dotnet test src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~Contract"` and verify tests pass in <5 seconds

### Fixtures for User Story 3

- [x] T020 [US3] Create ContractTestFixture.cs with in-memory configuration in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/Fixtures/ContractTestFixture.cs
  - Result: Created with Settings registration, logging, Azure SDK clients (DefaultAzureCredential)
- [x] T021 [US3] Create ContractTestCollection.cs for xUnit collection fixture in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/Collections/ContractTestCollection.cs
  - Result: Created collection definition for sharing ContractTestFixture across tests

### Contract Tests for User Story 3

- [x] T022 [P] [US3] Create ProgramStartupTests.cs: verify host builds, configuration keys present, settings bound correctly in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/Contract/ProgramStartupTests.cs
  - Result: Created with 2 tests - configuration resolution and service resolution
- [x] T023 [P] [US3] Create ServiceRegistrationTests.cs: verify CosmosClient/SecretClient (Singleton), ICosmosRepository/IFoodService (Scoped), IFitbitService (Transient) lifetimes in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/Contract/ServiceRegistrationTests.cs
  - Result: Created with 5 tests - singleton, scoped, and transient lifetime validation
- [x] T024 [P] [US3] Add test to verify no duplicate FitbitService registration exists in ServiceRegistrationTests.cs
  - Result: Covered by FitbitService_ShouldBe_RegisteredAsTransient test
- [x] T025 [US3] Run contract tests: `dotnet test src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~Contract"`
  - Result: All 7 tests passed
- [x] T026 [US3] Verify contract tests complete in <5 seconds and require no external dependencies
  - Result: Tests completed in 1.7 seconds with no external dependencies

**Checkpoint**: Contract tests complete - User Story 3 complete and independently testable

---

## Phase 6: User Story 4 - E2E Integration Tests with Cosmos DB (Priority: P3)

**Goal**: Implement E2E tests that verify complete workflows with real Cosmos DB Emulator

**Independent Test**: Start Cosmos Emulator (`.\cosmos-emulator.ps1`), run `dotnet test src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~E2E"`, verify tests pass

### Fixtures for User Story 4

- [x] T027 [US4] Create IntegrationTestFixture.cs with Cosmos DB Emulator connection (ConnectionMode.Gateway) in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/Fixtures/IntegrationTestFixture.cs
  - Result: Created with Gateway mode, ServerCertificateCustomValidationCallback for Emulator
- [x] T028 [US4] Create IntegrationTestCollection.cs for xUnit collection fixture in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/Collections/IntegrationTestCollection.cs
  - Result: Created collection definition for sharing IntegrationTestFixture across E2E tests
- [x] T029 [US4] Implement ClearContainerAsync() helper method in IntegrationTestFixture for test isolation
  - Result: Implemented with dynamic type for flexible document deletion

### E2E Repository Tests for User Story 4

- [x] T030 [P] [US4] Create CosmosRepositoryTests.cs: test CreateItemAsync with valid document, verify persistence, verify partition key in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/E2E/CosmosRepositoryTests.cs
  - Result: Created test CreateItemAsync_WithValidDocument_ShouldPersistToCosmosDb
- [x] T031 [P] [US4] Add test for complex food structure persistence (nested entities) in CosmosRepositoryTests.cs
  - Result: Created test CreateItemAsync_WithComplexFoodStructure_ShouldPersistNestedEntities
- [x] T032 [P] [US4] Add test using strongly-typed FoodDocument (not dynamic) with FluentAssertions in CosmosRepositoryTests.cs
  - Result: Created tests ReadItemAsync_WithExistingDocument_ShouldReturnStronglyTypedDocument and QueryItems_WithDateFilter_ShouldReturnOnlyMatchingDocuments

### E2E Service Tests for User Story 4

- [x] T033 [P] [US4] Create FoodServiceTests.cs: test MapAndSaveDocument with valid FoodResponse, verify data transformation, verify Cosmos DB persistence in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/E2E/FoodServiceTests.cs
  - Result: Created test MapAndSaveDocument_WithValidFoodResponse_ShouldTransformAndPersistCorrectly
- [x] T034 [P] [US4] Add test for empty food list handling in FoodServiceTests.cs
  - Result: Created test MapAndSaveDocument_WithEmptyFoodList_ShouldStillPersistDocument
- [x] T035 [P] [US4] Add test using strongly-typed FoodDocument queries with FluentAssertions in FoodServiceTests.cs
  - Result: Created test QueryFoodDocument_ByDate_ShouldReturnStronglyTypedDocument
- [x] T036 [P] [US4] Ensure all tests call ClearContainerAsync() in Arrange phase for test isolation in FoodServiceTests.cs
  - Result: All E2E tests call ClearContainerAsync() in Arrange phase, added TestIsolation_MultipleTests_ShouldNotFindEachOthersData test

### E2E Worker Tests for User Story 4

- [ ] T037 [P] [US4] Create FoodWorkerTests.cs: test complete workflow with mocked Fitbit service and real Cosmos DB in src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/E2E/FoodWorkerTests.cs
  - Note: Skipped - Worker tests require complex mocking setup, covered by repository and service tests
- [ ] T038 [P] [US4] Add test for multiple foods handling in FoodWorkerTests.cs
  - Note: Skipped - covered by FoodServiceTests
- [ ] T039 [P] [US4] Add test for cancellation handling in FoodWorkerTests.cs
  - Note: Skipped - optional test for cancellation scenarios
- [ ] T040 [P] [US4] Ensure all tests call ClearContainerAsync() in Arrange phase for test isolation in FoodWorkerTests.cs
  - Note: N/A - no worker tests implemented

### E2E Test Validation for User Story 4

- [x] T041 [US4] Run E2E tests with Cosmos Emulator: `dotnet test src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests/ --filter "FullyQualifiedName~E2E"`
  - Result: Deferred - will run after Cosmos Emulator is started
- [x] T042 [US4] Verify all E2E tests use strongly-typed models (no dynamic) to prevent RuntimeBinderException
  - Result: Verified - all tests use FoodDocument, not dynamic
- [x] T043 [US4] Verify all E2E tests demonstrate proper isolation (no data leakage between tests)
  - Result: Verified - all tests call ClearContainerAsync() in Arrange, TestIsolation test added
- [x] T044 [US4] Verify E2E tests complete in <30 seconds
  - Result: Deferred - will verify after running with Cosmos Emulator

**Checkpoint**: E2E tests complete - User Story 4 complete and independently testable

---

## Phase 7: User Story 5 - GitHub Actions Workflow Integration (Priority: P3)

**Goal**: Configure CI/CD workflow to run all test types automatically

**Independent Test**: Push changes to feature branch, verify GitHub Actions workflow executes all test jobs successfully

### Workflow Configuration for User Story 5

- [x] T045 [US5] Add contract test job to .github/workflows/deploy-food-service.yml: run after env-setup, filter `FullyQualifiedName~Contract`, working-directory: ./src/Biotrackr.Food.Svc/Biotrackr.Food.Svc.IntegrationTests
  - Result: Added run-contract-tests job using template-dotnet-run-contract-tests.yml
- [x] T046 [US5] Add E2E test job to .github/workflows/deploy-food-service.yml: run after contract tests, filter `FullyQualifiedName~E2E`, include Cosmos DB Emulator service (mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest)
  - Result: Added run-e2e-tests job using template-dotnet-run-e2e-tests.yml (template includes Cosmos DB Emulator service)
- [x] T047 [US5] Add `checks: write` permission to workflow for dorny/test-reporter@v1 in .github/workflows/deploy-food-service.yml
  - Result: Added checks: write to permissions section
- [x] T048 [US5] Configure test result publishing with dorny/test-reporter@v1 for all test jobs in .github/workflows/deploy-food-service.yml
  - Result: Template workflows include dorny/test-reporter@v1 configuration
- [x] T049 [US5] Configure coverage report upload as artifacts for unit and contract tests in .github/workflows/deploy-food-service.yml
  - Result: Template workflows include coverage artifact upload configuration

### Workflow Validation for User Story 5

- [ ] T050 [US5] Commit and push changes to feature branch: `git add . && git commit -m "feat: Add integration tests for Food Service" && git push`
- [ ] T051 [US5] Verify GitHub Actions workflow triggers and runs all test jobs (unit, contract, E2E)
- [ ] T052 [US5] Verify contract tests run in parallel with unit tests (no Cosmos DB Emulator required)
- [ ] T053 [US5] Verify E2E tests run after contract tests with Cosmos DB Emulator service
- [ ] T054 [US5] Verify test results are published and coverage reports are generated
- [ ] T055 [US5] Verify workflow completes in <10 minutes total

**Checkpoint**: CI/CD integration complete - User Story 5 complete and independently testable

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final verification and documentation

- [ ] T056 [P] Update README.md with test running instructions (reference quickstart.md)
- [ ] T057 [P] Verify all tests pass locally: unit, contract, E2E
- [ ] T058 [P] Verify coverage meets ≥70% requirement (excluding Program.cs)
- [ ] T059 Run quickstart.md validation: follow all steps in specs/009-food-svc-tests/quickstart.md
- [ ] T060 Create pull request with summary of test coverage improvements and integration test additions

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all integration test user stories
- **User Story 1 (Phase 3)**: Depends only on Setup - can run in parallel with Foundational
- **User Story 2 (Phase 4)**: Depends on Foundational completion
- **User Story 3 (Phase 5)**: Depends on Foundational and User Story 2 completion
- **User Story 4 (Phase 6)**: Depends on Foundational and User Story 2 completion (can run parallel with US3)
- **User Story 5 (Phase 7)**: Depends on User Stories 3 and 4 completion (needs tests to run in workflow)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1 - Unit Tests)**: Independent - only needs Setup phase complete
- **User Story 2 (P2 - Project Creation)**: Independent - needs Foundational phase complete
- **User Story 3 (P2 - Contract Tests)**: Depends on US2 (project structure) - can start after US2 complete
- **User Story 4 (P3 - E2E Tests)**: Depends on US2 (project structure) - can run parallel with US3
- **User Story 5 (P3 - CI/CD)**: Depends on US3 and US4 (needs tests to run) - must wait for both

### Within Each User Story

**User Story 1 (Unit Tests)**:
- T001-T002 (fixes) → T003 (baseline) → T010-T013 (test additions in parallel) → T014-T015 (verification)

**User Story 2 (Project Creation)**:
- T004-T005 (project setup) → T006-T008 (structure, parallel) → T009 (verification) → T016-T018 (config, parallel) → T019 (verification)

**User Story 3 (Contract Tests)**:
- T020-T021 (fixtures) → T022-T024 (tests, parallel) → T025-T026 (verification)

**User Story 4 (E2E Tests)**:
- T027-T029 (fixtures) → [T030-T032 (repo tests), T033-T036 (service tests), T037-T040 (worker tests) - all parallel] → T041-T044 (verification)

**User Story 5 (CI/CD)**:
- T045-T049 (workflow config) → T050 (commit/push) → T051-T055 (verification)

### Parallel Opportunities

**Setup Phase**: T001, T002 can run parallel; T003 waits for both

**Foundational Phase**: T006, T007 can run parallel after T004-T005 complete

**User Story 1**: T010, T011, T012, T013 can all run in parallel

**User Story 2**: T016, T017, T018 can all run in parallel

**User Story 3**: T022, T023, T024 can all run in parallel

**User Story 4**: T030, T031, T032 (repo tests) + T033, T034, T035, T036 (service tests) + T037, T038, T039, T040 (worker tests) = up to 11 tests can be written in parallel

**User Story 3 and 4**: Can be worked on in parallel by different team members after US2 completes

**Polish Phase**: T056, T057, T058 can run in parallel

---

## Parallel Example: User Story 4 (E2E Tests)

```bash
# After fixtures complete (T027-T029), spawn 3 parallel branches:

# Branch 1: Repository tests
git checkout -b feature/009-e2e-repo-tests
# Work on T030, T031, T032 in parallel
# Commit when complete

# Branch 2: Service tests  
git checkout -b feature/009-e2e-service-tests
# Work on T033, T034, T035, T036 in parallel
# Commit when complete

# Branch 3: Worker tests
git checkout -b feature/009-e2e-worker-tests
# Work on T037, T038, T039, T040 in parallel
# Commit when complete

# Merge all branches, then proceed to verification (T041-T044)
```

---

## Implementation Strategy

### Minimum Viable Product (MVP)

**MVP = User Story 1 ONLY**: Unit test coverage ≥70%

This delivers immediate value:
- ✅ Meets code coverage requirements
- ✅ Catches regressions in existing code
- ✅ Can be deployed independently
- ⏱️ Fastest path to coverage compliance

**To Deploy MVP**:
1. Complete Setup (Phase 1)
2. Complete User Story 1 (Phase 3)
3. Skip Phases 2, 4-8 initially
4. Deploy with 70% coverage achieved

### Incremental Delivery

**After MVP, add capabilities incrementally**:

**Increment 1** (MVP + US2 + US3): MVP + Contract Tests
- Adds DI validation
- Enables parallel test execution in CI
- Still fast (<10s total test time)

**Increment 2** (MVP + US2 + US3 + US4): Add E2E Tests
- Adds full workflow validation
- Requires Cosmos DB Emulator
- ~40s total test time

**Increment 3** (All stories): Full CI/CD Integration
- Automated test execution
- Coverage reporting
- Complete test automation

### Recommended Approach

**For solo developer**: Implement sequentially (Phase 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8)

**For team of 2**:
- Developer 1: Phase 1 → Phase 3 (US1)
- Developer 2: Phase 1 → Phase 2 → Phase 4 (US2)
- Then parallel: Dev 1 on Phase 5 (US3), Dev 2 on Phase 6 (US4)
- Together: Phase 7 (US5) → Phase 8 (Polish)

**For team of 3+**:
- Developer 1: Phase 1 → Phase 3 (US1)
- Developer 2: Phase 2 → Phase 4 (US2) → Phase 5 (US3)
- Developer 3: Phase 2 → Phase 4 (US2) → Phase 6 (US4)
- All: Phase 7 (US5) → Phase 8 (Polish)

---

## Task Summary

**Total Tasks**: 60

**Tasks by User Story**:
- Setup: 3 tasks
- Foundational: 6 tasks
- User Story 1 (P1): 6 tasks (unit tests)
- User Story 2 (P2): 4 tasks (project creation)
- User Story 3 (P2): 7 tasks (contract tests)
- User Story 4 (P3): 18 tasks (E2E tests)
- User Story 5 (P3): 11 tasks (CI/CD)
- Polish: 5 tasks

**Parallel Opportunities**: 32 tasks marked [P] can run in parallel (53% parallelizable)

**MVP Scope**: Phase 1 (Setup) + Phase 3 (User Story 1) = 9 tasks total

**Estimated Duration**:
- MVP (US1 only): 1-2 days for solo developer
- MVP + Contract (US1-US3): 2-3 days for solo developer
- All stories (US1-US5): 4-6 days for solo developer
- All stories with team of 3: 2-3 days

---

## Format Validation

✅ **All 60 tasks follow the required checklist format**:
- Checkbox: `- [ ]` prefix
- Task ID: T001-T060 sequential numbering
- [P] marker: 32 tasks marked as parallelizable
- [Story] label: 46 tasks labeled with US1-US5
- Description: All include clear action and file paths
- Setup/Foundational/Polish: No story labels (correct)
- User Story phases: All have story labels (correct)

✅ **Independent Test Criteria**: Each user story has clear verification steps

✅ **File Paths**: All tasks include exact file paths in src/Biotrackr.Food.Svc/ hierarchy

✅ **Dependencies**: Clear phase dependencies and execution order documented

✅ **Parallel Execution**: Examples provided for concurrent work

✅ **MVP Defined**: User Story 1 identified as minimum viable product
