using Biotrackr.Vitals.Svc.Configuration;
using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Models.WithingsEntities;
using Biotrackr.Vitals.Svc.Services.Interfaces;
using Biotrackr.Vitals.Svc.Workers;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Vitals.Svc.UnitTests.WorkerTests
{
    public class VitalsWorkerShould
    {
        private readonly Mock<IWithingsService> _withingsServiceMock;
        private readonly Mock<IVitalsService> _vitalsServiceMock;
        private readonly Mock<ILogger<VitalsWorker>> _loggerMock;
        private readonly Mock<IHostApplicationLifetime> _appLifetimeMock;
        private readonly IOptions<Settings> _settings;

        public VitalsWorkerShould()
        {
            _withingsServiceMock = new Mock<IWithingsService>();
            _vitalsServiceMock = new Mock<IVitalsService>();
            _loggerMock = new Mock<ILogger<VitalsWorker>>();
            _appLifetimeMock = new Mock<IHostApplicationLifetime>();
            _settings = Options.Create(new Settings { DatabaseName = "TestDb", ContainerName = "TestContainer", UserHeight = 1.88 });
        }

        [Fact]
        public void Constructor_Should_InitializeAllDependencies()
        {
            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            worker.Should().NotBeNull();
        }

        [Fact]
        public async Task ExecuteAsync_Should_FetchAndSaveVitalsMeasurements_Successfully()
        {
            var measureResponse = CreateWeightMeasureResponse(2);

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _vitalsServiceMock
                .Setup(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Returns(Task.CompletedTask);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _withingsServiceMock.Verify(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _vitalsServiceMock.Verify(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()), Times.Exactly(2));
            _appLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_HandleEmptyMeasureGroups()
        {
            var measureResponse = CreateWeightMeasureResponse(0);

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _withingsServiceMock.Verify(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _vitalsServiceMock.Verify(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()), Times.Never);
            _appLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_LogErrorAndStopApplication_WhenGetMeasurementsThrows()
        {
            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Withings API error"));

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _withingsServiceMock.Verify(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _vitalsServiceMock.Verify(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()), Times.Never);
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
        public async Task ExecuteAsync_Should_LogErrorAndStopApplication_WhenUpsertVitalsDocumentThrows()
        {
            var measureResponse = CreateWeightMeasureResponse(1);

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _vitalsServiceMock
                .Setup(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .ThrowsAsync(new Exception("Database error"));

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
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
            var measureResponse = CreateWeightMeasureResponse(0);

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("VitalsWorker executed at")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_GroupByDate_WeightOnly()
        {
            var measureResponse = CreateWeightMeasureResponse(1);
            VitalsDocument? capturedDoc = null;

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _vitalsServiceMock
                .Setup(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Callback<VitalsDocument>(doc => capturedDoc = doc)
                .Returns(Task.CompletedTask);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            capturedDoc.Should().NotBeNull();
            capturedDoc!.Weight.Should().NotBeNull();
            capturedDoc.Weight!.WeightKg.Should().BeGreaterThan(0);
            capturedDoc.BloodPressureReadings.Should().BeNull();
            capturedDoc.Provider.Should().Be("Withings");
        }

        [Fact]
        public async Task ExecuteAsync_Should_GroupByDate_BpOnly()
        {
            var measureResponse = CreateBpMeasureResponse(1);
            VitalsDocument? capturedDoc = null;

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _vitalsServiceMock
                .Setup(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Callback<VitalsDocument>(doc => capturedDoc = doc)
                .Returns(Task.CompletedTask);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            capturedDoc.Should().NotBeNull();
            capturedDoc!.Weight.Should().BeNull();
            capturedDoc.BloodPressureReadings.Should().NotBeNull();
            capturedDoc.BloodPressureReadings.Should().HaveCount(1);
            capturedDoc.BloodPressureReadings![0].Systolic.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ExecuteAsync_Should_GroupByDate_MixedWeightAndBp()
        {
            var measureResponse = CreateMixedMeasureResponse();
            var capturedDocs = new List<VitalsDocument>();

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _vitalsServiceMock
                .Setup(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Callback<VitalsDocument>(doc => capturedDocs.Add(doc))
                .Returns(Task.CompletedTask);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            capturedDocs.Should().HaveCount(1);
            var doc = capturedDocs[0];
            doc.Weight.Should().NotBeNull();
            doc.BloodPressureReadings.Should().NotBeNull();
            doc.BloodPressureReadings.Should().HaveCount(1);
        }

        [Fact]
        public async Task ExecuteAsync_Should_UseMostRecentWeightGroup_ForSameDate()
        {
            var baseDate = new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero);
            var groups = new List<MeasureGroup>
            {
                new MeasureGroup
                {
                    GrpId = 100001,
                    Attrib = 0,
                    Date = baseDate.AddHours(2).ToUnixTimeSeconds(),
                    Created = baseDate.AddHours(2).AddSeconds(50).ToUnixTimeSeconds(),
                    Category = 1,
                    DeviceId = "test-device",
                    Measures = [new Measure { Value = 80000, Type = 1, Unit = -3 }]
                },
                new MeasureGroup
                {
                    GrpId = 100002,
                    Attrib = 0,
                    Date = baseDate.AddHours(4).ToUnixTimeSeconds(),
                    Created = baseDate.AddHours(4).AddSeconds(50).ToUnixTimeSeconds(),
                    Category = 1,
                    DeviceId = "test-device",
                    Measures = [new Measure { Value = 81000, Type = 1, Unit = -3 }]
                }
            };

            var measureResponse = new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody { MeasureGroups = groups, More = 0, Offset = 0, Timezone = "Australia/Melbourne" }
            };

            VitalsDocument? capturedDoc = null;

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _vitalsServiceMock
                .Setup(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Callback<VitalsDocument>(doc => capturedDoc = doc)
                .Returns(Task.CompletedTask);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            capturedDoc.Should().NotBeNull();
            capturedDoc!.Weight.Should().NotBeNull();
            capturedDoc.Weight!.WeightKg.Should().BeApproximately(81.0, 0.001);
        }

        [Fact]
        public async Task ExecuteAsync_Should_UseConfiguredLookbackDays()
        {
            var customSettings = Options.Create(new Settings { DatabaseName = "TestDb", ContainerName = "TestContainer", UserHeight = 1.88, LookbackDays = 30 });
            var measureResponse = CreateWeightMeasureResponse(0);
            string? capturedStartDate = null;

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((start, end) => capturedStartDate = start)
                .ReturnsAsync(measureResponse);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                customSettings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            capturedStartDate.Should().NotBeNull();
            var expectedStart = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
            capturedStartDate.Should().Be(expectedStart);
        }

        [Fact]
        public async Task ExecuteAsync_Should_GroupByLocalDate_WhenTimezoneProvided()
        {
            // UTC 08:00 Apr 1 → AEST 18:00 Apr 1 (same day)
            // UTC 14:00 Apr 1 → AEST 00:00 Apr 2 (next day)
            var groups = new List<MeasureGroup>
            {
                new MeasureGroup
                {
                    GrpId = 400001,
                    Attrib = 0,
                    Date = new DateTimeOffset(2024, 4, 1, 8, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Created = new DateTimeOffset(2024, 4, 1, 8, 0, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Category = 1,
                    DeviceId = "test-device",
                    Measures = [new Measure { Value = 80000, Type = 1, Unit = -3 }]
                },
                new MeasureGroup
                {
                    GrpId = 400002,
                    Attrib = 0,
                    Date = new DateTimeOffset(2024, 4, 1, 14, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Created = new DateTimeOffset(2024, 4, 1, 14, 0, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Category = 1,
                    DeviceId = "test-device",
                    Measures = [new Measure { Value = 81000, Type = 1, Unit = -3 }]
                }
            };

            var measureResponse = new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody { MeasureGroups = groups, More = 0, Offset = 0, Timezone = "Australia/Melbourne" }
            };

            var capturedDocs = new List<VitalsDocument>();

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _vitalsServiceMock
                .Setup(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Callback<VitalsDocument>(doc => capturedDocs.Add(doc))
                .Returns(Task.CompletedTask);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            capturedDocs.Should().HaveCount(2);
            capturedDocs.Select(d => d.Date).Should().Contain("2024-04-01");
            capturedDocs.Select(d => d.Date).Should().Contain("2024-04-02");
        }

        [Fact]
        public async Task ExecuteAsync_Should_FallbackToUtc_WhenTimezoneIsEmpty()
        {
            // UTC 14:00 Apr 1 — with UTC grouping stays on Apr 1
            var groups = new List<MeasureGroup>
            {
                new MeasureGroup
                {
                    GrpId = 500001,
                    Attrib = 0,
                    Date = new DateTimeOffset(2024, 4, 1, 14, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Created = new DateTimeOffset(2024, 4, 1, 14, 0, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Category = 1,
                    DeviceId = "test-device",
                    Measures = [new Measure { Value = 80000, Type = 1, Unit = -3 }]
                }
            };

            var measureResponse = new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody { MeasureGroups = groups, More = 0, Offset = 0, Timezone = "" }
            };

            VitalsDocument? capturedDoc = null;

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _vitalsServiceMock
                .Setup(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Callback<VitalsDocument>(doc => capturedDoc = doc)
                .Returns(Task.CompletedTask);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            capturedDoc.Should().NotBeNull();
            capturedDoc!.Date.Should().Be("2024-04-01");
        }

        [Fact]
        public async Task ExecuteAsync_Should_FallbackToUtc_WhenTimezoneIsInvalid()
        {
            // UTC 14:00 Apr 1 — with UTC fallback stays on Apr 1
            var groups = new List<MeasureGroup>
            {
                new MeasureGroup
                {
                    GrpId = 600001,
                    Attrib = 0,
                    Date = new DateTimeOffset(2024, 4, 1, 14, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Created = new DateTimeOffset(2024, 4, 1, 14, 0, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Category = 1,
                    DeviceId = "test-device",
                    Measures = [new Measure { Value = 80000, Type = 1, Unit = -3 }]
                }
            };

            var measureResponse = new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody { MeasureGroups = groups, More = 0, Offset = 0, Timezone = "Invalid/Zone" }
            };

            VitalsDocument? capturedDoc = null;

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _vitalsServiceMock
                .Setup(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Callback<VitalsDocument>(doc => capturedDoc = doc)
                .Returns(Task.CompletedTask);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            capturedDoc.Should().NotBeNull();
            capturedDoc!.Date.Should().Be("2024-04-01");
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown timezone")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_PassTimezoneToAdapters()
        {
            // Use July (AEST, UTC+10 — no daylight saving): UTC 02:00 Jul 1 → AEST 12:00 Jul 1
            var groups = new List<MeasureGroup>
            {
                new MeasureGroup
                {
                    GrpId = 700001,
                    Attrib = 0,
                    Date = new DateTimeOffset(2024, 7, 1, 2, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Created = new DateTimeOffset(2024, 7, 1, 2, 0, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Category = 1,
                    DeviceId = "test-device",
                    Measures =
                    [
                        new Measure { Value = 80000, Type = 1, Unit = -3 },
                        new Measure { Value = 2050, Type = 6, Unit = -2 }
                    ]
                },
                new MeasureGroup
                {
                    GrpId = 700002,
                    Attrib = 0,
                    Date = new DateTimeOffset(2024, 7, 1, 2, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Created = new DateTimeOffset(2024, 7, 1, 2, 30, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Category = 1,
                    DeviceId = "bp-device",
                    Measures =
                    [
                        new Measure { Value = 120, Type = 10, Unit = 0 },
                        new Measure { Value = 80, Type = 9, Unit = 0 },
                        new Measure { Value = 72, Type = 11, Unit = 0 }
                    ]
                }
            };

            var measureResponse = new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody { MeasureGroups = groups, More = 0, Offset = 0, Timezone = "Australia/Melbourne" }
            };

            VitalsDocument? capturedDoc = null;

            _withingsServiceMock
                .Setup(w => w.GetMeasurements(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(measureResponse);

            _vitalsServiceMock
                .Setup(w => w.UpsertVitalsDocument(It.IsAny<VitalsDocument>()))
                .Callback<VitalsDocument>(doc => capturedDoc = doc)
                .Returns(Task.CompletedTask);

            var worker = new VitalsWorker(
                _withingsServiceMock.Object,
                _vitalsServiceMock.Object,
                _loggerMock.Object,
                _appLifetimeMock.Object,
                _settings);

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(200);

            capturedDoc.Should().NotBeNull();
            // Weight time should reflect AEST (UTC+10), so 02:00 UTC → 12:00 AEST
            capturedDoc!.Weight.Should().NotBeNull();
            capturedDoc.Weight!.Time.Should().Be("12:00:00");
            // BP time should also reflect AEST, so 02:30 UTC → 12:30 AEST
            capturedDoc.BloodPressureReadings.Should().NotBeNull();
            capturedDoc.BloodPressureReadings![0].Time.Should().Be("12:30:00");
        }

        private static WithingsMeasureResponse CreateWeightMeasureResponse(int groupCount)
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
                    new Measure { Value = 10, Type = 170, Unit = 0 }
                ]
            }).ToList();

            return new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody
                {
                    MeasureGroups = groups,
                    More = 0,
                    Offset = 0,
                    Timezone = "Australia/Melbourne"
                }
            };
        }

        private static WithingsMeasureResponse CreateBpMeasureResponse(int groupCount)
        {
            var groups = Enumerable.Range(0, groupCount).Select(i => new MeasureGroup
            {
                GrpId = 200000 + i,
                Attrib = 0,
                Date = new DateTimeOffset(2024, 4, 1, 8, 0 + i, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                Created = new DateTimeOffset(2024, 4, 1, 8, 0 + i, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                Category = 1,
                DeviceId = "bp-device",
                Measures =
                [
                    new Measure { Value = 120, Type = 10, Unit = 0 },
                    new Measure { Value = 80, Type = 9, Unit = 0 },
                    new Measure { Value = 72, Type = 11, Unit = 0 }
                ]
            }).ToList();

            return new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody
                {
                    MeasureGroups = groups,
                    More = 0,
                    Offset = 0,
                    Timezone = "Australia/Melbourne"
                }
            };
        }

        private static WithingsMeasureResponse CreateMixedMeasureResponse()
        {
            var groups = new List<MeasureGroup>
            {
                new MeasureGroup
                {
                    GrpId = 300000,
                    Attrib = 0,
                    Date = new DateTimeOffset(2024, 4, 1, 7, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Created = new DateTimeOffset(2024, 4, 1, 7, 30, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Category = 1,
                    DeviceId = "scale-device",
                    Measures =
                    [
                        new Measure { Value = 80250, Type = 1, Unit = -3 },
                        new Measure { Value = 2050, Type = 6, Unit = -2 }
                    ]
                },
                new MeasureGroup
                {
                    GrpId = 300001,
                    Attrib = 0,
                    Date = new DateTimeOffset(2024, 4, 1, 8, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Created = new DateTimeOffset(2024, 4, 1, 8, 0, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                    Category = 1,
                    DeviceId = "bp-device",
                    Measures =
                    [
                        new Measure { Value = 120, Type = 10, Unit = 0 },
                        new Measure { Value = 80, Type = 9, Unit = 0 },
                        new Measure { Value = 72, Type = 11, Unit = 0 }
                    ]
                }
            };

            return new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody
                {
                    MeasureGroups = groups,
                    More = 0,
                    Offset = 0,
                    Timezone = "Australia/Melbourne"
                }
            };
        }
    }
}
