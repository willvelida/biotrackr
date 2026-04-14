using Biotrackr.Reporting.Svc.Services.Interfaces;
using Biotrackr.Reporting.Svc.Workers;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Reporting.Svc.UnitTests.Workers;

public class ReportingWorkerShould
{
    private readonly Mock<ISummaryService> _summaryServiceMock;
    private readonly Mock<ILogger<ReportingWorker>> _loggerMock;
    private readonly Mock<IHostApplicationLifetime> _appLifetimeMock;

    public ReportingWorkerShould()
    {
        _summaryServiceMock = new Mock<ISummaryService>();
        _loggerMock = new Mock<ILogger<ReportingWorker>>();
        _appLifetimeMock = new Mock<IHostApplicationLifetime>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallGenerateAndSendSummary_WhenInvoked()
    {
        // Arrange
        _summaryServiceMock
            .Setup(x => x.GenerateAndSendSummaryAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new ReportingWorker(_summaryServiceMock.Object, _loggerMock.Object, _appLifetimeMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await worker.StopAsync(cts.Token);

        // Assert
        _summaryServiceMock.Verify(x => x.GenerateAndSendSummaryAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallStopApplication_WhenCompleted()
    {
        // Arrange
        _summaryServiceMock
            .Setup(x => x.GenerateAndSendSummaryAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new ReportingWorker(_summaryServiceMock.Object, _loggerMock.Object, _appLifetimeMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await worker.StopAsync(cts.Token);

        // Assert
        _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallStopApplication_WhenExceptionThrown()
    {
        // Arrange
        _summaryServiceMock
            .Setup(x => x.GenerateAndSendSummaryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        var worker = new ReportingWorker(_summaryServiceMock.Object, _loggerMock.Object, _appLifetimeMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await worker.StopAsync(cts.Token);

        // Assert
        _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotThrow_WhenSummaryServiceFails()
    {
        // Arrange
        _summaryServiceMock
            .Setup(x => x.GenerateAndSendSummaryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        var worker = new ReportingWorker(_summaryServiceMock.Object, _loggerMock.Object, _appLifetimeMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var act = async () =>
        {
            await worker.StartAsync(cts.Token);
            await Task.Delay(100);
            await worker.StopAsync(cts.Token);
        };

        // Assert
        await act.Should().NotThrowAsync();
    }
}
