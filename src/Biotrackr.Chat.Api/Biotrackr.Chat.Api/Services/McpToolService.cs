using Biotrackr.Chat.Api.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Biotrackr.Chat.Api.Services
{
    /// <summary>
    /// Manages MCP client lifecycle — connection, reconnection, and tool listing.
    /// Registered as a singleton hosted service in DI.
    /// The MCP Server Container App runs with minReplicas=1, so cold-start is not a concern.
    /// Reconnection timer handles transient failures (deployments, network blips).
    /// </summary>
    public sealed class McpToolService : IMcpToolService, IHostedService, IAsyncDisposable
    {
        private const string SubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        private readonly Settings _settings;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<McpToolService> _logger;
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(30);

        private McpClient? _mcpClient;
        private IList<AITool> _tools = [];
        private Timer? _reconnectTimer;
        private bool _disposed;

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

        public async Task<IList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected && !_disposed)
            {
                await TryConnectAsync(cancellationToken);
            }

            return _tools;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("McpToolService starting — connecting to MCP Server at {McpServerUrl}", _settings.McpServerUrl);

            await TryConnectAsync(cancellationToken);

            // Reconnection timer for recovery from transient failures (deployments, network blips)
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

        private async Task TryConnectAsync(CancellationToken cancellationToken)
        {
            if (IsConnected || _disposed || string.IsNullOrWhiteSpace(_settings.McpServerUrl))
                return;

            if (!await _connectLock.WaitAsync(0, cancellationToken))
                return; // Another attempt is already in progress

            try
            {
                if (IsConnected) return;

                var transportOptions = new HttpClientTransportOptions
                {
                    Endpoint = new Uri(_settings.McpServerUrl),
                    TransportMode = HttpTransportMode.AutoDetect,
                };

                var headers = new Dictionary<string, string>();

                if (!string.IsNullOrWhiteSpace(_settings.ApiSubscriptionKey))
                {
                    headers[SubscriptionKeyHeader] = _settings.ApiSubscriptionKey;
                }

                if (!string.IsNullOrWhiteSpace(_settings.McpServerApiKey))
                {
                    headers["X-Api-Key"] = _settings.McpServerApiKey;
                }

                if (headers.Count > 0)
                {
                    transportOptions.AdditionalHeaders = headers;
                }

                var transport = new HttpClientTransport(transportOptions, _loggerFactory);
                var client = await McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: cancellationToken);

                var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
                _mcpClient = client;
                _tools = tools.ToList<AITool>();
                IsConnected = true;

                _logger.LogInformation("Connected to MCP Server — {ToolCount} tools available: {ToolNames}",
                    _tools.Count, string.Join(", ", _tools.Select(t => t.Name)));
            }
            catch (OperationCanceledException)
            {
                // Caller cancelled — don't log as error
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
            await TryConnectAsync(CancellationToken.None);
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
