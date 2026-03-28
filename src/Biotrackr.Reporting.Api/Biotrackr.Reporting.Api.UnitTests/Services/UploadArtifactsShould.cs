using Biotrackr.Reporting.Api.Configuration;
using Biotrackr.Reporting.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Reporting.Api.UnitTests.Services
{
    public class UploadArtifactsShould
    {
        private readonly Mock<IBlobStorageService> _blobStorageService;
        private readonly ReportGenerationService _sut;

        public UploadArtifactsShould()
        {
            _blobStorageService = new Mock<IBlobStorageService>();

            var settings = Options.Create(new Settings
            {
                ReportGenerationEnabled = true,
                MaxConcurrentJobs = 3,
                ReportGenerationTimeoutMinutes = 10,
                MaxArtifactSizeBytes = 50 * 1024 * 1024,
                CopilotCliUrl = "http://localhost:4321"
            });

            _sut = new ReportGenerationService(
                _blobStorageService.Object,
                new Mock<ICopilotService>().Object,
                settings,
                new Mock<ILogger<ReportGenerationService>>().Object);
        }

        [Fact]
        public async Task SeparatePdfFromCharts()
        {
            var artifacts = new Dictionary<string, byte[]>
            {
                ["report.pdf"] = [0x25, 0x50, 0x44, 0x46],
                ["steps_chart.png"] = [0x89, 0x50, 0x4E, 0x47],
                ["sleep_chart.png"] = [0x89, 0x50, 0x4E, 0x47]
            };

            await _sut.UploadArtifactsAsync("job-1", artifacts, "Summary", new { });

            _blobStorageService.Verify(b => b.UploadReportAsync(
                "job-1",
                It.Is<byte[]>(pdf => pdf.Length == 4),
                It.Is<Dictionary<string, byte[]>>(charts => charts.Count == 2),
                "Summary",
                It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task PassEmptyPdfWhenNoPdfInArtifacts()
        {
            var artifacts = new Dictionary<string, byte[]>
            {
                ["chart.png"] = [0x89, 0x50]
            };

            await _sut.UploadArtifactsAsync("job-2", artifacts, "Charts only", new { });

            _blobStorageService.Verify(b => b.UploadReportAsync(
                "job-2",
                It.Is<byte[]>(pdf => pdf.Length == 0),
                It.Is<Dictionary<string, byte[]>>(charts => charts.Count == 1),
                "Charts only",
                It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task UseDefaultSummaryWhenNull()
        {
            var artifacts = new Dictionary<string, byte[]>
            {
                ["report.pdf"] = [0x25]
            };

            await _sut.UploadArtifactsAsync("job-3", artifacts, null, new { });

            _blobStorageService.Verify(b => b.UploadReportAsync(
                "job-3",
                It.IsAny<byte[]>(),
                It.IsAny<Dictionary<string, byte[]>>(),
                "Report generated successfully.",
                It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task HandleEmptyArtifactsDictionary()
        {
            var artifacts = new Dictionary<string, byte[]>();

            await _sut.UploadArtifactsAsync("job-4", artifacts, "Empty", new { });

            _blobStorageService.Verify(b => b.UploadReportAsync(
                "job-4",
                It.Is<byte[]>(pdf => pdf.Length == 0),
                It.Is<Dictionary<string, byte[]>>(charts => charts.Count == 0),
                "Empty",
                It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task TreatSvgAndJpgAsCharts()
        {
            var artifacts = new Dictionary<string, byte[]>
            {
                ["vector.svg"] = [0x3C],
                ["photo.jpg"] = [0xFF, 0xD8]
            };

            await _sut.UploadArtifactsAsync("job-5", artifacts, "Mixed formats", new { });

            _blobStorageService.Verify(b => b.UploadReportAsync(
                "job-5",
                It.Is<byte[]>(pdf => pdf.Length == 0),
                It.Is<Dictionary<string, byte[]>>(charts => charts.Count == 2 && charts.ContainsKey("vector.svg") && charts.ContainsKey("photo.jpg")),
                "Mixed formats",
                It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task ForwardSourceDataSnapshot()
        {
            var snapshot = new { steps = 8500, date = "2026-03-28" };
            var artifacts = new Dictionary<string, byte[]>
            {
                ["report.pdf"] = [0x25]
            };

            await _sut.UploadArtifactsAsync("job-6", artifacts, "Test", snapshot);

            _blobStorageService.Verify(b => b.UploadReportAsync(
                "job-6",
                It.IsAny<byte[]>(),
                It.IsAny<Dictionary<string, byte[]>>(),
                "Test",
                snapshot), Times.Once);
        }
    }
}
