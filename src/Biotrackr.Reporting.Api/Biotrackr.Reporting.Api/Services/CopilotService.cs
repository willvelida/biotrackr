using System.Text.Json;
using Biotrackr.Reporting.Api.Configuration;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Options;

namespace Biotrackr.Reporting.Api.Services
{
    public interface ICopilotService : IAsyncDisposable
    {
        CopilotClient Client { get; }
        SessionConfig CreateSessionConfig();
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    }

    public class CopilotService(
        IOptions<Settings> settings,
        ILogger<CopilotService> logger) : ICopilotService
    {
        private static readonly string[] AllowedPermissionKinds = ["shell", "read", "write"];
        private CopilotClient? _client;

        public CopilotClient Client
        {
            get
            {
                if (_client is null)
                {
                    _client = new CopilotClient(new CopilotClientOptions
                    {
                        CliUrl = settings.Value.CopilotCliUrl
                    });
                }
                return _client;
            }
        }

        public SessionConfig CreateSessionConfig()
        {
            var config = new SessionConfig
            {
                OnPermissionRequest = HandlePermissionRequest,
            };

            var systemPrompt = settings.Value.ReportGeneratorSystemPrompt;
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                config.SystemMessage = new SystemMessageConfig
                {
                    Mode = SystemMessageMode.Append,
                    Content = systemPrompt,
                };
            }

            return config;
        }

        /// <summary>
        /// Verifies the Copilot CLI sidecar is reachable via TCP connect.
        /// </summary>
        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var uri = new Uri(settings.Value.CopilotCliUrl);
                using var tcpClient = new System.Net.Sockets.TcpClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                await tcpClient.ConnectAsync(uri.Host, uri.Port, cts.Token);
                logger.LogDebug("Copilot CLI sidecar is reachable at {CliUrl}", settings.Value.CopilotCliUrl);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Copilot CLI sidecar is not reachable at {CliUrl}", settings.Value.CopilotCliUrl);
                return false;
            }
        }

        private Task<PermissionRequestResult> HandlePermissionRequest(
            PermissionRequest request, PermissionInvocation invocation)
        {
            // Audit-log every permission request for behavioral monitoring (ASI02, ASI10)
            var invocationDetail = SafeSerialize(invocation);
            logger.LogInformation(
                "Permission request: Kind={Kind}, Invocation={Invocation}",
                request.Kind, invocationDetail);

            // Allow only shell, read, and write — deny everything else (git, web fetches, etc.)
            // The sidecar container is the primary sandbox; this is a defense-in-depth layer
            if (!AllowedPermissionKinds.Contains(request.Kind))
            {
                logger.LogWarning("Denied permission request Kind={Kind}", request.Kind);
                return Task.FromResult(new PermissionRequestResult { Kind = "denied-by-rules" });
            }

            return Task.FromResult(new PermissionRequestResult { Kind = "approved" });
        }

        private static string SafeSerialize(object obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
            }
            catch
            {
                return obj?.ToString() ?? "(null)";
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_client is not null)
            {
                await _client.DisposeAsync();
                _client = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
