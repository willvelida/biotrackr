using AutoFixture;
using Biotrackr.Sleep.Svc.Models;
using Biotrackr.Sleep.Svc.Models.FitbitEntities;
using Biotrackr.Sleep.Svc.Repositories.Interfaces;
using Biotrackr.Sleep.Svc.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Sleep.Svc.UnitTests.ServiceTests
{
    public class SleepServiceShould
    {
        private readonly Mock<ICosmosRepository> _mockCosmosRepository;
        private readonly Mock<ILogger<SleepService>> _mockLogger;
        private readonly SleepService _sleepService;

        public SleepServiceShould()
        {
            _mockCosmosRepository = new Mock<ICosmosRepository>();
            _mockLogger = new Mock<ILogger<SleepService>>();
            _sleepService = new SleepService(_mockCosmosRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldMapAndSaveDocument()
        {
            // Arrange
            var date = "2023-10-01";
            var fixture = new Fixture();
            var sleepResponse = fixture.Create<SleepResponse>();

            _mockCosmosRepository.Setup(x => x.CreateSleepDocument(It.IsAny<SleepDocument>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> activityServiceAction = async () => await _sleepService.MapAndSaveDocument(date, sleepResponse);

            // Assert
            await activityServiceAction.Should().NotThrowAsync<Exception>();
            _mockCosmosRepository.Verify(x => x.CreateSleepDocument(It.IsAny<SleepDocument>()), Times.Once);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldThrowExceptionWhenFails()
        {
            // Arrange
            var date = "2023-10-01";
            var fixture = new Fixture();
            var sleepResponse = fixture.Create<SleepResponse>();

            _mockCosmosRepository.Setup(x => x.CreateSleepDocument(It.IsAny<SleepDocument>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            Func<Task> activityServiceAction = async () => await _sleepService.MapAndSaveDocument(date, sleepResponse);

            // Assert
            await activityServiceAction.Should().ThrowAsync<Exception>();
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in MapAndSaveDocument: Test exception"));
        }
    }
}
