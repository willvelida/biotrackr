using Biotrackr.Reporting.Api.Configuration;
using Biotrackr.Reporting.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Reporting.Api.UnitTests.Services
{
    public class ScanForArtifactsShould : IDisposable
    {
        private const string ReportsDirectory = "/tmp/reports";
        private readonly ReportGenerationService _sut;
        private readonly Mock<ILogger<ReportGenerationService>> _logger;

        public ScanForArtifactsShould()
        {
            _logger = new Mock<ILogger<ReportGenerationService>>();

            var settings = Options.Create(new Settings
            {
                ReportGenerationEnabled = true,
                MaxConcurrentJobs = 3,
                ReportGenerationTimeoutMinutes = 10,
                MaxArtifactSizeBytes = 1024, // 1KB limit for testing
                CopilotCliUrl = "http://localhost:4321"
            });

            _sut = new ReportGenerationService(
                new Mock<IBlobStorageService>().Object,
                new Mock<ICopilotService>().Object,
                settings,
                _logger.Object);

            // Ensure clean state
            if (Directory.Exists(ReportsDirectory))
            {
                foreach (var file in Directory.GetFiles(ReportsDirectory))
                    File.Delete(file);
            }
            else
            {
                Directory.CreateDirectory(ReportsDirectory);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(ReportsDirectory))
            {
                foreach (var file in Directory.GetFiles(ReportsDirectory))
                    File.Delete(file);
            }
        }

        [Fact]
        public void ReturnEmptyDictionary_WhenDirectoryDoesNotExist()
        {
            // Arrange
            if (Directory.Exists(ReportsDirectory))
                Directory.Delete(ReportsDirectory, true);

            // Act
            var result = _sut.ScanForArtifacts("test-job");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ReturnEmptyDictionary_WhenDirectoryIsEmpty()
        {
            // Arrange - directory exists but is empty

            // Act
            var result = _sut.ScanForArtifacts("test-job");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void IncludePdfFiles()
        {
            // Arrange
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            File.WriteAllBytes(Path.Combine(ReportsDirectory, "report.pdf"), pdfContent);

            // Act
            var result = _sut.ScanForArtifacts("test-job");

            // Assert
            result.Should().ContainKey("report.pdf");
            result["report.pdf"].Should().BeEquivalentTo(pdfContent);
        }

        [Fact]
        public void IncludePngFiles()
        {
            // Arrange
            var pngContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            File.WriteAllBytes(Path.Combine(ReportsDirectory, "chart.png"), pngContent);

            // Act
            var result = _sut.ScanForArtifacts("test-job");

            // Assert
            result.Should().ContainKey("chart.png");
            result["chart.png"].Should().BeEquivalentTo(pngContent);
        }

        [Fact]
        public void IncludeJpgFiles()
        {
            // Arrange
            var jpgContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
            File.WriteAllBytes(Path.Combine(ReportsDirectory, "photo.jpg"), jpgContent);

            // Act
            var result = _sut.ScanForArtifacts("test-job");

            // Assert
            result.Should().ContainKey("photo.jpg");
        }

        [Fact]
        public void IncludeSvgFiles()
        {
            // Arrange
            var svgContent = "<svg></svg>"u8.ToArray();
            File.WriteAllBytes(Path.Combine(ReportsDirectory, "vector.svg"), svgContent);

            // Act
            var result = _sut.ScanForArtifacts("test-job");

            // Assert
            result.Should().ContainKey("vector.svg");
        }

        [Fact]
        public void ExcludePythonScripts()
        {
            // Arrange
            File.WriteAllText(Path.Combine(ReportsDirectory, "generate.py"), "print('hello')");
            File.WriteAllBytes(Path.Combine(ReportsDirectory, "report.pdf"), new byte[] { 0x25 });

            // Act
            var result = _sut.ScanForArtifacts("test-job");

            // Assert
            result.Should().NotContainKey("generate.py");
            result.Should().ContainKey("report.pdf");
        }

        [Fact]
        public void ExcludeOversizedArtifacts()
        {
            // Arrange - MaxArtifactSizeBytes is 1024 in our test settings
            var oversizedContent = new byte[2048];
            File.WriteAllBytes(Path.Combine(ReportsDirectory, "huge.pdf"), oversizedContent);

            // Act
            var result = _sut.ScanForArtifacts("test-job");

            // Assert
            result.Should().NotContainKey("huge.pdf");
        }

        [Fact]
        public void LogWarning_WhenArtifactExceedsSizeLimit()
        {
            // Arrange
            var oversizedContent = new byte[2048];
            File.WriteAllBytes(Path.Combine(ReportsDirectory, "large.png"), oversizedContent);

            // Act
            _sut.ScanForArtifacts("test-job");

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anomalous artifact size")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogWarning_WhenUnexpectedFileTypeFound()
        {
            // Arrange
            File.WriteAllText(Path.Combine(ReportsDirectory, "data.csv"), "a,b,c");

            // Act
            _sut.ScanForArtifacts("test-job");

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected file type")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void ReturnMultipleArtifacts_WhenMixedTypesPresent()
        {
            // Arrange
            File.WriteAllBytes(Path.Combine(ReportsDirectory, "report.pdf"), new byte[] { 0x25, 0x50 });
            File.WriteAllBytes(Path.Combine(ReportsDirectory, "steps.png"), new byte[] { 0x89, 0x50 });
            File.WriteAllBytes(Path.Combine(ReportsDirectory, "sleep.svg"), "<svg/>"u8.ToArray());
            File.WriteAllText(Path.Combine(ReportsDirectory, "script.py"), "import pandas");

            // Act
            var result = _sut.ScanForArtifacts("test-job");

            // Assert
            result.Should().HaveCount(3);
            result.Should().ContainKeys("report.pdf", "steps.png", "sleep.svg");
        }
    }
}
