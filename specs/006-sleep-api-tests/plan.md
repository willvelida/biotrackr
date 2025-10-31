# Implementation Plan: Enhanced Test Coverage for Sleep API

**Branch**: `006-sleep-api-tests` | **Date**: 2025-10-31 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-sleep-api-tests/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Expand unit test coverage for the Sleep API to achieve ≥80% code coverage and implement integration tests (Contract and E2E) that can be executed in GitHub Actions workflows. This follows established patterns from Weight API and Activity API implementations, ensuring consistency across Biotrackr microservices. Key elements include: comprehensive unit tests for all components (handlers, repositories, models, extensions), Contract tests for service registration and startup validation, E2E tests with Cosmos DB Emulator using Gateway connection mode, proper test isolation with container cleanup, and integration with CI/CD pipelines using reusable workflow templates.

## Technical Context

**Language/Version**: C# / .NET 9.0  
**Primary Dependencies**: xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0  
**Storage**: Azure Cosmos DB (via Emulator in tests - mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest)  
**Testing**: xUnit with FluentAssertions for unit/integration tests, Cosmos DB Emulator for E2E tests  
**Target Platform**: Cross-platform (.NET 9.0), GitHub Actions runners (ubuntu-latest)  
**Project Type**: Web API microservice with test projects  
**Performance Goals**: Unit tests <5 minutes, Contract tests <10 minutes, E2E tests <15 minutes, zero flaky tests  
**Constraints**: ≥80% code coverage required, test isolation mandatory, Gateway connection mode for Emulator, no duplicate service registrations  
**Scale/Scope**: 3 test projects (UnitTests, IntegrationTests with Contract/E2E namespaces), ~83 existing unit tests to analyze and expand, 3 API endpoints to cover

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Code Quality Excellence**: Design follows established test patterns from Weight/Activity APIs, clear separation of Contract/E2E tests, minimal duplication through shared fixtures
- [x] **Testing Strategy**: Test pyramid fully implemented (unit ≥80%, contract for startup/registration, E2E for full flows), follows TDD-compatible approach with test-first mindset
- [x] **User Experience**: Consistent test patterns across all Biotrackr microservices, clear test naming conventions, comprehensive coverage reporting
- [x] **Performance Requirements**: Explicit time limits per test category (unit <5min, contract <10min, E2E <15min), parallel execution where possible
- [x] **Technical Debt**: No new debt introduced - follows proven patterns from common-resolutions.md and decision records, uses Gateway mode to avoid SSL issues, implements proper test isolation

## Project Structure

### Documentation (this feature)

```text
specs/006-sleep-api-tests/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
├── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
└── checklists/
    └── requirements.md  # Already created during /speckit.specify
```

### Source Code (repository root)

```text
src/Biotrackr.Sleep.Api/
├── Biotrackr.Sleep.Api/
│   ├── Configuration/
│   │   └── Settings.cs
│   ├── EndpointHandlers/
│   │   └── SleepHandlers.cs
│   ├── Extensions/
│   │   └── EndpointRouteBuilderExtensions.cs
│   ├── Models/
│   │   ├── FitbitEntities/
│   │   │   ├── Sleep.cs
│   │   │   ├── SleepResponse.cs
│   │   │   ├── SleepData.cs
│   │   │   ├── Levels.cs
│   │   │   ├── Summary.cs
│   │   │   └── Stages.cs
│   │   ├── PaginationRequest.cs
│   │   ├── PaginationResponse.cs
│   │   └── SleepDocument.cs
│   ├── Repositories/
│   │   ├── Interfaces/
│   │   │   └── ICosmosRepository.cs
│   │   └── CosmosRepository.cs
│   └── Program.cs
│
├── Biotrackr.Sleep.Api.UnitTests/          # Existing - to be expanded
│   ├── EndpointHandlerTests/
│   │   └── SleepHandlersShould.cs          # 83 existing tests
│   ├── RepositoryTests/
│   │   └── CosmosRepositoryShould.cs
│   └── [NEW] ModelTests/                   # To be added for coverage gaps
│       ├── SettingsShould.cs
│       ├── PaginationRequestShould.cs
│       └── FitbitEntitiesShould.cs
│
└── Biotrackr.Sleep.Api.IntegrationTests/   # NEW - to be created
    ├── Contract/                            # Fast tests, no Cosmos DB
    │   ├── ProgramStartupTests.cs
    │   ├── ServiceRegistrationTests.cs
    │   └── ApiSmokeTests.cs
    ├── E2E/                                 # Full tests with Cosmos DB Emulator
    │   ├── HealthCheckTests.cs
    │   ├── SleepEndpointTests.cs
    │   └── CosmosRepositoryIntegrationTests.cs
    ├── Fixtures/
    │   ├── ContractTestFixture.cs
    │   └── IntegrationTestFixture.cs
    └── WebApplicationFactories/
        ├── ContractTestWebApplicationFactory.cs
        └── SleepApiWebApplicationFactory.cs
```

**Structure Decision**: Single solution with three test projects following established Biotrackr patterns. Unit tests expand existing project, Integration tests create new project with Contract/E2E namespace separation for parallel execution. This matches the structure from Weight API (001-weight-api-tests) and Activity API (004-activity-api-tests) implementations.

## Complexity Tracking

> **No constitutional violations to justify** - This implementation follows established patterns and adheres to all constitution principles.
