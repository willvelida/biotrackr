using Biotrackr.Sleep.Svc.Configuration;
using Biotrackr.Sleep.Svc.IntegrationTests.Fixtures;
using Biotrackr.Sleep.Svc.IntegrationTests.Helpers;
using Biotrackr.Sleep.Svc.Repositories;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Sleep.Svc.IntegrationTests.E2E
{
    [Collection("SleepServiceIntegrationTests")]
    public class CosmosRepositoryTests : IAsyncLifetime
    {
        private readonly IntegrationTestFixture _fixture;
        private CosmosRepository _repository = null!;

        public CosmosRepositoryTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await ClearContainerAsync();

            var mockLogger = new Mock<ILogger<CosmosRepository>>();
            var mockSettings = new Mock<IOptions<Settings>>();
            mockSettings.Setup(x => x.Value).Returns(new Settings
            {
                DatabaseName = "BiotrackrTestDb",
                ContainerName = "SleepTestContainer"
            });

            _repository = new CosmosRepository(
                _fixture.CosmosClient,
                mockSettings.Object,
                mockLogger.Object);
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
        public async Task CreateSleepDocument_ShouldPersistToDatabase()
        {
            // Arrange
            var sleepDocument = TestDataGenerator.GenerateSleepDocument();

            // Act
            await _repository.CreateSleepDocument(sleepDocument);

            // Assert - Query Cosmos DB to verify document was saved
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", sleepDocument.Id);

            var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);
            var documents = new List<dynamic>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                documents.AddRange(response);
            }

            documents.Should().ContainSingle("exactly one document should be saved");
            var savedDoc = documents.First();
            savedDoc.id.ToString().Should().Be(sleepDocument.Id);
            savedDoc.documentType.ToString().Should().Be("Sleep");
            savedDoc.date.ToString().Should().Be(sleepDocument.Date);
        }

        [Fact]
        public async Task CreateSleepDocument_ShouldUseCorrectPartitionKey()
        {
            // Arrange
            var sleepDocument = TestDataGenerator.GenerateSleepDocument();
            sleepDocument.DocumentType = "Sleep";

            // Act
            await _repository.CreateSleepDocument(sleepDocument);

            // Assert - Can read document using partition key
            var readResponse = await _fixture.Container.ReadItemAsync<dynamic>(
                sleepDocument.Id,
                new PartitionKey("Sleep"));

            readResponse.Resource.Should().NotBeNull();
            readResponse.Resource.id.ToString().Should().Be(sleepDocument.Id);
        }
    }
}
