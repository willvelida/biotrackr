# Data Model: Enhanced Test Coverage for Activity API

**Phase**: 1 - Design & Contracts  
**Created**: 2025-10-29  
**Status**: Complete

## Test Entities

### Unit Test Suite
**Purpose**: Comprehensive collection of unit tests covering all code paths and edge cases for Activity API

**Key Attributes**:
- Test class organization by component (Handlers, Repositories, Models, Extensions, Configuration)
- Individual test methods with descriptive names following Given-When-Then pattern
- Mock dependencies with Moq framework
- Test data generation with AutoFixture
- Coverage tracking with coverlet.collector
- Specific coverage for Fitbit entity models

**Validation Rules**:
- Each public method must have corresponding test methods
- All exception paths must be tested
- Edge cases must be covered (null inputs, boundary conditions, malformed Fitbit data)
- Test methods must be independent and isolated
- Test names must clearly describe the scenario being tested (e.g., `ActivityHandlersShould_ReturnNotFound_WhenNoActivitiesExist`)

**State Transitions**:
- Created → Passing → Maintained
- Failing tests block CI/CD pipeline progression

### Integration Test Suite
**Purpose**: End-to-end tests verifying Activity API functionality with real dependencies

**Key Attributes**:
- Organized into Contract and E2E test categories
- Contract tests: Fast validation without database dependencies
- E2E tests: Full integration with Cosmos DB
- WebApplicationFactory for full application testing
- xUnit collection fixtures for resource management
- HTTP client testing of API endpoints
- Health check endpoint validation
- Service registration validation

**Validation Rules**:
- Contract tests must run without database initialization
- E2E tests must use isolated test database (biotrackr-test/activity-test)
- Test data must be cleaned up after execution
- Tests must handle async operations correctly
- Response validation must include status codes and content
- Service lifetime registrations must match established patterns

**State Transitions**:
- Setup (via IAsyncLifetime) → Execute → Cleanup (via IAsyncLifetime) → Report Results

**Test Organization**:
```
Contract/
  - ProgramStartupTests (service registration, configuration)
  - ApiSmokeTests (health checks, swagger)
E2E/
  - ActivityEndpointsTests (full endpoint testing with database)
```

### Test Fixtures
**Purpose**: Shared test infrastructure for managing application lifecycle and resources

**Key Entities**:

#### IntegrationTestFixture (Base)
- Manages WebApplicationFactory lifecycle
- Configures test environment with appsettings.Test.json overrides
- Provides optional database initialization
- Implements IAsyncLifetime for async setup/cleanup
- Shares CosmosClient and test configuration

#### ContractTestFixture (Derived)
- Inherits from IntegrationTestFixture
- Sets `InitializeDatabase = false` for fast tests
- Used by contract tests that don't need database
- Execution time: <1 minute

#### ActivityApiWebApplicationFactory
- Custom WebApplicationFactory<Program>
- Overrides configuration for test environment
- Manages Azure service configuration
- Provides test-specific service registration

### xUnit Collections
**Purpose**: Manage shared test context and proper test isolation

**Key Attributes**:
- [CollectionDefinition] attribute for fixture sharing
- ICollectionFixture<T> for fixture injection
- Prevents test interdependencies
- Optimizes resource usage

**Collection Definitions**:
```csharp
[CollectionDefinition(nameof(ContractTestCollection))]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture> { }
```

### Coverage Report
**Purpose**: Detailed analysis of code coverage metrics and quality gates

**Key Attributes**:
- Overall coverage percentage
- Component-level coverage breakdown
- Line coverage, branch coverage, method coverage
- Coverage trends over time
- Uncovered code identification
- HTML reports with detailed analysis
- Summary.txt for quick overview

**Validation Rules**:
- Overall coverage must be ≥80%
- New code coverage must be ≥90%
- Coverage reports must be generated for every build
- Coverage degradation must trigger alerts

### Test Environment Configuration
**Purpose**: Isolated infrastructure setup for running integration tests safely

**Key Attributes**:
- Separate Cosmos DB database for testing (biotrackr-test/activity-test)
- appsettings.Test.json for configuration overrides
- Test-specific application configuration
- Managed identity configuration for Azure services (bypassed in tests)
- Network isolation from production resources

**Validation Rules**:
- Test environment must be completely isolated from production
- Test data must never contain real user information
- Environment must support parallel test execution
- appsettings.Test.json must override all production settings

**Configuration Example**:
```json
{
  "Biotrackr": {
    "DatabaseName": "biotrackr-test",
    "ContainerName": "activity-test"
  }
}
```

### GitHub Actions Workflow Configuration
**Purpose**: Automated CI/CD pipeline configuration for test execution and reporting

**Key Attributes**:
- Separate jobs for unit tests and integration tests
- Coverage report generation and artifact collection
- Conditional execution based on code changes
- Consistent with Weight.Api workflow patterns

**Validation Rules**:
- Unit tests must complete within 5 minutes
- Integration tests must complete within 15 minutes
- Test failures must block deployment pipeline
- Coverage reports must be published as artifacts

## Activity API Specific Entities

### Fitbit Entity Models
**Purpose**: Data models representing Fitbit activity data

**Key Entities**:

#### Activity
- activityId, activityParentId, activityParentName
- calories, duration, steps, distance
- startDate, startTime, lastModified
- hasActiveZoneMinutes, hasStartTime, isFavorite
- **Testing Focus**: Null/missing properties, date format validation, distance unit handling

#### ActivityResponse
- activities (List<Activity>)
- goals (Goals)
- summary (Summary)
- **Testing Focus**: Empty lists, null goals/summary, nested object validation

#### Distance
- activity, distance unit conversion
- **Testing Focus**: Unit conversions, null distance values

#### Goals
- Activity goals and targets
- **Testing Focus**: Goal tracking, progress calculation

#### HeartRateZone
- Heart rate zone data and calculations
- **Testing Focus**: Incomplete data, zone boundary validation

#### Summary
- Activity summary aggregation
- **Testing Focus**: Aggregation logic, null value handling

### ActivityDocument
- id, Activity (ActivityResponse), Date, DocumentType
- **Testing Focus**: Cosmos DB document structure, date format consistency

### PaginationRequest
- continuationToken, maxItemCount
- **Testing Focus**: Boundary conditions, negative values, null tokens, token corruption

### Settings
- DatabaseName, ContainerName
- **Testing Focus**: Configuration loading, default values, validation

## Test Data Relationships

### Unit Test Data
- **Test Fixtures**: Shared setup code for test classes
- **Mock Objects**: Simulated dependencies using Moq framework
- **Test Data Builders**: AutoFixture configurations for consistent test data
- **Assertion Helpers**: FluentAssertions extensions for readable test verification
- **Fitbit Test Data**: Synthetic Fitbit entity objects covering all edge cases

### Integration Test Data
- **Test Database**: Isolated Cosmos DB database (biotrackr-test/activity-test)
- **Seed Data**: Minimal dataset required for comprehensive testing
- **Cleanup Procedures**: Automated data removal via IAsyncLifetime
- **Test Fixtures**: Shared via xUnit collection fixtures

## Test Execution Flow

### Unit Test Execution
1. **Setup**: Initialize mocks and test data using AutoFixture
2. **Act**: Execute the method under test with prepared inputs
3. **Assert**: Verify outputs and interactions using FluentAssertions
4. **Cleanup**: Dispose of resources (handled automatically by xUnit)

### Contract Test Execution
1. **Fixture Setup**: ContractTestFixture creates WebApplicationFactory (no database)
2. **Test Execution**: Verify service registration, configuration, health checks
3. **Assertion**: Validate startup behavior and dependencies
4. **Fixture Cleanup**: Dispose of factory via IAsyncLifetime
5. **Execution Time**: <1 minute total

### E2E Integration Test Execution
1. **Fixture Setup**: IntegrationTestFixture initializes database and factory
2. **Database Preparation**: Ensure clean test database state
3. **Test Execution**: Make HTTP requests to API endpoints
4. **Response Validation**: Verify HTTP status codes, headers, and content
5. **Data Verification**: Confirm database state changes
6. **Fixture Cleanup**: Remove test data via IAsyncLifetime
7. **Execution Time**: ~5-10 minutes

## Coverage Tracking Strategy

### Component Coverage Targets
- **EndpointHandlers**: ≥90% (critical business logic including ActivityHandlers)
- **Repositories**: ≥85% (data access patterns for CosmosRepository)
- **Models**: ≥80% (ActivityDocument, PaginationRequest validation)
- **Extensions**: ≥85% (EndpointRouteBuilderExtensions)
- **Configuration**: ≥80% (Settings loading and validation)
- **Fitbit Entities**: ≥80% (Activity, ActivityResponse, Distance, Goals, HeartRateZone, Summary)

### Exclusions from Coverage
- Program.cs startup code (contract tested via ProgramStartupTests)
- [ExcludeFromCodeCoverage] attributes (Fitbit entity models already marked)
- Generated code and attributes
- Third-party library integrations (tested via integration tests)
- Exception constructors and trivial properties

## Quality Gates

### Unit Test Quality Gates
- All tests must pass (0 failures)
- Coverage must meet component-specific targets
- Test execution time must be <5 minutes
- No code duplication in test methods
- All Fitbit entity edge cases covered

### Integration Test Quality Gates
- All contract tests must pass (<1 minute)
- All E2E tests must pass (~5-10 minutes)
- Database operations must complete successfully
- Service registration tests verify correct lifetimes
- Health check endpoints return healthy status
- Test execution time must be <15 minutes total

### Overall Quality Gates
- Combined coverage must be ≥80%
- No high-severity static analysis violations
- All tests must be deterministic (no flaky tests)
- Test maintenance burden must be sustainable
- Consistent patterns with Weight.Api for microservice uniformity

## Service Registration Testing

### Required Service Lifetime Validations
Following decision record 2025-10-28-service-lifetime-registration.md:

- **CosmosClient**: Singleton (expensive to create, thread-safe)
- **ICosmosRepository**: Transient (cheap to instantiate)
- **IOptions<Settings>**: Singleton (configuration)
- **HealthCheckService**: Registered and functional

**Test Pattern Example**:
```csharp
[Fact]
public void CosmosClient_Should_Be_Registered_As_Singleton()
{
    var services = _fixture.Factory.Services;
    var client1 = services.GetService<CosmosClient>();
    var client2 = services.GetService<CosmosClient>();
    client1.Should().BeSameAs(client2);
}
```
