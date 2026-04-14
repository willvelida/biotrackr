using Biotrackr.Reporting.Api.Configuration;
using FluentAssertions;

namespace Biotrackr.Reporting.Api.UnitTests.Configuration
{
    public class SettingsShould
    {
        [Fact]
        public void HaveCorrectDefaultValues()
        {
            var settings = new Settings();

            settings.ReportingBlobStorageEndpoint.Should().BeEmpty();
            settings.CopilotCliUrl.Should().Be("http://localhost:4321");
            settings.ChatApiUaiPrincipalId.Should().BeEmpty();
            settings.ChatApiAgentIdentityId.Should().BeEmpty();
            settings.ReportingApiUrl.Should().BeEmpty();
            settings.ReportGeneratorSystemPrompt.Should().BeEmpty();
            settings.ReportGenerationEnabled.Should().BeTrue();
            settings.MaxConcurrentJobs.Should().Be(3);
            settings.ReportGenerationTimeoutMinutes.Should().Be(20);
            settings.MaxArtifactSizeBytes.Should().Be(50 * 1024 * 1024);
        }

        [Fact]
        public void AllowSettingProperties()
        {
            var settings = new Settings
            {
                ReportingBlobStorageEndpoint = "https://test.blob.core.windows.net/",
                CopilotCliUrl = "http://sidecar:4321",
                ChatApiUaiPrincipalId = "principal-id",
                ChatApiAgentIdentityId = "agent-id",
                ReportingApiUrl = "https://api.example.com",
                ReportGeneratorSystemPrompt = "Test prompt",
                ReportGenerationEnabled = false,
                MaxConcurrentJobs = 5,
                ReportGenerationTimeoutMinutes = 30,
                MaxArtifactSizeBytes = 100 * 1024 * 1024
            };

            settings.ReportingBlobStorageEndpoint.Should().Be("https://test.blob.core.windows.net/");
            settings.CopilotCliUrl.Should().Be("http://sidecar:4321");
            settings.ReportGenerationEnabled.Should().BeFalse();
            settings.MaxConcurrentJobs.Should().Be(5);
            settings.ReportGenerationTimeoutMinutes.Should().Be(30);
            settings.MaxArtifactSizeBytes.Should().Be(100 * 1024 * 1024);
        }
    }
}
