using Biotrackr.Chat.Api.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Biotrackr.Chat.Api.Services
{
    /// <summary>
    /// Manages MCP client lifecycle — connection, reconnection, and tool listing.
    /// Registered as a singleton hosted service in DI.
    /// </summary>
    public sealed class McpToolService : IMcpToolService, IHostedService, IAsyncDisposable
    {
        private const string SubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        private readonly Settings _settings;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<McpToolService> _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

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
            _logger.LogInformation("McpToolService starting — attempting connection to MCP Server at {McpServerUrl}", _settings.McpServerUrl);

            await TryConnectAsync(cancellationToken);

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
            _lock.Dispose();
        }

        private async Task TryConnectAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_settings.McpServerUrl))
            {
                _logger.LogWarning("McpServerUrl is not configured — starting in degraded mode");
                return;
            }

            var startupTimeout = TimeSpan.FromSeconds(_settings.McpStartupTimeoutSeconds);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(startupTimeout);

            var attempt = 0;
            while (!timeoutCts.Token.IsCancellationRequested)
            {
                attempt++;
                await _lock.WaitAsync(timeoutCts.Token);
                try
                {
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
                    var client = await McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: timeoutCts.Token);

                    var tools = await client.ListToolsAsync(cancellationToken: timeoutCts.Token);
                    _mcpClient = client;
                    _tools = tools.ToList<AITool>();
                    IsConnected = true;

                    _logger.LogInformation("Connected to MCP Server on attempt {Attempt} — {ToolCount} tools available: {ToolNames}",
                        attempt, _tools.Count, string.Join(", ", _tools.Select(t => t.Name)));
                    return;
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MCP Server connection attempt {Attempt} failed (timeout: {TimeoutSeconds}s)", attempt, _settings.McpStartupTimeoutSeconds);
                }
                finally
                {
                    _lock.Release();
                }

                try
                {
                    await Task.Delay(RetryDelay, timeoutCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogWarning("MCP Server not available after {TimeoutSeconds}s ({Attempts} attempts) — starting in degraded mode", _settings.McpStartupTimeoutSeconds, attempt);
            IsConnected = false;
            _tools = [];
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
            await _lock.WaitAsync();
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
                _lock.Release();
            }
        }
    }
}
