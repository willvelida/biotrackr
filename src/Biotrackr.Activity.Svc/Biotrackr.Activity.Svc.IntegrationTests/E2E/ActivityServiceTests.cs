using Biotrackr.Activity.Svc.Configuration;
using Biotrackr.Activity.Svc.IntegrationTests.Collections;
using Biotrackr.Activity.Svc.IntegrationTests.Fixtures;
using Biotrackr.Activity.Svc.IntegrationTests.Helpers;
using Biotrackr.Activity.Svc.Models;
using Biotrackr.Activity.Svc.Repositories;
using Biotrackr.Activity.Svc.Services;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Activity.Svc.IntegrationTests.E2E;

/// <summary>
/// E2E tests verifying ActivityService operations with real Cosmos DB.
/// These tests verify data transformation and persistence logic.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class ActivityServiceTests(IntegrationTestFixture fixture)
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
    public async Task MapAndSaveDocument_Should_TransformAndPersistData()
    {
        // Arrange - Clear container for test isolation
        await ClearContainerAsync();

        var settings = Options.Create(new Settings { DatabaseName = _fixture.Database!.Id, ContainerName = _fixture.Container!.Id });
        var mockRepoLogger = new Mock<ILogger<CosmosRepository>>();
        var repository = new CosmosRepository(_fixture.CosmosClient!, settings, mockRepoLogger.Object);
        
        var mockServiceLogger = new Mock<ILogger<ActivityService>>();
        var activityService = new ActivityService(repository, mockServiceLogger.Object);
        
        var date = "2025-10-31";
        var activityResponse = TestDataGenerator.CreateActivityResponse();

        // Act
        await activityService.MapAndSaveDocument(date, activityResponse);

        // Assert - Verify document was transformed and saved correctly
        var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
            .WithParameter("@date", date);

        var iterator = _fixture.Container!.GetItemQueryIterator<ActivityDocument>(query);
        var documents = new List<ActivityDocument>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            documents.AddRange(response);
        }

        documents.Should().ContainSingle("exactly one document should be saved");
        var savedDocument = documents.First();
        
        savedDocument.Date.Should().Be(date);
        savedDocument.DocumentType.Should().Be("Activity");
        savedDocument.Activity.Should().NotBeNull();
        savedDocument.Activity.summary.Should().NotBeNull();
        savedDocument.Activity.summary.steps.Should().Be(activityResponse.summary.steps);
        savedDocument.Activity.summary.caloriesOut.Should().Be(activityResponse.summary.caloriesOut);
    }

    [Fact]
    public async Task MapAndSaveDocument_Should_HandleMultipleDocuments()
    {
        // Arrange - Clear container for test isolation
        await ClearContainerAsync();

        var settings = Options.Create(new Settings { DatabaseName = _fixture.Database!.Id, ContainerName = _fixture.Container!.Id });
        var mockRepoLogger = new Mock<ILogger<CosmosRepository>>();
        var repository = new CosmosRepository(_fixture.CosmosClient!, settings, mockRepoLogger.Object);
        
        var mockServiceLogger = new Mock<ILogger<ActivityService>>();
        var activityService = new ActivityService(repository, mockServiceLogger.Object);
        
        var date1 = "2025-10-30";
        var date2 = "2025-10-31";
        var activityResponse1 = TestDataGenerator.CreateActivityResponse();
        var activityResponse2 = TestDataGenerator.CreateActivityResponse();

        // Act - Save multiple documents
        await activityService.MapAndSaveDocument(date1, activityResponse1);
        await activityService.MapAndSaveDocument(date2, activityResponse2);

        // Assert - Verify both documents exist independently
        var query = new QueryDefinition("SELECT * FROM c WHERE c.documentType = @docType")
            .WithParameter("@docType", "Activity");

        var iterator = _fixture.Container!.GetItemQueryIterator<ActivityDocument>(query);
        var documents = new List<ActivityDocument>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            documents.AddRange(response);
        }

        documents.Should().HaveCount(2, "both documents should be saved");
        documents.Should().Contain(d => d.Date == date1);
        documents.Should().Contain(d => d.Date == date2);
        
        var doc1 = documents.First(d => d.Date == date1);
        var doc2 = documents.First(d => d.Date == date2);
        
        doc1.Activity.summary.steps.Should().Be(activityResponse1.summary.steps);
        doc2.Activity.summary.steps.Should().Be(activityResponse2.summary.steps);
    }
}
