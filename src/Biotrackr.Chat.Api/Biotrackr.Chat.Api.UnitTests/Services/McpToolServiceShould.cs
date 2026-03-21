using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Chat.Api.UnitTests.Services
{
    public class McpToolServiceShould
    {
        [Fact]
        public void NotBeConnectedByDefault()
        {
            var service = CreateService(mcpServerUrl: "https://example.com/mcp");

            service.IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task ReturnEmptyToolsWhenUrlIsEmpty()
        {
            var service = CreateService(mcpServerUrl: "");

            var tools = await service.GetToolsAsync();

            tools.Should().BeEmpty();
        }

        [Fact]
        public async Task ReturnEmptyToolsWhenUrlIsNull()
        {
            var service = CreateService(mcpServerUrl: null);

            var tools = await service.GetToolsAsync();

            tools.Should().BeEmpty();
        }

        [Fact]
        public async Task ReturnEmptyToolsAfterDispose()
        {
            var service = CreateService(mcpServerUrl: "https://example.com/mcp");
            await service.DisposeAsync();

            var tools = await service.GetToolsAsync();

            tools.Should().BeEmpty();
        }

        [Fact]
        public async Task StartInDegradedModeWhenMcpServerUrlIsEmpty()
        {
            var service = CreateService(mcpServerUrl: "");

            await service.StartAsync(CancellationToken.None);

            service.IsConnected.Should().BeFalse();
            var tools = await service.GetToolsAsync();
            tools.Should().BeEmpty();
        }

        [Fact]
        public async Task StartInDegradedModeWhenMcpServerUrlIsNull()
        {
            var service = CreateService(mcpServerUrl: null);

            await service.StartAsync(CancellationToken.None);

            service.IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task StartInDegradedModeWhenMcpServerIsUnreachable()
        {
            var service = CreateService(mcpServerUrl: "https://unreachable-server.example.com/mcp");

            await service.StartAsync(CancellationToken.None);

            service.IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task TimeOutWaitingForUnreachableServer()
        {
            var service = CreateService(mcpServerUrl: "https://unreachable-server.example.com/mcp");

            var tools = await service.GetToolsAsync();

            service.IsConnected.Should().BeFalse();
            tools.Should().BeEmpty();
        }

        [Fact]
        public void ThrowWhenSettingsIsNull()
        {
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var act = () => new McpToolService(null!, loggerFactory.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("settings");
        }

        [Fact]
        public void ThrowWhenLoggerFactoryIsNull()
        {
            var settings = Options.Create(new Settings { McpServerUrl = "https://example.com/mcp" });

            var act = () => new McpToolService(settings, null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("loggerFactory");
        }

        [Fact]
        public async Task DisposeCleanly()
        {
            var service = CreateService(mcpServerUrl: "https://example.com/mcp");

            await service.DisposeAsync();

            service.IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task DisposeIdempotently()
        {
            var service = CreateService(mcpServerUrl: "https://example.com/mcp");

            await service.DisposeAsync();
            await service.DisposeAsync();

            service.IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task StopCleanly()
        {
            var service = CreateService(mcpServerUrl: "");
            await service.StartAsync(CancellationToken.None);

            await service.StopAsync(CancellationToken.None);

            service.IsConnected.Should().BeFalse();
        }

        private static McpToolService CreateService(string? mcpServerUrl, string? subscriptionKey = null)
        {
            var settings = Options.Create(new Settings
            {
                McpServerUrl = mcpServerUrl!,
                ApiSubscriptionKey = subscriptionKey!
            });
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            return new McpToolService(settings, loggerFactory.Object);
        }
    }
}
