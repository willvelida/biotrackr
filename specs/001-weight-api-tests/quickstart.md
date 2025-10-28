# Quickstart Guide: Enhanced Test Coverage for Weight API

**Target Audience**: Developers implementing the enhanced testing strategy  
**Prerequisites**: .NET 9.0 SDK, Azure CLI, Visual Studio Code or similar IDE  
**Estimated Time**: 30-45 minutes for initial setup

## Overview

This guide walks you through implementing the enhanced test coverage for the Biotrackr Weight API, including:
- Extending unit tests from 39% to ≥80% coverage
- Creating a new integration test project
- Setting up GitHub Actions workflows for automated testing

## Phase 1: Extend Unit Test Coverage (Priority P1)

### Step 1: Analyze Current Coverage

1. **Run existing tests with coverage**:
   ```bash
   cd src/Biotrackr.Weight.Api/Biotrackr.Weight.Api.UnitTests
   dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
   ```

2. **Generate coverage report**:
   ```bash
   dotnet tool install -g dotnet-reportgenerator-globaltool
   reportgenerator -reports:"./coverage/**/coverage.cobertura.xml" -targetdir:"./coverage/html" -reporttypes:"Html"
   ```

3. **Open coverage report** in browser: `./coverage/html/index.html`

### Step 2: Identify Coverage Gaps

Current coverage gaps to address:

1. **Configuration/Settings.cs** - Add tests for property validation
2. **Extensions/EndpointRouteBuilderExtensions.cs** - Test endpoint registration
3. **Program.cs startup logic** - Integration tests will cover this
4. **Error handling paths** in existing handlers
5. **Edge cases** in pagination and date validation

### Step 3: Add Missing Unit Tests

1. **Create ConfigurationTests folder**:
   ```bash
   mkdir src/Biotrackr.Weight.Api/Biotrackr.Weight.Api.UnitTests/ConfigurationTests
   ```

2. **Add SettingsShould.cs**:
   ```csharp
   public class SettingsShould
   {
       [Fact]
       public void BeConstructableWithValidProperties()
       {
           // Test Settings class construction and property validation
       }
   }
   ```

3. **Create ExtensionTests folder**:
   ```bash
   mkdir src/Biotrackr.Weight.Api/Biotrackr.Weight.Api.UnitTests/ExtensionTests
   ```

4. **Add EndpointRouteBuilderExtensionsShould.cs**:
   ```csharp
   public class EndpointRouteBuilderExtensionsShould
   {
       [Fact]
       public void RegisterWeightEndpoints_ShouldConfigureCorrectRoutes()
       {
           // Test endpoint registration logic
       }
   }
   ```

### Step 4: Enhance Existing Tests

1. **Add error handling tests** to WeightHandlersShould.cs:
   ```csharp
   [Fact]
   public async Task GetWeightsByDateRange_ShouldReturnBadRequest_WhenStartDateAfterEndDate()
   {
       // Test date validation logic
   }
   ```

2. **Add edge case tests** for pagination:
   ```csharp
   [Theory]
   [InlineData(0, 20)]
   [InlineData(-1, 20)]
   [InlineData(1, 0)]
   [InlineData(1, -1)]
   public async Task GetAllWeights_ShouldHandleInvalidPagination(int pageNumber, int pageSize)
   {
       // Test pagination edge cases
   }
   ```

### Step 5: Verify Coverage Target

1. **Run tests with coverage**:
   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```

2. **Check coverage percentage** - should be ≥80%

## Phase 2: Create Integration Test Project (Priority P2)

### Step 1: Create Integration Test Project

1. **Create project directory**:
   ```bash
   mkdir src/Biotrackr.Weight.Api/Biotrackr.Weight.Api.IntegrationTests
   cd src/Biotrackr.Weight.Api/Biotrackr.Weight.Api.IntegrationTests
   ```

2. **Initialize project**:
   ```bash
   dotnet new xunit
   ```

3. **Add required packages**:
   ```bash
   dotnet add package Microsoft.AspNetCore.Mvc.Testing
   dotnet add package FluentAssertions
   dotnet add package Microsoft.Extensions.Configuration
   dotnet add package Azure.Identity
   dotnet add package Microsoft.Azure.Cosmos
   ```

4. **Add project reference**:
   ```bash
   dotnet add reference ../Biotrackr.Weight.Api/Biotrackr.Weight.Api.csproj
   ```

### Step 2: Create Test Infrastructure

1. **Create TestFixtures folder** with base test class:
   ```csharp
   public class WeightApiIntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
   {
       protected readonly WebApplicationFactory<Program> Factory;
       protected readonly HttpClient Client;
       
       public WeightApiIntegrationTestBase(WebApplicationFactory<Program> factory)
       {
           Factory = factory;
           Client = factory.CreateClient();
       }
   }
   ```

2. **Create TestData folder** with data helpers:
   ```csharp
   public static class TestDataHelper
   {
       public static WeightDocument CreateTestWeightDocument(string date)
       {
           // Create test data
       }
       
       public static async Task CleanupTestData(CosmosClient cosmosClient)
       {
           // Cleanup logic
       }
   }
   ```

### Step 3: Create API Integration Tests

1. **Create ApiTests folder** with endpoint tests:
   ```csharp
   public class WeightApiEndpointsShould : WeightApiIntegrationTestBase
   {
       [Fact]
       public async Task GetAllWeights_ShouldReturnSuccessStatusCode()
       {
           // Test GET / endpoint
       }
       
       [Fact]
       public async Task GetWeightByDate_ShouldReturnCorrectWeight()
       {
           // Test GET /{date} endpoint
       }
   }
   ```

2. **Create HealthCheckTests folder**:
   ```csharp
   public class HealthCheckEndpointsShould : WeightApiIntegrationTestBase
   {
       [Fact]
       public async Task HealthCheck_ShouldReturnHealthy()
       {
           // Test health check endpoints
       }
   }
   ```

### Step 4: Configure Test Environment

1. **Create appsettings.Test.json**:
   ```json
   {
     "Biotrackr": {
       "DatabaseName": "biotrackr-test",
       "ContainerName": "weight-test"
     }
   }
   ```

2. **Configure test startup** in TestFixtures:
   ```csharp
   public class TestWebApplicationFactory : WebApplicationFactory<Program>
   {
       protected override void ConfigureWebHost(IWebHostBuilder builder)
       {
           builder.ConfigureAppConfiguration((context, config) =>
           {
               config.AddJsonFile("appsettings.Test.json");
           });
       }
   }
   ```

## Phase 3: GitHub Actions Integration (Priority P3)

### Step 1: Create Workflow Templates

1. **Create enhanced unit test template**:
   ```bash
   mkdir -p .github/workflows
   # Copy template from contracts/github-actions-workflows.md
   ```

2. **Create integration test template**:
   ```bash
   # Copy integration test template from contracts/github-actions-workflows.md
   ```

### Step 2: Update Weight API Workflow

1. **Modify deploy-weight-api.yml**:
   - Replace existing unit test job with enhanced version
   - Add integration test job after deployment
   - Add quality gates enforcement

2. **Add required secrets** in GitHub repository:
   - `TEST_DATABASE_CONNECTION`: Test Cosmos DB connection string
   - Ensure other Azure secrets are properly configured

### Step 3: Test Workflow Integration

1. **Create pull request** with changes
2. **Verify workflow execution**:
   - Unit tests run with coverage reporting
   - Integration tests run after deployment
   - Quality gates block deployment on failures

## Verification Checklist

### Unit Test Verification
- [ ] Coverage report shows ≥80% overall coverage
- [ ] All new test classes follow naming convention (`ClassNameShould`)
- [ ] Tests use Given-When-Then pattern in naming
- [ ] Mock dependencies are properly configured
- [ ] Test execution completes in <5 minutes

### Integration Test Verification
- [ ] Integration tests run against deployed DEV environment
- [ ] All API endpoints are tested end-to-end
- [ ] Test data cleanup completes successfully
- [ ] Integration tests complete in <15 minutes
- [ ] Health checks are validated

### CI/CD Verification
- [ ] GitHub Actions workflows execute successfully
- [ ] Coverage reports are generated and uploaded
- [ ] Quality gates block deployment on test failures
- [ ] Test results are preserved as artifacts

## Troubleshooting

### Common Issues

1. **Coverage Below Threshold**:
   - Review coverage report HTML for uncovered lines
   - Add tests for missing edge cases and error paths
   - Verify test execution includes all code paths

2. **Integration Test Failures**:
   - Check test environment configuration
   - Verify Azure authentication in GitHub Actions
   - Ensure test database is properly configured
   - Review test data cleanup procedures

3. **Workflow Timeouts**:
   - Optimize test execution for performance
   - Consider parallel test execution
   - Review resource allocation in test environment

### Performance Optimization

1. **Unit Test Performance**:
   - Use `ParallelAlgorithm.Conservative` for test collection
   - Minimize test setup overhead
   - Cache expensive mock setups

2. **Integration Test Performance**:
   - Use connection pooling for database access
   - Implement efficient test data cleanup
   - Run tests in parallel where possible

## Next Steps

After completing this quickstart:

1. **Monitor coverage trends** over time
2. **Set up automated coverage reporting** in pull requests
3. **Consider adding performance tests** for critical endpoints
4. **Implement mutation testing** for higher quality assurance
5. **Document test maintenance procedures** for the team

## Support

For issues or questions:
- Review the detailed specifications in the `/specs/001-weight-api-tests/` directory
- Check GitHub Actions workflow logs for detailed error information
- Verify Azure resource configuration and permissions