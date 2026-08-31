using Biotrackr.Reporting.Api.Configuration;
using Biotrackr.Reporting.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Reporting.Api.UnitTests.Services
{
    public class ValidateGeneratedCodeFilesystemShould : IDisposable
    {
        private const string ReportsDirectory = "/tmp/reports";
        private readonly ReportGenerationService _sut;
        private readonly Mock<ILogger<ReportGenerationService>> _logger;

        public ValidateGeneratedCodeFilesystemShould()
        {
            _logger = new Mock<ILogger<ReportGenerationService>>();

            var settings = Options.Create(new Settings
            {
                ReportGenerationEnabled = true,
                MaxConcurrentJobs = 3,
                ReportGenerationTimeoutMinutes = 10,
                MaxArtifactSizeBytes = 50 * 1024 * 1024,
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
        public void NotThrow_WhenDirectoryDoesNotExist()
        {
            // Arrange
            if (Directory.Exists(ReportsDirectory))
                Directory.Delete(ReportsDirectory, true);

            // Act
            var act = () => _sut.ValidateGeneratedCode("test-job");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void NotLogWarning_WhenScriptIsSafe()
        {
            // Arrange
            var safeScript = "import pandas\nimport matplotlib\ndf = pandas.DataFrame()\ndf.plot()\n";
            File.WriteAllText(Path.Combine(ReportsDirectory, "generate_report.py"), safeScript);

            // Act
            _sut.ValidateGeneratedCode("test-job");

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Dangerous code pattern")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public void LogWarning_WhenScriptContainsDangerousPattern()
        {
            // Arrange
            var dangerousScript = "import os\nos.system('rm -rf /')\n";
            File.WriteAllText(Path.Combine(ReportsDirectory, "malicious.py"), dangerousScript);

            // Act
            _sut.ValidateGeneratedCode("test-job");

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Dangerous code pattern")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void LogInformation_ForEachScriptValidated()
        {
            // Arrange
            File.WriteAllText(Path.Combine(ReportsDirectory, "script1.py"), "print('hello')");
            File.WriteAllText(Path.Combine(ReportsDirectory, "script2.py"), "print('world')");

            // Act
            _sut.ValidateGeneratedCode("test-job");

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating generated script")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Exactly(2));
        }

        [Fact]
        public void IgnoreNonPythonFiles()
        {
            // Arrange
            File.WriteAllText(Path.Combine(ReportsDirectory, "data.csv"), "os.system('hack')");
            File.WriteAllBytes(Path.Combine(ReportsDirectory, "report.pdf"), new byte[] { 0x25 });

            // Act
            _sut.ValidateGeneratedCode("test-job");

            // Assert - no scripts to validate, so no information log about validation
            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating generated script")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Theory]
        [InlineData("subprocess.run(['ls'])")]
        [InlineData("import socket\nsocket.connect(('evil.com', 80))")]
        [InlineData("eval('malicious')")]
        [InlineData("exec('code')")]
        [InlineData("os.popen('whoami')")]
        public void DetectVariousDangerousPatterns_InActualFiles(string dangerousContent)
        {
            // Arrange
            File.WriteAllText(Path.Combine(ReportsDirectory, "test.py"), dangerousContent);

            // Act
            _sut.ValidateGeneratedCode("test-job");

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Defense-in-depth")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}
