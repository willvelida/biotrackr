using Biotrackr.Chat.Api.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Biotrackr.Chat.Api.Services
{
    /// <summary>
    /// Manages MCP client lifecycle — connection, reconnection, and tool listing.
    /// Registered as a singleton hosted service in DI.
    /// Uses a shared TaskCompletionSource so concurrent callers wait on one connection attempt.
    /// </summary>
    public sealed class McpToolService : IMcpToolService, IHostedService, IAsyncDisposable
    {
        private const string SubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        private readonly Settings _settings;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<McpToolService> _logger;
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

        private McpClient? _mcpClient;
        private IList<AITool> _tools = [];
        private Timer? _reconnectTimer;
        private bool _disposed;
        private TaskCompletionSource<bool> _connectionTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public McpToolService(
            IOptions<Settings> settings,
            ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _settings = settings.Value;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<McpToolService>();
        }

        public bool IsConnected { get; private set; }

        /// <summary>
        /// Returns available MCP tools. If not connected, waits for the connection
        /// to be established (up to McpStartupTimeoutSeconds). All concurrent callers
        /// share the same wait — no duplicate retry loops.
        /// </summary>
        public async Task<IList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected)
                return _tools;

            if (_disposed || string.IsNullOrWhiteSpace(_settings.McpServerUrl))
                return _tools;

            // Wait for the connection to be established (by StartAsync or reconnection timer)
            var timeout = TimeSpan.FromSeconds(_settings.McpStartupTimeoutSeconds);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                // If no connection attempt is in progress, start one
                await TryStartConnectionAsync(timeoutCts.Token);

                // Wait for the shared TCS to complete (connection established or timeout)
                await _connectionTcs.Task.WaitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timed out waiting for MCP Server connection ({TimeoutSeconds}s)", _settings.McpStartupTimeoutSeconds);
            }

            return _tools;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("McpToolService starting — will connect to MCP Server at {McpServerUrl}", _settings.McpServerUrl);

            // Attempt initial connection (non-blocking — reconnection timer handles ongoing retries)
            await TryStartConnectionAsync(cancellationToken);

            // Start periodic reconnection timer for recovery from disconnection
            _reconnectTimer = new Timer(
                callback: _ => _ = ReconnectIfNeededAsync(),
                state: null,
                dueTime: _reconnectInterval,
                period: _reconnectInterval);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("McpToolService stopping");

            if (_reconnectTimer is not null)
            {
                await _reconnectTimer.DisposeAsync();
                _reconnectTimer = null;
            }

            await DisconnectAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_reconnectTimer is not null)
            {
                await _reconnectTimer.DisposeAsync();
                _reconnectTimer = null;
            }

            await DisconnectAsync();
            _connectLock.Dispose();
        }

        /// <summary>
        /// Attempts a single connection to the MCP Server.
        /// If already connecting or connected, returns immediately.
        /// On success, completes the shared TaskCompletionSource so all waiters unblock.
        /// </summary>
        private async Task TryStartConnectionAsync(CancellationToken cancellationToken)
        {
            if (IsConnected || _disposed || string.IsNullOrWhiteSpace(_settings.McpServerUrl))
                return;

            if (!await _connectLock.WaitAsync(0, cancellationToken))
                return; // Another attempt is already in progress

            try
            {
                if (IsConnected) return; // Double-check after acquiring lock

                var transportOptions = new HttpClientTransportOptions
                {
                    Endpoint = new Uri(_settings.McpServerUrl),
                    TransportMode = HttpTransportMode.Sse,
                };

                if (!string.IsNullOrWhiteSpace(_settings.ApiSubscriptionKey))
                {
                    transportOptions.AdditionalHeaders = new Dictionary<string, string>
                    {
                        [SubscriptionKeyHeader] = _settings.ApiSubscriptionKey
                    };
                }

                var transport = new HttpClientTransport(transportOptions, _loggerFactory);
                var client = await McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: cancellationToken);

                var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
                _mcpClient = client;
                _tools = tools.ToList<AITool>();
                IsConnected = true;

                _logger.LogInformation("Connected to MCP Server — {ToolCount} tools available: {ToolNames}",
                    _tools.Count, string.Join(", ", _tools.Select(t => t.Name)));

                // Signal all waiters that connection is ready
                _connectionTcs.TrySetResult(true);
            }
            catch (OperationCanceledException)
            {
                // Caller cancelled or timeout — don't log as error
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCP Server connection attempt failed");
            }
            finally
            {
                _connectLock.Release();
            }
        }

        private async Task ReconnectIfNeededAsync()
        {
            if (IsConnected || _disposed)
                return;

            _logger.LogDebug("Attempting MCP Server reconnection");
            await TryStartConnectionAsync(CancellationToken.None);
        }

        private async Task DisconnectAsync()
        {
            await _connectLock.WaitAsync();
            try
            {
                if (_mcpClient is not null)
                {
                    await _mcpClient.DisposeAsync();
                    _mcpClient = null;
                }

                _tools = [];
                IsConnected = false;
            }
            finally
            {
                _connectLock.Release();
            }
        }
    }
}
