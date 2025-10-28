# Decision Record: Integration Test Project Folder Structure

- **Status**: Accepted
- **Deciders**: willvelida, GitHub Copilot
- **Date**: 28 October 2025
- **Related Docs**: [PR #79](https://github.com/willvelida/biotrackr/pull/79)

## Context

After implementing 165 tests (80 unit + 13 contract + 52 E2E), the integration test project had a flat structure with all files in the root:

```
Biotrackr.Weight.Api.IntegrationTests/
├── ApiSmokeTests.cs
├── ProgramStartupTests.cs
├── WeightEndpointsTests.cs
├── ContractTestFixture.cs
├── IntegrationTestFixture.cs
├── WeightApiWebApplicationFactory.cs
├── ContractTestCollection.cs
├── TestDataHelper.cs
├── appsettings.Test.json
└── Biotrackr.Weight.Api.IntegrationTests.csproj
```

This flat structure had several problems:
1. No clear distinction between contract tests and E2E tests
2. Difficult to locate specific types of test infrastructure
3. Not scalable for future test additions
4. Doesn't follow common test project conventions
5. Hard to run specific test categories in isolation

With 13 contract tests and 52 E2E tests, organization was becoming critical for maintainability.

## Decision

**Reorganize integration test project into logical folder structure based on test type and infrastructure role.**

New structure:
```
Biotrackr.Weight.Api.IntegrationTests/
├── Contract/                    # Smoke/contract tests (no database)
│   ├── ApiSmokeTests.cs         # 2 tests
│   └── ProgramStartupTests.cs   # 11 tests
├── E2E/                         # Full integration tests (with database)
│   └── WeightEndpointsTests.cs  # 52 tests (1 skipped)
├── Fixtures/                    # Test fixtures and factories
│   ├── ContractTestFixture.cs
│   ├── IntegrationTestFixture.cs
│   └── WeightApiWebApplicationFactory.cs
├── Collections/                 # xUnit collection definitions
│   └── ContractTestCollection.cs
├── Helpers/                     # Test utilities
│   └── TestDataHelper.cs
├── appsettings.Test.json
└── Biotrackr.Weight.Api.IntegrationTests.csproj
```

All files moved using `git mv` to preserve commit history.

## Consequences

### Positive
- ✅ Clear separation between contract and E2E tests
- ✅ Easy to locate specific test types or infrastructure
- ✅ Can run test categories independently: `dotnet test --filter "FullyQualifiedName~Contract"`
- ✅ Scalable for future test additions (E2E can have subdirectories)
- ✅ Follows common test project patterns (.NET, Java, Python projects)
- ✅ New developers can immediately understand project organization
- ✅ Easier to apply different test configurations per category
- ✅ Test execution time tracking by category

### Negative
- ⚠️ Slightly longer file paths
- ⚠️ Need to update namespace references (auto-handled by IDE)
- ⚠️ Git blame shows move commits (mitigated by preserving history)

### Trade-offs
- **Accepted**: Minor path length increase for significant organization benefits
- **Mitigated**: Used `git mv` to preserve history, namespaces updated automatically

## Alternatives Considered

### Alternative 1: Keep Flat Structure
**Why rejected**:
- Doesn't scale beyond current 165 tests
- No clear organization principles
- Difficult to navigate as project grows
- Doesn't communicate test categorization
- Makes selective test execution harder

### Alternative 2: Organize by Feature/Domain
```
Tests/
├── Weight/
├── Health/
├── Swagger/
```
**Why rejected**:
- Weight API only has one domain (Weight)
- Over-engineered for current scope
- Test type distinction more important than domain
- All tests are for same domain already

### Alternative 3: Organize by Test Framework
```
Tests/
├── xUnit/
├── Fixtures/
```
**Why rejected**:
- All tests use xUnit - no framework distinction needed
- Doesn't provide meaningful organization
- Test type (contract vs E2E) is more important than framework

### Alternative 4: Keep Tests at Root, Organize Infrastructure Only
```
Tests/
├── ApiSmokeTests.cs
├── ProgramStartupTests.cs
├── WeightEndpointsTests.cs
└── Infrastructure/
    ├── Fixtures/
    ├── Collections/
    └── Helpers/
```
**Why rejected**:
- Doesn't solve the main problem (distinguishing test types)
- Contract vs E2E distinction is critical for understanding test purpose
- Mixing test files at root causes confusion

## Follow-up Actions

- [x] Create folder structure: Contract/, E2E/, Fixtures/, Collections/, Helpers/
- [x] Move contract tests to Contract/ folder using git mv
- [x] Move E2E tests to E2E/ folder using git mv
- [x] Move fixtures to Fixtures/ folder using git mv
- [x] Move collections to Collections/ folder using git mv
- [x] Move helpers to Helpers/ folder using git mv
- [x] Verify all tests still pass after reorganization
- [x] Push reorganized structure to GitHub
- [ ] Add README.md to test project explaining structure
- [ ] Update CI/CD workflows to use folder-based filters if needed
- [ ] Apply same structure to other API test projects (Activity, Sleep)
- [ ] Document test organization guidelines in project wiki

## Notes

### Folder Purposes

**Contract/**
- Tests that verify service registration and basic API startup
- No database dependency
- Fast execution (<1s total)
- Run first in CI pipeline for quick feedback

**E2E/**
- Full integration tests with database
- HTTP endpoint testing
- Slower execution (~10s total)
- Run after contract tests pass

**Fixtures/**
- Test infrastructure shared across test types
- WebApplicationFactory implementations
- Base fixture classes
- Configuration management

**Collections/**
- xUnit collection definitions for test isolation
- Fixture lifecycle management
- Shared test context

**Helpers/**
- Test data builders
- Common assertion helpers
- Test utilities
- Mock data generators

### Test Filter Examples
```bash
# Run only contract tests
dotnet test --filter "FullyQualifiedName~Contract"

# Run only E2E tests
dotnet test --filter "FullyQualifiedName~E2E"

# Run specific test file
dotnet test --filter "FullyQualifiedName~WeightEndpointsTests"
```

### Scalability
As the test suite grows:
- E2E/ can have subdirectories: `E2E/GetEndpoints/`, `E2E/PostEndpoints/`
- Contract/ can separate by concern: `Contract/Startup/`, `Contract/Configuration/`
- Fixtures/ can group by purpose: `Fixtures/Database/`, `Fixtures/Auth/`

### Pattern Adoption
This structure should be applied to all API integration test projects:
- `Biotrackr.Activity.Api.IntegrationTests`
- `Biotrackr.Sleep.Api.IntegrationTests`
- Future API projects

Consistent structure across projects improves:
- Developer productivity (familiar structure)
- Code reviews (easier to navigate)
- Onboarding (clear conventions)
- Tooling (consistent paths for scripts)
