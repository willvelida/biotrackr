# Implementation Plan: Weight Service Integration Tests

**Branch**: `003-weight-svc-integration-tests` | **Date**: October 28, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-weight-svc-integration-tests/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Create comprehensive integration test project for Biotrackr.Weight.Svc to verify end-to-end workflow of weight data synchronization from Fitbit API to Cosmos DB. Tests will use Azure Cosmos DB Emulator for database operations, mock HTTP responses for Fitbit API calls, and mock SecretClient for Key Vault access tokens. Integration tests will be structured into Contract/ and E2E/ folders following established patterns from Weight API, with two separate GitHub Actions jobs for fast feedback (contract tests <2s, all tests <30s).

## Technical Context

**Language/Version**: C# / .NET 9.0  
**Primary Dependencies**: 
- xUnit 2.9.3 (test framework)
- FluentAssertions 8.4.0 (assertions)
- Moq 4.20.72 (mocking framework)
- AutoFixture 4.18.1 (test data generation)
- Microsoft.AspNetCore.Mvc.Testing 9.0.10 (integration test infrastructure)
- Azure.Identity 1.14.1 (Azure authentication)
- Microsoft.Azure.Cosmos 3.52.0 (Cosmos DB client)
- coverlet.collector 6.0.4 (code coverage)

**Storage**: Azure Cosmos DB (via Emulator in tests - mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest)  
**Testing**: xUnit with integration test fixtures, GitHub Actions services for Cosmos DB Emulator  
**Target Platform**: Linux containers (GitHub Actions ubuntu-latest runners)  
**Project Type**: Worker Service (.NET SDK.Worker) with integration tests  
**Performance Goals**: 
- Contract tests: <2 seconds total execution
- All integration tests: <30 seconds total execution
- Test suite: 100% reliability (100 consecutive runs without environmental failures)

**Constraints**: 
- Must follow existing integration test project structure (Contract/, E2E/, Fixtures/, Collections/, Helpers/)
- Must integrate into existing GitHub Actions workflow (deploy-weight-service.yml)
- Must use Cosmos DB Emulator (not Testcontainers) for consistency with Weight API tests
- Must mock all external dependencies (Fitbit API, Key Vault) for test isolation
- Minimum 80% code coverage of service layer components

**Scale/Scope**: 
- 4 main components to test: CosmosRepository, WeightService, FitbitService, WeightWorker
- Estimated 15-25 integration tests across contract and E2E categories
- 2 GitHub Actions workflow jobs (run-contract-tests, run-e2e-tests)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Initial Check (Before Phase 0)
- [x] **Code Quality Excellence**: Test code will follow SOLID principles with clear fixture responsibilities, DRY test helpers, and minimal cognitive load through consistent patterns
- [x] **Testing Strategy**: Test pyramid implemented (existing unit tests ≥80%, adding integration contract + E2E tests), TDD approach for test infrastructure (write failing test, implement fixture, verify)
- [x] **User Experience**: Consistent test patterns across all Biotrackr services, clear test naming conventions, predictable test organization (Contract/, E2E/, Fixtures/, Collections/, Helpers/)
- [x] **Performance Requirements**: Contract tests <2s, all tests <30s, 100 consecutive runs without environmental failures, test execution optimized through proper fixture lifecycle management
- [x] **Technical Debt**: No new technical debt expected; following established patterns from Weight API integration tests, reusing proven Cosmos DB Emulator approach

**Initial Gate Status**: ✅ PASSED - All constitution principles satisfied

### Post-Design Re-Check (After Phase 1)
- [x] **Code Quality Excellence**: ✅ MAINTAINED
  - Fixtures follow Single Responsibility Principle (ContractTestFixture for DI, IntegrationTestFixture for full infrastructure)
  - TestDataBuilder provides DRY test data generation
  - Clear separation: Contract/ (fast, no deps) vs E2E/ (full workflow)
  - Helper classes (MockHttpMessageHandler, TestDataBuilder) reduce cognitive load

- [x] **Testing Strategy**: ✅ ENHANCED
  - Comprehensive test scenarios documented (9 contract tests, 16 E2E tests)
  - Contract tests verify DI configuration and service registration
  - E2E tests cover complete workflows, error handling, and edge cases
  - Test pyramid completed: Unit (existing ≥80%) → Contract (2s) → E2E (28s)
  - All acceptance criteria from spec mapped to specific tests

- [x] **User Experience**: ✅ CONSISTENT
  - Matches Weight API integration test structure exactly
  - Reuses template-dotnet-run-e2e-tests.yml workflow (proven pattern)
  - Quickstart guide provides clear developer onboarding
  - Test naming follows Given-When-Then pattern

- [x] **Performance Requirements**: ✅ VERIFIED
  - Contract tests: <2s (5-10 lightweight tests, no external deps)
  - E2E tests: <28s (10-15 tests with shared Cosmos DB Emulator fixture)
  - Parallel execution via xUnit default
  - Shared fixtures minimize setup/teardown overhead
  - Performance benchmarks documented in quickstart.md

- [x] **Technical Debt**: ✅ NONE INTRODUCED
  - No new patterns or approaches (reuses Weight API patterns)
  - Cosmos DB Emulator approach already proven in production
  - Moq/AutoFixture/xUnit already standard across project
  - GitHub Actions workflow templates already exist
  - No deprecated dependencies or workarounds

**Post-Design Gate Status**: ✅ PASSED - All constitution principles maintained and enhanced through design

### Design Quality Assessment
- **SOLID Principles**: ✅ Applied throughout (SRP for fixtures, DIP for interfaces)
- **Test Coverage**: ✅ 25+ tests covering all service components and workflows
- **Documentation**: ✅ Comprehensive (research.md, data-model.md, contracts/, quickstart.md)
- **Maintainability**: ✅ Clear structure, reusable helpers, well-documented patterns
- **Consistency**: ✅ Follows established Biotrackr testing standards

**Final Verdict**: Design approved for implementation (/speckit.tasks)

## Project Structure

### Documentation (this feature)

```text
specs/003-weight-svc-integration-tests/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── contract-tests.md     # Contract test scenarios
│   └── e2e-tests.md          # E2E test scenarios
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/Biotrackr.Weight.Svc/
├── Biotrackr.Weight.Svc/              # Main service project (existing)
│   ├── Configuration/
│   ├── Models/
│   ├── Repositories/
│   ├── Services/
│   └── Workers/
├── Biotrackr.Weight.Svc.UnitTests/    # Unit tests (existing)
│   ├── RepositoryTests/
│   ├── ServiceTests/
│   └── WorkerTests/
└── Biotrackr.Weight.Svc.IntegrationTests/  # NEW - Integration test project
    ├── Contract/                      # Contract tests (no external dependencies)
    │   ├── ProgramStartupTests.cs     # DI configuration verification
    │   └── ServiceRegistrationTests.cs # Service registration tests
    ├── E2E/                           # End-to-end tests (with Cosmos DB Emulator)
    │   ├── WeightWorkerTests.cs       # Complete workflow tests
    │   ├── WeightServiceTests.cs      # Service integration tests
    │   └── CosmosRepositoryTests.cs   # Database integration tests
    ├── Fixtures/                      # Test infrastructure
    │   ├── ContractTestFixture.cs     # Contract test base fixture
    │   ├── IntegrationTestFixture.cs  # E2E test base fixture
    │   └── ServiceCollectionExtensions.cs  # Test DI helpers
    ├── Collections/                   # xUnit test collections
    │   ├── ContractTestCollection.cs  # Contract test collection
    │   └── IntegrationTestCollection.cs # E2E test collection
    ├── Helpers/                       # Test utilities
    │   ├── TestDataBuilder.cs         # Test data creation
    │   ├── MockHttpMessageHandler.cs  # HTTP mocking
    │   └── MockSecretClientFactory.cs # Key Vault mocking
    ├── appsettings.Test.json          # Test configuration
    └── Biotrackr.Weight.Svc.IntegrationTests.csproj

.github/workflows/
├── deploy-weight-service.yml          # UPDATED - Add integration test jobs
├── template-dotnet-run-contract-tests.yml  # NEW - Contract test workflow
└── template-dotnet-run-e2e-tests.yml  # EXISTING - Reuse for E2E tests
```

**Structure Decision**: Single test project following established Weight API integration test pattern with Contract/ and E2E/ separation. Contract tests verify service configuration and DI setup without external dependencies. E2E tests verify complete workflows using Cosmos DB Emulator. This structure enables independent test execution and clear separation of concerns.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations detected. All constitution principles satisfied:
- Test code follows established patterns and SOLID principles
- Test pyramid completed with integration tests
- Consistent test structure across all Biotrackr services
- Performance targets defined and achievable
- No technical debt introduced
