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
        private static readonly string[] AllowedTools = ["shell", "read", "write"];

        // Dangerous patterns in generated Python code (ASI05)
        internal static readonly string[] DangerousCodePatterns =
        [
            "os.system", "subprocess", "socket.", "urllib",
            "requests.", "__import__", "eval(", "exec(",
            "shutil.rmtree", "os.remove", "open('/etc",
            "open(\"/etc", "curl ", "wget ", "nc ",
            "bash ", "sh -c", "os.popen"
        ];

        private CopilotClient? _client;

        public CopilotClient Client
        {
            get
            {
                if (_client is null)
                {
                    _client = new CopilotClient(new CopilotClientOptions
                    {
                        CliUrl = settings.Value.CopilotCliUrl,
                        Telemetry = new TelemetryConfig
                        {
                            SourceName = "Biotrackr.Reporting.Api.Copilot",
                            CaptureContent = false, // Avoid logging health data in traces
                        },
                    });
                }
                return _client;
            }
        }

        public SessionConfig CreateSessionConfig()
        {
            var config = new SessionConfig
            {
                // OnPermissionRequest is required by the SDK even when using Hooks.
                // Approve all here — actual access control is enforced by OnPreToolUse hook.
                OnPermissionRequest = PermissionHandler.ApproveAll,
                Hooks = new SessionHooks
                {
                    OnPreToolUse = OnPreToolUse,
                    OnPostToolUse = OnPostToolUse,
                    OnErrorOccurred = OnErrorOccurred,
                    OnSessionStart = OnSessionStart,
                    OnSessionEnd = OnSessionEnd,
                },
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

        /// <summary>
        /// Hook: Pre-tool-use gate — replaces HandlePermissionRequest with tool-name and argument-level control (ASI02).
        /// Only allows shell, read, and write tools. File operations restricted to /tmp/reports paths.
        /// </summary>
        private Task<PreToolUseHookOutput?> OnPreToolUse(
            PreToolUseHookInput input, HookInvocation invocation)
        {
            var argsDetail = SafeSerialize(input.ToolArgs);
            logger.LogInformation(
                "Pre-tool-use: Tool={ToolName}, Args={Args}",
                input.ToolName, argsDetail);

            // Deny tools not in the allow-list (ASI02 defense-in-depth)
            if (!AllowedTools.Contains(input.ToolName))
            {
                logger.LogWarning("Denied tool execution: Tool={ToolName}", input.ToolName);
                return Task.FromResult<PreToolUseHookOutput?>(new PreToolUseHookOutput { PermissionDecision = "deny" });
            }

            // Restrict file operations to /tmp/reports paths only (ASI05 defense-in-depth)
            if (input.ToolName is "read" or "write" && input.ToolArgs is not null)
            {
                var argsJson = SafeSerialize(input.ToolArgs);
                if (!argsJson.Contains("/tmp/reports", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "Denied file operation outside /tmp/reports: Tool={ToolName}, Args={Args}",
                        input.ToolName, argsDetail);
                    return Task.FromResult<PreToolUseHookOutput?>(new PreToolUseHookOutput { PermissionDecision = "deny" });
                }
            }

            return Task.FromResult<PreToolUseHookOutput?>(new PreToolUseHookOutput { PermissionDecision = "allow" });
        }

        /// <summary>
        /// Hook: Post-tool-use — scans shell tool results for dangerous code patterns in real-time (ASI05).
        /// Returns additional context instructing the model to regenerate safely when a pattern is detected.
        /// </summary>
        private Task<PostToolUseHookOutput?> OnPostToolUse(
            PostToolUseHookInput input, HookInvocation invocation)
        {
            // Only scan shell tool results for dangerous patterns
            var toolResult = SafeSerialize(input.ToolResult);
            if (input.ToolName != "shell" || string.IsNullOrEmpty(toolResult))
            {
                return Task.FromResult<PostToolUseHookOutput?>(null);
            }

            foreach (var pattern in DangerousCodePatterns)
            {
                if (toolResult.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "Dangerous code pattern '{Pattern}' detected in tool result for Tool={ToolName} (ASI05)",
                        pattern, input.ToolName);

                    return Task.FromResult<PostToolUseHookOutput?>(new PostToolUseHookOutput
                    {
                        AdditionalContext = $"SECURITY: The generated code contains a forbidden pattern ('{pattern}'). " +
                            "Regenerate the script without using dangerous operations such as subprocess, eval, exec, " +
                            "network calls, or file operations outside /tmp/reports."
                    });
                }
            }

            return Task.FromResult<PostToolUseHookOutput?>(null);
        }

        /// <summary>
        /// Hook: Error handling — returns retry for transient errors, logs all errors for diagnostics.
        /// </summary>
        private Task<ErrorOccurredHookOutput?> OnErrorOccurred(
            ErrorOccurredHookInput input, HookInvocation invocation)
        {
            logger.LogError(
                "Copilot hook error: Context={ErrorContext}, Error={Error}",
                input.ErrorContext, input.Error);

            return Task.FromResult<ErrorOccurredHookOutput?>(new ErrorOccurredHookOutput
            {
                ErrorHandling = "retry"
            });
        }

        /// <summary>
        /// Hook: Session start — logs session lifecycle for telemetry.
        /// </summary>
        private Task<SessionStartHookOutput?> OnSessionStart(
            SessionStartHookInput input, HookInvocation invocation)
        {
            logger.LogInformation("Copilot session started: Source={Source}", input.Source);
            return Task.FromResult<SessionStartHookOutput?>(null);
        }

        /// <summary>
        /// Hook: Session end — logs session lifecycle for telemetry.
        /// </summary>
        private Task<SessionEndHookOutput?> OnSessionEnd(
            SessionEndHookInput input, HookInvocation invocation)
        {
            logger.LogInformation("Copilot session ended");
            return Task.FromResult<SessionEndHookOutput?>(null);
        }

        private static string SafeSerialize(object? obj)
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
