namespace Biotrackr.Food.Svc.IntegrationTests.E2E;

/// <summary>
/// E2E tests for CosmosRepository that verify CRUD operations against Cosmos DB Emulator.
/// </summary>
[Collection("IntegrationTests")]
public class CosmosRepositoryTests
{
    private readonly IntegrationTestFixture _fixture;

    public CosmosRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateItemAsync_WithValidDocument_ShouldPersistToCosmosDb()
    {
        // Arrange - Clear container for test isolation
        await _fixture.ClearContainerAsync();

        var testDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var document = TestDataGenerator.GenerateFoodDocument(date: testDate);

        // Act - Save document using Container directly
        var response = await _fixture.Container!.CreateItemAsync(
            document,
            new PartitionKey(document.DocumentType));

        // Assert - Verify document was saved
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        response.Resource.Should().NotBeNull();
        response.Resource.Id.Should().NotBeEmpty();
        response.Resource.Date.Should().Be(testDate);
        response.Resource.DocumentType.Should().Be("food");
    }

    [Fact]
    public async Task CreateItemAsync_WithComplexFoodStructure_ShouldPersistNestedEntities()
    {
        // Arrange - Clear container for test isolation
        await _fixture.ClearContainerAsync();

        var testDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var document = TestDataGenerator.GenerateFoodDocument(date: testDate);

        // Verify complex structure exists
        document.Food.Should().NotBeNull();
        document.Food.foods.Should().NotBeEmpty();
        document.Food.goals.Should().NotBeNull();
        document.Food.summary.Should().NotBeNull();

        // Act - Save document
        var response = await _fixture.Container!.CreateItemAsync(
            document,
            new PartitionKey(document.DocumentType));

        // Assert - Verify nested entities persisted correctly using strongly-typed model
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var savedDoc = response.Resource;

        savedDoc.Food.foods.Should().HaveCount(document.Food.foods.Count);
        savedDoc.Food.goals.calories.Should().Be(document.Food.goals.calories);
        savedDoc.Food.summary.calories.Should().Be(document.Food.summary.calories);
        savedDoc.Food.foods[0].loggedFood.name.Should().Be(document.Food.foods[0].loggedFood.name);
        savedDoc.Food.foods[0].loggedFood.calories.Should().Be(document.Food.foods[0].loggedFood.calories);
    }

    [Fact]
    public async Task ReadItemAsync_WithExistingDocument_ShouldReturnStronglyTypedDocument()
    {
        // Arrange - Clear container and create test document
        await _fixture.ClearContainerAsync();

        var testDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var document = TestDataGenerator.GenerateFoodDocument(date: testDate);

        await _fixture.Container!.CreateItemAsync(
            document,
            new PartitionKey(document.DocumentType));

        // Act - Read document using strongly-typed model (not dynamic)
        var readResponse = await _fixture.Container.ReadItemAsync<FoodDocument>(
            document.Id,
            new PartitionKey(document.DocumentType));

        // Assert - Verify strongly-typed model returned correctly
        readResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        readResponse.Resource.Should().NotBeNull();
        readResponse.Resource.Id.Should().Be(document.Id);
        readResponse.Resource.Date.Should().Be(testDate);
        readResponse.Resource.Food.foods.Should().NotBeEmpty();
        readResponse.Resource.Food.goals.calories.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task QueryItems_WithDateFilter_ShouldReturnOnlyMatchingDocuments()
    {
        // Arrange - Clear container for test isolation
        await _fixture.ClearContainerAsync();

        var testDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var doc1 = TestDataGenerator.GenerateFoodDocument(testDate);
        var doc2 = TestDataGenerator.GenerateFoodDocument(testDate);

        await _fixture.Container!.CreateItemAsync(doc1, new PartitionKey(doc1.DocumentType));
        await _fixture.Container.CreateItemAsync(doc2, new PartitionKey(doc2.DocumentType));

        // Act - Query using strongly-typed model (not dynamic)
        var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
            .WithParameter("@date", testDate);

        var iterator = _fixture.Container.GetItemQueryIterator<FoodDocument>(query);
        var documents = new List<FoodDocument>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            documents.AddRange(response);
        }

        // Assert - Verify only matching documents returned
        documents.Should().HaveCount(2, "exactly two documents should match the date filter");
        documents.Should().OnlyContain(d => d.Date == testDate);
        documents.Should().OnlyContain(d => d.DocumentType == "food");
    }
}
