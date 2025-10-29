# Test Execution Contracts for Activity API

## Unit Test Execution Contract

### Input Parameters
```csharp
public interface IUnitTestExecution
{
    string TestProject { get; }           // Biotrackr.Activity.Api.UnitTests
    string[] TestCategories { get; }      // Optional: EndpointHandlers, Models, Repositories, etc.
    bool EnableCoverage { get; }          // Enable coverage collection
    string CoverageFormat { get; }        // Coverage output format (cobertura, html)
    int TimeoutMinutes { get; }           // Maximum execution time (5 minutes)
}
```

### Output Results
```csharp
public interface IUnitTestResults
{
    bool Success { get; }                 // Overall test execution success
    int TotalTests { get; }              // Total number of tests executed
    int PassedTests { get; }             // Number of passed tests
    int FailedTests { get; }             // Number of failed tests
    int SkippedTests { get; }            // Number of skipped tests
    TimeSpan ExecutionTime { get; }      // Total execution time
    double CoveragePercentage { get; }   // Overall coverage percentage (target: ≥80%)
    string CoverageReportPath { get; }   // Path to coverage report
    TestFailure[] Failures { get; }     // Details of any test failures
}
```

### Test Failure Details
```csharp
public interface ITestFailure
{
    string TestName { get; }             // Full test method name (e.g., ActivityHandlersShould_ReturnOk_WhenActivitiesExist)
    string TestClass { get; }            // Test class name (e.g., ActivityHandlersShould)
    string ErrorMessage { get; }         // Failure error message
    string StackTrace { get; }           // Exception stack trace
    string Category { get; }             // Test category (EndpointHandlers, Models, Repositories, etc.)
}
```

## Integration Test Execution Contract

### Contract Test Execution (Fast, No Database)
```csharp
public interface IContractTestExecution
{
    string TestProject { get; }           // Biotrackr.Activity.Api.IntegrationTests
    string TestCollection { get; }        // "ContractTestCollection"
    bool InitializeDatabase { get; }      // false (ContractTestFixture)
    int TimeoutMinutes { get; }          // Maximum execution time (1 minute)
}
```

### E2E Test Execution (Full Integration)
```csharp
public interface IE2ETestExecution
{
    string TestProject { get; }           // Biotrackr.Activity.Api.IntegrationTests
    string TestCollection { get; }        // E2E tests
    string DatabaseConnectionString { get; } // Test database connection (biotrackr-test/activity-test)
    bool CleanupAfterTests { get; }      // true (automatic via IAsyncLifetime)
    int TimeoutMinutes { get; }          // Maximum execution time (15 minutes)
}
```

### Integration Test Output Results
```csharp
public interface IIntegrationTestResults
{
    bool Success { get; }                 // Overall test execution success
    int TotalContractTests { get; }      // Number of contract tests
    int TotalE2ETests { get; }          // Number of E2E tests
    int PassedTests { get; }             // Number of passed tests
    int FailedTests { get; }             // Number of failed tests
    TimeSpan ContractTestTime { get; }   // Contract test execution time (<1 min)
    TimeSpan E2ETestTime { get; }       // E2E test execution time (~5-10 min)
    TimeSpan TotalExecutionTime { get; } // Total execution time (<15 min)
    EndpointTestResult[] EndpointResults { get; } // Per-endpoint test results
    ServiceRegistrationResult[] ServiceResults { get; } // Service registration validation
    TestFailure[] Failures { get; }     // Details of any test failures
    bool CleanupCompleted { get; }       // Whether cleanup was successful
}
```

### Endpoint Test Results
```csharp
public interface IEndpointTestResult
{
    string EndpointPath { get; }         // API endpoint path (e.g., "/activity")
    string HttpMethod { get; }           // HTTP method (GET, POST, etc.)
    bool Success { get; }                // Endpoint test success
    int ResponseStatusCode { get; }      // HTTP response status code
    TimeSpan ResponseTime { get; }       // Response time
    string ErrorMessage { get; }         // Error message if failed
    bool DatabaseOperationValidated { get; } // Whether database state was verified
}
```

### Service Registration Results
```csharp
public interface IServiceRegistrationResult
{
    string ServiceName { get; }          // Service interface name (e.g., "ICosmosRepository")
    string ExpectedLifetime { get; }     // Expected lifetime (Singleton, Transient, Scoped)
    string ActualLifetime { get; }       // Actual lifetime registered
    bool CorrectlyRegistered { get; }    // Whether registration matches expectations
    string ErrorMessage { get; }         // Error if registration incorrect
}
```

## Coverage Reporting Contract

### Coverage Report Structure
```csharp
public interface ICoverageReport
{
    double OverallCoverage { get; }       // Overall coverage percentage (target: ≥80%)
    ComponentCoverage[] ComponentCoverage { get; } // Per-component coverage
    string ReportFormat { get; }          // Report format (cobertura, html, summary.txt)
    string ReportPath { get; }            // Path to generated report
    DateTime GeneratedAt { get; }         // Report generation timestamp
    bool MeetsThreshold { get; }          // Whether coverage meets 80% threshold
}
```

### Component Coverage Details
```csharp
public interface IComponentCoverage
{
    string ComponentName { get; }         // Component name (ActivityHandlers, CosmosRepository, etc.)
    double LineCoverage { get; }          // Line coverage percentage
    double BranchCoverage { get; }        // Branch coverage percentage
    double MethodCoverage { get; }        // Method coverage percentage
    int TotalLines { get; }               // Total lines of code
    int CoveredLines { get; }             // Lines covered by tests
    int TotalBranches { get; }            // Total branches
    int CoveredBranches { get; }          // Branches covered by tests
    string[] UncoveredMethods { get; }    // Methods not covered by tests
    double TargetCoverage { get; }        // Component-specific target (e.g., 90% for handlers)
}
```

### Activity API Component Coverage Targets
- **ActivityHandlers**: ≥90% (critical business logic)
- **CosmosRepository**: ≥85% (data access patterns)
- **ActivityDocument/Models**: ≥80% (data structures)
- **PaginationRequest**: ≥80% (validation logic)
- **EndpointRouteBuilderExtensions**: ≥85% (routing setup)
- **Settings**: ≥80% (configuration)
- **Fitbit Entities**: ≥80% (Activity, ActivityResponse, Distance, Goals, HeartRateZone, Summary)

## GitHub Actions Workflow Contract

### Workflow Input Parameters
```yaml
inputs:
  dotnet-version:
    description: '.NET version to use'
    required: true
    type: string
    default: '9.0.x'
  unit-test-project:
    description: 'Path to unit test project'
    required: true
    type: string
    default: 'src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.UnitTests/Biotrackr.Activity.Api.UnitTests.csproj'
  integration-test-project:
    description: 'Path to integration test project'
    required: true
    type: string
    default: 'src/Biotrackr.Activity.Api/Biotrackr.Activity.Api.IntegrationTests/Biotrackr.Activity.Api.IntegrationTests.csproj'
  coverage-threshold:
    description: 'Minimum coverage percentage required'
    required: false
    type: number
    default: 80
  unit-test-timeout:
    description: 'Unit test timeout in minutes'
    required: false
    type: number
    default: 5
  integration-test-timeout:
    description: 'Integration test timeout in minutes'
    required: false
    type: number
    default: 15
```

### Workflow Output Parameters
```yaml
outputs:
  unit-test-success:
    description: 'Whether all unit tests passed'
    value: ${{ steps.unit-test.outputs.success }}
  integration-test-success:
    description: 'Whether all integration tests passed'
    value: ${{ steps.integration-test.outputs.success }}
  coverage-percentage:
    description: 'Overall code coverage percentage'
    value: ${{ steps.coverage-report.outputs.coverage }}
  coverage-meets-threshold:
    description: 'Whether coverage meets 80% threshold'
    value: ${{ steps.coverage-report.outputs.meets-threshold }}
  coverage-report-path:
    description: 'Path to coverage report artifact'
    value: ${{ steps.coverage-report.outputs.report-path }}
  test-results-path:
    description: 'Path to test results artifact'
    value: ${{ steps.test-execution.outputs.results-path }}
```

## Test Fixture Configuration Contract

### Integration Test Fixture Interface
```csharp
public interface IIntegrationTestFixture : IAsyncLifetime
{
    WebApplicationFactory<Program> Factory { get; }
    HttpClient Client { get; }
    CosmosClient CosmosClient { get; }
    bool InitializeDatabase { get; }
    string DatabaseName { get; }
    string ContainerName { get; }
    
    Task InitializeAsync();  // Setup (create database, seed data)
    Task DisposeAsync();     // Cleanup (remove test data, dispose resources)
}
```

### Contract Test Fixture Configuration
```csharp
public class ContractTestFixture : IntegrationTestFixture
{
    protected override bool InitializeDatabase => false;  // Fast tests, no database
}
```

### WebApplicationFactory Configuration
```csharp
public interface IActivityApiWebApplicationFactory
{
    IServiceProvider Services { get; }
    HttpClient CreateClient();
    
    // Configuration overrides for testing
    void ConfigureTestServices(IServiceCollection services);
    void ConfigureTestAppConfiguration(IConfigurationBuilder config);
}
```

## Test Environment Configuration Contract

### Test Database Configuration
```csharp
public interface ITestDatabaseConfig
{
    string ConnectionString { get; }      // Cosmos DB endpoint
    string DatabaseName { get; }         // "biotrackr-test"
    string ContainerName { get; }        // "activity-test"
    bool AutoCleanup { get; }            // true (via IAsyncLifetime)
    int CleanupDelaySeconds { get; }     // 0 (immediate cleanup)
    string PartitionKeyPath { get; }     // "/documentType"
}
```

### Test Configuration Override (appsettings.Test.json)
```json
{
  "Biotrackr": {
    "DatabaseName": "biotrackr-test",
    "ContainerName": "activity-test"
  },
  "azureappconfigendpoint": "http://localhost:5000",  // Bypassed in tests
  "cosmosdbendpoint": "https://test-cosmos.documents.azure.com:443/",
  "managedidentityclientid": "00000000-0000-0000-0000-000000000000"  // Test identity
}
```

## API Test Validation Contract

### HTTP Response Validation
```csharp
public interface IApiResponseValidation
{
    int ExpectedStatusCode { get; }       // Expected HTTP status code (200, 404, etc.)
    string[] RequiredHeaders { get; }     // Required response headers
    string ContentType { get; }          // Expected content type (application/json)
    object ExpectedContent { get; }      // Expected response content structure
    TimeSpan MaxResponseTime { get; }    // Maximum acceptable response time
    bool ValidateSchema { get; }         // Whether to validate against OpenAPI schema
}
```

### Database State Validation
```csharp
public interface IDatabaseStateValidation
{
    string ContainerName { get; }        // "activity-test"
    string DocumentId { get; }           // Document to validate
    string PartitionKey { get; }         // Partition key value
    object ExpectedState { get; }        // Expected document state
    bool DocumentShouldExist { get; }    // Whether document should exist after operation
}
```

## Service Registration Validation Contract

### Service Lifetime Validation
```csharp
public interface IServiceLifetimeValidation
{
    Type ServiceType { get; }            // Service interface (e.g., typeof(ICosmosRepository))
    Type ImplementationType { get; }     // Implementation type (e.g., typeof(CosmosRepository))
    ServiceLifetime ExpectedLifetime { get; } // Expected lifetime (Singleton, Transient, Scoped)
    
    // Validation method
    bool ValidateLifetime(IServiceProvider services);
}
```

### Required Service Registrations
Following decision record 2025-10-28-service-lifetime-registration.md:

- **CosmosClient**: Singleton (expensive, thread-safe, connection pooling)
- **ICosmosRepository**: Transient (cheap to instantiate, stateless)
- **IOptions<Settings>**: Singleton (configuration)
- **HealthCheckService**: Singleton (health monitoring)

### Validation Test Example
```csharp
[Fact]
public void CosmosClient_Should_Be_Registered_As_Singleton()
{
    // Arrange
    var services = _fixture.Factory.Services;
    
    // Act
    var client1 = services.GetService<CosmosClient>();
    var client2 = services.GetService<CosmosClient>();
    
    // Assert
    client1.Should().NotBeNull();
    client2.Should().NotBeNull();
    client1.Should().BeSameAs(client2);  // Same instance = Singleton
}
```
