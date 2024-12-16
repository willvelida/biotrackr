using AutoFixture;
using Biotrackr.Sleep.Svc.Configuration;
using Biotrackr.Sleep.Svc.Models;
using Biotrackr.Sleep.Svc.Repositories;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Sleep.Svc.UnitTests.RepositoryTests
{
    public class CosmosRepositoryShould
    {
        private readonly Mock<CosmosClient> _cosmosClientMock;
        private readonly Mock<ILogger<CosmosRepository>> _loggerMock;
        private readonly Mock<IOptions<Settings>> _optionsMock;
        private readonly Mock<Container> _containerMock;

        private CosmosRepository _cosmosRepository;

        public CosmosRepositoryShould()
        {
            _cosmosClientMock = new Mock<CosmosClient>();
            _loggerMock = new Mock<ILogger<CosmosRepository>>();
            _optionsMock = new Mock<IOptions<Settings>>();
            _containerMock = new Mock<Container>();

            _optionsMock.Setup(x => x.Value).Returns(new Settings
            {
                DatabaseName = "DatabaseName",
                ContainerName = "ContainerName"
            });

            _cosmosClientMock.Setup(x => x.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_containerMock.Object);

            _cosmosRepository = new CosmosRepository(_cosmosClientMock.Object, _optionsMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateSleepDocument_ShouldSucceed()
        {
            // Arrange
            var fixture = new Fixture();
            var sleepDocument = fixture.Create<SleepDocument>();
            var mockedItemResponse = new Mock<ItemResponse<SleepDocument>>();

            mockedItemResponse.Setup(x => x.StatusCode)
                .Returns(System.Net.HttpStatusCode.OK);

            _containerMock.Setup(x => x.CreateItemAsync(It.IsAny<SleepDocument>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockedItemResponse.Object)
                .Callback<SleepDocument, PartitionKey?, RequestOptions, CancellationToken>(
                    (t, p, r, c) => sleepDocument = t);

            // Act
            Func<Task> repositoryAction = async () => await _cosmosRepository.CreateSleepDocument(sleepDocument);

            // Assert
            await repositoryAction.Should().NotThrowAsync<Exception>();
            _containerMock.Verify(x => x.CreateItemAsync(sleepDocument, new PartitionKey(sleepDocument.DocumentType), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateSleepDocument_ShouldThrowExceptionWhenFails()
        {
            // Arrange
            var fixture = new Fixture();
            var sleepDocument = fixture.Create<SleepDocument>();

            _containerMock.Setup(x => x.CreateItemAsync(It.IsAny<SleepDocument>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Mock Failure"));

            // Act
            Func<Task> repositoryAction = async () => await _cosmosRepository.CreateSleepDocument(sleepDocument);

            // Assert
            await repositoryAction.Should().ThrowAsync<Exception>();
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in CreateSleepDocument: Mock Failure"));
        }
    }
}
