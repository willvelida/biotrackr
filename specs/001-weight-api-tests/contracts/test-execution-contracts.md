# Test Execution Contracts

## Unit Test Execution Contract

### Input Parameters
```csharp
public interface IUnitTestExecution
{
    string TestProject { get; }           // Project path containing tests
    string[] TestCategories { get; }      // Optional test categories to run
    bool EnableCoverage { get; }          // Enable coverage collection
    string CoverageFormat { get; }        // Coverage output format (cobertura, opencover)
    int TimeoutMinutes { get; }           // Maximum execution time
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
    double CoveragePercentage { get; }   // Overall coverage percentage
    string CoverageReportPath { get; }   // Path to coverage report
    TestFailure[] Failures { get; }     // Details of any test failures
}
```

### Test Failure Details
```csharp
public interface ITestFailure
{
    string TestName { get; }             // Full test method name
    string TestClass { get; }            // Test class name
    string ErrorMessage { get; }         // Failure error message
    string StackTrace { get; }           // Exception stack trace
    string Category { get; }             // Test category (if applicable)
}
```

## Integration Test Execution Contract

### Input Parameters
```csharp
public interface IIntegrationTestExecution
{
    string TestProject { get; }           // Integration test project path
    string EnvironmentUrl { get; }        // Target environment base URL
    string DatabaseConnectionString { get; } // Test database connection
    string AuthenticationToken { get; }   // Authentication for API calls
    bool CleanupAfterTests { get; }      // Cleanup test data after execution
    int TimeoutMinutes { get; }          // Maximum execution time
}
```

### Output Results
```csharp
public interface IIntegrationTestResults
{
    bool Success { get; }                 // Overall test execution success
    int TotalTests { get; }              // Total number of integration tests
    int PassedTests { get; }             // Number of passed tests
    int FailedTests { get; }             // Number of failed tests
    TimeSpan ExecutionTime { get; }      // Total execution time
    EndpointTestResult[] EndpointResults { get; } // Per-endpoint test results
    TestFailure[] Failures { get; }     // Details of any test failures
    bool CleanupCompleted { get; }       // Whether cleanup was successful
}
```

### Endpoint Test Results
```csharp
public interface IEndpointTestResult
{
    string EndpointPath { get; }         // API endpoint path
    string HttpMethod { get; }           // HTTP method (GET, POST, etc.)
    bool Success { get; }                // Endpoint test success
    int ResponseStatusCode { get; }      // HTTP response status code
    TimeSpan ResponseTime { get; }       // Response time
    string ErrorMessage { get; }         // Error message if failed
}
```

## Coverage Reporting Contract

### Coverage Report Structure
```csharp
public interface ICoverageReport
{
    double OverallCoverage { get; }       // Overall coverage percentage
    ComponentCoverage[] ComponentCoverage { get; } // Per-component coverage
    string ReportFormat { get; }          // Report format (cobertura, html, json)
    string ReportPath { get; }            // Path to generated report
    DateTime GeneratedAt { get; }         // Report generation timestamp
    bool MeetsThreshold { get; }          // Whether coverage meets 80% threshold
}
```

### Component Coverage Details
```csharp
public interface IComponentCoverage
{
    string ComponentName { get; }         // Component name (e.g., "EndpointHandlers")
    double LineCoverage { get; }          // Line coverage percentage
    double BranchCoverage { get; }        // Branch coverage percentage
    double MethodCoverage { get; }        // Method coverage percentage
    int TotalLines { get; }               // Total lines of code
    int CoveredLines { get; }             // Lines covered by tests
    int TotalBranches { get; }            // Total branches
    int CoveredBranches { get; }          // Branches covered by tests
    string[] UncoveredMethods { get; }    // Methods not covered by tests
}
```

## GitHub Actions Workflow Contract

### Workflow Input Parameters
```yaml
inputs:
  dotnet-version:
    description: '.NET version to use'
    required: true
    type: string
  test-project-path:
    description: 'Path to test project'
    required: true
    type: string
  coverage-threshold:
    description: 'Minimum coverage percentage required'
    required: false
    type: number
    default: 80
  timeout-minutes:
    description: 'Maximum execution time in minutes'
    required: false
    type: number
    default: 15
```

### Workflow Output Parameters
```yaml
outputs:
  test-success:
    description: 'Whether all tests passed'
    value: ${{ steps.test-execution.outputs.success }}
  coverage-percentage:
    description: 'Overall code coverage percentage'
    value: ${{ steps.coverage-report.outputs.coverage }}
  coverage-report-path:
    description: 'Path to coverage report artifact'
    value: ${{ steps.coverage-report.outputs.report-path }}
  test-results-path:
    description: 'Path to test results artifact'
    value: ${{ steps.test-execution.outputs.results-path }}
```

## Test Environment Configuration Contract

### Test Database Configuration
```csharp
public interface ITestDatabaseConfig
{
    string ConnectionString { get; }      // Test database connection string
    string DatabaseName { get; }         // Test database name
    string ContainerName { get; }        // Test container name
    bool AutoCleanup { get; }            // Enable automatic cleanup
    int CleanupDelaySeconds { get; }     // Delay before cleanup
}
```

### Test Authentication Configuration
```csharp
public interface ITestAuthConfig
{
    string ClientId { get; }             // Azure AD client ID for tests
    string TenantId { get; }             // Azure AD tenant ID
    string TestUserPrincipal { get; }    // Test user principal name
    string[] RequiredScopes { get; }     // Required OAuth scopes
}
```

## API Test Validation Contract

### HTTP Response Validation
```csharp
public interface IApiResponseValidation
{
    int ExpectedStatusCode { get; }       // Expected HTTP status code
    string[] RequiredHeaders { get; }     // Required response headers
    string ContentType { get; }          // Expected content type
    object ExpectedContent { get; }      // Expected response content structure
    TimeSpan MaxResponseTime { get; }    // Maximum acceptable response time
}
```

### Database State Validation
```csharp
public interface IDatabaseStateValidation
{
    string ContainerName { get; }         // Container to validate
    string PartitionKey { get; }         // Partition key for query
    object ExpectedDocument { get; }     // Expected document structure
    int ExpectedDocumentCount { get; }   // Expected number of documents
    string[] RequiredFields { get; }     // Required fields in documents
}
```