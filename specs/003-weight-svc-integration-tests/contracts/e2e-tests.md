# E2E Test Scenarios

**Date**: October 28, 2025  
**Feature**: Weight Service Integration Tests - E2E Tests

## Overview

E2E tests verify complete end-to-end workflows using Cosmos DB Emulator for database operations, mocked HTTP responses for Fitbit API, and mocked SecretClient for Key Vault. These tests validate that all components work together correctly in realistic scenarios.

---

## Test Class: WeightWorkerTests

**Purpose**: Verify complete workflow orchestration through WeightWorker  
**Fixture**: IntegrationTestFixture  
**Collection**: "Integration Tests"

### Test: Worker_Successfully_Syncs_Weight_Data_From_Fitbit_To_Cosmos

**Scenario**: WeightWorker executes complete workflow successfully

**Given**:
- Cosmos DB Emulator is running and accessible
- MockHttpMessageHandler returns successful Fitbit API response with 7 weight entries
- MockSecretClient provides valid access token

**When**:
- WeightWorker.ExecuteAsync() is invoked

**Then**:
- Worker retrieves weight logs for past 7 days from Fitbit
- Each weight entry is mapped to WeightDocument
- All 7 documents are saved to Cosmos DB
- Documents can be queried from Cosmos DB
- Worker completes successfully (return code 0)
- Application stops gracefully

**Implementation**:
```csharp
[Fact]
public async Task Worker_Successfully_Syncs_Weight_Data_From_Fitbit_To_Cosmos()
{
    // Arrange
    var testResponse = TestDataBuilder.BuildWeightResponse(count: 7);
    _fixture.MockHttpMessageHandler.SetResponse(
        TestDataBuilder.BuildSuccessfulFitbitResponse);
    
    var worker = BuildWorker();
    
    // Act
    var result = await worker.ExecuteAsync(CancellationToken.None);
    
    // Assert
    result.Should().Be(0);
    
    // Verify documents in Cosmos DB
    var query = new QueryDefinition(
        "SELECT * FROM c WHERE c.documentType = @docType")
        .WithParameter("@docType", "Weight");
    
    var iterator = _fixture.Container.GetItemQueryIterator<WeightDocument>(query);
    var documents = new List<WeightDocument>();
    
    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();
        documents.AddRange(response);
    }
    
    documents.Should().HaveCount(7);
    documents.All(d => d.DocumentType == "Weight").Should().BeTrue();
}
```

**Expected Result**: 7 weight documents persisted to Cosmos DB

---

### Test: Worker_Handles_Empty_Weight_Response_Gracefully

**Scenario**: Worker completes successfully when Fitbit returns empty weight array

**Given**:
- MockHttpMessageHandler returns successful response with empty weight array
- Cosmos DB is accessible

**When**:
- WeightWorker.ExecuteAsync() is invoked

**Then**:
- Worker completes without error (return code 0)
- No documents are saved to Cosmos DB
- No exceptions are thrown

**Implementation**:
```csharp
[Fact]
public async Task Worker_Handles_Empty_Weight_Response_Gracefully()
{
    // Arrange
    _fixture.MockHttpMessageHandler.SetResponse(req => 
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"weight\": []}")
        });
    
    var worker = BuildWorker();
    
    // Act
    var result = await worker.ExecuteAsync(CancellationToken.None);
    
    // Assert
    result.Should().Be(0);
    
    // Verify no documents in Cosmos DB
    var query = new QueryDefinition(
        "SELECT VALUE COUNT(1) FROM c WHERE c.documentType = @docType")
        .WithParameter("@docType", "Weight");
    
    var iterator = _fixture.Container.GetItemQueryIterator<int>(query);
    var count = (await iterator.ReadNextAsync()).FirstOrDefault();
    
    count.Should().Be(0);
}
```

**Expected Result**: Worker completes successfully with no documents created

---

### Test: Worker_Returns_Error_Code_When_Fitbit_API_Fails

**Scenario**: Worker handles Fitbit API failure gracefully

**Given**:
- MockHttpMessageHandler returns error response (401 Unauthorized)

**When**:
- WeightWorker.ExecuteAsync() is invoked

**Then**:
- Worker catches exception
- Error is logged
- Worker returns error code (1)
- No documents are saved to Cosmos DB

**Implementation**:
```csharp
[Fact]
public async Task Worker_Returns_Error_Code_When_Fitbit_API_Fails()
{
    // Arrange
    _fixture.MockHttpMessageHandler.SetResponse(
        TestDataBuilder.BuildErrorFitbitResponse);
    
    var worker = BuildWorker();
    
    // Act
    var result = await worker.ExecuteAsync(CancellationToken.None);
    
    // Assert
    result.Should().Be(1);
    
    // Verify no documents saved
    var query = new QueryDefinition(
        "SELECT VALUE COUNT(1) FROM c WHERE c.documentType = @docType")
        .WithParameter("@docType", "Weight");
    
    var iterator = _fixture.Container.GetItemQueryIterator<int>(query);
    var count = (await iterator.ReadNextAsync()).FirstOrDefault();
    
    count.Should().Be(0);
}
```

**Expected Result**: Worker returns error code, no documents persisted

---

### Test: Worker_Respects_Cancellation_Token

**Scenario**: Worker stops execution when cancellation is requested

**Given**:
- CancellationToken is cancelled before worker starts

**When**:
- WeightWorker.ExecuteAsync() is invoked with cancelled token

**Then**:
- Worker stops execution early
- OperationCanceledException is thrown or handled
- Partial work may be saved but worker terminates

**Implementation**:
```csharp
[Fact]
public async Task Worker_Respects_Cancellation_Token()
{
    // Arrange
    var cts = new CancellationTokenSource();
    cts.Cancel(); // Cancel immediately
    
    var worker = BuildWorker();
    
    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(async () =>
    {
        await worker.ExecuteAsync(cts.Token);
    });
}
```

**Expected Result**: Worker stops on cancellation

---

## Test Class: WeightServiceTests

**Purpose**: Verify WeightService integration with CosmosRepository  
**Fixture**: IntegrationTestFixture  
**Collection**: "Integration Tests"

### Test: MapAndSaveDocument_Saves_Weight_Document_To_Cosmos

**Scenario**: WeightService correctly maps and persists weight data

**Given**:
- Valid Weight entity
- Date string in yyyy-MM-dd format
- Cosmos DB container is accessible

**When**:
- WeightService.MapAndSaveDocument() is called

**Then**:
- WeightDocument is created with correct structure
- Document has unique GUID id
- Document has correct partition key ("Weight")
- Document is saved to Cosmos DB
- Document can be retrieved by id

**Implementation**:
```csharp
[Fact]
public async Task MapAndSaveDocument_Saves_Weight_Document_To_Cosmos()
{
    // Arrange
    var weight = TestDataBuilder.BuildWeight(DateTime.Now);
    var date = DateTime.Now.ToString("yyyy-MM-dd");
    var service = BuildWeightService();
    
    // Act
    await service.MapAndSaveDocument(date, weight);
    
    // Assert - Query by date to find document
    var query = new QueryDefinition(
        "SELECT * FROM c WHERE c.date = @date AND c.documentType = @docType")
        .WithParameter("@date", date)
        .WithParameter("@docType", "Weight");
    
    var iterator = _fixture.Container.GetItemQueryIterator<WeightDocument>(query);
    var documents = new List<WeightDocument>();
    
    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();
        documents.AddRange(response);
    }
    
    documents.Should().HaveCount(1);
    var document = documents.First();
    
    document.Id.Should().NotBeNullOrEmpty();
    document.Date.Should().Be(date);
    document.Weight.Should().BeEquivalentTo(weight);
    document.DocumentType.Should().Be("Weight");
}
```

**Expected Result**: Document saved and retrievable from Cosmos DB

---

### Test: MapAndSaveDocument_Creates_Unique_Document_Ids

**Scenario**: Multiple calls create documents with unique IDs

**Given**:
- Multiple weight entities
- Same date

**When**:
- MapAndSaveDocument is called multiple times

**Then**:
- Each document has unique GUID
- All documents are saved
- No conflicts occur

**Implementation**:
```csharp
[Fact]
public async Task MapAndSaveDocument_Creates_Unique_Document_Ids()
{
    // Arrange
    var date = DateTime.Now.ToString("yyyy-MM-dd");
    var weights = Enumerable.Range(0, 5)
        .Select(_ => TestDataBuilder.BuildWeight(DateTime.Now))
        .ToList();
    
    var service = BuildWeightService();
    
    // Act
    foreach (var weight in weights)
    {
        await service.MapAndSaveDocument(date, weight);
    }
    
    // Assert
    var query = new QueryDefinition(
        "SELECT c.id FROM c WHERE c.date = @date AND c.documentType = @docType")
        .WithParameter("@date", date)
        .WithParameter("@docType", "Weight");
    
    var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);
    var ids = new List<string>();
    
    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();
        ids.AddRange(response.Select(x => (string)x.id));
    }
    
    ids.Should().HaveCount(5);
    ids.Distinct().Should().HaveCount(5); // All unique
}
```

**Expected Result**: All documents have unique IDs

---

## Test Class: FitbitServiceTests

**Purpose**: Verify FitbitService integration with mocked HTTP client  
**Fixture**: IntegrationTestFixture  
**Collection**: "Integration Tests"

### Test: GetWeightLogs_Returns_Deserialized_Weight_Data

**Scenario**: FitbitService retrieves and deserializes weight data

**Given**:
- MockHttpMessageHandler returns valid Fitbit API response
- MockSecretClient provides access token
- Start and end dates provided

**When**:
- FitbitService.GetWeightLogs() is called

**Then**:
- HTTP request sent to correct Fitbit API endpoint
- Authorization header includes Bearer token
- Response is deserialized to WeightResponse
- Weight array contains expected entries

**Implementation**:
```csharp
[Fact]
public async Task GetWeightLogs_Returns_Deserialized_Weight_Data()
{
    // Arrange
    var startDate = "2025-10-21";
    var endDate = "2025-10-28";
    var expectedResponse = TestDataBuilder.BuildWeightResponse(7);
    
    HttpRequestMessage? capturedRequest = null;
    _fixture.MockHttpMessageHandler.SetResponse(req =>
    {
        capturedRequest = req;
        return TestDataBuilder.BuildSuccessfulFitbitResponse(req);
    });
    
    var service = BuildFitbitService();
    
    // Act
    var result = await service.GetWeightLogs(startDate, endDate);
    
    // Assert
    result.Should().NotBeNull();
    result.Weight.Should().HaveCount(7);
    
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString()
        .Should().Contain($"/body/log/weight/date/{startDate}/{endDate}");
    capturedRequest.Headers.Authorization.Should().NotBeNull();
    capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
}
```

**Expected Result**: Weight data retrieved and deserialized correctly

---

### Test: GetWeightLogs_Includes_Access_Token_From_KeyVault

**Scenario**: FitbitService uses access token from mocked SecretClient

**Given**:
- MockSecretClient configured to return "test-fitbit-access-token"

**When**:
- FitbitService.GetWeightLogs() is called

**Then**:
- SecretClient.GetSecretAsync("AccessToken") is called
- HTTP Authorization header includes the token

**Implementation**:
```csharp
[Fact]
public async Task GetWeightLogs_Includes_Access_Token_From_KeyVault()
{
    // Arrange
    var startDate = "2025-10-21";
    var endDate = "2025-10-28";
    
    HttpRequestMessage? capturedRequest = null;
    _fixture.MockHttpMessageHandler.SetResponse(req =>
    {
        capturedRequest = req;
        return TestDataBuilder.BuildSuccessfulFitbitResponse(req);
    });
    
    var service = BuildFitbitService();
    
    // Act
    await service.GetWeightLogs(startDate, endDate);
    
    // Assert
    _fixture.MockSecretClient.Verify(
        x => x.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()),
        Times.Once);
    
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Headers.Authorization!.Parameter
        .Should().Be("test-fitbit-access-token");
}
```

**Expected Result**: Token retrieved from SecretClient and used in request

---

### Test: GetWeightLogs_Throws_Exception_On_API_Error

**Scenario**: FitbitService handles API errors appropriately

**Given**:
- MockHttpMessageHandler returns error response (401)

**When**:
- FitbitService.GetWeightLogs() is called

**Then**:
- HttpRequestException or similar is thrown
- Exception is logged
- Exception propagates to caller

**Implementation**:
```csharp
[Fact]
public async Task GetWeightLogs_Throws_Exception_On_API_Error()
{
    // Arrange
    _fixture.MockHttpMessageHandler.SetResponse(
        TestDataBuilder.BuildErrorFitbitResponse);
    
    var service = BuildFitbitService();
    
    // Act & Assert
    await Assert.ThrowsAsync<HttpRequestException>(async () =>
    {
        await service.GetWeightLogs("2025-10-21", "2025-10-28");
    });
}
```

**Expected Result**: Exception thrown on API error

---

## Test Class: CosmosRepositoryTests

**Purpose**: Verify CosmosRepository database operations  
**Fixture**: IntegrationTestFixture  
**Collection**: "Integration Tests"

### Test: CreateWeightDocument_Persists_Document_To_Container

**Scenario**: Repository saves document to Cosmos DB

**Given**:
- Valid WeightDocument
- Container is accessible

**When**:
- CosmosRepository.CreateWeightDocument() is called

**Then**:
- Document is saved with correct partition key
- Document can be retrieved by ID
- All properties are persisted correctly

**Implementation**:
```csharp
[Fact]
public async Task CreateWeightDocument_Persists_Document_To_Container()
{
    // Arrange
    var document = TestDataBuilder.BuildWeightDocument();
    var repository = BuildCosmosRepository();
    
    // Act
    await repository.CreateWeightDocument(document);
    
    // Assert - Retrieve by ID
    var response = await _fixture.Container.ReadItemAsync<WeightDocument>(
        document.Id,
        new PartitionKey(document.DocumentType));
    
    var savedDocument = response.Resource;
    savedDocument.Should().BeEquivalentTo(document);
}
```

**Expected Result**: Document persisted and retrievable

---

### Test: CreateWeightDocument_Uses_Correct_Partition_Key

**Scenario**: Repository uses DocumentType as partition key

**Given**:
- WeightDocument with DocumentType = "Weight"

**When**:
- Document is saved

**Then**:
- Document is stored in "Weight" partition
- Partition key matches DocumentType property

**Implementation**:
```csharp
[Fact]
public async Task CreateWeightDocument_Uses_Correct_Partition_Key()
{
    // Arrange
    var document = TestDataBuilder.BuildWeightDocument();
    var repository = BuildCosmosRepository();
    
    // Act
    await repository.CreateWeightDocument(document);
    
    // Assert - Query by partition key
    var query = new QueryDefinition(
        "SELECT * FROM c WHERE c.id = @id")
        .WithParameter("@id", document.Id);
    
    var iterator = _fixture.Container.GetItemQueryIterator<WeightDocument>(
        query,
        requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey("Weight")
        });
    
    var results = (await iterator.ReadNextAsync()).ToList();
    results.Should().HaveCount(1);
    results.First().Id.Should().Be(document.Id);
}
```

**Expected Result**: Document stored with correct partition key

---

## Test Execution

### Run All E2E Tests
```bash
dotnet test --filter "FullyQualifiedName~E2E"
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~WeightWorkerTests"
```

### Expected Performance
- Total execution time: <28 seconds
- All tests should pass
- Cosmos DB Emulator must be running

---

## Success Criteria

E2E tests are successful when:
1. All tests pass consistently
2. Total execution time <30 seconds
3. Tests can run 100 times consecutively without failures
4. All external dependencies properly mocked
5. Cosmos DB Emulator connects successfully
6. Database cleanup completes after each test class
7. No test data conflicts between parallel test executions

---

## Coverage

E2E tests provide coverage for:
- WeightWorker complete workflow orchestration
- WeightService data mapping and persistence
- FitbitService HTTP communication and deserialization
- CosmosRepository database operations
- Error handling across all components
- Integration of all services together

These tests complement:
- Unit tests (individual method logic)
- Contract tests (DI configuration)

Together, all test types achieve ≥80% code coverage of service layer components.
