using Biotrackr.Activity.Svc.Configuration;
using Biotrackr.Activity.Svc.IntegrationTests.Collections;
using Biotrackr.Activity.Svc.IntegrationTests.Fixtures;
using Biotrackr.Activity.Svc.IntegrationTests.Helpers;
using Biotrackr.Activity.Svc.Models;
using Biotrackr.Activity.Svc.Repositories;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Activity.Svc.IntegrationTests.E2E;

/// <summary>
/// E2E tests verifying Cosmos DB repository operations with real database.
/// These tests require Cosmos DB Emulator running on localhost:8081.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class CosmosRepositoryTests(IntegrationTestFixture fixture)
{
    private readonly IntegrationTestFixture _fixture = fixture;

    /// <summary>
    /// Clears all documents from the test container to ensure test isolation.
    /// </summary>
    private async Task ClearContainerAsync()
    {
        var query = new QueryDefinition("SELECT c.id, c.documentType FROM c");
        var iterator = _fixture.Container!.GetItemQueryIterator<dynamic>(query);

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

    [Fact]
    public async Task CreateItemAsync_Should_PersistActivityDocument()
    {
        // Arrange - Clear container for test isolation
        await ClearContainerAsync();

        var settings = Options.Create(new Settings { DatabaseName = _fixture.Database!.Id, ContainerName = _fixture.Container!.Id });
        var mockLogger = new Mock<ILogger<CosmosRepository>>();
        var repository = new CosmosRepository(_fixture.CosmosClient!, settings, mockLogger.Object);
        var activityDocument = TestDataGenerator.CreateActivityDocument();

        // Act
        await repository.CreateActivityDocument(activityDocument);

        // Assert - Verify document was persisted
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
            .WithParameter("@id", activityDocument.Id);

        var iterator = _fixture.Container.GetItemQueryIterator<ActivityDocument>(query);
        var documents = new List<ActivityDocument>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            documents.AddRange(response);
        }

        documents.Should().ContainSingle("exactly one document should be saved");
        var savedDocument = documents.First();
        savedDocument.Id.Should().Be(activityDocument.Id);
        savedDocument.DocumentType.Should().Be(activityDocument.DocumentType);
        savedDocument.Date.Should().Be(activityDocument.Date);
    }

    [Fact]
    public async Task CreateItemAsync_Should_UseCorrectPartitionKey()
    {
        // Arrange - Clear container for test isolation
        await ClearContainerAsync();

        var settings = Options.Create(new Settings { DatabaseName = _fixture.Database!.Id, ContainerName = _fixture.Container!.Id });
        var mockLogger = new Mock<ILogger<CosmosRepository>>();
        var repository = new CosmosRepository(_fixture.CosmosClient!, settings, mockLogger.Object);
        var activityDocument = TestDataGenerator.CreateActivityDocument();

        // Act
        await repository.CreateActivityDocument(activityDocument);

        // Assert - Verify document uses correct partition key
        var response = await _fixture.Container!.ReadItemAsync<ActivityDocument>(
            activityDocument.Id,
            new PartitionKey(activityDocument.DocumentType));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Resource.DocumentType.Should().Be("Activity", "documentType is the partition key");
    }

    [Fact]
    public async Task GetItemAsync_Should_RetrieveActivityDocumentById()
    {
        // Arrange - Clear container and create test document
        await ClearContainerAsync();

        var settings = Options.Create(new Settings { DatabaseName = _fixture.Database!.Id, ContainerName = _fixture.Container!.Id });
        var mockLogger = new Mock<ILogger<CosmosRepository>>();
        var repository = new CosmosRepository(_fixture.CosmosClient!, settings, mockLogger.Object);
        var activityDocument = TestDataGenerator.CreateActivityDocument();
        await repository.CreateActivityDocument(activityDocument);

        // Act - Retrieve document by ID
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
            .WithParameter("@id", activityDocument.Id);

        var iterator = _fixture.Container!.GetItemQueryIterator<ActivityDocument>(query);
        var documents = new List<ActivityDocument>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            documents.AddRange(response);
        }

        // Assert
        documents.Should().ContainSingle();
        var retrievedDocument = documents.First();
        retrievedDocument.Id.Should().Be(activityDocument.Id);
        retrievedDocument.Date.Should().Be(activityDocument.Date);
        retrievedDocument.Activity.Should().NotBeNull();
    }

    [Fact]
    public async Task GetItemsAsync_Should_QueryActivityDocumentsByDate()
    {
        // Arrange - Clear container and create multiple documents
        await ClearContainerAsync();

        var settings = Options.Create(new Settings { DatabaseName = _fixture.Database!.Id, ContainerName = _fixture.Container!.Id });
        var mockLogger = new Mock<ILogger<CosmosRepository>>();
        var repository = new CosmosRepository(_fixture.CosmosClient!, settings, mockLogger.Object);
        var targetDate = "2025-10-31";
        
        var document1 = TestDataGenerator.CreateActivityDocument(targetDate);
        var document2 = TestDataGenerator.CreateActivityDocument(targetDate);
        var document3 = TestDataGenerator.CreateActivityDocument("2025-10-30"); // Different date

        await repository.CreateActivityDocument(document1);
        await repository.CreateActivityDocument(document2);
        await repository.CreateActivityDocument(document3);

        // Act - Query by date
        var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
            .WithParameter("@date", targetDate);

        var iterator = _fixture.Container!.GetItemQueryIterator<ActivityDocument>(query);
        var documents = new List<ActivityDocument>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            documents.AddRange(response);
        }

        // Assert - Should only find documents with target date
        documents.Should().HaveCount(2, "only documents with target date should be returned");
        documents.Should().OnlyContain(d => d.Date == targetDate);
        documents.Should().NotContain(d => d.Id == document3.Id);
    }
}
