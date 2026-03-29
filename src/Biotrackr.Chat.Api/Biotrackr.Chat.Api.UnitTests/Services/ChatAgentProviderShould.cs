using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Chat.Api.UnitTests.Services
{
    public class ChatAgentProviderShould
    {
        [Fact]
        public async Task BuildAgentWithZeroToolsWhenMcpNotConnected()
        {
            var provider = CreateProvider(mcpTools: []);

            var agent = await provider.GetAgentAsync();

            agent.Should().NotBeNull();
        }

        [Fact]
        public async Task ReturnCachedAgentOnSubsequentCalls()
        {
            var provider = CreateProvider(mcpTools: []);

            var agent1 = await provider.GetAgentAsync();
            var agent2 = await provider.GetAgentAsync();

            agent1.Should().BeSameAs(agent2);
        }

        [Fact]
        public async Task RebuildAgentWhenToolCountChanges()
        {
            var toolService = new Mock<IMcpToolService>();
            var firstCall = true;
            toolService.Setup(s => s.GetToolsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (firstCall)
                    {
                        firstCall = false;
                        return new List<AITool>();
                    }
                    return new List<AITool>
                    {
                        AIFunctionFactory.Create(() => "test", name: "TestTool")
                    };
                });

            var provider = CreateProvider(toolService.Object);

            var agent1 = await provider.GetAgentAsync();
            var agent2 = await provider.GetAgentAsync();

            agent1.Should().NotBeSameAs(agent2);
        }

        [Fact]
        public void ThrowWhenMcpToolServiceIsNull()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var loggerFactory = CreateLoggerFactory();
            var settings = Options.Create(CreateSettings());
            var serviceProvider = new Mock<IServiceProvider>().Object;

            var act = () => new ChatAgentProvider(null!, cache, loggerFactory, settings, serviceProvider);

            act.Should().Throw<ArgumentNullException>().WithParameterName("mcpToolService");
        }

        [Fact]
        public void ThrowWhenCacheIsNull()
        {
            var toolService = new Mock<IMcpToolService>().Object;
            var loggerFactory = CreateLoggerFactory();
            var settings = Options.Create(CreateSettings());
            var serviceProvider = new Mock<IServiceProvider>().Object;

            var act = () => new ChatAgentProvider(toolService, null!, loggerFactory, settings, serviceProvider);

            act.Should().Throw<ArgumentNullException>().WithParameterName("memoryCache");
        }

        [Fact]
        public void ThrowWhenLoggerFactoryIsNull()
        {
            var toolService = new Mock<IMcpToolService>().Object;
            var cache = new MemoryCache(new MemoryCacheOptions());
            var settings = Options.Create(CreateSettings());
            var serviceProvider = new Mock<IServiceProvider>().Object;

            var act = () => new ChatAgentProvider(toolService, cache, null!, settings, serviceProvider);

            act.Should().Throw<ArgumentNullException>().WithParameterName("loggerFactory");
        }

        [Fact]
        public void ThrowWhenSettingsIsNull()
        {
            var toolService = new Mock<IMcpToolService>().Object;
            var cache = new MemoryCache(new MemoryCacheOptions());
            var loggerFactory = CreateLoggerFactory();
            var serviceProvider = new Mock<IServiceProvider>().Object;

            var act = () => new ChatAgentProvider(toolService, cache, loggerFactory, null!, serviceProvider);

            act.Should().Throw<ArgumentNullException>().WithParameterName("settings");
        }

        [Fact]
        public void ThrowWhenServiceProviderIsNull()
        {
            var toolService = new Mock<IMcpToolService>().Object;
            var cache = new MemoryCache(new MemoryCacheOptions());
            var loggerFactory = CreateLoggerFactory();
            var settings = Options.Create(CreateSettings());

            var act = () => new ChatAgentProvider(toolService, cache, loggerFactory, settings, null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
        }

        private static ChatAgentProvider CreateProvider(IList<AITool> mcpTools)
        {
            var toolService = new Mock<IMcpToolService>();
            toolService.Setup(s => s.GetToolsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mcpTools);

            return CreateProvider(toolService.Object);
        }

        private static ChatAgentProvider CreateProvider(IMcpToolService toolService)
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var loggerFactory = CreateLoggerFactory();
            var settings = Options.Create(CreateSettings());

            var chatHistoryRepo = new Mock<IChatHistoryRepository>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(IChatHistoryRepository)))
                .Returns(chatHistoryRepo.Object);
            serviceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<AIFunction>)))
                .Returns(Enumerable.Empty<AIFunction>());

            return new ChatAgentProvider(toolService, cache, loggerFactory, settings, serviceProvider.Object);
        }

        private static Settings CreateSettings()
        {
            return new Settings
            {
                AnthropicApiKey = "test-key",
                ChatAgentModel = "claude-sonnet-4-6",
                ChatSystemPrompt = "You are a test agent.",
                ToolCallBudgetPerSession = 20
            };
        }

        private static ILoggerFactory CreateLoggerFactory()
        {
            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            return factory.Object;
        }
    }
}
