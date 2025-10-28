# Decision Record: Contract Test Architecture Without Database Dependency

- **Status**: Accepted
- **Deciders**: willvelida, GitHub Copilot
- **Date**: 28 October 2025
- **Related Docs**: [PR #79](https://github.com/willvelida/biotrackr/pull/79), [Weight API Testing Transcript](../github-copilot-transcripts/2025-10-28-weight-api-tests.md)

## Context

During Phase 4 of the Weight API testing strategy, contract tests were initially designed to use the full `IntegrationTestFixture` with Cosmos DB initialization. However, all 13 contract tests failed in CI with "cosmosdbendpoint cannot be null" errors. Investigation revealed that:

1. Contract/smoke tests don't need database access - they only verify service registration and basic API startup
2. The `IntegrationTestFixture` always initialized the database, causing unnecessary dependencies
3. Configuration timing issues between `WebApplicationFactory.ConfigureAppConfiguration` and `Program.cs` reading config
4. Tests were making HTTP calls to endpoints that didn't exist without the database

The original design didn't differentiate between contract tests (service validation) and E2E tests (full integration).

## Decision

**Created a separate `ContractTestFixture` that extends `IntegrationTestFixture` with database initialization disabled.**

Architecture changes:
1. **IntegrationTestFixture**: Added `protected virtual bool InitializeDatabase => true` property
2. **ContractTestFixture**: New lightweight fixture with `InitializeDatabase => false`
3. **ContractTestCollection**: New xUnit collection for contract test isolation
4. **Contract Tests Refactored**: Changed from HTTP endpoint calls to service registration validation

This separates concerns:
- **Contract tests** (13 tests): Verify WebApplicationFactory can create clients and services are registered
- **E2E tests** (52 tests): Full integration testing with database and HTTP calls

## Consequences

### Positive
- ✅ Contract tests run faster (no database initialization overhead)
- ✅ Clear separation between contract validation and integration testing
- ✅ Contract tests more reliable in CI (no Cosmos DB Emulator dependency)
- ✅ Service registration validation catches configuration issues early
- ✅ Reduced test complexity for simple validation scenarios

### Negative
- ⚠️ Need to maintain two separate fixture classes
- ⚠️ Developers must understand which fixture to use for new tests
- ⚠️ Some duplication between fixtures (both inherit from same base)

### Trade-offs
- **Accepted**: Slightly more complex test infrastructure for better separation of concerns
- **Mitigated**: Clear folder structure (Contract/ vs E2E/) makes fixture choice obvious

## Alternatives Considered

### Alternative 1: Keep Using IntegrationTestFixture for All Tests
**Why rejected**: 
- Contract tests don't need database access
- Causes unnecessary CI failures due to Cosmos DB Emulator issues
- Slower test execution
- Violates separation of concerns

### Alternative 2: Skip Contract Tests Entirely
**Why rejected**:
- Contract tests provide valuable early validation of service configuration
- They catch issues before expensive E2E tests run
- Fast feedback loop for CI/CD

### Alternative 3: Use Mock Database for Contract Tests
**Why rejected**:
- Unnecessary complexity for tests that don't need database at all
- Contract tests should validate service registration, not database operations
- Mocking would add maintenance burden

### Alternative 4: Make Database Initialization Conditional in Single Fixture
**Why chosen alternative approach**:
- We did implement this, but created separate derived fixtures for clarity
- Using inheritance with virtual properties provides explicit intent
- Separate fixtures make test purpose immediately clear

## Follow-up Actions

- [x] Create `ContractTestFixture.cs` with `InitializeDatabase => false`
- [x] Create `ContractTestCollection.cs` for xUnit collection
- [x] Refactor `ApiSmokeTests.cs` to use `ContractTestFixture`
- [x] Refactor `ProgramStartupTests.cs` to use `ContractTestFixture`
- [x] Change contract tests from HTTP calls to service registration checks
- [x] Verify all 13 contract tests pass in CI
- [ ] Document fixture selection guidelines in test project README
- [ ] Consider applying this pattern to other API projects (Activity, Sleep)

## Notes

### Configuration Timing Issue
The root cause of the initial failures was that `WebApplicationFactory.ConfigureAppConfiguration` runs **after** `Program.cs` reads configuration. Solution: Use `Environment.SetEnvironmentVariable` before host builds.

### Test Philosophy
- **Contract tests**: "Can the application start and are services registered?"
- **E2E tests**: "Do the endpoints behave correctly with real data?"

This aligns with the testing pyramid: fast contract tests catch configuration issues, slower E2E tests validate business logic.

### Implementation Pattern
```csharp
// IntegrationTestFixture.cs
protected virtual bool InitializeDatabase => true;

// ContractTestFixture.cs
protected override bool InitializeDatabase => false;
```

This pattern is extensible for future fixture variations (e.g., MockAuthFixture, InMemoryDatabaseFixture).
