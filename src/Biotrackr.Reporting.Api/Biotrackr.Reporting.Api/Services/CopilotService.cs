using System.Collections.Concurrent;
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
        Task WarmUpSidecarAsync(CancellationToken cancellationToken = default);
    }

    public class CopilotService(
        IOptions<Settings> settings,
        ILogger<CopilotService> logger) : ICopilotService
    {
        // Copilot CLI tool names allowed for report generation (ASI02)
        // Note: These are actual tool names (bash, create, etc.), not permission kinds (shell, read, write).
        // The OnPermissionRequest handler uses kinds; OnPreToolUse hooks use tool names.
        private static readonly string[] AllowedTools =
        [
            "bash", "shell",           // Command execution
            "create", "edit", "write", // File creation/modification
            "read", "view",            // File reading
            "glob", "grep",            // File search/discovery
            "task",                    // Sub-task/sub-agent delegation
            "read_agent",              // Read sub-agent results
            "skill",                   // Load SKILL.md content into session
            "report_intent",           // CLI internal intent tracking
        ];

        // Dangerous patterns in generated Python code (ASI05)
        internal static readonly string[] DangerousCodePatterns =
        [
            "os.system", "subprocess", "socket.", "urllib",
            "requests.", "__import__", "eval(", "exec(",
            "shutil.rmtree", "os.remove", "open('/etc",
            "open(\"/etc", "curl ", "wget ", "nc ",
            "bash ", "sh -c", "os.popen"
        ];

        // Maximum read_agent polls per agent before denying to break infinite loops (ASI08)
        // Each poll waits ~60s; 8 polls ≈ 8 min per sub-agent — enough for legitimate runs,
        // short enough to break stuck loops well before the 19-min session timeout.
        internal const int MaxReadAgentPollsPerAgent = 8;

        private readonly ConcurrentDictionary<string, int> _readAgentPollCounts = new();
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
                Model = "claude-opus-4.6",
                ReasoningEffort = "high",
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

            // Sub-agent specialization — each agent has a focused role and prompt
            config.CustomAgents =
            [
                new CustomAgentConfig
                {
                    Name = "data-analyst",
                    DisplayName = "Health Data Analyst",
                    Description = "Analyzes health and fitness data using pandas. Computes daily metrics, " +
                        "weekly averages, goal achievement, trend analysis, and identifies standout days.",
                    Prompt = "You are a health data analyst specializing in fitness tracking data. " +
                        "Use pandas to analyze the provided health data JSON. Calculate daily metrics, " +
                        "weekly averages, goal achievement (steps\u226510000, distance\u22658km, activeMinutes\u226530, " +
                        "caloriesOut\u22652500), identify standout days, and detect trends. " +
                        "Write analysis scripts to /tmp/reports/ and execute them. " +
                        "Active minutes = fairlyActiveMinutes + veryActiveMinutes. " +
                        "Duration values are milliseconds \u2014 convert to minutes by dividing by 60000. " +
                        "IMPORTANT: Save ALL computed results to /tmp/reports/analysis.json (JSON format " +
                        "only \u2014 do NOT use pickle or any other format). The JSON must include: " +
                        "daily_metrics (array), weekly_summary (dict), trends (dict), standout_days (dict), " +
                        "observations (list of strings), and recommendations (list of 4-6 objects with " +
                        "category, icon, and text fields). Other agents will read this file. " +
                        "Each recommendation must reference specific numbers and dates from the analysis. " +
                        "Use positive framing \u2014 lead with strengths, suggest improvements gently. " +
                        "Never diagnose conditions or prescribe treatments.",
                    Tools = null, // All tools — OnPreToolUse hook provides access control
                    Infer = true, // Allow runtime auto-delegation based on intent matching
                },
                new CustomAgentConfig
                {
                    Name = "chart-generator",
                    DisplayName = "Chart Generator",
                    Description = "Creates professional data visualizations using matplotlib and seaborn. " +
                        "Generates PNG chart files for steps, calories, active minutes, distance, floors, " +
                        "heart rate, goal achievement, and weekly overview.",
                    Prompt = "You are a data visualization specialist. Use matplotlib and seaborn to create " +
                        "clear, professional charts from health data. Always use matplotlib.use('Agg') before " +
                        "importing pyplot. Use seaborn 'whitegrid' style with 'muted' palette. " +
                        "IMPORTANT: Read pre-computed metrics from /tmp/reports/analysis.json instead of " +
                        "re-parsing raw data. Load the JSON file and use the daily_metrics array and " +
                        "weekly_summary dict for chart values. This avoids redundant data processing. " +
                        "Save all charts as PNG at dpi=150 with bbox_inches='tight' to /tmp/reports/. " +
                        "Include goal lines (red dashed), value annotations on bars, and clear axis labels.",
                    Tools = null,
                    Infer = true,
                },
                new CustomAgentConfig
                {
                    Name = "pdf-builder",
                    DisplayName = "PDF Report Builder",
                    Description = "Assembles multi-page professional PDF health reports using reportlab " +
                        "PLATYPUS. Combines analysis text, data tables, and chart images into a cohesive document.",
                    Prompt = "You are a PDF report builder. Use reportlab SimpleDocTemplate with A4 page size " +
                        "and 2cm margins. Follow the report-template skill for the standardized page structure. " +
                        "IMPORTANT: Read pre-computed metrics, observations, and recommendations from " +
                        "/tmp/reports/analysis.json instead of re-parsing raw data. Load the JSON file and " +
                        "use daily_metrics, weekly_summary, observations, and recommendations arrays directly. " +
                        "Create professional tables with header styling, embed chart PNG " +
                        "images from /tmp/reports/, add page breaks between sections. " +
                        "Include a 'Personalized Recommendations' page with the recommendations from " +
                        "analysis.json, followed by a prominent AI disclaimer warning box " +
                        "(light yellow background, orange border) stating these are AI-generated suggestions " +
                        "and not medical advice. " +
                        "Use the onPage callback for the page footer disclaimer: " +
                        "'This report is generated from personal health data and is not medical advice. " +
                        "Consult a healthcare provider for medical guidance.' " +
                        "Save the final PDF to /tmp/reports/report.pdf.",
                    Tools = null,
                    Infer = true,
                },
            ];

            // Skill directories reference the CLI sidecar's filesystem (not the .NET host's)
            config.SkillDirectories =
            [
                "/app/skills/chart-best-practices",
                "/app/skills/health-data-analysis",
                "/app/skills/report-template",
            ];

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
        /// Sends a lightweight session to force the Copilot CLI sidecar to initialize its Python runtime.
        /// Without this, the first real session often produces an empty response while the runtime cold-starts.
        /// </summary>
        public async Task WarmUpSidecarAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(90));

                logger.LogInformation("Warming up Copilot CLI sidecar...");

                // Use a separate disposable client so we don't dispose the shared _client singleton
                await using var warmUpClient = new CopilotClient(new CopilotClientOptions
                {
                    CliUrl = settings.Value.CopilotCliUrl,
                });
                await warmUpClient.StartAsync(cts.Token);

                var warmUpConfig = new SessionConfig
                {
                    Model = "claude-sonnet-4-6",
                    OnPermissionRequest = PermissionHandler.ApproveAll,
                };

                await using var session = await warmUpClient.CreateSessionAsync(warmUpConfig, cts.Token);
                await session.SendAndWaitAsync(
                    new MessageOptions { Prompt = "Run: echo 'warm-up complete'" },
                    timeout: TimeSpan.FromSeconds(60),
                    cancellationToken: cts.Token);

                logger.LogInformation("Copilot CLI sidecar warm-up completed");
            }
            catch (Exception ex)
            {
                // Warm-up failure is non-fatal — the real session may still succeed
                logger.LogWarning(ex, "Copilot CLI sidecar warm-up failed, proceeding with report generation");
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
            if (input.ToolName is "read" or "write" or "create" or "edit" or "view" && input.ToolArgs is not null)
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

            // Track and limit read_agent polling to prevent infinite sub-agent loops (ASI08)
            if (input.ToolName == "read_agent")
            {
                var agentId = ExtractJsonField(argsDetail, "agent_id") ?? "unknown";
                var count = _readAgentPollCounts.AddOrUpdate(agentId, 1, (_, c) => c + 1);

                if (count > MaxReadAgentPollsPerAgent)
                {
                    logger.LogWarning(
                        "Denied read_agent for agent '{AgentId}': poll count {Count} exceeded limit {Max} (ASI08)",
                        agentId, count, MaxReadAgentPollsPerAgent);
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
            // Only scan command execution tool results for dangerous patterns
            var toolResult = SafeSerialize(input.ToolResult);
            if (input.ToolName is not ("shell" or "bash") || string.IsNullOrEmpty(toolResult))
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
            _readAgentPollCounts.Clear();
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

        internal static string? ExtractJsonField(string json, string fieldName)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(fieldName, out var value))
                {
                    return value.GetString();
                }
            }
            catch
            {
                // ToolArgs may not be valid JSON
            }
            return null;
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
