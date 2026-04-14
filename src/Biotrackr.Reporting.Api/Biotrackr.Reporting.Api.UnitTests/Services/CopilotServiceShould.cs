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

        [Fact]
        public async Task AllowReadToolWithTmpReportsPath()
        {
            var sut = CreateService();
            var config = sut.CreateSessionConfig();

            var input = new PreToolUseHookInput
            {
                ToolName = "read",
                ToolArgs = new Dictionary<string, object> { ["path"] = "/tmp/reports/output.pdf" }
            };
            var invocation = new HookInvocation { SessionId = "test-session" };

            var result = await config.Hooks!.OnPreToolUse!(input, invocation);

            result.Should().NotBeNull();
            result!.PermissionDecision.Should().Be("allow");
        }

        [Fact]
        public async Task DenyWriteToolOutsideTmpReports()
        {
            var sut = CreateService();
            var config = sut.CreateSessionConfig();

            var input = new PreToolUseHookInput
            {
                ToolName = "write",
                ToolArgs = new Dictionary<string, object> { ["path"] = "/etc/passwd" }
            };
            var invocation = new HookInvocation { SessionId = "test-session" };

            var result = await config.Hooks!.OnPreToolUse!(input, invocation);

            result.Should().NotBeNull();
            result!.PermissionDecision.Should().Be("deny");
        }

        [Fact]
        public async Task DetectDangerousPatternInPostToolUse()
        {
            var sut = CreateService();
            var config = sut.CreateSessionConfig();

            var input = new PostToolUseHookInput
            {
                ToolName = "shell",
                ToolResult = "import subprocess; subprocess.run(['ls'])"
            };
            var invocation = new HookInvocation { SessionId = "test-session" };

            var result = await config.Hooks!.OnPostToolUse!(input, invocation);

            result.Should().NotBeNull();
            result!.AdditionalContext.Should().Contain("SECURITY");
            result.AdditionalContext.Should().Contain("subprocess");
        }

        [Fact]
        public async Task AllowSafeCodeInPostToolUse()
        {
            var sut = CreateService();
            var config = sut.CreateSessionConfig();

            var input = new PostToolUseHookInput
            {
                ToolName = "shell",
                ToolResult = "import pandas as pd\ndf = pd.read_csv('/tmp/reports/data.csv')"
            };
            var invocation = new HookInvocation { SessionId = "test-session" };

            var result = await config.Hooks!.OnPostToolUse!(input, invocation);

            result.Should().BeNull();
        }

        [Fact]
        public async Task ReturnNullForNonShellToolInPostToolUse()
        {
            var sut = CreateService();
            var config = sut.CreateSessionConfig();

            var input = new PostToolUseHookInput
            {
                ToolName = "read",
                ToolResult = "file contents with subprocess pattern"
            };
            var invocation = new HookInvocation { SessionId = "test-session" };

            var result = await config.Hooks!.OnPostToolUse!(input, invocation);

            result.Should().BeNull();
        }

        [Fact]
        public async Task ReturnRetryOnErrorOccurred()
        {
            var sut = CreateService();
            var config = sut.CreateSessionConfig();

            var input = new ErrorOccurredHookInput
            {
                ErrorContext = "tool_execution",
                Error = "Connection timeout"
            };
            var invocation = new HookInvocation { SessionId = "test-session" };

            var result = await config.Hooks!.OnErrorOccurred!(input, invocation);

            result.Should().NotBeNull();
            result!.ErrorHandling.Should().Be("retry");
        }

        [Fact]
        public async Task ReturnNullOnSessionStart()
        {
            var sut = CreateService();
            var config = sut.CreateSessionConfig();

            var input = new SessionStartHookInput { Source = "new" };
            var invocation = new HookInvocation { SessionId = "test-session" };

            var result = await config.Hooks!.OnSessionStart!(input, invocation);

            result.Should().BeNull();
        }

        [Fact]
        public async Task ReturnNullOnSessionEnd()
        {
            var sut = CreateService();
            var config = sut.CreateSessionConfig();

            var input = new SessionEndHookInput { Reason = "completed" };
            var invocation = new HookInvocation { SessionId = "test-session" };

            var result = await config.Hooks!.OnSessionEnd!(input, invocation);

            result.Should().BeNull();
        }

        [Fact]
        public async Task AllowReadAgentWithinPollLimit()
        {
            // Arrange
            var sut = CreateService();
            var config = sut.CreateSessionConfig();
            var invocation = new HookInvocation { SessionId = "test-session" };

            // Simulate session start to reset counters
            await config.Hooks!.OnSessionStart!(new SessionStartHookInput { Source = "new" }, invocation);

            // Act — poll read_agent up to the limit (5 times)
            PreToolUseHookOutput? lastResult = null;
            for (var i = 0; i < CopilotService.MaxReadAgentPollsPerAgent; i++)
            {
                var input = new PreToolUseHookInput
                {
                    ToolName = "read_agent",
                    ToolArgs = new Dictionary<string, object> { ["agent_id"] = "pdf-build", ["timeout"] = 60 }
                };
                lastResult = await config.Hooks.OnPreToolUse!(input, invocation);
            }

            // Assert — all calls within the limit should be allowed
            lastResult.Should().NotBeNull();
            lastResult!.PermissionDecision.Should().Be("allow");
        }

        [Fact]
        public async Task DenyReadAgentAfterExceedingPollLimit()
        {
            // Arrange
            var sut = CreateService();
            var config = sut.CreateSessionConfig();
            var invocation = new HookInvocation { SessionId = "test-session" };

            await config.Hooks!.OnSessionStart!(new SessionStartHookInput { Source = "new" }, invocation);

            // Exhaust the poll limit
            for (var i = 0; i < CopilotService.MaxReadAgentPollsPerAgent; i++)
            {
                var input = new PreToolUseHookInput
                {
                    ToolName = "read_agent",
                    ToolArgs = new Dictionary<string, object> { ["agent_id"] = "pdf-build", ["timeout"] = 60 }
                };
                await config.Hooks.OnPreToolUse!(input, invocation);
            }

            // Act — one more poll beyond the limit
            var overLimitInput = new PreToolUseHookInput
            {
                ToolName = "read_agent",
                ToolArgs = new Dictionary<string, object> { ["agent_id"] = "pdf-build", ["timeout"] = 60 }
            };
            var result = await config.Hooks.OnPreToolUse!(overLimitInput, invocation);

            // Assert
            result.Should().NotBeNull();
            result!.PermissionDecision.Should().Be("deny");
        }

        [Fact]
        public async Task ResetReadAgentPollCountOnSessionStart()
        {
            // Arrange
            var sut = CreateService();
            var config = sut.CreateSessionConfig();
            var invocation = new HookInvocation { SessionId = "test-session" };

            await config.Hooks!.OnSessionStart!(new SessionStartHookInput { Source = "new" }, invocation);

            // Exhaust the poll limit
            for (var i = 0; i < CopilotService.MaxReadAgentPollsPerAgent; i++)
            {
                var input = new PreToolUseHookInput
                {
                    ToolName = "read_agent",
                    ToolArgs = new Dictionary<string, object> { ["agent_id"] = "pdf-build", ["timeout"] = 60 }
                };
                await config.Hooks.OnPreToolUse!(input, invocation);
            }

            // Act — start a new session to reset counters
            await config.Hooks.OnSessionStart!(new SessionStartHookInput { Source = "new" }, invocation);

            var postResetInput = new PreToolUseHookInput
            {
                ToolName = "read_agent",
                ToolArgs = new Dictionary<string, object> { ["agent_id"] = "pdf-build", ["timeout"] = 60 }
            };
            var result = await config.Hooks.OnPreToolUse!(postResetInput, invocation);

            // Assert — first call after reset should be allowed
            result.Should().NotBeNull();
            result!.PermissionDecision.Should().Be("allow");
        }

        [Fact]
        public async Task TrackReadAgentPollsPerAgentIndependently()
        {
            // Arrange
            var sut = CreateService();
            var config = sut.CreateSessionConfig();
            var invocation = new HookInvocation { SessionId = "test-session" };

            await config.Hooks!.OnSessionStart!(new SessionStartHookInput { Source = "new" }, invocation);

            // Exhaust the poll limit for one agent
            for (var i = 0; i < CopilotService.MaxReadAgentPollsPerAgent; i++)
            {
                var input = new PreToolUseHookInput
                {
                    ToolName = "read_agent",
                    ToolArgs = new Dictionary<string, object> { ["agent_id"] = "pdf-build", ["timeout"] = 60 }
                };
                await config.Hooks.OnPreToolUse!(input, invocation);
            }

            // Act — poll a different agent
            var differentAgentInput = new PreToolUseHookInput
            {
                ToolName = "read_agent",
                ToolArgs = new Dictionary<string, object> { ["agent_id"] = "data-analyst", ["timeout"] = 60 }
            };
            var result = await config.Hooks.OnPreToolUse!(differentAgentInput, invocation);

            // Assert — different agent should still be allowed
            result.Should().NotBeNull();
            result!.PermissionDecision.Should().Be("allow");
        }

        [Fact]
        public void ExtractJsonField_ShouldReturnValue_WhenFieldExists()
        {
            // Arrange
            var json = "{\"agent_id\":\"pdf-build\",\"timeout\":60}";

            // Act
            var result = CopilotService.ExtractJsonField(json, "agent_id");

            // Assert
            result.Should().Be("pdf-build");
        }

        [Fact]
        public void ExtractJsonField_ShouldReturnNull_WhenFieldDoesNotExist()
        {
            // Arrange
            var json = "{\"timeout\":60}";

            // Act
            var result = CopilotService.ExtractJsonField(json, "agent_id");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractJsonField_ShouldReturnNull_WhenJsonIsInvalid()
        {
            // Arrange
            var json = "not valid json";

            // Act
            var result = CopilotService.ExtractJsonField(json, "agent_id");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void IncludeSystemPromptWhenConfigured()
        {
            var sut = CreateService(systemPrompt: "You are a report generator.");

            var config = sut.CreateSessionConfig();

            config.SystemMessage.Should().NotBeNull();
            config.SystemMessage!.Mode.Should().Be(SystemMessageMode.Append);
            config.SystemMessage.Content.Should().Be("You are a report generator.");
        }

        [Fact]
        public void ExcludeSystemPromptWhenNotConfigured()
        {
            var sut = CreateService();

            var config = sut.CreateSessionConfig();

            config.SystemMessage.Should().BeNull();
        }

        [Fact]
        public void CreateSessionConfig_ShouldIncludeThreeCustomAgents_WhenCalled()
        {
            // Arrange
            var sut = CreateService();

            // Act
            var config = sut.CreateSessionConfig();

            // Assert
            config.CustomAgents.Should().NotBeNull();
            config.CustomAgents.Should().HaveCount(3);
            config.CustomAgents!.Select(a => a.Name).Should().BeEquivalentTo(
                ["data-analyst", "chart-generator", "pdf-builder"]);
            config.CustomAgents.Should().AllSatisfy(a =>
            {
                a.Prompt.Should().NotBeNullOrWhiteSpace();
                a.Description.Should().NotBeNullOrWhiteSpace();
                a.DisplayName.Should().NotBeNullOrWhiteSpace();
            });
        }

        [Fact]
        public void CreateSessionConfig_ShouldIncludeSkillDirectories_WhenCalled()
        {
            // Arrange
            var sut = CreateService();

            // Act
            var config = sut.CreateSessionConfig();

            // Assert
            config.SkillDirectories.Should().NotBeNull();
            config.SkillDirectories.Should().HaveCount(3);
            config.SkillDirectories.Should().AllSatisfy(d => d.Should().StartWith("/app/skills/"));
        }

        private CopilotService CreateService(
            string copilotCliUrl = "http://localhost:4321",
            string? systemPrompt = null)
        {
            var settings = Options.Create(new Settings
            {
                CopilotCliUrl = copilotCliUrl,
                ReportGenerationEnabled = true,
                MaxConcurrentJobs = 3,
                ReportGenerationTimeoutMinutes = 10,
                ReportGeneratorSystemPrompt = systemPrompt ?? string.Empty
            });

            return new CopilotService(settings, _logger.Object);
        }
    }
}
