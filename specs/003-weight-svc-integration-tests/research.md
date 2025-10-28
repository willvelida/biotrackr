# Research: Weight Service Integration Tests

**Date**: October 28, 2025  
**Feature**: Integration test infrastructure for Biotrackr.Weight.Svc

## Overview

This document consolidates research findings for implementing integration tests for the Weight Service, focusing on test patterns, mocking strategies, CI/CD integration, and alignment with existing project standards.

## 1. Integration Test Patterns for .NET Worker Services

### Decision: xUnit with Collection Fixtures and Dependency Injection

**Rationale**:
- xUnit is already used in all Biotrackr test projects (unit tests and Weight API integration tests)
- Collection fixtures provide shared context across tests while maintaining isolation
- IClassFixture<T> for test-class-level setup
- ICollectionFixture<T> for test-collection-level setup (ideal for expensive resources like Cosmos DB connections)
- Supports async initialization via IAsyncLifetime

**Implementation Pattern**:
```csharp
// Contract tests - lightweight, no external dependencies
public class ContractTestFixture
{
    public IServiceProvider ServiceProvider { get; }
    // Build minimal service collection for DI verification
}

// E2E tests - full infrastructure with Cosmos DB Emulator
public class IntegrationTestFixture : IAsyncLifetime
{
    public CosmosClient CosmosClient { get; }
    public Container Container { get; }
    
    public async Task InitializeAsync()
    {
        // Connect to emulator, create database/container
    }
    
    public async Task DisposeAsync()
    {
        // Cleanup database/container
    }
}
```

**Alternatives Considered**:
- **NUnit**: Rejected - xUnit already standard in project
- **MSTest**: Rejected - Less flexible fixture model, not used in project
- **In-memory mocking only**: Rejected - Need to verify actual Cosmos DB operations

**References**:
- xUnit documentation: https://xunit.net/docs/shared-context
- Microsoft integration testing guidance: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests

---

## 2. Mocking Strategy for External Dependencies

### Decision: Moq for SecretClient, HttpMessageHandler for Fitbit API

**Rationale**:
- Moq 4.20.72 already used extensively in Weight Service unit tests
- HttpMessageHandler mocking is standard practice for testing HttpClient-based services
- Provides full control over response scenarios (success, errors, timeouts)
- No external API dependencies or rate limits in tests
- Fast, deterministic test execution

**Implementation Pattern**:

**SecretClient Mocking**:
```csharp
var mockSecretClient = new Mock<SecretClient>();
mockSecretClient
    .Setup(x => x.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
    .ReturnsAsync(SecretModelFactory.KeyVaultSecret(
        new SecretProperties("AccessToken"), 
        "test-access-token"));
```

**HttpMessageHandler Mocking**:
```csharp
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
    
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}

// Usage
var mockHandler = new MockHttpMessageHandler(request => 
    new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(fitbitResponseJson)
    });
```

**Alternatives Considered**:
- **WireMock.Net**: Rejected - Adds complexity, overhead for simple scenarios
- **Actual API calls**: Rejected - Slow, unreliable, requires credentials, rate limits
- **TestServer with stub API**: Rejected - Over-engineered for HTTP mocking needs

**References**:
- Moq documentation: https://github.com/moq/moq4
- HttpClient testing patterns: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices

---

## 3. Azure Cosmos DB Emulator Integration

### Decision: Use Cosmos DB Emulator via GitHub Actions Services

**Rationale**:
- Already successfully implemented for Weight API integration tests
- Proven reliable in CI/CD pipeline
- No additional dependencies or complexity
- Provides actual Cosmos DB compatibility (unlike in-memory alternatives)
- Certificate handling already established in template-dotnet-run-e2e-tests.yml

**Implementation Details**:

**GitHub Actions Service Configuration**:
```yaml
services:
  cosmos-emulator:
    image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
    ports:
      - 8081:8081
      - 10251:10251
      - 10252:10252
      - 10253:10253
      - 10254:10254
    env:
      AZURE_COSMOS_EMULATOR_PARTITION_COUNT: 10
      AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE: false
      AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE: 127.0.0.1
    options: >-
      --health-cmd "curl -k https://localhost:8081/_explorer/emulator.pem"
      --health-interval 10s
      --health-timeout 5s
      --health-retries 30
```

**Test Configuration**:
```json
{
  "cosmosdbendpoint": "https://localhost:8081",
  "Biotrackr:CosmosDb:AccountKey": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
  "Biotrackr:DatabaseName": "biotrackr-test",
  "Biotrackr:ContainerName": "weight-test"
}
```

**Cleanup Strategy**:
- Delete database after each test class
- No data persistence between test runs
- Fresh container for each collection fixture

**Alternatives Considered**:
- **Testcontainers.CosmosDb**: Rejected - Previously caused issues, added complexity
- **In-memory repository**: Rejected - Doesn't test actual Cosmos DB serialization, queries, partition keys
- **Actual Cosmos DB account**: Rejected - Cost, cleanup complexity, slower

**References**:
- Cosmos DB Emulator documentation: https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator
- Existing implementation: .github/workflows/template-dotnet-run-e2e-tests.yml

---

## 4. GitHub Actions Workflow Integration

### Decision: Two Separate Jobs (Contract → E2E) in deploy-weight-service.yml

**Rationale**:
- Fast feedback from contract tests (<2s) before running E2E tests
- Clear separation allows independent execution: `--filter "FullyQualifiedName~Contract"`
- Parallel execution of contract tests with build/lint steps
- E2E tests only run if contract tests pass
- Matches established pattern from Weight API

**Workflow Structure**:
```yaml
jobs:
  run-unit-tests:
    # ... existing unit test job

  run-contract-tests:
    name: Run Contract Tests
    needs: run-unit-tests
    uses: willvelida/biotrackr/.github/workflows/template-dotnet-run-contract-tests.yml@main
    with:
      dotnet-version: ${{ needs.env-setup.outputs.dotnet-version }}
      working-directory: ./src/Biotrackr.Weight.Svc
      test-filter: 'FullyQualifiedName~Contract'

  run-e2e-tests:
    name: Run E2E Tests
    needs: run-contract-tests
    uses: willvelida/biotrackr/.github/workflows/template-dotnet-run-e2e-tests.yml@main
    with:
      dotnet-version: ${{ needs.env-setup.outputs.dotnet-version }}
      working-directory: ./src/Biotrackr.Weight.Svc
      test-filter: 'FullyQualifiedName~E2E'
```

**New Template Required**:
- `template-dotnet-run-contract-tests.yml` - Simplified version without Cosmos DB Emulator service

**Alternatives Considered**:
- **Single job for all integration tests**: Rejected - No fast feedback, can't run independently
- **Parallel contract and E2E**: Rejected - Contract tests should gate E2E tests
- **Manual workflow only**: Rejected - Tests must run on every PR
- **E2E tests in parallel with unit tests**: Rejected - Slower feedback, wastes runner time on unit test failures

**References**:
- GitHub Actions reusable workflows: https://docs.github.com/en/actions/using-workflows/reusing-workflows
- Existing templates: .github/workflows/template-dotnet-run-*.yml

---

## 5. Test Data Management and Fixtures

### Decision: AutoFixture for Test Data, Builder Pattern for Complex Scenarios

**Rationale**:
- AutoFixture 4.18.1 already used in Weight Service unit tests
- Reduces boilerplate for simple test data
- Customizable for specific scenarios
- Builder pattern for complex multi-step data setup

**Implementation Pattern**:

**Simple Test Data**:
```csharp
var fixture = new Fixture();
var weight = fixture.Create<Weight>();
```

**Complex Scenarios**:
```csharp
public class TestDataBuilder
{
    public static WeightResponse BuildWeightResponse(int count = 7)
    {
        return new WeightResponse
        {
            Weight = Enumerable.Range(0, count)
                .Select(i => new Weight
                {
                    Date = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd"),
                    Weight = 75.5 + i * 0.1,
                    // ... other properties
                })
                .ToList()
        };
    }
}
```

**Alternatives Considered**:
- **Manual object construction**: Rejected - Verbose, maintenance burden
- **Bogus library**: Rejected - AutoFixture already in use, consistent patterns
- **JSON fixtures**: Rejected - Less flexible, harder to maintain

**References**:
- AutoFixture documentation: https://github.com/AutoFixture/AutoFixture
- Test data patterns: https://enterprisecraftsmanship.com/posts/test-data-builders-vs-object-mothers/

---

## 6. Code Coverage and Reporting

### Decision: Coverlet with ReportGenerator, 80% Minimum Coverage

**Rationale**:
- Coverlet.collector 6.0.4 already used in unit tests
- ReportGenerator provides HTML reports and GitHub summaries
- 80% target balances thoroughness with pragmatism
- Excludes Program.cs and configuration classes (already excluded via attributes)

**Configuration**:
```bash
dotnet test \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

reportgenerator \
  "-reports:./TestResults/**/coverage.cobertura.xml" \
  "-targetdir:./CoverageReport" \
  "-reporttypes:Cobertura;Html;MarkdownSummaryGithub" \
  "-filefilters:-*.g.cs"
```

**Coverage Targets**:
- CosmosRepository: 100% (simple create operations)
- WeightService: 100% (simple mapping logic)
- FitbitService: 90% (exclude error handling edge cases in mocked scenarios)
- WeightWorker: 85% (complex orchestration with multiple paths)

**Alternatives Considered**:
- **90% minimum**: Rejected - Too strict for integration tests with external dependencies
- **Separate coverage for integration tests**: Rejected - Combined coverage provides complete picture
- **No coverage tracking**: Rejected - Spec requires 80% coverage verification

**References**:
- Coverlet documentation: https://github.com/coverlet-coverage/coverlet
- ReportGenerator: https://github.com/danielpalme/ReportGenerator

---

## 7. Test Execution Performance Optimization

### Decision: Parallel Test Execution with Shared Fixtures, Database Pooling

**Rationale**:
- xUnit runs test classes in parallel by default
- Shared collection fixtures reduce setup/teardown overhead
- Database/container creation expensive - share across tests in collection
- Individual test isolation via unique document IDs

**Optimization Strategies**:

**Collection-Level Fixtures**:
```csharp
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // Shared fixture across all tests in collection
}

[Collection("Integration Tests")]
public class WeightWorkerTests
{
    // Reuses fixture from collection
}
```

**Test Isolation**:
```csharp
public async Task Test_Worker_Saves_Weight_Documents()
{
    var testId = Guid.NewGuid().ToString(); // Unique per test
    var documentId = $"test-{testId}";
    // ... test logic with unique IDs
}
```

**Performance Targets**:
- Contract tests: <2 seconds (5-10 tests, no external dependencies)
- E2E tests: <28 seconds (10-15 tests, shared Cosmos DB Emulator connection)
- Total: <30 seconds

**Alternatives Considered**:
- **Sequential execution**: Rejected - Too slow, doesn't meet <30s target
- **One database per test**: Rejected - Massive overhead, slow
- **No fixture sharing**: Rejected - Excessive setup/teardown time

**References**:
- xUnit parallel execution: https://xunit.net/docs/running-tests-in-parallel
- Test performance optimization: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices

---

## Summary

All research findings support the clarified decisions:
1. **Test Framework**: xUnit with collection fixtures
2. **Mocking**: Moq for SecretClient, HttpMessageHandler for Fitbit API
3. **Database**: Cosmos DB Emulator via GitHub Actions services
4. **CI/CD**: Two-job workflow (contract → E2E)
5. **Test Data**: AutoFixture + Builder pattern
6. **Coverage**: Coverlet with 80% minimum
7. **Performance**: Parallel execution with shared fixtures

No unresolved clarifications remain. Ready for Phase 1 design.
