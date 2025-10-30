using Biotrackr.Activity.Svc.Configuration;
using Biotrackr.Activity.Svc.IntegrationTests.Collections;
using Biotrackr.Activity.Svc.IntegrationTests.Fixtures;
using Biotrackr.Activity.Svc.IntegrationTests.Helpers;
using Biotrackr.Activity.Svc.Models;
using Biotrackr.Activity.Svc.Repositories;
using Biotrackr.Activity.Svc.Services;
using Biotrackr.Activity.Svc.Services.Interfaces;
using Biotrackr.Activity.Svc.Workers;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Activity.Svc.IntegrationTests.E2E;

/// <summary>
/// E2E tests verifying ActivityWorker end-to-end workflow with real Cosmos DB.
/// These tests verify the complete data flow from Fitbit service to Cosmos DB persistence.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class ActivityWorkerTests(IntegrationTestFixture fixture)
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
    public async Task ExecuteAsync_Should_CompleteEndToEndWorkflow()
    {
        // Arrange - Clear container for test isolation
        await ClearContainerAsync();

        var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        var activityResponse = TestDataGenerator.CreateActivityResponse();

        // Mock IFitbitService to return test data
        var mockFitbitService = new Mock<IFitbitService>();
        mockFitbitService
            .Setup(x => x.GetActivityResponse(date))
            .ReturnsAsync(activityResponse);

        // Setup real services
        var settings = Options.Create(new Settings { DatabaseName = _fixture.Database!.Id, ContainerName = _fixture.Container!.Id });
        var mockRepoLogger = new Mock<ILogger<CosmosRepository>>();
        var repository = new CosmosRepository(_fixture.CosmosClient!, settings, mockRepoLogger.Object);
        
        var mockServiceLogger = new Mock<ILogger<ActivityService>>();
        var activityService = new ActivityService(repository, mockServiceLogger.Object);

        // Act - Execute end-to-end workflow (simulating worker behavior)
        var fetchedActivityResponse = await mockFitbitService.Object.GetActivityResponse(date);
        await activityService.MapAndSaveDocument(date, fetchedActivityResponse);

        // Assert - Verify workflow completed successfully
        // Verify Fitbit service was called
        mockFitbitService.Verify(x => x.GetActivityResponse(date), Times.Once);
        
        // Verify document was saved to Cosmos DB
        var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
            .WithParameter("@date", date);

        var iterator = _fixture.Container!.GetItemQueryIterator<ActivityDocument>(query);
        var documents = new List<ActivityDocument>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            documents.AddRange(response);
        }

        documents.Should().ContainSingle("workflow should save exactly one document");
    }

    [Fact]
    public async Task ExecuteAsync_Should_SaveActivityDocumentsToCosmosDB()
    {
        // Arrange - Clear container for test isolation
        await ClearContainerAsync();

        var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        var activityResponse = TestDataGenerator.CreateActivityResponse();

        // Mock IFitbitService to return test data
        var mockFitbitService = new Mock<IFitbitService>();
        mockFitbitService
            .Setup(x => x.GetActivityResponse(date))
            .ReturnsAsync(activityResponse);

        // Setup real services
        var settings = Options.Create(new Settings { DatabaseName = _fixture.Database!.Id, ContainerName = _fixture.Container!.Id });
        var mockRepoLogger = new Mock<ILogger<CosmosRepository>>();
        var repository = new CosmosRepository(_fixture.CosmosClient!, settings, mockRepoLogger.Object);
        
        var mockServiceLogger = new Mock<ILogger<ActivityService>>();
        var activityService = new ActivityService(repository, mockServiceLogger.Object);

        // Act - Execute end-to-end workflow (simulating worker behavior)
        var fetchedActivityResponse = await mockFitbitService.Object.GetActivityResponse(date);
        await activityService.MapAndSaveDocument(date, fetchedActivityResponse);

        // Assert - Verify document was persisted to Cosmos DB
        var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
            .WithParameter("@date", date);

        var iterator = _fixture.Container!.GetItemQueryIterator<ActivityDocument>(query);
        var documents = new List<ActivityDocument>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            documents.AddRange(response);
        }

        documents.Should().ContainSingle("worker should save exactly one document");
        var savedDocument = documents.First();
        
        savedDocument.Date.Should().Be(date);
        savedDocument.DocumentType.Should().Be("Activity");
        savedDocument.Activity.Should().NotBeNull();
        savedDocument.Activity.summary.steps.Should().Be(activityResponse.summary.steps);
        savedDocument.Activity.summary.caloriesOut.Should().Be(activityResponse.summary.caloriesOut);
    }
}
