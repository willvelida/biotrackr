using Anthropic;
using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Middleware;
using Biotrackr.Chat.Api.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Biotrackr.Chat.Api.Services
{
    /// <summary>
    /// Builds and caches the AIAgent with middleware pipeline.
    /// Rebuilds the agent when MCP tools become available after a degraded start.
    /// Thread-safe: concurrent callers wait on the same build operation.
    /// </summary>
    public sealed class ChatAgentProvider
    {
        private readonly IMcpToolService _mcpToolService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Settings _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly SemaphoreSlim _buildLock = new(1, 1);

        private volatile AIAgent? _currentAgent;
        private int _lastToolCount;

        public ChatAgentProvider(
            IMcpToolService mcpToolService,
            IMemoryCache memoryCache,
            ILoggerFactory loggerFactory,
            IOptions<Settings> settings,
            IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(mcpToolService);
            ArgumentNullException.ThrowIfNull(memoryCache);
            ArgumentNullException.ThrowIfNull(loggerFactory);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _mcpToolService = mcpToolService;
            _memoryCache = memoryCache;
            _loggerFactory = loggerFactory;
            _settings = settings.Value;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Returns the current agent, rebuilding it if MCP tools have changed
        /// (e.g., MCP Server came online after a degraded start).
        /// </summary>
        public async Task<AIAgent> GetAgentAsync(CancellationToken cancellationToken = default)
        {
            var tools = await _mcpToolService.GetToolsAsync(cancellationToken);
            var toolCount = tools.Count;

            // Fast path: agent exists and tool count hasn't changed
            if (_currentAgent is not null && toolCount == _lastToolCount)
            {
                return _currentAgent;
            }

            await _buildLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check after acquiring lock
                if (_currentAgent is not null && toolCount == _lastToolCount)
                {
                    return _currentAgent;
                }

                var logger = _loggerFactory.CreateLogger("ChatAgentProvider");
                logger.LogInformation("Building agent with {ToolCount} tools (previous: {PreviousToolCount})", toolCount, _lastToolCount);

                _currentAgent = BuildAgent(tools);
                _lastToolCount = toolCount;

                return _currentAgent;
            }
            finally
            {
                _buildLock.Release();
            }
        }

        /// <summary>
        /// Resolves the latest agent and streams its response.
        /// Used as the delegate for the dynamic agent wrapper in Program.cs.
        /// </summary>
        public async IAsyncEnumerable<AgentResponseUpdate> RunStreamingWithLatestAgentAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session,
            AgentRunOptions? options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var currentAgent = await GetAgentAsync(cancellationToken);
            await foreach (var update in currentAgent.RunStreamingAsync(messages, session, options, cancellationToken))
            {
                yield return update;
            }
        }

        private AIAgent BuildAgent(IList<AITool> mcpTools)
        {
            var cachingLogger = _loggerFactory.CreateLogger("CachingMcpToolWrapper");
            var wrappedTools = mcpTools
                .Select(tool => CachingMcpToolWrapper.Wrap(tool, _memoryCache, cachingLogger))
                .ToList();

            // Add native report tools (RequestReport, GetReportStatus)
            var reportTools = _serviceProvider.GetServices<AIFunction>()
                .Where(f => f.Name is "RequestReport" or "GetReportStatus");
            wrappedTools.AddRange(reportTools);

            AnthropicClient anthropicClient = new() { ApiKey = _settings.AnthropicApiKey };

            ChatClientAgent chatAgent = anthropicClient.AsAIAgent(
                model: _settings.ChatAgentModel,
                name: "BiotrackrChatAgent",
                instructions: _settings.ChatSystemPrompt,
                tools: [.. wrappedTools]);

            // Enable concurrent tool execution — Claude batches parallel tool calls,
            // but the framework executes them sequentially by default
            var functionInvoker = chatAgent.ChatClient.GetService<FunctionInvokingChatClient>();
            if (functionInvoker is not null)
            {
                functionInvoker.AllowConcurrentInvocation = true;
            }

            // Middleware pipeline
            var chatHistoryRepository = _serviceProvider.GetRequiredService<IChatHistoryRepository>();
            var persistenceLogger = _loggerFactory.CreateLogger<ConversationPersistenceMiddleware>();
            var conversationPolicyOptions = Options.Create(new ConversationPolicyOptions());
            var persistenceMiddleware = new ConversationPersistenceMiddleware(chatHistoryRepository, conversationPolicyOptions, persistenceLogger);

            var toolPolicyOptions = Options.Create(new ToolPolicyOptions
            {
                MaxToolCallsPerSession = _settings.ToolCallBudgetPerSession
            });
            var toolPolicyLogger = _loggerFactory.CreateLogger<ToolPolicyMiddleware>();
            var toolPolicyMiddleware = new ToolPolicyMiddleware(_memoryCache, toolPolicyOptions, toolPolicyLogger);

            var degradationLogger = _loggerFactory.CreateLogger<GracefulDegradationMiddleware>();
            var degradationMiddleware = new GracefulDegradationMiddleware(degradationLogger);

            return chatAgent
                .AsBuilder()
                    .Use(runFunc: null, runStreamingFunc: toolPolicyMiddleware.HandleAsync)
                    .Use(runFunc: null, runStreamingFunc: persistenceMiddleware.HandleAsync)
                    .Use(runFunc: null, runStreamingFunc: degradationMiddleware.HandleAsync)
                .Build();
        }
    }
}
