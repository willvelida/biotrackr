# Data Model: Sleep Service Test Coverage and Integration Tests

**Feature**: 007-sleep-svc-tests  
**Created**: October 31, 2025  
**Purpose**: Define data structures and relationships for test infrastructure components

---

## Test Infrastructure Entities

### Test Fixture Configuration

**Purpose**: Configuration data for initializing test fixtures with appropriate settings

**Attributes**:
- `CosmosDbEndpoint` (string): Connection endpoint for test Cosmos DB instance (e.g., "https://localhost:8081")
- `CosmosDbAccountKey` (string): Account key for Cosmos DB Emulator authentication
- `DatabaseName` (string): Name of test database (e.g., "BiotrackrTestDb")
- `ContainerName` (string): Name of test container (e.g., "SleepTestContainer")
- `InitializeDatabase` (bool): Flag indicating whether fixture should initialize database connection

**Validation Rules**:
- CosmosDbEndpoint MUST be valid URI format
- DatabaseName MUST NOT exceed 255 characters
- ContainerName MUST NOT exceed 255 characters
- For Contract tests: InitializeDatabase = false
- For E2E tests: InitializeDatabase = true

---

### Test Collection Configuration

**Purpose**: xUnit collection definitions for grouping tests that share fixtures

**Attributes**:
- `CollectionName` (string): Name of the test collection (e.g., "SleepServiceContractTests", "SleepServiceIntegrationTests")
- `FixtureType` (Type): Type of fixture to use (ContractTestFixture or IntegrationTestFixture)
- `DisableParallelization` (bool): Whether tests in collection run sequentially

**Validation Rules**:
- CollectionName MUST be unique within test project
- Contract test collections SHOULD enable parallelization (faster execution)
- E2E test collections SHOULD disable parallelization (share Cosmos DB connection)

---

### Test Data Generator Output

**Purpose**: Generated test data for Sleep Service integration tests

**Attributes**:
- `SleepDocument` (SleepDocument): Complete sleep document with all required fields
- `SleepResponse` (SleepResponse): Fitbit API response mock data
- `Date` (string): ISO 8601 date string (e.g., "2025-10-31")
- `TestId` (Guid): Unique identifier for test execution

**Validation Rules**:
- Date MUST be valid ISO 8601 format (yyyy-MM-dd)
- SleepDocument.DocumentType MUST equal "Sleep"
- SleepDocument.Id MUST be unique per test run
- TestId MUST be unique per test execution

---

### Coverage Report Data

**Purpose**: Code coverage metrics collected during test execution

**Attributes**:
- `ProjectName` (string): Name of project under test (e.g., "Biotrackr.Sleep.Svc")
- `LineCoverage` (decimal): Percentage of lines covered (0.0 - 100.0)
- `BranchCoverage` (decimal): Percentage of branches covered (0.0 - 100.0)
- `CoverageFormat` (string): Report format (e.g., "Cobertura", "OpenCover")
- `ExcludedFiles` (List<string>): Files excluded from coverage (e.g., ["Program.cs"])

**Validation Rules**:
- LineCoverage MUST be >= 70.0 to meet acceptance criteria
- BranchCoverage SHOULD be >= 60.0 for quality
- ExcludedFiles MUST contain "Program.cs" for all projects

---

## Domain Entities (From Sleep Service)

These entities are tested but not modified by this feature.

### SleepDocument

**Purpose**: Main document stored in Cosmos DB

**Attributes**:
- `Id` (string): Unique identifier (GUID)
- `Sleep` (SleepResponse): Sleep data from Fitbit API
- `Date` (string): ISO date string (yyyy-MM-dd)
- `DocumentType` (string): Partition key value ("Sleep")

**Validation Rules**:
- Id MUST be unique within container
- Date MUST be valid ISO format
- DocumentType MUST equal "Sleep"

---

### SleepResponse

**Purpose**: Response from Fitbit Sleep API

**Attributes**:
- `Sleep` (List<Sleep>): Array of sleep sessions
- `Summary` (Summary): Aggregated sleep summary data

**Validation Rules**:
- Sleep list MAY be empty if no sleep data recorded
- Summary SHOULD contain valid aggregated metrics

---

## Test Execution Flow

### Unit Test Execution

```
1. Test Discovery → xUnit discovers all [Fact] and [Theory] tests
2. Test Setup → Initialize mocks (IFitbitService, ISleepService, ICosmosRepository)
3. Test Execution → Run test method with assertions
4. Coverage Collection → coverlet tracks executed lines
5. Test Teardown → Dispose mocks and cleanup
6. Report Generation → Generate Cobertura XML report
```

**Data Flow**:
- Input: Test parameters (AutoFixture-generated data)
- Processing: Execute method under test with mocks
- Output: Test result (Pass/Fail) + Coverage data

---

### Contract Test Execution

```
1. Collection Setup → ContractTestFixture initializes (no DB)
2. Test Discovery → xUnit discovers tests in Contract/ namespace
3. Test Execution → Verify service registration and lifetimes
4. Assertions → Validate DI container configuration
5. Collection Teardown → Dispose fixtures
```

**Data Flow**:
- Input: In-memory configuration (appsettings.Test.json)
- Processing: Build service provider and resolve services
- Output: Test result (Pass/Fail)

---

### E2E Test Execution

```
1. Collection Setup → IntegrationTestFixture initializes Cosmos DB
2. Database Setup → Create test database and container
3. Test Discovery → xUnit discovers tests in E2E/ namespace
4. Pre-Test Cleanup → ClearContainerAsync() removes existing data
5. Test Execution → Run test with real Cosmos DB
6. Assertions → Verify data persistence and retrieval
7. Post-Test Cleanup → Delete test documents
8. Collection Teardown → Delete test database and container
```

**Data Flow**:
- Input: Test data (SleepDocument, SleepResponse)
- Processing: Persist to Cosmos DB, query, verify
- Output: Test result (Pass/Fail) + Cleanup confirmation

---

## Test Isolation Strategy

### Data Cleanup Pattern

```csharp
private async Task ClearContainerAsync()
{
    var query = new QueryDefinition("SELECT c.id, c.documentType FROM c");
    var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);
    
    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();
        foreach (var item in response)
        {
            await _fixture.Container.DeleteItemAsync<dynamic>(
                item.id.ToString(),
                new PartitionKey(item.documentType.ToString()));
        }
    }
}
```

**Purpose**: Ensure each E2E test starts with clean container state

**Execution Timing**: Called before each test method in E2E test classes

---

## GitHub Actions Workflow Data

### Test Job Configuration

**Purpose**: Configuration for test execution in CI/CD pipeline

**Attributes**:
- `JobName` (string): Name of test job (e.g., "run-unit-tests", "run-contract-tests", "run-e2e-tests")
- `WorkingDirectory` (string): Path to test project (e.g., "./src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc.UnitTests")
- `TestFilter` (string): xUnit filter expression (e.g., "FullyQualifiedName~Contract")
- `RequiresCosmosDb` (bool): Whether job needs Cosmos DB Emulator service
- `RunsInParallel` (bool): Whether job can run in parallel with others

**Validation Rules**:
- Unit tests: RequiresCosmosDb = false, RunsInParallel = true
- Contract tests: RequiresCosmosDb = false, RunsInParallel = true
- E2E tests: RequiresCosmosDb = true, RunsInParallel = false (runs after contract tests)

---

### Coverage Artifact Data

**Purpose**: Coverage report artifacts uploaded to GitHub Actions

**Attributes**:
- `ArtifactName` (string): Name of artifact (e.g., "sleep-svc-coverage")
- `FilePath` (string): Path to coverage XML file
- `RetentionDays` (int): Days to retain artifact (default: 30)
- `Format` (string): Report format ("Cobertura")

**Validation Rules**:
- FilePath MUST point to valid Cobertura XML file
- RetentionDays SHOULD be >= 30 for compliance tracking

---

## References

- [Sleep Service Models](../../src/Biotrackr.Sleep.Svc/Biotrackr.Sleep.Svc/Models/)
- [xUnit Collection Fixtures Documentation](https://xunit.net/docs/shared-context)
- [Coverlet Coverage Configuration](https://github.com/coverlet-coverage/coverlet)
- [Cosmos DB Test Patterns](../../docs/decision-records/2025-10-28-integration-test-project-structure.md)
