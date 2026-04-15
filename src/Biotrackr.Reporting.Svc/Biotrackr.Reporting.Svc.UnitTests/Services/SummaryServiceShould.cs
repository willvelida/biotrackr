using AutoFixture;
using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Models;
using Biotrackr.Reporting.Svc.Services;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Reporting.Svc.UnitTests.Services;

public class SummaryServiceShould
{
    private readonly Mock<IHealthDataService> _healthDataServiceMock;
    private readonly Mock<IReportingApiService> _reportingApiServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IMetricExtractor> _metricExtractorMock;
    private readonly Mock<ILogger<SummaryService>> _loggerMock;
    private readonly Fixture _fixture;

    public SummaryServiceShould()
    {
        _healthDataServiceMock = new Mock<IHealthDataService>();
        _reportingApiServiceMock = new Mock<IReportingApiService>();
        _emailServiceMock = new Mock<IEmailService>();
        _metricExtractorMock = new Mock<IMetricExtractor>();
        _loggerMock = new Mock<ILogger<SummaryService>>();
        _fixture = new Fixture();

        _metricExtractorMock
            .Setup(x => x.ExtractMetrics(It.IsAny<HealthDataSnapshot>()))
            .Returns(new List<MetricCard>());
    }

    private SummaryService CreateService(string cadence = "weekly")
    {
        var settings = new Settings { SummaryCadence = cadence };
        var options = Options.Create(settings);
        return new SummaryService(
            _healthDataServiceMock.Object,
            _reportingApiServiceMock.Object,
            _emailServiceMock.Object,
            _metricExtractorMock.Object,
            options,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateAndSendSummaryAsync_ShouldOrchestrateFull_WhenWeeklyCadence()
    {
        // Arrange
        var snapshot = _fixture.Create<HealthDataSnapshot>();
        var summaryResult = new SummaryResult
        {
            JobId = "job-1",
            Status = "generated",
            Summary = "Weekly summary",
            PdfUrl = "https://example.com/report.pdf"
        };

        _healthDataServiceMock
            .Setup(x => x.FetchHealthDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        _reportingApiServiceMock
            .Setup(x => x.GenerateReportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HealthDataSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryResult);

        _reportingApiServiceMock
            .Setup(x => x.DownloadArtifactAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([0x01, 0x02]);

        _emailServiceMock
            .Setup(x => x.SendReportEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<byte[]>(), It.IsAny<List<MetricCard>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateService("weekly");

        // Act
        await sut.GenerateAndSendSummaryAsync(CancellationToken.None);

        // Assert
        _healthDataServiceMock.Verify(x => x.FetchHealthDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _reportingApiServiceMock.Verify(x => x.GenerateReportAsync("weekly_summary", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), snapshot, It.IsAny<CancellationToken>()), Times.Once);
        _reportingApiServiceMock.Verify(x => x.DownloadArtifactAsync("https://example.com/report.pdf", It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendReportEmailAsync("weekly", It.IsAny<string>(), It.IsAny<string>(), "Weekly summary", It.IsAny<byte[]>(), It.IsAny<List<MetricCard>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAndSendSummaryAsync_ShouldUseMonthlyReportType_WhenMonthlyCadence()
    {
        // Arrange
        var snapshot = _fixture.Create<HealthDataSnapshot>();
        var summaryResult = new SummaryResult { JobId = "job-2", Status = "generated", PdfUrl = "https://example.com/report.pdf" };

        _healthDataServiceMock
            .Setup(x => x.FetchHealthDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        _reportingApiServiceMock
            .Setup(x => x.GenerateReportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HealthDataSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryResult);

        _reportingApiServiceMock
            .Setup(x => x.DownloadArtifactAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([0x01]);

        var sut = CreateService("monthly");

        // Act
        await sut.GenerateAndSendSummaryAsync(CancellationToken.None);

        // Assert
        _reportingApiServiceMock.Verify(x => x.GenerateReportAsync("monthly_summary", It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(m => m.Contains("monthly")), It.IsAny<HealthDataSnapshot>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAndSendSummaryAsync_ShouldUseTrendAnalysisReportType_WhenYearlyCadence()
    {
        // Arrange
        var snapshot = _fixture.Create<HealthDataSnapshot>();
        var summaryResult = new SummaryResult { JobId = "job-3", Status = "generated", PdfUrl = "https://example.com/report.pdf" };

        _healthDataServiceMock
            .Setup(x => x.FetchHealthDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        _reportingApiServiceMock
            .Setup(x => x.GenerateReportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HealthDataSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryResult);

        _reportingApiServiceMock
            .Setup(x => x.DownloadArtifactAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([0x01]);

        var sut = CreateService("yearly");

        // Act
        await sut.GenerateAndSendSummaryAsync(CancellationToken.None);

        // Assert
        _reportingApiServiceMock.Verify(x => x.GenerateReportAsync("trend_analysis", It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(m => m.Contains("annual")), It.IsAny<HealthDataSnapshot>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAndSendSummaryAsync_ShouldSkipDownload_WhenNoPdfUrl()
    {
        // Arrange
        var snapshot = _fixture.Create<HealthDataSnapshot>();
        var summaryResult = new SummaryResult { JobId = "job-4", Status = "generated", PdfUrl = null };

        _healthDataServiceMock
            .Setup(x => x.FetchHealthDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        _reportingApiServiceMock
            .Setup(x => x.GenerateReportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HealthDataSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryResult);

        var sut = CreateService("weekly");

        // Act
        await sut.GenerateAndSendSummaryAsync(CancellationToken.None);

        // Assert
        _reportingApiServiceMock.Verify(x => x.DownloadArtifactAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailServiceMock.Verify(x => x.SendReportEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), Array.Empty<byte>(), It.IsAny<List<MetricCard>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAndSendSummaryAsync_ShouldPropagateException_WhenHealthDataFails()
    {
        // Arrange
        _healthDataServiceMock
            .Setup(x => x.FetchHealthDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("MCP error"));

        var sut = CreateService("weekly");

        // Act
        var act = () => sut.GenerateAndSendSummaryAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("MCP error");
    }

    [Fact]
    public async Task GenerateAndSendSummaryAsync_ShouldPropagateException_WhenReportGenerationFails()
    {
        // Arrange
        var snapshot = _fixture.Create<HealthDataSnapshot>();

        _healthDataServiceMock
            .Setup(x => x.FetchHealthDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        _reportingApiServiceMock
            .Setup(x => x.GenerateReportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HealthDataSnapshot>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Report timed out"));

        var sut = CreateService("weekly");

        // Act
        var act = () => sut.GenerateAndSendSummaryAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>().WithMessage("Report timed out");
    }

    [Theory]
    [InlineData("weekly")]
    [InlineData("monthly")]
    [InlineData("yearly")]
    public void CalculateDateRange_ShouldReturnValidDatePair_WhenCadenceIsValid(string cadence)
    {
        // Act
        var (startDate, endDate) = SummaryService.CalculateDateRange(cadence);

        // Assert
        DateOnly.TryParseExact(startDate, "yyyy-MM-dd", out var start).Should().BeTrue();
        DateOnly.TryParseExact(endDate, "yyyy-MM-dd", out var end).Should().BeTrue();
        start.Should().BeBefore(end);
    }

    [Fact]
    public void CalculateDateRange_ShouldReturnPreviousWeek_WhenWeekly()
    {
        // Act
        var (startDate, endDate) = SummaryService.CalculateDateRange("weekly");

        // Assert
        var start = DateOnly.ParseExact(startDate, "yyyy-MM-dd");
        var end = DateOnly.ParseExact(endDate, "yyyy-MM-dd");

        start.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        end.DayOfWeek.Should().Be(DayOfWeek.Saturday);
        (end.DayNumber - start.DayNumber).Should().Be(6);
    }

    [Fact]
    public void CalculateDateRange_ShouldReturnPreviousMonth_WhenMonthly()
    {
        // Act
        var (startDate, endDate) = SummaryService.CalculateDateRange("monthly");

        // Assert
        var start = DateOnly.ParseExact(startDate, "yyyy-MM-dd");
        var end = DateOnly.ParseExact(endDate, "yyyy-MM-dd");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        start.Day.Should().Be(1);
        end.Should().Be(new DateOnly(today.Year, today.Month, 1).AddDays(-1));
    }

    [Fact]
    public void CalculateDateRange_ShouldThrow_WhenCadenceIsUnknown()
    {
        // Act
        var act = () => SummaryService.CalculateDateRange("daily");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Unknown cadence: daily");
    }

    [Fact]
    public void CalculateDateRange_ShouldReturnYearlyRange_WhenYearly()
    {
        // Act
        var (startDate, endDate) = SummaryService.CalculateDateRange("yearly");

        // Assert
        var start = DateOnly.ParseExact(startDate, "yyyy-MM-dd");
        var end = DateOnly.ParseExact(endDate, "yyyy-MM-dd");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        start.Year.Should().Be(today.Year - 1);
        end.Year.Should().Be(today.Year);
    }
}
