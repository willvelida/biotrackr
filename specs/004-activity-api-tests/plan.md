# Implementation Plan: Enhanced Test Coverage for Activity API

**Branch**: `004-activity-api-tests` | **Date**: 2025-10-29 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-activity-api-tests/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Increase unit test coverage for Biotrackr.Activity.Api to ≥80% and implement comprehensive integration tests following the proven patterns established in Biotrackr.Weight.Api. Create separate contract tests (fast, no database dependencies) and E2E tests (full integration with Cosmos DB) using xUnit collection fixtures, WebApplicationFactory, and test-specific configuration overrides. This directly supports constitutional testing requirements while ensuring consistency across microservices.

## Technical Context

**Language/Version**: .NET 9.0 (C#)  
**Primary Dependencies**: xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0  
**Storage**: Azure Cosmos DB (for integration tests), In-memory mocks (for unit tests)  
**Testing**: xUnit framework with existing test infrastructure, WebApplicationFactory for integration tests  
**Target Platform**: Linux containers in Azure Container Apps, GitHub Actions runners (ubuntu-latest)
**Project Type**: Web API microservice architecture (part of Biotrackr ecosystem)  
**Performance Goals**: Unit tests <5 minutes execution, Integration tests <15 minutes execution, Coverage reports generation <30 seconds  
**Constraints**: ≥80% code coverage requirement (constitutional), Integration tests must use isolated test environment, Test failures must block deployment pipeline, Must mirror Weight.Api patterns for consistency  
**Scale/Scope**: Single Activity API service, ~20-25 test classes expected (unit + integration), ~200+ test methods, GitHub Actions workflow integration

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Initial Check (Pre-Phase 0):**
- [x] **Code Quality Excellence**: Test design follows SOLID principles with clear separation of unit vs integration concerns, reuses proven patterns from Weight.Api
- [x] **Testing Strategy**: Test pyramid implemented (unit ≥80%, contract tests for quick validation, E2E tests for full integration), TDD approach for new test creation
- [x] **User Experience**: Consistent test patterns across all Biotrackr microservices, leveraging established fixture and collection patterns
- [x] **Performance Requirements**: Test execution time limits defined (unit <5min, integration <15min), contract tests optimized to run without database
- [x] **Technical Debt**: Current insufficient coverage debt identified, systematic remediation plan with measurable targets following Weight.Api success pattern

**Gate Status**: ✅ **PASSED** - All constitutional requirements satisfied, ready for Phase 0 research

**Post-Phase 1 Design Re-evaluation:**
- [x] **Code Quality Excellence**: Detailed test architecture maintains SOLID principles with clear separation between unit tests (mocked dependencies), contract tests (fast validation), and E2E tests (full integration). Fixture pattern reuse from Weight.Api ensures consistent quality.
- [x] **Testing Strategy**: Comprehensive test pyramid design with specific coverage targets per component (handlers ≥90%, repositories ≥85%, models/entities ≥80%). TDD workflow defined in quickstart.md. Contract/E2E split optimizes feedback loop.
- [x] **User Experience**: Consistent test patterns defined following Weight.Api, standardized fixture and collection usage, clear test naming conventions (Given-When-Then), reusable test helpers.
- [x] **Performance Requirements**: Detailed performance targets with optimization strategies (contract tests <1min, E2E tests ~5-10min, total <15min). Coverage report generation optimized. xUnit collection fixtures optimize resource usage.
- [x] **Technical Debt**: Systematic approach to coverage improvement with clear quality gates (80% overall, component-specific targets), automated enforcement in CI/CD, Fitbit entity edge cases identified and addressed.

**Final Gate Status**: ✅ **PASSED** - All constitutional requirements satisfied with comprehensive implementation plan

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
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
```text
src/Biotrackr.Activity.Api/
├── Biotrackr.Activity.Api/              # Main API project
│   ├── EndpointHandlers/
│   │   └── ActivityHandlers.cs
│   ├── Extensions/
│   │   └── EndpointRouteBuilderExtensions.cs
│   ├── Models/
│   │   ├── ActivityDocument.cs
│   │   ├── PaginationRequest.cs
│   │   └── FitbitEntities/
│   │       ├── Activity.cs
│   │       ├── ActivityResponse.cs
│   │       ├── Distance.cs
│   │       ├── Goals.cs
│   │       ├── HeartRateZone.cs
│   │       └── Summary.cs
│   ├── Repositories/
│   │   ├── CosmosRepository.cs
│   │   └── Interfaces/
│   │       └── ICosmosRepository.cs
│   └── Configuration/
│       └── Settings.cs
├── Biotrackr.Activity.Api.UnitTests/    # Existing unit tests (to be extended)
│   ├── EndpointHandlerTests/
│   │   └── ActivityHandlersShould.cs
│   ├── ModelTests/
│   │   ├── PaginationRequestShould.cs
│   │   ├── PaginationResponseShould.cs
│   │   └── FitbitEntityTests/          # New - comprehensive model tests
│   ├── RepositoryTests/
│   │   └── CosmosRepositoryShould.cs
│   ├── ExtensionTests/                 # New - to be added
│   │   └── EndpointRouteBuilderExtensionsShould.cs
│   └── ConfigurationTests/             # New - to be added
│       └── SettingsShould.cs
└── Biotrackr.Activity.Api.IntegrationTests/ # New integration test project
    ├── Contract/                       # Fast tests, no database
    │   ├── ApiSmokeTests.cs
    │   └── ProgramStartupTests.cs
    ├── E2E/                            # Full integration tests with Cosmos DB
    │   └── ActivityEndpointsTests.cs
    ├── Fixtures/                       # Shared test infrastructure
    │   ├── ContractTestFixture.cs
    │   ├── IntegrationTestFixture.cs
    │   └── ActivityApiWebApplicationFactory.cs
    ├── Collections/
    │   └── ContractTestCollection.cs
    ├── Helpers/
    │   └── TestDataHelper.cs
    └── appsettings.Test.json

.github/workflows/
├── deploy-activity-api.yml            # Existing workflow (to be extended with tests)
└── template-dotnet-run-integration-tests.yml # Reusable template (if exists)
```

**Structure Decision**: Microservice architecture with separated test concerns following established Weight.Api patterns. Unit tests extend existing project structure with additional test classes for uncovered components (Extensions, Configuration, Fitbit entities). Integration tests are isolated in a new project (`Biotrackr.Activity.Api.IntegrationTests`) with clear separation between Contract tests (fast validation without database) and E2E tests (full integration with Cosmos DB). This maintains clean test pyramid implementation and aligns with constitutional testing strategy.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitutional violations. All complexity is justified:
- Test project separation (unit vs integration) follows best practices and constitutional requirements
- Fixture pattern reuse from Weight.Api maintains consistency and reduces complexity
- Test organization (Contract/E2E split) optimizes for fast feedback while ensuring comprehensive validation
