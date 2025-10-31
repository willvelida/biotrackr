using Biotrackr.Sleep.Svc.Configuration;
using Biotrackr.Sleep.Svc.IntegrationTests.Fixtures;
using Biotrackr.Sleep.Svc.IntegrationTests.Helpers;
using Biotrackr.Sleep.Svc.Repositories;
using Biotrackr.Sleep.Svc.Services;
using Biotrackr.Sleep.Svc.Services.Interfaces;
using Biotrackr.Sleep.Svc.Worker;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Sleep.Svc.IntegrationTests.E2E
{
    [Collection("SleepServiceIntegrationTests")]
    public class SleepWorkerTests : IAsyncLifetime
    {
        private readonly IntegrationTestFixture _fixture;
        private readonly Mock<IFitbitService> _mockFitbitService;
        private SleepWorker _worker = null!;

        public SleepWorkerTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _mockFitbitService = new Mock<IFitbitService>();
        }

        public async Task InitializeAsync()
        {
            await ClearContainerAsync();

            var mockRepoLogger = new Mock<ILogger<CosmosRepository>>();
            var mockServiceLogger = new Mock<ILogger<SleepService>>();
            var mockWorkerLogger = new Mock<ILogger<SleepWorker>>();
            var mockAppLifetime = new Mock<IHostApplicationLifetime>();
            var mockSettings = new Mock<IOptions<Settings>>();
            mockSettings.Setup(x => x.Value).Returns(new Settings
            {
                DatabaseName = "BiotrackrTestDb",
                ContainerName = "SleepTestContainer"
            });

            var repository = new CosmosRepository(
                _fixture.CosmosClient,
                mockSettings.Object,
                mockRepoLogger.Object);

            var sleepService = new SleepService(repository, mockServiceLogger.Object);

            _worker = new SleepWorker(
                _mockFitbitService.Object,
                sleepService,
                mockWorkerLogger.Object,
                mockAppLifetime.Object);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>
        /// Clears all documents from the test container to ensure test isolation.
        /// </summary>
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

        [Fact]
        public async Task ExecuteAsync_ShouldCompleteFullWorkflow()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var sleepResponse = TestDataGenerator.GenerateSleepResponse();

            _mockFitbitService
                .Setup(x => x.GetSleepResponse(expectedDate))
                .ReturnsAsync(sleepResponse);

            // Act
            await _worker.StartAsync(CancellationToken.None);
            await Task.Delay(200); // Allow background task to complete

            // Assert - Verify Fitbit service was called
            _mockFitbitService.Verify(
                x => x.GetSleepResponse(It.Is<string>(d => d == expectedDate)),
                Times.Once);

            // Assert - Verify document was saved to Cosmos DB
            var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
                .WithParameter("@date", expectedDate);

            var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);
            var documents = new List<dynamic>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                documents.AddRange(response);
            }

            documents.Should().ContainSingle("exactly one document should be saved");
            var savedDoc = documents.First();
            string savedDate = savedDoc.date.ToString();
            string savedDocType = savedDoc.documentType.ToString();
            
            savedDate.Should().Be(expectedDate);
            savedDocType.Should().Be("Sleep");
        }
    }
}
