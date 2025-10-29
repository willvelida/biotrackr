# Research: Enhanced Test Coverage for Activity API

**Phase**: 0 - Research & Planning  
**Created**: 2025-10-29  
**Status**: Complete

## Research Tasks Completed

### 1. .NET 9.0 Testing Best Practices

**Decision**: Continue using xUnit 2.9.3 with existing testing stack (FluentAssertions, Moq, AutoFixture)  
**Rationale**: 
- Existing codebase already uses this stack successfully across Weight.Api
- xUnit is the de facto standard for .NET testing with excellent performance
- FluentAssertions provides readable assertions that improve test maintainability
- Moq enables comprehensive mocking of dependencies
- AutoFixture reduces test setup boilerplate
- Consistency across microservices is critical for maintainability

**Alternatives considered**: 
- NUnit: Good alternative but would require migration and break consistency
- MSTest: Microsoft's framework but less community adoption than xUnit

### 2. Code Coverage Tools for .NET 9.0

**Decision**: Use coverlet.collector 6.0.4 (already integrated) with Cobertura format for GitHub Actions reporting  
**Rationale**:
- Already configured and proven in Weight.Api test project
- Native integration with dotnet test command
- Supports multiple output formats including Cobertura for CI/CD
- Excellent performance with minimal overhead
- Consistent tooling across all Biotrackr microservices

**Alternatives considered**:
- Fine Code Coverage: Visual Studio specific, not suitable for CI/CD
- dotCover: JetBrains tool, requires licensing and additional setup

### 3. Integration Testing Patterns for Microservices

**Decision**: Use WebApplicationFactory<Program> with test fixtures following Weight.Api patterns  
**Rationale**:
- Proven successful in Weight.Api integration tests
- Enables true integration testing of the entire request pipeline
- Can configure real Azure services for comprehensive testing
- Supports test isolation through separate test databases
- ContractTestFixture pattern enables fast tests without database
- IntegrationTestFixture pattern provides full database integration
- Aligns with Microsoft's recommended testing patterns

**Alternatives considered**:
- TestServer with mocked dependencies: Would not catch integration issues
- Docker containers: Adds complexity and CI/CD overhead

### 4. Test Fixture Architecture for Activity API

**Decision**: Implement ContractTestFixture and IntegrationTestFixture mirroring Weight.Api  
**Rationale**:
- Weight.Api pattern proven to work effectively
- ContractTestFixture (InitializeDatabase=false) enables fast startup validation
- IntegrationTestFixture (InitializeDatabase=true) provides full E2E testing
- xUnit collection fixtures manage shared resources efficiently
- Clear separation between contract tests (<1 min) and E2E tests (~5 min)
- Reduces test execution time while maintaining comprehensive coverage

**Implementation Details**:
```csharp
// ContractTestFixture - Fast, no database
public class ContractTestFixture : IntegrationTestFixture
{
    protected override bool InitializeDatabase => false;
}

// IntegrationTestFixture - Full integration with Cosmos DB
public class IntegrationTestFixture : IAsyncLifetime
{
    protected virtual bool InitializeDatabase => true;
    // Cosmos DB initialization and cleanup logic
}
```

### 5. xUnit Collection Fixtures for Test Organization

**Decision**: Use xUnit collection fixtures with [CollectionDefinition] attribute  
**Rationale**:
- Proven pattern in Weight.Api for managing shared test context
- Enables proper test isolation while sharing expensive resources
- IAsyncLifetime support for async setup/cleanup
- Prevents test interdependencies while optimizing performance
- Clear test organization (Contract tests vs E2E tests)

**Implementation Details**:
```csharp
[CollectionDefinition(nameof(ContractTestCollection))]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
    // Collection definition for contract tests
}
```

### 6. GitHub Actions Integration Testing Strategy

**Decision**: Extend existing deploy-activity-api.yml workflow with integration tests  
**Rationale**:
- Consistent with Weight.Api CI/CD patterns
- Tests against real deployed environment
- Validates end-to-end functionality including infrastructure
- Separate test job allows for different timeout and failure handling
- Can leverage template-dotnet-run-integration-tests.yml if available

**Alternatives considered**:
- Run integration tests before deployment: Would not test actual deployed state
- Combined unit/integration job: Would slow down feedback loop

### 7. Azure Test Environment Configuration

**Decision**: Use separate test Cosmos DB database (biotrackr-test/activity-test) with automated cleanup  
**Rationale**:
- Mirrors Weight.Api configuration pattern (biotrackr-test/weight-test)
- Ensures test isolation from production data
- Enables parallel test execution across microservices
- Automated cleanup prevents data pollution
- Cost-effective by reusing existing Cosmos DB account
- appsettings.Test.json overrides production configuration

**Configuration Pattern**:
```json
{
  "Biotrackr": {
    "DatabaseName": "biotrackr-test",
    "ContainerName": "activity-test"
  }
}
```

### 8. Test Data Management Strategy

**Decision**: Use builder pattern with AutoFixture for test data creation and deterministic cleanup  
**Rationale**:
- Consistent test data creation across unit and integration tests
- Deterministic cleanup ensures test isolation
- Builder pattern provides flexibility for specific test scenarios
- AutoFixture reduces boilerplate while maintaining control
- TestDataHelper utility class centralizes test data logic

**Alternatives considered**:
- Static test data: Difficult to maintain and lacks flexibility
- Database seeding: Can cause test interdependencies

### 9. Activity API Specific Testing Considerations

**Decision**: Comprehensive coverage of Fitbit entity models and their relationships  
**Rationale**:
- Activity API has complex Fitbit entity models (Activity, ActivityResponse, Distance, Goals, HeartRateZone, Summary)
- These models require thorough validation testing for edge cases
- Heart rate zone data can be incomplete or malformed
- Distance measurements have unit conversion considerations
- Proper testing ensures data integrity from Fitbit API

**Test Focus Areas**:
- Null/missing property handling in Fitbit entities
- Heart rate zone calculation and validation
- Distance unit conversions and validation
- Goals tracking and progress calculation
- Summary aggregation logic

## Performance Research

### Unit Test Performance Optimization

**Target**: <5 minutes execution time  
**Strategy**: 
- Parallel test execution (xUnit default)
- Lightweight mocking with Moq
- Minimal test setup with AutoFixture
- Focused coverage collection
- Efficient Fitbit entity model testing

### Integration Test Performance Optimization

**Target**: <15 minutes execution time  
**Strategy**:
- Connection pooling for Azure services
- Contract tests run in parallel (<1 min total)
- E2E tests use connection pooling and cleanup
- Efficient test data generation and cleanup
- Targeted endpoint testing

**Performance Breakdown**:
- Contract tests (no database): ~30-60 seconds
- E2E tests (with Cosmos DB): ~5-10 minutes
- Total integration test suite: <15 minutes

## Security Considerations

### Test Environment Security

**Approach**: Use managed identity for Azure service authentication in integration tests  
**Rationale**:
- Eliminates need for secrets in test code
- Follows Azure security best practices
- Aligns with existing production configuration
- Consistent with Weight.Api security patterns

### Test Data Security

**Approach**: Use synthetic test data only, no production data access  
**Rationale**:
- Ensures compliance with data protection requirements
- Eliminates risk of data exposure
- Enables faster test execution
- Synthetic Fitbit data covers all edge cases

## Architecture Decisions

### Test Project Structure

**Decision**: Separate unit and integration test projects following Weight.Api pattern  
**Benefits**:
- Clear separation of concerns
- Different execution contexts (mocked vs real dependencies)
- Independent performance characteristics
- Easier CI/CD pipeline management
- Consistent structure across all Biotrackr microservices

**Project Organization**:
```
Biotrackr.Activity.Api.UnitTests/
  - Fast, focused tests with mocks
  - ~100+ tests covering all components
  - Execution time: <5 minutes

Biotrackr.Activity.Api.IntegrationTests/
  Contract/
    - Fast startup and configuration tests
    - No database dependencies
    - Execution time: <1 minute
  E2E/
    - Full integration tests with Cosmos DB
    - End-to-end endpoint testing
    - Execution time: ~5-10 minutes
```

### Coverage Reporting Strategy

**Decision**: Component-level coverage reports with overall 80% target  
**Benefits**:
- Granular visibility into coverage gaps
- Enables targeted improvement efforts
- Supports quality gate enforcement
- Provides actionable feedback to developers
- HTML reports for detailed analysis
- Summary.txt for quick overview

**Coverage Targets by Component**:
- EndpointHandlers: ≥90% (critical API logic)
- Repositories: ≥85% (data access layer)
- Models: ≥80% (validation and properties)
- Extensions: ≥85% (infrastructure)
- Configuration: ≥80% (settings management)
- Fitbit Entities: ≥80% (model validation)

### Service Lifetime Testing

**Decision**: Verify service registration lifetimes following established patterns  
**Rationale**:
- Weight.Api decision record (2025-10-28-service-lifetime-registration.md) establishes clear guidelines
- CosmosClient: Singleton (expensive, thread-safe)
- ICosmosRepository: Transient (cheap, lightweight)
- Settings: Singleton via IOptions pattern
- Contract tests must verify correct registration

**Test Pattern**:
```csharp
[Fact]
public void CosmosClient_Should_Be_Registered_As_Singleton()
{
    var services = _fixture.Factory.Services;
    var client1 = services.GetService<CosmosClient>();
    var client2 = services.GetService<CosmosClient>();
    client1.Should().BeSameAs(client2); // Same instance
}
```

## Implementation Risk Assessment

### Low Risk
- Extending existing unit tests (familiar patterns and tools)
- Using established .NET testing practices
- Leveraging Weight.Api proven patterns
- Reusing existing CI/CD infrastructure

### Medium Risk
- Integration test environment configuration for Activity API
- Fitbit entity model edge case coverage
- Test data cleanup automation
- GitHub Actions workflow timing

### Mitigation Strategies
- Follow Weight.Api integration test patterns exactly
- Comprehensive documentation of Fitbit entity test scenarios
- Phased rollout starting with unit test improvements
- Robust error handling and cleanup procedures
- Test fixture reuse from Weight.Api reduces implementation risk

## Dependencies on Weight.Api Patterns

**Critical Dependencies**:
1. IntegrationTestFixture base class pattern
2. ContractTestFixture lightweight fixture pattern
3. xUnit collection fixtures with [CollectionDefinition]
4. WebApplicationFactory configuration overrides
5. appsettings.Test.json configuration pattern
6. TestDataHelper utility patterns
7. Service lifetime registration patterns

**Implementation Approach**: Copy and adapt Weight.Api patterns to Activity.Api context, maintaining consistency while handling Activity-specific requirements (Fitbit entities, heart rate zones, etc.)
