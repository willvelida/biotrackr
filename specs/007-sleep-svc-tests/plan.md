# Implementation Plan: Sleep Service Test Coverage and Integration Tests

**Branch**: `007-sleep-svc-tests` | **Date**: October 31, 2025 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/007-sleep-svc-tests/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Expand unit test coverage for the Sleep Service to achieve ≥70% code coverage and implement integration tests (Contract and E2E) that can be executed in GitHub Actions workflows. This follows established patterns from Weight Service and Activity Service implementations, ensuring consistency across Biotrackr microservices. Key elements include: comprehensive unit tests for all components (SleepWorker, services, repository), Contract tests for service registration and startup validation, E2E tests with Cosmos DB Emulator using Gateway connection mode, proper test isolation with container cleanup, and integration with CI/CD pipelines using reusable workflow templates.

## Technical Context

**Language/Version**: C# / .NET 9.0  
**Primary Dependencies**: xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0  
**Storage**: Azure Cosmos DB (via Emulator in tests - mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest)  
**Testing**: xUnit test framework with test pyramid approach (unit, contract, E2E)  
**Target Platform**: .NET 9.0 Worker Service (background service), GitHub Actions (ubuntu-latest runners)  
**Project Type**: Worker service with integration test projects  
**Performance Goals**: Unit tests <5s total, Contract tests <5s total, E2E tests <30s total, all workflows complete <10 minutes  
**Constraints**: ≥70% code coverage required, tests must be reliable (no flaky tests), test isolation required (cleanup between tests)  
**Scale/Scope**: 3 main components to test (SleepWorker, SleepService, CosmosRepository), 2 test projects (UnitTests, IntegrationTests), ~40-50 total tests expected

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Code Quality Excellence**: Design follows SOLID principles (single responsibility for test fixtures, services), minimal cognitive load through clear test naming conventions (MethodName_Should_ExpectedBehavior), clear separation of concerns (unit vs integration vs E2E tests)
- [x] **Testing Strategy**: Test pyramid explicitly planned (unit ≥70% coverage, contract tests for DI validation, E2E tests for critical workflows), TDD approach implied by creating tests before implementation gaps are filled
- [x] **User Experience**: Consistent test patterns across all microservices (matches Weight/Activity Service patterns), clear test output and error messages for debugging
- [x] **Performance Requirements**: Response time targets defined (<5s for unit/contract, <30s for E2E, <10min for full workflow), scalability through parallel test execution (unit + contract run in parallel)
- [x] **Technical Debt**: Potential debt identified (missing SleepWorker tests, duplicate service registration), mitigation strategies planned (fix duplicate registration, add [ExcludeFromCodeCoverage] attribute), GitHub Issues to be created for tracking

**Status**: ✅ All constitution checks pass. No complexity violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/007-sleep-svc-tests/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (already created during spec generation)
├── data-model.md        # Phase 1 output (already created during spec generation)
├── quickstart.md        # Phase 1 output (already created during spec generation)
├── checklists/
│   └── requirements.md  # Specification validation checklist (✅ PASSED)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── test-contracts.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/Biotrackr.Sleep.Svc/
├── Biotrackr.Sleep.Svc/                    # Main worker service
│   ├── Program.cs                          # Entry point (needs [ExcludeFromCodeCoverage])
│   ├── Configuration/
│   │   └── Settings.cs
│   ├── Models/
│   │   ├── SleepDocument.cs
│   │   └── FitbitEntities/
│   ├── Repositories/
│   │   ├── CosmosRepository.cs
│   │   └── Interfaces/
│   │       └── ICosmosRepository.cs
│   ├── Services/
│   │   ├── FitbitService.cs                # Needs duplicate registration fix
│   │   ├── SleepService.cs
│   │   └── Interfaces/
│   │       ├── IFitbitService.cs
│   │       └── ISleepService.cs
│   └── Worker/
│       └── SleepWorker.cs                  # Needs tests (currently 0% coverage)
│
├── Biotrackr.Sleep.Svc.UnitTests/          # Existing unit test project
│   ├── RepositoryTests/
│   │   └── CosmosRepositoryShould.cs       # ✅ Exists (2 tests)
│   ├── ServiceTests/
│   │   ├── FitbitServiceShould.cs          # ✅ Exists (5 tests)
│   │   └── SleepServiceShould.cs           # ✅ Exists (2 tests)
│   └── WorkerTests/                        # ❌ NEW - needs creation
│       └── SleepWorkerShould.cs            # To be created (≥4 tests)
│
└── Biotrackr.Sleep.Svc.IntegrationTests/   # ❌ NEW - to be created
    ├── Contract/                            # Fast tests, no DB
    │   ├── ProgramStartupTests.cs
    │   └── ServiceRegistrationTests.cs
    ├── E2E/                                 # Full workflow tests with Cosmos DB
    │   ├── CosmosRepositoryTests.cs
    │   ├── SleepServiceTests.cs
    │   └── SleepWorkerTests.cs
    ├── Fixtures/
    │   ├── ContractTestFixture.cs          # No DB initialization
    │   └── IntegrationTestFixture.cs       # With DB initialization
    ├── Collections/
    │   ├── ContractTestCollection.cs       # xUnit collection for contract tests
    │   └── IntegrationTestCollection.cs    # xUnit collection for E2E tests
    ├── Helpers/
    │   └── TestDataGenerator.cs            # Helper for generating test data
    └── appsettings.Test.json               # Test configuration
```

### GitHub Workflows

```text
.github/workflows/
├── deploy-sleep-service.yml                # To be updated with new test jobs
└── template-dotnet-run-*.yml              # Existing reusable templates (no changes)
```

**Structure Decision**: Following established Worker Service pattern with separate unit and integration test projects. Integration tests use Contract/E2E separation pattern matching Weight Service (003-weight-svc-integration-tests) and Activity Service (005-activity-svc-tests). This ensures consistency across all Biotrackr microservices and enables parallel test execution (unit + contract tests run simultaneously, E2E tests run after with Cosmos DB Emulator).

## Complexity Tracking

> **No violations to justify** - All design decisions align with constitution principles and established patterns.

## Phase 0: Research (✅ COMPLETED)

**Status**: Research was completed during specification generation phase.

**Key Findings** (from [research.md](./research.md)):

### Current State Analysis
- **Existing Coverage**: ~40-50% (9 tests total)
- **Gaps Identified**: 
  - ❌ No SleepWorker tests
  - ❌ Program.cs not excluded from coverage
  - ⚠️ Duplicate IFitbitService registration
  - ❌ No integration tests

### Reference Implementation Patterns
- **Weight Service**: Contract/E2E separation, Gateway mode for Cosmos DB
- **Activity Service**: Test isolation via ClearContainerAsync(), [ExcludeFromCodeCoverage] attribute

### Technical Decisions
1. **Test Framework**: xUnit 2.9.3 (parallelization, collection fixtures)
2. **Assertions**: FluentAssertions 8.4.0 (readable syntax, better error messages)
3. **Mocking**: Moq 4.20.72 (standard .NET mocking library)
4. **Test Data**: AutoFixture 4.18.1 (rapid test data generation)
5. **Coverage**: coverlet.collector 6.0.4 (Cobertura format for GitHub Actions)
6. **Cosmos DB Mode**: Gateway mode to avoid SSL negotiation issues with emulator

### Service Lifetime Resolution
**Issue Found**: Duplicate registration for `IFitbitService`
- First: `services.AddScoped<IFitbitService, FitbitService>()`
- Second: `services.AddHttpClient<IFitbitService, FitbitService>()` (overrides to Transient)

**Resolution**: Remove `AddScoped` line, keep only `AddHttpClient` registration

**Reference**: [Decision Record: Service Lifetime Registration](../../docs/decision-records/2025-10-28-service-lifetime-registration.md)

## Phase 1: Design & Contracts

### Data Model (✅ COMPLETED)

**Status**: Data model was completed during specification generation phase.

**Key Entities** (from [data-model.md](./data-model.md)):

1. **Test Fixture Configuration**
   - Properties: CosmosDbEndpoint, CosmosDbAccountKey, DatabaseName, ContainerName, InitializeDatabase
   - Validation: URI format, max lengths, InitializeDatabase flag per test type

2. **Test Collection Configuration**
   - Properties: CollectionName, FixtureType, DisableParallelization
   - Rules: Contract tests parallelize, E2E tests sequential

3. **Coverage Report Data**
   - Properties: ProjectName, LineCoverage, BranchCoverage, CoverageFormat, ExcludedFiles
   - Target: LineCoverage ≥ 70.0%

### API Contracts

#### Test Contracts

**Contract Test Interface** (validates service registration without external dependencies):

```csharp
// ProgramStartupTests.cs Contract
public interface IStartupValidation
{
    // Test: Application_ShouldResolveAllServices
    // Given: Service provider with registered services
    // When: Resolve each service type
    // Then: All services resolve successfully (not null)
    void ValidateServiceResolution(IServiceProvider serviceProvider);
    
    // Test: Application_ShouldBuildHost_WithoutExceptions
    // Given: Host builder with test configuration
    // When: Build host
    // Then: No exceptions thrown during build
    void ValidateHostBuilding();
}

// ServiceRegistrationTests.cs Contract
public interface IServiceLifetimeValidation
{
    // Test: SingletonServices_ShouldReturnSameInstance
    // Given: Service provider with singleton registrations
    // When: Resolve same service multiple times
    // Then: Same instance returned each time
    void ValidateSingletonLifetime<TService>(IServiceProvider serviceProvider);
    
    // Test: ScopedServices_ShouldReturnSameInstance_WithinScope
    // Given: Service provider with scoped registrations
    // When: Resolve same service multiple times within one scope
    // Then: Same instance returned within scope, different across scopes
    void ValidateScopedLifetime<TService>(IServiceProvider serviceProvider);
    
    // Test: TransientServices_ShouldReturnDifferentInstances
    // Given: Service provider with transient registrations
    // When: Resolve same service multiple times
    // Then: Different instance returned each time
    void ValidateTransientLifetime<TService>(IServiceProvider serviceProvider);
}
```

**E2E Test Interface** (validates full workflow with Cosmos DB):

```csharp
// CosmosRepositoryTests.cs Contract
public interface ICosmosRepositoryE2EValidation
{
    // Test: CreateSleepDocument_ShouldPersistToDatabase
    // Given: Sleep document with valid data
    // When: Call CreateSleepDocument
    // Then: Document persists in Cosmos DB and can be queried
    Task ValidateDocumentCreation(SleepDocument document, Container container);
    
    // Test: CreateSleepDocument_ShouldHandlePartitionKey_Correctly
    // Given: Sleep document with DocumentType = "Sleep"
    // When: Create document
    // Then: Document stored with correct partition key
    Task ValidatePartitionKeyHandling(Container container);
}

// SleepServiceTests.cs Contract
public interface ISleepServiceE2EValidation
{
    // Test: MapAndSaveDocument_ShouldTransformAndPersist
    // Given: Date string and SleepResponse
    // When: Call MapAndSaveDocument
    // Then: Document created in Cosmos DB with correct mapping
    Task ValidateEndToEndMapping(string date, SleepResponse response, Container container);
}

// SleepWorkerTests.cs Contract
public interface ISleepWorkerE2EValidation
{
    // Test: ExecuteAsync_ShouldCompleteFullWorkflow
    // Given: Mocked Fitbit service, real Cosmos DB
    // When: Execute worker
    // Then: Data fetched, mapped, and saved successfully
    Task ValidateCompleteWorkflow(Container container);
}
```

**Test Isolation Contract**:

```csharp
// ClearContainerAsync pattern for test isolation
public interface ITestIsolation
{
    // Called before each test in IAsyncLifetime.InitializeAsync()
    // Purpose: Ensure clean state for each test
    // Implementation: Query all documents, delete by id+partitionKey
    Task ClearContainerAsync(Container container);
}
```

### Quickstart Guide (✅ COMPLETED)

**Status**: Quickstart guide was completed during specification generation phase.

See [quickstart.md](./quickstart.md) for step-by-step implementation instructions covering:
1. Integration test project creation
2. NuGet package installation
3. Directory structure setup
4. Unit test completion (SleepWorker tests)
5. Integration test fixtures
6. Contract test implementation
7. E2E test implementation
8. GitHub workflow updates

## Phase 2: Task Breakdown

**Note**: Detailed task breakdown will be generated by the `/speckit.tasks` command (not part of this plan).

**High-Level Task Groups**:

1. **Unit Test Completion** (~4 hours)
   - Create SleepWorkerShould.cs with ≥4 tests
   - Add [ExcludeFromCodeCoverage] to Program.cs
   - Fix duplicate IFitbitService registration
   - Verify 70% coverage threshold

2. **Integration Test Project Setup** (~2 hours)
   - Create IntegrationTests project
   - Add NuGet packages
   - Create directory structure
   - Configure appsettings.Test.json

3. **Test Fixtures Implementation** (~3 hours)
   - Implement ContractTestFixture (no DB)
   - Implement IntegrationTestFixture (with DB)
   - Create xUnit collection definitions
   - Test fixture initialization

4. **Contract Tests** (~3 hours)
   - Implement ProgramStartupTests
   - Implement ServiceRegistrationTests
   - Verify service lifetimes (Singleton, Scoped, Transient)

5. **E2E Tests** (~4 hours)
   - Implement CosmosRepositoryTests with cleanup
   - Implement SleepServiceTests
   - Implement SleepWorkerTests
   - Verify test isolation

6. **GitHub Workflow Integration** (~2 hours)
   - Update deploy-sleep-service.yml
   - Add contract test job
   - Add E2E test job with Cosmos DB Emulator
   - Configure test result publishing

**Estimated Total**: 18-20 hours of implementation work

## Success Criteria Validation

From [spec.md](./spec.md) Success Criteria section:

- [ ] **SC-001**: Overall code coverage for Biotrackr.Sleep.Svc project reaches at least 70% line coverage
- [ ] **SC-002**: Unit test suite executes in under 5 seconds total with 100% pass rate
- [ ] **SC-003**: Contract integration tests execute in under 5 seconds total without external dependencies
- [ ] **SC-004**: E2E integration tests execute in under 30 seconds total with Cosmos DB Emulator
- [ ] **SC-005**: All test jobs in GitHub Actions workflow complete successfully within 10 minutes
- [ ] **SC-006**: Test coverage reports uploaded as artifacts and published in pull request comments
- [ ] **SC-007**: Integration test project structure matches established Weight Service pattern with 100% consistency
- [ ] **SC-008**: E2E tests demonstrate 100% test isolation (no test failures due to leftover data)
- [ ] **SC-009**: SleepWorker tests cover at least 4 distinct scenarios
- [ ] **SC-010**: Integration tests achieve at least 80% coverage of integration points

## References

- [Feature Specification](./spec.md)
- [Research Document](./research.md)
- [Data Model](./data-model.md)
- [Quickstart Guide](./quickstart.md)
- [Decision Record: Integration Test Project Structure](../../docs/decision-records/2025-10-28-integration-test-project-structure.md)
- [Decision Record: Service Lifetime Registration](../../docs/decision-records/2025-10-28-service-lifetime-registration.md)
- [Common Resolutions](.specify/memory/common-resolutions.md)
- [Weight Service Integration Tests](../003-weight-svc-integration-tests/spec.md)
- [Activity Service Tests](../005-activity-svc-tests/spec.md)

## Next Steps

1. Run `/speckit.tasks` to generate detailed task breakdown with acceptance criteria
2. Implement tasks systematically following priority order (P1 → P2 → P3)
3. Validate each phase against success criteria
4. Update feature specification status to "Implemented" when complete
