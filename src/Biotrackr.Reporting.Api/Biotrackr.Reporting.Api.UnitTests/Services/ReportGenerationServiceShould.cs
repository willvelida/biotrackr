using Biotrackr.Reporting.Api.Configuration;
using Biotrackr.Reporting.Api.Models;
using Biotrackr.Reporting.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Reporting.Api.UnitTests.Services
{
    public class ReportGenerationServiceShould
    {
        private readonly Mock<IBlobStorageService> _blobStorageService;
        private readonly Mock<ICopilotService> _copilotService;
        private readonly Mock<ILogger<ReportGenerationService>> _logger;

        public ReportGenerationServiceShould()
        {
            _blobStorageService = new Mock<IBlobStorageService>();
            _copilotService = new Mock<ICopilotService>();
            _logger = new Mock<ILogger<ReportGenerationService>>();
        }

        [Fact]
        public async Task ReturnFailedWhenKillSwitchIsDisabled()
        {
            var sut = CreateService(reportGenerationEnabled: false);

            var result = await sut.StartReportGenerationAsync(
                "weekly_summary", "2026-03-01", "2026-03-07", "Generate a report", new object());

            result.Status.Should().Be(ReportStatus.Failed);
            result.Message.Should().Contain("disabled");
            _copilotService.Verify(c => c.IsHealthyAsync(It.IsAny<CancellationToken>()), Times.Never);
            _blobStorageService.Verify(b => b.CreateJobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ReturnFailedWhenSidecarIsUnhealthy()
        {
            _copilotService.Setup(c => c.IsHealthyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var sut = CreateService();

            var result = await sut.StartReportGenerationAsync(
                "weekly_summary", "2026-03-01", "2026-03-07", "Generate a report", new object());

            result.Status.Should().Be(ReportStatus.Failed);
            result.Message.Should().Contain("sidecar");
        }

        [Fact]
        public async Task CreateJobAndReturnGeneratingWhenHealthy()
        {
            _copilotService.Setup(c => c.IsHealthyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _blobStorageService.Setup(b => b.CreateJobAsync("weekly_summary", "2026-03-01", "2026-03-07"))
                .ReturnsAsync("test-job-id");
            var sut = CreateService();

            var result = await sut.StartReportGenerationAsync(
                "weekly_summary", "2026-03-01", "2026-03-07", "Generate a report", new object());

            result.Status.Should().Be(ReportStatus.Generating);
            result.JobId.Should().Be("test-job-id");
            result.Message.Should().Contain("started");
        }

        [Fact]
        public async Task RejectWhenConcurrencyLimitReached()
        {
            _copilotService.Setup(c => c.IsHealthyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _blobStorageService.Setup(b => b.CreateJobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("job-id");

            // Keep the background task alive by blocking UpdateJobStatusAsync
            // (called from Task.Run's catch when GenerateReportAsync throws).
            // Without this, the background task completes instantly and releases
            // the semaphore before the second call executes — causing a flaky test.
            var backgroundGate = new TaskCompletionSource();
            _blobStorageService.Setup(b => b.UpdateJobStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(backgroundGate.Task);

            var sut = CreateService(maxConcurrentJobs: 1);

            // First call should succeed (takes the semaphore slot)
            var result1 = await sut.StartReportGenerationAsync(
                "weekly_summary", "2026-03-01", "2026-03-07", "Generate report 1", new object());
            result1.Status.Should().Be(ReportStatus.Generating);

            // Second call should fail due to concurrency limit
            var result2 = await sut.StartReportGenerationAsync(
                "weekly_summary", "2026-03-08", "2026-03-14", "Generate report 2", new object());

            result2.Status.Should().Be(ReportStatus.Failed);
            result2.Message.Should().Contain("concurrent");

            // Release the background task so it can reach finally block and clean up
            backgroundGate.SetResult();
        }

        [Fact]
        public async Task ReleaseSemaphoreOnSidecarHealthFailure()
        {
            _copilotService.Setup(c => c.IsHealthyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var sut = CreateService(maxConcurrentJobs: 1);

            // First call fails due to unhealthy sidecar, should release the semaphore
            var result1 = await sut.StartReportGenerationAsync(
                "weekly_summary", "2026-03-01", "2026-03-07", "Generate report", new object());
            result1.Status.Should().Be(ReportStatus.Failed);

            // Second call should also be able to try (semaphore was released)
            _copilotService.Setup(c => c.IsHealthyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _blobStorageService.Setup(b => b.CreateJobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("job-id");

            var result2 = await sut.StartReportGenerationAsync(
                "weekly_summary", "2026-03-01", "2026-03-07", "Generate report", new object());
            result2.Status.Should().Be(ReportStatus.Generating);
        }

        [Fact]
        public async Task PassCorrectParametersToBlobStorageService()
        {
            _copilotService.Setup(c => c.IsHealthyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _blobStorageService.Setup(b => b.CreateJobAsync("diet_analysis", "2026-01-01", "2026-03-01"))
                .ReturnsAsync("test-job");
            var sut = CreateService();

            var result = await sut.StartReportGenerationAsync(
                "diet_analysis", "2026-01-01", "2026-03-01", "Analyze diet", new { calories = 2000 });

            _blobStorageService.Verify(b => b.CreateJobAsync("diet_analysis", "2026-01-01", "2026-03-01"), Times.Once);
            result.JobId.Should().Be("test-job");
        }

        private ReportGenerationService CreateService(
            bool reportGenerationEnabled = true,
            int maxConcurrentJobs = 3,
            int timeoutMinutes = 10)
        {
            var settings = Options.Create(new Settings
            {
                ReportGenerationEnabled = reportGenerationEnabled,
                MaxConcurrentJobs = maxConcurrentJobs,
                ReportGenerationTimeoutMinutes = timeoutMinutes,
                MaxArtifactSizeBytes = 50 * 1024 * 1024,
                CopilotCliUrl = "http://localhost:4321"
            });

            return new ReportGenerationService(
                _blobStorageService.Object,
                _copilotService.Object,
                settings,
                _logger.Object);
        }
    }
}
