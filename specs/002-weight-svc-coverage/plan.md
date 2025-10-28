# Implementation Plan: Weight Service Unit Test Coverage Improvement

**Branch**: `002-weight-svc-coverage` | **Date**: 2025-10-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-weight-svc-coverage/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Improve unit test coverage for Biotrackr.Weight.Svc from 41.36% to at least 70% by creating comprehensive unit tests for the WeightWorker background service (currently 0% coverage) and configuring coverage exclusions for Program.cs entry point. Tests will follow existing patterns using xUnit, Moq, FluentAssertions, and AutoFixture, focusing on dependency orchestration, async execution flows, error handling, and application lifecycle management.

## Technical Context

**Language/Version**: .NET 9.0 (C#)  
**Primary Dependencies**: xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4  
**Storage**: Azure Cosmos DB (existing, no changes needed)  
**Testing**: xUnit with Moq for mocking, FluentAssertions for assertions, coverlet for coverage  
**Target Platform**: Linux server (Azure Container Apps)
**Project Type**: Single service project (Biotrackr.Weight.Svc)  
**Performance Goals**: Tests execute in <1 second total, maintain fast CI/CD pipeline  
**Constraints**: 70% minimum line coverage, 100% test pass rate, no breaking changes to existing code  
**Scale/Scope**: 1 new test file (WeightWorkerShould.cs), ~6-8 test methods, coverage configuration updates

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Code Quality Excellence**: Design follows existing patterns, no new production code needed, tests follow SOLID principles with clear responsibilities per test method
- [x] **Testing Strategy**: Test pyramid expanded at unit level (targeting ≥80% coverage for WeightWorker), tests will be fast and independent
- [x] **User Experience**: Not applicable - internal testing improvements, no user-facing changes
- [x] **Performance Requirements**: Tests designed to execute in <1 second total, maintains fast CI/CD feedback loop
- [x] **Technical Debt**: Addresses existing debt (0% WeightWorker coverage), no new debt introduced, documents Program.cs exclusion as best practice

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/Biotrackr.Weight.Svc/
├── Biotrackr.Weight.Svc/
│   ├── Program.cs                          # Excluded from coverage
│   ├── Workers/
│   │   └── WeightWorker.cs                 # Target: 0% → 85% coverage
│   ├── Services/
│   │   ├── FitbitService.cs                # Already 100% coverage
│   │   └── WeightService.cs                # Already 100% coverage
│   ├── Repositories/
│   │   └── CosmosRepository.cs             # Already 100% coverage
│   └── Models/                             # Already 100% coverage
│
└── Biotrackr.Weight.Svc.UnitTests/
    ├── ServiceTests/
    │   ├── FitbitServiceShould.cs          # Existing tests
    │   └── WeightServiceShould.cs          # Existing tests
    ├── RepositoryTests/
    │   └── CosmosRepositoryShould.cs       # Existing tests
    └── WorkerTests/                        # NEW DIRECTORY
        └── WeightWorkerShould.cs           # NEW FILE - Primary deliverable
```

**Structure Decision**: Following existing test organization pattern with parallel directory structure. New WorkerTests directory aligns with existing ServiceTests and RepositoryTests conventions. All tests remain in Biotrackr.Weight.Svc.UnitTests project.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations identified. This feature:
- Uses existing test patterns and frameworks
- Adds tests without modifying production code
- Follows established project structure
- Aligns with constitution principles for comprehensive testing
