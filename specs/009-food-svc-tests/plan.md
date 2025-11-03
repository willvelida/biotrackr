# Implementation Plan: Food Service Test Coverage and Integration Tests

**Branch**: `009-food-svc-tests` | **Date**: November 3, 2025 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/009-food-svc-tests/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Expand unit test coverage for the Food Service to achieve ≥70% code coverage and implement integration tests (Contract and E2E) that can be executed in GitHub Actions workflows. This follows established patterns from Weight Service, Activity Service, and Sleep Service implementations, ensuring consistency across Biotrackr microservices. Key elements include: comprehensive unit tests for all components (FoodWorker, services, repository), Contract tests for service registration and startup validation, E2E tests with Cosmos DB Emulator using Gateway connection mode, proper test isolation with container cleanup, fixing duplicate FitbitService registration, and integration with CI/CD pipelines using reusable workflow templates.

## Technical Context

**Language/Version**: C# / .NET 9.0  
**Primary Dependencies**: xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0  
**Storage**: Azure Cosmos DB (via Emulator in tests - mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest)  
**Testing**: xUnit test framework with test pyramid approach (unit, contract, E2E)  
**Target Platform**: .NET 9.0 Worker Service (background service), GitHub Actions (ubuntu-latest runners)  
**Project Type**: Worker service with integration test projects  
**Performance Goals**: Unit tests <5s total, Contract tests <5s total, E2E tests <30s total, all workflows complete <10 minutes  
**Constraints**: ≥70% code coverage required, tests must be reliable (no flaky tests), test isolation required (cleanup between tests), strongly-typed models required (no dynamic types with FluentAssertions)  
**Scale/Scope**: 3 main components to test (FoodWorker, FoodService, CosmosRepository), 2 test projects (UnitTests, IntegrationTests), ~40-50 total tests expected

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Code Quality Excellence**: Design follows SOLID principles (single responsibility for test fixtures, services), minimal cognitive load through clear test naming conventions (MethodName_Should_ExpectedBehavior), clear separation of concerns (unit vs integration vs E2E tests), fixes duplicate service registration anti-pattern
- [x] **Testing Strategy**: Test pyramid explicitly planned (unit ≥70% coverage, contract tests for DI validation, E2E tests for critical workflows), follows TDD approach by creating tests to validate existing functionality and fill gaps
- [x] **User Experience**: Consistent test patterns across all microservices (matches Weight/Activity/Sleep Service patterns), clear test output and error messages for debugging, comprehensive documentation in quickstart.md
- [x] **Performance Requirements**: Response time targets defined (<5s for unit/contract, <30s for E2E, <10min for full workflow), scalability through parallel test execution (unit + contract run in parallel), E2E tests run sequentially after contract tests
- [x] **Technical Debt**: Potential debt identified (duplicate FitbitService registration, missing [ExcludeFromCodeCoverage] attribute, incomplete unit test coverage), mitigation strategies planned (fix duplicate registration, add [ExcludeFromCodeCoverage] attribute to Program.cs, fill test gaps), follows common-resolutions.md patterns

**Status**: ✅ All constitution checks pass. No complexity violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/009-food-svc-tests/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── checklists/
│   └── requirements.md  # Specification validation checklist (✅ PASSED)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── test-contracts.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/Biotrackr.Food.Svc/
├── Biotrackr.Food.Svc/                     # Main worker service
│   ├── Program.cs                          # Entry point (needs [ExcludeFromCodeCoverage] + fix duplicate registration)
│   ├── Configuration/
│   │   └── Settings.cs
│   ├── Models/
│   │   ├── FoodDocument.cs
│   │   └── FitbitEntities/
│   │       ├── Food.cs
│   │       ├── FoodResponse.cs
│   │       ├── Goals.cs
│   │       ├── LoggedFood.cs
│   │       ├── NutritionalValues.cs
│   │       ├── Summary.cs
│   │       └── Unit.cs
│   ├── Repositories/
│   │   ├── CosmosRepository.cs
│   │   └── Interfaces/
│   │       └── ICosmosRepository.cs
│   ├── Services/
│   │   ├── FitbitService.cs                # Needs duplicate registration fix (remove AddScoped)
│   │   ├── FoodService.cs
│   │   └── Interfaces/
│   │       ├── IFitbitService.cs
│   │       └── IFoodService.cs
│   └── Workers/
│       └── FoodWorker.cs                   # Already has tests (currently 4 tests)
│
├── Biotrackr.Food.Svc.UnitTests/           # Existing unit test project
│   ├── ProgramTests/
│   │   └── ProgramShould.cs                # ✅ Exists (1 test)
│   ├── RepositoryTests/
│   │   └── CosmosRepositoryShould.cs       # ✅ Exists (needs coverage review)
│   ├── ServiceTests/
│   │   ├── FitbitServiceShould.cs          # ✅ Exists (needs coverage review)
│   │   └── FoodServiceShould.cs            # ✅ Exists (needs coverage review)
│   └── WorkerTests/
│       └── FoodWorkerShould.cs             # ✅ Exists (4 tests - needs edge case expansion)
│
└── Biotrackr.Food.Svc.IntegrationTests/    # ❌ NEW - to be created
    ├── Contract/                            # Fast tests, no DB
    │   ├── ProgramStartupTests.cs          # Service resolution + app startup
    │   └── ServiceRegistrationTests.cs     # Service lifetime validation
    ├── E2E/                                 # Full workflow tests with Cosmos DB
    │   ├── CosmosRepositoryTests.cs        # Document CRUD operations
    │   ├── FoodServiceTests.cs             # Service orchestration with DB
    │   └── FoodWorkerTests.cs              # Complete worker workflow
    ├── Fixtures/
    │   ├── ContractTestFixture.cs          # No DB initialization
    │   └── IntegrationTestFixture.cs       # With DB initialization (Gateway mode)
    ├── Collections/
    │   ├── ContractTestCollection.cs       # xUnit collection for contract tests
    │   └── IntegrationTestCollection.cs    # xUnit collection for E2E tests
    ├── Helpers/
    │   └── TestDataGenerator.cs            # Generate FoodDocument/FoodResponse test data
    ├── appsettings.Test.json               # Test configuration (Cosmos Emulator connection)
    └── Biotrackr.Food.Svc.IntegrationTests.csproj
```

**Structure Decision**: Worker service structure maintained with separate unit and integration test projects. This follows the established pattern across all Biotrackr microservices (Weight, Activity, Sleep). Unit tests remain fast and focused on business logic, while integration tests validate full workflows with external dependencies. Contract tests run in parallel with unit tests (no DB required), E2E tests run sequentially after contract tests (require Cosmos DB Emulator).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No complexity violations identified. All constitution checks pass without requiring justification.
