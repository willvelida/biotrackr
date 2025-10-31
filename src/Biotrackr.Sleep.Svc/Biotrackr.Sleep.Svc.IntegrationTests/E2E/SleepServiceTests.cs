using Biotrackr.Sleep.Svc.Configuration;
using Biotrackr.Sleep.Svc.IntegrationTests.Fixtures;
using Biotrackr.Sleep.Svc.IntegrationTests.Helpers;
using Biotrackr.Sleep.Svc.Repositories;
using Biotrackr.Sleep.Svc.Services;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Sleep.Svc.IntegrationTests.E2E
{
    [Collection("SleepServiceIntegrationTests")]
    public class SleepServiceTests : IAsyncLifetime
    {
        private readonly IntegrationTestFixture _fixture;
        private SleepService _sleepService = null!;

        public SleepServiceTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await ClearContainerAsync();

            var mockRepoLogger = new Mock<ILogger<CosmosRepository>>();
            var mockServiceLogger = new Mock<ILogger<SleepService>>();
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

            _sleepService = new SleepService(repository, mockServiceLogger.Object);
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
        public async Task MapAndSaveDocument_ShouldTransformAndPersist()
        {
            // Arrange
            var date = TestDataGenerator.GenerateDate();
            var sleepResponse = TestDataGenerator.GenerateSleepResponse();

            // Act
            await _sleepService.MapAndSaveDocument(date, sleepResponse);

            // Assert - Query Cosmos DB to verify document was saved
            var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
                .WithParameter("@date", date);

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
            
            savedDate.Should().Be(date);
            savedDocType.Should().Be("Sleep");
            savedDoc.id.Should().NotBeNull("ID should be generated");
            
            // Verify sleep data was mapped correctly
            savedDoc.duration.Should().NotBeNull();
            savedDoc.efficiency.Should().NotBeNull();
            savedDoc.minutesAsleep.Should().NotBeNull();
        }
    }
}
