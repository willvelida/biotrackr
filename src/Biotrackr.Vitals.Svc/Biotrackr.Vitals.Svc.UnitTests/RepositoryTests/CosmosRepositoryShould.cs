using AutoFixture;
using Biotrackr.Vitals.Svc.Configuration;
using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Repositories;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Vitals.Svc.UnitTests.RepositoryTests
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
                ContainerName = "ContainerName",
                UserHeight = 1.88
            });

            _cosmosClientMock.Setup(x => x.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_containerMock.Object);

            _cosmosRepository = new CosmosRepository(_cosmosClientMock.Object, _optionsMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task UpsertVitalsDocument_ShouldSucceed()
        {
            // Arrange
            var fixture = new Fixture();
            var vitalsDocument = fixture.Create<VitalsDocument>();
            var mockedItemResponse = new Mock<ItemResponse<VitalsDocument>>();

            mockedItemResponse.Setup(x => x.StatusCode)
                .Returns(System.Net.HttpStatusCode.OK);

            _containerMock.Setup(x => x.UpsertItemAsync(It.IsAny<VitalsDocument>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockedItemResponse.Object);

            // Act
            Func<Task> repositoryAction = async () => await _cosmosRepository.UpsertVitalsDocument(vitalsDocument);

            // Assert
            await repositoryAction.Should().NotThrowAsync<Exception>();
            _containerMock.Verify(x => x.UpsertItemAsync(vitalsDocument, new PartitionKey(vitalsDocument.DocumentType), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpsertVitalsDocument_ShouldThrowExceptionWhenFails()
        {
            // Arrange
            var fixture = new Fixture();
            var vitalsDocument = fixture.Create<VitalsDocument>();

            _containerMock.Setup(x => x.UpsertItemAsync(It.IsAny<VitalsDocument>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Mock Failure"));

            // Act
            Func<Task> repositoryAction = async () => await _cosmosRepository.UpsertVitalsDocument(vitalsDocument);

            // Assert
            await repositoryAction.Should().ThrowAsync<Exception>();
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in UpsertVitalsDocument: Mock Failure"));
        }
    }
}
