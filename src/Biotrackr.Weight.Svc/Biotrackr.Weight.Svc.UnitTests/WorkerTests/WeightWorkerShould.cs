using Biotrackr.Weight.Svc.Configuration;
using Biotrackr.Weight.Svc.Models;
using Biotrackr.Weight.Svc.Models.WithingsEntities;
using Biotrackr.Weight.Svc.Services.Interfaces;
using Biotrackr.Weight.Svc.Workers;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Weight.Svc.UnitTests.WorkerTests
{
    public class WeightWorkerShould
    {
        private readonly Mock<IWithingsService> _withingsServiceMock;
        private readonly Mock<IWeightService> _weightServiceMock;
        private readonly Mock<ILogger<WeightWorker>> _loggerMock;
        private readonly Mock<IHostApplicationLifetime> _appLifetimeMock;
        private readonly IOptions<Settings> _settings;

        public WeightWorkerShould()
        {
            _withingsServiceMock = new Mock<IWithingsService>();
            _weightServiceMock = new Mock<IWeightService>();
            _loggerMock = new Mock<ILogger<WeightWorker>>();
            _appLifetimeMock = new Mock<IHostApplicationLifetime>();
            _settings = Options.Create(new Settings { DatabaseName = "TestDb", ContainerName = "TestContainer", UserHeight = 1.88 });
        }

        [Fact]
        public void Constructor_Should_InitializeAllDependencies()
        {
            var worker = new WeightWorker(
                _withingsServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            worker.Should().NotBeNull();
        }

        [Fact]
        public async Task ExecuteAsync_Should_FetchAndSaveWeightMeasurements_Successfully()
        {
            var measureResponse = CreateMeasureResponse(2);

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _weightServiceMock
                .Setup(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<WeightMeasurement>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var worker = new WeightWorker(
                _withingsServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _withingsServiceMock.Verify(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _weightServiceMock.Verify(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<WeightMeasurement>(), "Withings"), Times.Exactly(2));
            _appLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_HandleEmptyMeasureGroups()
        {
            var measureResponse = CreateMeasureResponse(0);

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            var worker = new WeightWorker(
                _withingsServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _withingsServiceMock.Verify(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _weightServiceMock.Verify(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<WeightMeasurement>(), It.IsAny<string>()), Times.Never);
            _appLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_LogErrorAndStopApplication_WhenGetMeasurementsThrows()
        {
            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Withings API error"));

            var worker = new WeightWorker(
                _withingsServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _withingsServiceMock.Verify(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _weightServiceMock.Verify(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<WeightMeasurement>(), It.IsAny<string>()), Times.Never);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception thrown")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _appLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_LogErrorAndStopApplication_WhenMapAndSaveDocumentThrows()
        {
            var measureResponse = CreateMeasureResponse(1);

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _weightServiceMock
                .Setup(w => w.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<WeightMeasurement>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            var worker = new WeightWorker(
                _withingsServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception thrown")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _appLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_LogInformationMessage()
        {
            var measureResponse = CreateMeasureResponse(0);

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            var worker = new WeightWorker(
                _withingsServiceMock.Object,
                _weightServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("WeightWorker executed at")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static WithingsMeasureResponse CreateMeasureResponse(int groupCount)
        {
            var groups = Enumerable.Range(0, groupCount).Select(i => new MeasureGroup
            {
                GrpId = 100000 + i,
                Attrib = 0,
                Date = new DateTimeOffset(2024, 4, 1 + i, 7, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                Created = new DateTimeOffset(2024, 4, 1 + i, 7, 30, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                Category = 1,
                DeviceId = "test-device",
                Measures =
                [
                    new Measure { Value = 80250, Type = 1, Unit = -3 },
                    new Measure { Value = 2050, Type = 6, Unit = -2 },
                    new Measure { Value = 15230, Type = 8, Unit = -3 },
                    new Measure { Value = 65020, Type = 5, Unit = -3 },
                    new Measure { Value = 45200, Type = 76, Unit = -3 },
                    new Measure { Value = 3100, Type = 88, Unit = -3 },
                    new Measure { Value = 48900, Type = 77, Unit = -3 },
                    new Measure { Value = 10, Type = 123, Unit = 0 }
                ]
            }).ToList();

            return new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody
                {
                    MeasureGroups = groups,
                    More = 0,
                    Offset = 0
                }
            };
        }
    }
}
