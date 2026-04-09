using AutoFixture;
using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Repositories.Interfaces;
using Biotrackr.Vitals.Svc.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Vitals.Svc.UnitTests.ServiceTests
{
    public class VitalsServiceShould
    {
        private readonly Mock<ICosmosRepository> _cosmosRepositoryMock;
        private readonly Mock<ILogger<VitalsService>> _loggerMock;
        private readonly VitalsService _vitalsService;

        public VitalsServiceShould()
        {
            _cosmosRepositoryMock = new Mock<ICosmosRepository>();
            _loggerMock = new Mock<ILogger<VitalsService>>();
            _vitalsService = new VitalsService(_cosmosRepositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task UpsertVitalsDocument_ShouldAssignNewGuid_WhenNoExistingDocument()
        {
            // Arrange
            var vitalsDoc = new VitalsDocument
            {
                Date = "2024-01-15",
                Weight = new WeightMeasurement { WeightKg = 80.25, Date = "2024-01-15", Source = "Withings", LogId = 12345L },
                Provider = "Withings"
            };

            _cosmosRepositoryMock.Setup(x => x.GetVitalsDocumentByDate("2024-01-15"))
                .ReturnsAsync((VitalsDocument?)null);

            VitalsDocument? capturedDocument = null;
            _cosmosRepositoryMock.Setup(x => x.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Callback<VitalsDocument>(doc => capturedDocument = doc)
                .Returns(Task.CompletedTask);

            // Act
            await _vitalsService.UpsertVitalsDocument(vitalsDoc);

            // Assert
            capturedDocument.Should().NotBeNull();
            capturedDocument!.Id.Should().NotBeNullOrEmpty();
            Guid.TryParse(capturedDocument.Id, out _).Should().BeTrue("Id should be a valid GUID");
            capturedDocument.DocumentType.Should().Be("Vitals");
            _cosmosRepositoryMock.Verify(x => x.UpsertVitalsDocument(It.IsAny<VitalsDocument>()), Times.Once);
        }

        [Fact]
        public async Task UpsertVitalsDocument_ShouldReuseExistingId_WhenDocumentExists()
        {
            // Arrange
            var existingId = Guid.NewGuid().ToString();
            var existingDoc = new VitalsDocument
            {
                Id = existingId,
                Date = "2024-01-15",
                DocumentType = "Vitals",
                Provider = "Withings"
            };

            var vitalsDoc = new VitalsDocument
            {
                Date = "2024-01-15",
                Weight = new WeightMeasurement { WeightKg = 81.0, Date = "2024-01-15", Source = "Withings", LogId = 99999L },
                Provider = "Withings"
            };

            _cosmosRepositoryMock.Setup(x => x.GetVitalsDocumentByDate("2024-01-15"))
                .ReturnsAsync(existingDoc);

            VitalsDocument? capturedDocument = null;
            _cosmosRepositoryMock.Setup(x => x.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Callback<VitalsDocument>(doc => capturedDocument = doc)
                .Returns(Task.CompletedTask);

            // Act
            await _vitalsService.UpsertVitalsDocument(vitalsDoc);

            // Assert
            capturedDocument.Should().NotBeNull();
            capturedDocument!.Id.Should().Be(existingId);
            capturedDocument.DocumentType.Should().Be("Vitals");
        }

        [Fact]
        public async Task UpsertVitalsDocument_ShouldSetDocumentTypeToVitals()
        {
            // Arrange
            var vitalsDoc = new VitalsDocument
            {
                Date = "2024-01-15",
                Weight = new WeightMeasurement { WeightKg = 80.25, Date = "2024-01-15", Source = "Withings", LogId = 12345L },
                Provider = "Withings"
            };

            _cosmosRepositoryMock.Setup(x => x.GetVitalsDocumentByDate("2024-01-15"))
                .ReturnsAsync((VitalsDocument?)null);

            VitalsDocument? capturedDocument = null;
            _cosmosRepositoryMock.Setup(x => x.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Callback<VitalsDocument>(doc => capturedDocument = doc)
                .Returns(Task.CompletedTask);

            // Act
            await _vitalsService.UpsertVitalsDocument(vitalsDoc);

            // Assert
            capturedDocument.Should().NotBeNull();
            capturedDocument!.DocumentType.Should().Be("Vitals");
        }

        [Fact]
        public async Task UpsertVitalsDocument_ShouldThrowExceptionWhenFails()
        {
            // Arrange
            var vitalsDoc = new VitalsDocument
            {
                Date = "2024-01-15",
                Provider = "Withings"
            };

            _cosmosRepositoryMock.Setup(x => x.GetVitalsDocumentByDate("2024-01-15"))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            Func<Task> vitalsServiceAction = async () => await _vitalsService.UpsertVitalsDocument(vitalsDoc);

            // Assert
            await vitalsServiceAction.Should().ThrowAsync<Exception>();
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in UpsertVitalsDocument: Test exception"));
        }
    }
}
