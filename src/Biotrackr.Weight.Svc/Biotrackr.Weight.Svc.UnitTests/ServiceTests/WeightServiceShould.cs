using AutoFixture;
using Biotrackr.Weight.Svc.Models;
using Biotrackr.Weight.Svc.Repositories.Interfaces;
using Biotrackr.Weight.Svc.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Weight.Svc.UnitTests.ServiceTests
{
    public class WeightServiceShould
    {
        private readonly Mock<ICosmosRepository> _cosmosRepositoryMock;
        private readonly Mock<ILogger<WeightService>> _loggerMock;
        private readonly WeightService _weightService;

        public WeightServiceShould()
        {
            _cosmosRepositoryMock = new Mock<ICosmosRepository>();
            _loggerMock = new Mock<ILogger<WeightService>>();
            _weightService = new WeightService(_cosmosRepositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldMapAndSaveDocument()
        {
            // Arrange
            var date = "2023-01-01";
            var fixture = new Fixture();
            var weight = fixture.Create<WeightMeasurement>();

            _cosmosRepositoryMock.Setup(x => x.UpsertWeightDocument(It.IsAny<WeightDocument>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> weightServiceAction = async () => await _weightService.MapAndSaveDocument(date, weight, "Withings");

            // Assert
            await weightServiceAction.Should().NotThrowAsync();
            _cosmosRepositoryMock.Verify(x => x.UpsertWeightDocument(It.IsAny<WeightDocument>()), Times.Once);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldThrowExceptionWhenFails()
        {
            // Arrange
            var date = "2023-10-01";
            var fixture = new Fixture();
            var weight = fixture.Create<WeightMeasurement>();

            _cosmosRepositoryMock.Setup(x => x.UpsertWeightDocument(It.IsAny<WeightDocument>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            Func<Task> weightServiceAction = async () => await _weightService.MapAndSaveDocument(date, weight, "Withings");

            // Assert
            await weightServiceAction.Should().ThrowAsync<Exception>();
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in MapAndSaveDocument: Test exception"));
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldSetProviderOnDocument()
        {
            // Arrange
            var date = "2024-01-15";
            var weight = new WeightMeasurement { WeightKg = 80.25, Date = date, Source = "Withings", LogId = 12345L };
            WeightDocument? capturedDocument = null;

            _cosmosRepositoryMock.Setup(x => x.UpsertWeightDocument(It.IsAny<WeightDocument>()))
                .Callback<WeightDocument>(doc => capturedDocument = doc)
                .Returns(Task.CompletedTask);

            // Act
            await _weightService.MapAndSaveDocument(date, weight, "Withings");

            // Assert
            capturedDocument.Should().NotBeNull();
            capturedDocument!.Provider.Should().Be("Withings");
            capturedDocument.DocumentType.Should().Be("Weight");
            capturedDocument.Date.Should().Be(date);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldUseLogIdAsDocumentId()
        {
            // Arrange
            var date = "2024-01-15";
            var weight = new WeightMeasurement { WeightKg = 80.25, Date = date, Source = "Withings", LogId = 12345L };
            WeightDocument? capturedDocument = null;

            _cosmosRepositoryMock.Setup(x => x.UpsertWeightDocument(It.IsAny<WeightDocument>()))
                .Callback<WeightDocument>(doc => capturedDocument = doc)
                .Returns(Task.CompletedTask);

            // Act
            await _weightService.MapAndSaveDocument(date, weight, "Withings");

            // Assert
            capturedDocument.Should().NotBeNull();
            capturedDocument!.Id.Should().Be("12345");
        }
    }
}
