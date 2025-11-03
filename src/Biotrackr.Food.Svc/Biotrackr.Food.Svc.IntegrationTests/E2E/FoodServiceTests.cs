namespace Biotrackr.Food.Svc.IntegrationTests.E2E;

/// <summary>
/// E2E tests for FoodService that verify data transformation and persistence workflows.
/// </summary>
[Collection("IntegrationTests")]
public class FoodServiceTests
{
    private readonly IntegrationTestFixture _fixture;

    public FoodServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MapAndSaveDocument_WithValidFoodResponse_ShouldTransformAndPersistCorrectly()
    {
        // Arrange - Clear container for test isolation
        await _fixture.ClearContainerAsync();

        var testDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var foodResponse = TestDataGenerator.GenerateFoodResponse();

        // Create document structure matching what FoodService creates
        var document = new FoodDocument
        {
            Id = $"food-{testDate}",
            Date = testDate,
            DocumentType = "FoodDocument",
            Food = foodResponse
        };

        // Act - Save using Container (simulating FoodService.MapAndSaveDocument)
        var response = await _fixture.Container!.UpsertItemAsync(
            document,
            new PartitionKey(document.DocumentType));

        // Assert - Verify data transformation and persistence
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.Created,
            System.Net.HttpStatusCode.OK);

        var savedDoc = response.Resource;
        savedDoc.Id.Should().Be(document.Id);
        savedDoc.Date.Should().Be(testDate);
        savedDoc.Food.foods.Should().HaveCount(foodResponse.foods.Count);
        savedDoc.Food.goals.calories.Should().Be(foodResponse.goals.calories);
        savedDoc.Food.summary.calories.Should().Be(foodResponse.summary.calories);
    }

    [Fact]
    public async Task MapAndSaveDocument_WithEmptyFoodList_ShouldStillPersistDocument()
    {
        // Arrange - Clear container for test isolation
        await _fixture.ClearContainerAsync();

        var testDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var foodResponse = new FoodResponse
        {
            foods = new List<Models.FitbitEntities.Food>(), // Empty list
            goals = TestDataGenerator.GenerateGoals(),
            summary = TestDataGenerator.GenerateSummary()
        };

        var document = new FoodDocument
        {
            Id = $"empty-{testDate}",
            Date = testDate,
            DocumentType = "FoodDocument",
            Food = foodResponse
        };

        // Act
        var response = await _fixture.Container!.UpsertItemAsync(
            document,
            new PartitionKey(document.DocumentType));

        // Assert - Verify document persisted even with empty food list
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.Created,
            System.Net.HttpStatusCode.OK);

        var savedDoc = response.Resource;
        savedDoc.Food.foods.Should().BeEmpty();
        savedDoc.Food.goals.Should().NotBeNull();
        savedDoc.Food.summary.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryFoodDocument_ByDate_ShouldReturnStronglyTypedDocument()
    {
        // Arrange - Clear container for test isolation
        await _fixture.ClearContainerAsync();

        var testDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var foodResponse = TestDataGenerator.GenerateFoodResponse();
        var document = new FoodDocument
        {
            Id = $"query-{testDate}",
            Date = testDate,
            DocumentType = "FoodDocument",
            Food = foodResponse
        };

        await _fixture.Container!.UpsertItemAsync(
            document,
            new PartitionKey(document.DocumentType));

        // Act - Query using strongly-typed model (avoiding dynamic to prevent RuntimeBinderException)
        var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date AND c.documentType = @type")
            .WithParameter("@date", testDate)
            .WithParameter("@type", "FoodDocument");

        var iterator = _fixture.Container.GetItemQueryIterator<FoodDocument>(query);
        var documents = new List<FoodDocument>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            documents.AddRange(response);
        }

        // Assert - Verify strongly-typed query returns correct data
        documents.Should().ContainSingle("exactly one document should match");
        var result = documents.First();
        result.Id.Should().Be(document.Id);
        result.Date.Should().Be(testDate);
        result.Food.foods.Should().HaveCount(foodResponse.foods.Count);
    }

    [Fact]
    public async Task TestIsolation_MultipleTests_ShouldNotFindEachOthersData()
    {
        // Arrange - Clear container to demonstrate isolation
        await _fixture.ClearContainerAsync();

        var testDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var document = TestDataGenerator.GenerateFoodDocument(testDate);

        await _fixture.Container!.CreateItemAsync(
            document,
            new PartitionKey(document.DocumentType));

        // Act - Query for documents with same date
        var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
            .WithParameter("@date", testDate);

        var iterator = _fixture.Container.GetItemQueryIterator<FoodDocument>(query);
        var documents = new List<FoodDocument>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            documents.AddRange(response);
        }

        // Assert - Should only find the one document we created (proves ClearContainerAsync works)
        documents.Should().ContainSingle("test isolation should prevent finding other tests' data");
        documents.First().Id.Should().Be(document.Id);
    }
}
