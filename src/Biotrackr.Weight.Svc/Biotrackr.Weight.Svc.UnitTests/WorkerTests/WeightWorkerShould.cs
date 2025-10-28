using Biotrackr.Weight.Svc.Services.Interfaces;
using Biotrackr.Weight.Svc.Workers;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ent = Biotrackr.Weight.Svc.Models.Entities;

namespace Biotrackr.Weight.Svc.UnitTests.WorkerTests
{
    public class WeightWorkerShould
    {
        private readonly Mock<IFitbitService> _fitbitServiceMock;
        private readonly Mock<IWeightService> _weightServiceMock;
        private readonly Mock<ILogger<WeightWorker>> _loggerMock;
        private readonly Mock<IHostApplicationLifetime> _appLifetimeMock;

        public WeightWorkerShould()
        {
            _fitbitServiceMock = new Mock<IFitbitService>();
            _weightServiceMock = new Mock<IWeightService>();
            _loggerMock = new Mock<ILogger<WeightWorker>>();
            _appLifetimeMock = new Mock<IHostApplicationLifetime>();
        }

        [Fact]
        public void Constructor_Should_InitializeAllDependencies()
        {
            var worker = new WeightWorker(
                _fitbitServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object);

            worker.Should().NotBeNull();
        }

        [Fact]
        public async Task ExecuteAsync_Should_FetchAndSaveWeightLogs_Successfully()
        {
            var weightLogs = new List<ent.Weight>
            {
                new ent.Weight { Date = "2024-01-15", Bmi = 25.5, Fat = 20.5, Source = "API", Time = "08:00:00", weight = 70.5 },
                new ent.Weight { Date = "2024-01-16", Bmi = 25.4, Fat = 20.4, Source = "API", Time = "08:15:00", weight = 70.3 }
            };

            var weightResponse = new ent.WeightResponse { Weight = weightLogs };

            _fitbitServiceMock
                .Setup(f => f.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(weightResponse);

            _weightServiceMock
                .Setup(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ent.Weight>()))
                .Returns(Task.CompletedTask);

            var worker = new WeightWorker(
                _fitbitServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _fitbitServiceMock.Verify(f => f.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _weightServiceMock.Verify(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ent.Weight>()), Times.Exactly(2));
            _appLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_HandleMultipleWeightEntries()
        {
            var weightLogs = new List<ent.Weight>
            {
                new ent.Weight { Date = "2024-01-15", Bmi = 25.5, Fat = 20.5, Source = "API", Time = "08:00:00", weight = 70.5 },
                new ent.Weight { Date = "2024-01-16", Bmi = 25.4, Fat = 20.4, Source = "API", Time = "08:15:00", weight = 70.3 },
                new ent.Weight { Date = "2024-01-17", Bmi = 25.3, Fat = 20.3, Source = "API", Time = "08:30:00", weight = 70.0 }
            };

            var weightResponse = new ent.WeightResponse { Weight = weightLogs };

            _fitbitServiceMock
                .Setup(f => f.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(weightResponse);

            _weightServiceMock
                .Setup(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ent.Weight>()))
                .Returns(Task.CompletedTask);

            var worker = new WeightWorker(
                _fitbitServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _fitbitServiceMock.Verify(f => f.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _weightServiceMock.Verify(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ent.Weight>()), Times.Exactly(3));
            _appLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_HandleEmptyWeightLogs()
        {
            var weightResponse = new ent.WeightResponse { Weight = new List<ent.Weight>() };

            _fitbitServiceMock
                .Setup(f => f.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(weightResponse);

            var worker = new WeightWorker(
                _fitbitServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _fitbitServiceMock.Verify(f => f.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _weightServiceMock.Verify(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ent.Weight>()), Times.Never);
            _appLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_LogErrorAndStopApplication_WhenGetWeightLogsThrows()
        {
            _fitbitServiceMock
                .Setup(f => f.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Fitbit API error"));

            var worker = new WeightWorker(
                _fitbitServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _fitbitServiceMock.Verify(f => f.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _weightServiceMock.Verify(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ent.Weight>()), Times.Never);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Exception thrown")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _appLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_LogErrorAndStopApplication_WhenMapAndSaveDocumentThrows()
        {
            var weightLogs = new List<ent.Weight>
            {
                new ent.Weight { Date = "2024-01-15", Bmi = 25.5, Fat = 20.5, Source = "API", Time = "08:00:00", weight = 70.5 }
            };

            var weightResponse = new ent.WeightResponse { Weight = weightLogs };

            _fitbitServiceMock
                .Setup(f => f.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(weightResponse);

            _weightServiceMock
                .Setup(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ent.Weight>()))
                .ThrowsAsync(new Exception("Database error"));

            var worker = new WeightWorker(
                _fitbitServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _fitbitServiceMock.Verify(f => f.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _weightServiceMock.Verify(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ent.Weight>()), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Exception thrown")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _appLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_LogInformationMessage()
        {
            var weightResponse = new ent.WeightResponse { Weight = new List<ent.Weight>() };

            _fitbitServiceMock
                .Setup(f => f.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(weightResponse);

            var worker = new WeightWorker(
                _fitbitServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("WeightWorker executed at")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
