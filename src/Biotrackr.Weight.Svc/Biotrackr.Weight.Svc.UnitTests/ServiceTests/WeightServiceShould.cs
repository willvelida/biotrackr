using AutoFixture;
using Biotrackr.Weight.Svc.Repositories.Interfaces;
using Biotrackr.Weight.Svc.Services;
using Microsoft.Extensions.Logging;
using ent = Biotrackr.Weight.Svc.Models.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biotrackr.Weight.Svc.Models;
using FluentAssertions;

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
            var weight = fixture.Create<ent.Weight>();

            _cosmosRepositoryMock.Setup(x => x.CreateWeightDocument(It.IsAny<WeightDocument>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> weightServiceAction = async () => await _weightService.MapAndSaveDocument(date, weight);

            // Assert
            await weightServiceAction.Should().NotThrowAsync();
            _cosmosRepositoryMock.Verify(x => x.CreateWeightDocument(It.IsAny<WeightDocument>()), Times.Once);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldThrowExceptionWhenFails()
        {
            // Arrange
            var date = "2023-10-01";
            var fixture = new Fixture();
            var weight = fixture.Create<ent.Weight>();

            _cosmosRepositoryMock.Setup(x => x.CreateWeightDocument(It.IsAny<WeightDocument>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            Func<Task> activityServiceAction = async () => await _weightService.MapAndSaveDocument(date, weight);

            // Assert
            await activityServiceAction.Should().ThrowAsync<Exception>();
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in MapAndSaveDocument: Test exception"));
        }
    }
}
