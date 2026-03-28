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

            // Ensure clean state
            if (Directory.Exists(ReportsDir))
            {
                foreach (var f in Directory.GetFiles(ReportsDir, "scantest_*"))
                    File.Delete(f);
            }
            else
            {
                Directory.CreateDirectory(ReportsDir);
            }
        }

        [Fact]
        public void ReturnEmptyWhenDirectoryDoesNotExist()
        {
            // Temporarily remove directory
            var tempBackup = ReportsDir + "_backup_" + Guid.NewGuid().ToString("N");
            if (Directory.Exists(ReportsDir))
            {
                Directory.Move(ReportsDir, tempBackup);
            }

            try
            {
                _sut.ScanForArtifacts("test-job").Should().BeEmpty();
            }
            finally
            {
                if (Directory.Exists(tempBackup))
                    Directory.Move(tempBackup, ReportsDir);
            }
        }

        [Fact]
        public void IncludePdfFiles()
        {
            var filePath = Path.Combine(ReportsDir, "scantest_report.pdf");
            File.WriteAllBytes(filePath, new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF

            try
            {
                var result = _sut.ScanForArtifacts("test-job");
                result.Should().ContainKey("scantest_report.pdf");
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void IncludePngFiles()
        {
            var filePath = Path.Combine(ReportsDir, "scantest_chart.png");
            File.WriteAllBytes(filePath, new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header

            try
            {
                var result = _sut.ScanForArtifacts("test-job");
                result.Should().ContainKey("scantest_chart.png");
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void IncludeJpgFiles()
        {
            var filePath = Path.Combine(ReportsDir, "scantest_photo.jpg");
            File.WriteAllBytes(filePath, new byte[] { 0xFF, 0xD8, 0xFF });

            try
            {
                var result = _sut.ScanForArtifacts("test-job");
                result.Should().ContainKey("scantest_photo.jpg");
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void IncludeSvgFiles()
        {
            var filePath = Path.Combine(ReportsDir, "scantest_vector.svg");
            File.WriteAllText(filePath, "<svg></svg>");

            try
            {
                var result = _sut.ScanForArtifacts("test-job");
                result.Should().ContainKey("scantest_vector.svg");
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void ExcludePythonScripts()
        {
            var filePath = Path.Combine(ReportsDir, "scantest_generate.py");
            File.WriteAllText(filePath, "import pandas");

            try
            {
                var result = _sut.ScanForArtifacts("test-job");
                result.Should().NotContainKey("scantest_generate.py");
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void SkipOversizedArtifacts()
        {
            // MaxArtifactSizeBytes is set to 1024 (1KB)
            var filePath = Path.Combine(ReportsDir, "scantest_huge.pdf");
            File.WriteAllBytes(filePath, new byte[2048]); // 2KB > 1KB limit

            try
            {
                var result = _sut.ScanForArtifacts("test-job");
                result.Should().NotContainKey("scantest_huge.pdf");
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void ReturnMultipleArtifacts()
        {
            var pdf = Path.Combine(ReportsDir, "scantest_report.pdf");
            var png = Path.Combine(ReportsDir, "scantest_chart.png");
            File.WriteAllBytes(pdf, new byte[] { 0x25, 0x50 });
            File.WriteAllBytes(png, new byte[] { 0x89, 0x50 });

            try
            {
                var result = _sut.ScanForArtifacts("test-job");
                result.Should().HaveCount(2);
                result.Should().ContainKey("scantest_report.pdf");
                result.Should().ContainKey("scantest_chart.png");
            }
            finally
            {
                File.Delete(pdf);
                File.Delete(png);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(ReportsDir))
            {
                foreach (var f in Directory.GetFiles(ReportsDir, "scantest_*"))
                    File.Delete(f);
            }
        }
    }
}
