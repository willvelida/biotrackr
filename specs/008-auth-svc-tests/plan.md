# Implementation Plan: Auth Service Test Coverage and Integration Tests

**Branch**: `008-auth-svc-tests` | **Date**: November 3, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-auth-svc-tests/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Extend unit test coverage for Biotrackr.Auth.Svc to meet 70% code coverage threshold and add comprehensive integration tests (Contract and E2E) following the established Weight Service pattern. Integration tests will run in GitHub Actions CI/CD pipeline with proper test isolation and mocked external dependencies (Azure Key Vault, Fitbit API). This ensures authentication token refresh workflow is thoroughly tested without requiring actual Azure resources.

## Technical Context

**Language/Version**: C# / .NET 9.0  
**Primary Dependencies**: xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0  
**Storage**: N/A (Auth Service uses Azure Key Vault for secrets, mocked in tests)  
**Testing**: xUnit with coverlet for coverage, Moq for mocking, FluentAssertions for readable assertions  
**Target Platform**: Azure Container Apps (Linux server), GitHub Actions (ubuntu-latest runners)  
**Project Type**: Single .NET Worker Service with separate test projects (Unit, Integration)  
**Performance Goals**: Unit tests <5s, Contract tests <5s, E2E tests <10s, Total CI/CD <5 minutes  
**Constraints**: 70% minimum code coverage, no external service dependencies in tests, test isolation required  
**Scale/Scope**: 3 test projects (existing UnitTests + new IntegrationTests), ~15-20 new test classes, following 003-weight-svc-integration-tests pattern

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Phase 0 Check (Before Research)**: ✅ PASSED
**Phase 1 Check (After Design)**: ✅ PASSED

- [x] **Code Quality Excellence**: Design follows SOLID principles - test fixtures use dependency injection, separation of concerns between Contract/E2E tests, single responsibility per test class. Data model defines clear entities with single responsibilities. Helper classes follow Single Responsibility Principle.
- [x] **Testing Strategy**: Test pyramid planned (unit ≥70%, contract tests for DI, E2E tests for workflows), follows established Weight Service pattern with proven reliability. 29 test scenarios documented (5 existing, 24 to create). Test execution filters enable selective testing.
- [x] **User Experience**: N/A (internal testing infrastructure, but consistent with other service test patterns for developer experience). Quickstart guide provides clear developer onboarding.
- [x] **Performance Requirements**: Response time targets defined (unit <5s, contract <5s, E2E <10s, CI/CD <5min), lightweight fixtures for contract tests. Performance goals validated in research phase. No performance bottlenecks identified in design.
- [x] **Technical Debt**: No new debt anticipated - following established patterns; existing pattern in Weight Service validates approach; Program.cs coverage exclusion uses [ExcludeFromCodeCoverage] attribute per 2025-10-28 decision record. All implementation decisions documented in research.md.

## Project Structure

### Documentation (this feature)

```text
specs/008-auth-svc-tests/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── test-scenarios.md  # Test scenario contracts
├── checklists/
│   └── requirements.md  # Already created - spec validation
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/Biotrackr.Auth.Svc/
├── Biotrackr.Auth.Svc/
│   ├── Program.cs                          # [TO MODIFY] Add [ExcludeFromCodeCoverage] attribute
│   ├── AuthWorker.cs                       # [TO TEST] Background service orchestration
│   ├── Services/
│   │   ├── RefreshTokenService.cs          # [TO TEST] Token refresh implementation
│   │   └── Interfaces/
│   │       └── IRefreshTokenService.cs     # [EXISTING] Service interface
│   └── Models/
│       └── RefreshTokenResponse.cs         # [TO TEST] Data model
├── Biotrackr.Auth.Svc.UnitTests/           # [TO EXTEND] Add missing edge cases
│   ├── WorkerTests/
│   │   └── AuthWorkerShould.cs             # [TO EXTEND] Add cancellation, failure scenarios
│   ├── ServiceTests/
│   │   └── RefreshTokenServiceShould.cs    # [TO EXTEND] Add error handling, edge cases
│   └── ModelTests/
│       └── RefreshTokenResponseShould.cs   # [EXISTING] Model validation tests
└── Biotrackr.Auth.Svc.IntegrationTests/    # [TO CREATE] New integration test project
    ├── Biotrackr.Auth.Svc.IntegrationTests.csproj  # [TO CREATE]
    ├── appsettings.Test.json                # [TO CREATE] Test configuration
    ├── Contract/                            # [TO CREATE] Fast DI/startup tests
    │   ├── ProgramStartupTests.cs           # [TO CREATE] Host builder validation
    │   └── ServiceRegistrationTests.cs      # [TO CREATE] DI lifetime verification
    ├── E2E/                                 # [TO CREATE] Full workflow tests
    │   ├── RefreshTokenServiceTests.cs      # [TO CREATE] Service integration tests
    │   └── AuthWorkerTests.cs               # [TO CREATE] Worker E2E tests
    ├── Fixtures/                            # [TO CREATE] Test infrastructure
    │   ├── ContractTestFixture.cs           # [TO CREATE] Lightweight fixture (no mocks)
    │   └── IntegrationTestFixture.cs        # [TO CREATE] Full fixture (mocked services)
    ├── Collections/                         # [TO CREATE] xUnit collections
    │   ├── ContractTestCollection.cs        # [TO CREATE]
    │   └── IntegrationTestCollection.cs     # [TO CREATE]
    ├── Helpers/                             # [TO CREATE] Test utilities
    │   ├── TestDataGenerator.cs             # [TO CREATE] Generate RefreshTokenResponse
    │   └── MockHttpMessageHandlerBuilder.cs # [TO CREATE] Simplify HttpClient mocking
    └── README.md                            # [TO CREATE] Test project documentation

.github/workflows/
└── deploy-auth-service.yml                  # [TO MODIFY] Add contract/E2E test jobs
```

**Structure Decision**: Following established single .NET Worker Service pattern with separate test projects. Integration test structure matches Weight Service pattern (003-weight-svc-integration-tests) with Contract/ and E2E/ separation. This proven pattern ensures consistency across microservices and validated reliability in CI/CD environment.

## Complexity Tracking

> **No constitutional violations requiring justification**

All aspects of the implementation plan align with constitutional principles:
- Code follows established patterns (Weight Service integration test structure)
- Test pyramid is comprehensive (unit ≥70%, contract, E2E)
- Performance targets are clearly defined and achievable
- No technical debt introduced (using proven patterns)
- Test isolation and mocking strategies prevent flaky tests
