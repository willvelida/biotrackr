using Biotrackr.Reporting.Api.Configuration;
using Biotrackr.Reporting.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Reporting.Api.UnitTests.Services
{
    [Collection("FileSystemTests")]
    public class ScanForArtifactsShould : IDisposable
    {
        private readonly ReportGenerationService _sut;
        private const string ReportsDir = "/tmp/reports";

        public ScanForArtifactsShould()
        {
            var settings = Options.Create(new Settings
            {
                ReportGenerationEnabled = true,
                MaxConcurrentJobs = 3,
                ReportGenerationTimeoutMinutes = 10,
                MaxArtifactSizeBytes = 1024, // 1KB for testing
                CopilotCliUrl = "http://localhost:4321"
            });

            _sut = new ReportGenerationService(
                new Mock<IBlobStorageService>().Object,
                new Mock<ICopilotService>().Object,
                settings,
                new Mock<ILogger<ReportGenerationService>>().Object);

            CleanDirectory();
        }

        private static void CleanDirectory()
        {
            if (Directory.Exists(ReportsDir))
            {
                foreach (var f in Directory.GetFiles(ReportsDir))
                    File.Delete(f);
            }
            else
            {
                Directory.CreateDirectory(ReportsDir);
            }
        }

        [Fact]
        public void ReturnEmptyWhenNoArtifactFilesExist()
        {
            CleanDirectory();
            _sut.ScanForArtifacts("test-job").Should().BeEmpty();
        }

        [Fact]
        public void IncludePdfFiles()
        {
            CleanDirectory();
            File.WriteAllBytes(Path.Combine(ReportsDir, "report.pdf"), [0x25, 0x50, 0x44, 0x46]);

            var result = _sut.ScanForArtifacts("test-job");
            result.Should().ContainKey("report.pdf");
        }

        [Fact]
        public void IncludePngFiles()
        {
            CleanDirectory();
            File.WriteAllBytes(Path.Combine(ReportsDir, "chart.png"), [0x89, 0x50, 0x4E, 0x47]);

            var result = _sut.ScanForArtifacts("test-job");
            result.Should().ContainKey("chart.png");
        }

        [Fact]
        public void IncludeJpgFiles()
        {
            CleanDirectory();
            File.WriteAllBytes(Path.Combine(ReportsDir, "photo.jpg"), [0xFF, 0xD8, 0xFF]);

            var result = _sut.ScanForArtifacts("test-job");
            result.Should().ContainKey("photo.jpg");
        }

        [Fact]
        public void IncludeSvgFiles()
        {
            CleanDirectory();
            File.WriteAllText(Path.Combine(ReportsDir, "vector.svg"), "<svg></svg>");

            var result = _sut.ScanForArtifacts("test-job");
            result.Should().ContainKey("vector.svg");
        }

        [Fact]
        public void ExcludePythonScripts()
        {
            CleanDirectory();
            File.WriteAllText(Path.Combine(ReportsDir, "generate.py"), "import pandas");

            var result = _sut.ScanForArtifacts("test-job");
            result.Should().NotContainKey("generate.py");
        }

        [Fact]
        public void SkipOversizedArtifacts()
        {
            CleanDirectory();
            // MaxArtifactSizeBytes is set to 1024 (1KB)
            File.WriteAllBytes(Path.Combine(ReportsDir, "huge.pdf"), new byte[2048]);

            var result = _sut.ScanForArtifacts("test-job");
            result.Should().NotContainKey("huge.pdf");
        }

        [Fact]
        public void ReturnMultipleArtifacts()
        {
            CleanDirectory();
            File.WriteAllBytes(Path.Combine(ReportsDir, "report.pdf"), [0x25, 0x50]);
            File.WriteAllBytes(Path.Combine(ReportsDir, "chart.png"), [0x89, 0x50]);

            var result = _sut.ScanForArtifacts("test-job");
            result.Should().HaveCount(2);
        }

        public void Dispose()
        {
            CleanDirectory();
        }
    }
}
