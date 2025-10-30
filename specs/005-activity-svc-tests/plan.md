# Implementation Plan: Activity Service Test Coverage and Integration Tests

**Branch**: `005-activity-svc-tests` | **Date**: 2025-10-31 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-activity-svc-tests/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Expand unit test coverage for the Activity Service to meet 70% threshold and establish a comprehensive integration test suite following the Weight Service pattern. Integration tests will be organized into Contract tests (fast, no external dependencies) and E2E tests (full workflow with Cosmos DB Emulator), all executable via GitHub Actions CI/CD pipeline.

## Technical Context

**Language/Version**: C# / .NET 9.0  
**Primary Dependencies**: xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0  
**Storage**: Azure Cosmos DB (via Emulator in tests using mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest)  
**Testing**: xUnit with coverlet for coverage analysis, separate fixtures for Contract and E2E tests  
**Target Platform**: .NET 9.0 background service (Worker Service), GitHub Actions Ubuntu runners  
**Project Type**: Background service with existing unit tests, adding integration test project  
**Performance Goals**: Unit tests <5 seconds, Contract tests <5 seconds, E2E tests <30 seconds, full workflow <10 minutes  
**Constraints**: 70% minimum code coverage, Gateway mode required for Cosmos DB Emulator, test isolation mandatory  
**Scale/Scope**: Existing service with ~435 lines of unit tests, adding integration test project with Contract and E2E suites

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Code Quality Excellence**: Test code follows existing patterns (ActivityWorkerShould.cs), SOLID principles applied to test fixtures, clear test naming conventions (MethodName_Should_ExpectedBehavior)
- [x] **Testing Strategy**: Test pyramid maintained (unit tests P1, contract tests P2, E2E tests P3), coverage target 70% for service code + 80% for integration points, TDD approach for new test scenarios
- [x] **User Experience**: Consistent test patterns across services (Weight Service as reference), clear test output and failure messages via FluentAssertions
- [x] **Performance Requirements**: Unit tests <5s, contract tests <5s (no DB), E2E tests <30s, CI workflow <10 minutes, test isolation ensures no cross-test interference
- [x] **Technical Debt**: Program.cs excluded from coverage (decision record 2025-10-28), flaky tests removed per policy, duplicate service registrations addressed per service lifetime guidelines

## Project Structure

### Documentation (this feature)

```text
specs/005-activity-svc-tests/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
├── checklists/          # Quality validation checklists
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/Biotrackr.Activity.Svc/
├── Biotrackr.Activity.Svc/                    # Existing service code
│   ├── Configuration/
│   ├── Models/
│   ├── Repositories/
│   ├── Services/
│   ├── Workers/
│   └── Program.cs
├── Biotrackr.Activity.Svc.UnitTests/          # Existing unit tests (expand coverage)
│   ├── ProgramTests/
│   ├── RepositoryTests/
│   ├── ServiceTests/
│   ├── WorkerTests/
│   └── Biotrackr.Activity.Svc.UnitTests.csproj
└── Biotrackr.Activity.Svc.IntegrationTests/   # NEW - Integration test project
    ├── Contract/                               # Contract tests (no DB)
    │   ├── ProgramStartupTests.cs
    │   └── ServiceRegistrationTests.cs
    ├── E2E/                                    # E2E tests (with Cosmos DB)
    │   ├── CosmosRepositoryTests.cs
    │   ├── ActivityServiceTests.cs
    │   └── ActivityWorkerTests.cs
    ├── Fixtures/                               # Shared test infrastructure
    │   ├── ContractTestFixture.cs              # Lightweight fixture (no DB)
    │   └── IntegrationTestFixture.cs           # Full fixture (with DB)
    ├── Collections/                            # xUnit collection definitions
    │   ├── ContractTestCollection.cs
    │   └── IntegrationTestCollection.cs
    ├── Helpers/                                # Test utility code
    │   └── TestDataGenerator.cs
    ├── appsettings.Test.json                   # Test configuration
    └── Biotrackr.Activity.Svc.IntegrationTests.csproj

.github/workflows/
└── deploy-activity-service.yml                 # Updated workflow (add contract/E2E test jobs)
```

**Structure Decision**: Using single solution structure with three test project types: existing UnitTests (expand coverage), new IntegrationTests with Contract/ and E2E/ namespaces following Weight Service pattern (003-weight-svc-integration-tests). This maintains consistency across microservices and leverages proven patterns from Weight Service implementation.

## Complexity Tracking

> **No violations to justify - Constitution Check passed**

All design decisions align with constitution principles. The test architecture follows established patterns (Weight Service), complexity is managed through clear separation of concerns (Contract vs E2E tests), and technical debt is proactively addressed through decision records and common resolutions documentation.

---

## Phase 0: Research & Analysis ✅ COMPLETE

All technical unknowns resolved through reference to existing patterns and decision records.

**Key Decisions**:
- Test organization: Contract vs E2E namespaces
- Fixture architecture: Separate ContractTestFixture and IntegrationTestFixture
- Cosmos DB connection: Gateway mode for Emulator compatibility
- Test isolation: ClearContainerAsync pattern
- Flaky test policy: Remove entirely (user preference)
- Service lifetimes: Follow established guidelines
- Coverage exclusions: Program.cs excluded

**Artifacts**:
- ✅ [research.md](./research.md) - Comprehensive research findings

---

## Phase 1: Design & Contracts ✅ COMPLETE

Design artifacts generated based on research findings.

**Artifacts**:
- ✅ [data-model.md](./data-model.md) - Test fixtures, entities, and data flow
- ✅ [contracts/test-contracts.md](./contracts/test-contracts.md) - Test execution contracts, service lifetimes, workflow expectations
- ✅ [quickstart.md](./quickstart.md) - Step-by-step development guide
- ✅ `.github/copilot-instructions.md` - Updated with .NET 9.0 + xUnit testing stack

**Constitution Re-Check**:
- ✅ **Code Quality Excellence**: Test fixtures follow SOLID principles, clear separation between Contract and E2E concerns
- ✅ **Testing Strategy**: Test pyramid maintained (unit 70%, integration Contract+E2E 80%), fixtures support independent test execution
- ✅ **User Experience**: Consistent patterns with Weight Service, clear test naming, detailed failure messages via FluentAssertions
- ✅ **Performance Requirements**: All performance targets documented (unit <5s, contract <5s, E2E <30s, workflow <10min)
- ✅ **Technical Debt**: All decisions documented in research.md, references to decision records, no known debt introduced

---

## Phase 2: Task Breakdown

**NOT COMPLETED BY THIS COMMAND**

Run `/speckit.tasks` to generate detailed implementation tasks based on this plan.
