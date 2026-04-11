using Biotrackr.Reporting.Api.Configuration;
using Biotrackr.Reporting.Api.Services;
using FluentAssertions;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Reporting.Api.UnitTests.Services
{
    public class CopilotServiceShould
    {
        private readonly Mock<ILogger<CopilotService>> _logger;

        public CopilotServiceShould()
        {
            _logger = new Mock<ILogger<CopilotService>>();
        }

        [Fact]
        public void CreateSessionConfigWithHooks()
        {
            var sut = CreateService();

            var config = sut.CreateSessionConfig();

            config.Should().NotBeNull();
            config.Hooks.Should().NotBeNull();
            config.Hooks!.OnPreToolUse.Should().NotBeNull();
            config.Hooks.OnPostToolUse.Should().NotBeNull();
            config.Hooks.OnErrorOccurred.Should().NotBeNull();
            config.Hooks.OnSessionStart.Should().NotBeNull();
            config.Hooks.OnSessionEnd.Should().NotBeNull();
        }

        [Theory]
        [InlineData("shell")]
        [InlineData("read")]
        [InlineData("write")]
        public async Task ApproveAllowedTools(string toolName)
        {
            var sut = CreateService();
            var config = sut.CreateSessionConfig();

            var input = new PreToolUseHookInput { ToolName = toolName };
            var invocation = new HookInvocation { SessionId = "test-session" };

            var result = await config.Hooks!.OnPreToolUse!(input, invocation);

            result.Should().NotBeNull();
            result!.PermissionDecision.Should().Be("allow");
        }

        [Theory]
        [InlineData("git")]
        [InlineData("web")]
        [InlineData("network")]
        [InlineData("custom_tool")]
        [InlineData("")]
        [InlineData("admin")]
        public async Task DenyDisallowedTools(string toolName)
        {
            var sut = CreateService();
            var config = sut.CreateSessionConfig();

            var input = new PreToolUseHookInput { ToolName = toolName };
            var invocation = new HookInvocation { SessionId = "test-session" };

            var result = await config.Hooks!.OnPreToolUse!(input, invocation);

            result.Should().NotBeNull();
            result!.PermissionDecision.Should().Be("deny");
        }

        [Fact]
        public async Task ReturnFalseForHealthCheckWhenSidecarUnreachable()
        {
            // Use a port that nobody is listening on
            var sut = CreateService(copilotCliUrl: "http://localhost:19999");

            var isHealthy = await sut.IsHealthyAsync();

            isHealthy.Should().BeFalse();
        }

        [Fact]
        public async Task DisposeCleanlyWhenClientNotCreated()
        {
            var sut = CreateService();

            await sut.DisposeAsync();

            // Should not throw — no client was created
        }

        [Fact]
        public void CreateClientWithConfiguredCliUrl()
        {
            var sut = CreateService(copilotCliUrl: "http://sidecar:4321");

            var client = sut.Client;

            client.Should().NotBeNull();
        }

        [Fact]
        public void ReturnSameClientOnMultipleAccesses()
        {
            var sut = CreateService();

            var client1 = sut.Client;
            var client2 = sut.Client;

            client1.Should().BeSameAs(client2);
        }

        private CopilotService CreateService(string copilotCliUrl = "http://localhost:4321")
        {
            var settings = Options.Create(new Settings
            {
                CopilotCliUrl = copilotCliUrl,
                ReportGenerationEnabled = true,
                MaxConcurrentJobs = 3,
                ReportGenerationTimeoutMinutes = 10
            });

            return new CopilotService(settings, _logger.Object);
        }
    }
}
