using System.Diagnostics;
using System.Text.Json;
using Biotrackr.Reporting.Api.Configuration;
using Biotrackr.Reporting.Api.Models;
using Biotrackr.Reporting.Api.Telemetry;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Options;

namespace Biotrackr.Reporting.Api.Services
{
    public interface IReportGenerationService
    {
        Task<ReportJobResult> StartReportGenerationAsync(
            string reportType, string startDate, string endDate, string taskMessage, object sourceDataSnapshot);
    }

    public class ReportGenerationService : IReportGenerationService
    {
        private const string ReportsDirectory = "/tmp/reports";
        private const int MaxRetries = 1;

        private readonly IBlobStorageService _blobStorageService;
        private readonly ICopilotService _copilotService;
        private readonly ILogger<ReportGenerationService> _logger;
        private readonly Settings _settings;
        private readonly SemaphoreSlim _concurrencyLimiter;

        public ReportGenerationService(
            IBlobStorageService blobStorageService,
            ICopilotService copilotService,
            IOptions<Settings> settings,
            ILogger<ReportGenerationService> logger)
        {
            _blobStorageService = blobStorageService;
            _copilotService = copilotService;
            _logger = logger;
            _settings = settings.Value;
            _concurrencyLimiter = new SemaphoreSlim(_settings.MaxConcurrentJobs);
        }

        public async Task<ReportJobResult> StartReportGenerationAsync(
            string reportType, string startDate, string endDate, string taskMessage, object sourceDataSnapshot)
        {
            // Kill switch (ASI10)
            if (!_settings.ReportGenerationEnabled)
            {
                _logger.LogWarning("Report generation is disabled via configuration");
                return new ReportJobResult
                {
                    Status = ReportStatus.Failed,
                    Message = "Report generation is currently disabled."
                };
            }

            // Concurrency limit (ASI08)
            if (!_concurrencyLimiter.Wait(0))
            {
                _logger.LogWarning("Report generation rejected — max concurrent jobs ({Max}) reached",
                    _settings.MaxConcurrentJobs);
                return new ReportJobResult
                {
                    Status = ReportStatus.Failed,
                    Message = "Too many concurrent report generation requests. Try again later."
                };
            }

            // Check sidecar health first (DR-07)
            var isHealthy = await _copilotService.IsHealthyAsync();
            if (!isHealthy)
            {
                _concurrencyLimiter.Release();
                _logger.LogError("Copilot CLI sidecar is not reachable. Cannot generate report.");
                return new ReportJobResult
                {
                    Status = ReportStatus.Failed,
                    Message = "Report generation service is temporarily unavailable. The Copilot sidecar is not responding."
                };
            }

            // Force sidecar Python runtime initialization so the first real session starts hot
            await _copilotService.WarmUpSidecarAsync();

            // Create job in Blob Storage
            var jobId = await _blobStorageService.CreateJobAsync(reportType, startDate, endDate);
            _logger.LogInformation("Created report job {JobId} for {ReportType} ({StartDate} to {EndDate})",
                jobId, reportType, startDate, endDate);

            ReportingTelemetry.ConcurrentJobs.Add(1);

            // Run generation in background with timeout (ASI08)
            _ = Task.Run(async () =>
            {
                try
                {
                    // Job-level deadline: total budget across all attempts (ASI08)
                    using var jobCts = new CancellationTokenSource(
                        TimeSpan.FromMinutes(_settings.ReportGenerationTimeoutMinutes * (MaxRetries + 1)));
                    await GenerateReportAsync(jobId, taskMessage, sourceDataSnapshot, jobCts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError("Report generation timed out after {Timeout}m for job {JobId}",
                        _settings.ReportGenerationTimeoutMinutes, jobId);
                    await _blobStorageService.UpdateJobStatusAsync(jobId, ReportStatus.Failed,
                        $"Report generation timed out after {_settings.ReportGenerationTimeoutMinutes} minutes.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background report generation failed for job {JobId}", jobId);
                    await _blobStorageService.UpdateJobStatusAsync(jobId, ReportStatus.Failed, ex.Message);
                }
                finally
                {
                    ReportingTelemetry.ConcurrentJobs.Add(-1);
                    _concurrencyLimiter.Release();
                }
            });

            return new ReportJobResult
            {
                JobId = jobId,
                Status = ReportStatus.Generating,
                Message = "Report generation started."
            };
        }

        private async Task GenerateReportAsync(
            string jobId, string taskMessage, object sourceDataSnapshot, CancellationToken cancellationToken)
        {
            using var activity = ReportingTelemetry.ActivitySource.StartActivity("report.generate");
            activity?.SetTag("report.job_id", jobId);
            var stopwatch = Stopwatch.StartNew();

            // Clean working directory before generation
            CleanReportsDirectory();

            var success = false;
            var attempt = 0;

            while (!success && attempt <= MaxRetries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                attempt++;
                _logger.LogInformation("Report generation attempt {Attempt}/{MaxAttempts} for job {JobId}",
                    attempt, MaxRetries + 1, jobId);

                // Per-attempt timeout so a wasted attempt doesn't steal budget from a productive one
                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                attemptCts.CancelAfter(TimeSpan.FromMinutes(_settings.ReportGenerationTimeoutMinutes));

                try
                {
                    // Create a Copilot CLI session and send the task message directly via the SDK
                    await using var client = _copilotService.Client;
                    await client.StartAsync(attemptCts.Token);

                    var sessionConfig = _copilotService.CreateSessionConfig();
                    await using var session = await client.CreateSessionAsync(sessionConfig, attemptCts.Token);

                    using var sessionActivity = ReportingTelemetry.ActivitySource.StartActivity("copilot.session");
                    sessionActivity?.SetTag("report.job_id", jobId);
                    sessionActivity?.SetTag("report.attempt", attempt);

                    // Track tool execution and session events for observability
                    using var eventSubscription = session.On(evt =>
                    {
                        switch (evt)
                        {
                            case SubagentStartedEvent started:
                                _logger.LogInformation(
                                    "Subagent started for job {JobId}: Agent={AgentName}, DisplayName={DisplayName}, ToolCallId={ToolCallId}",
                                    jobId, started.Data?.AgentName, started.Data?.AgentDisplayName, started.Data?.ToolCallId);
                                break;
                            case SubagentCompletedEvent completed:
                                _logger.LogInformation(
                                    "Subagent completed for job {JobId}: Agent={AgentName}, DisplayName={DisplayName}, Duration={DurationMs}ms, ToolCalls={ToolCalls}, Tokens={Tokens}, Model={Model}",
                                    jobId, completed.Data?.AgentName, completed.Data?.AgentDisplayName, completed.Data?.DurationMs,
                                    completed.Data?.TotalToolCalls, completed.Data?.TotalTokens, completed.Data?.Model);
                                ReportingTelemetry.SubagentDuration.Record(
                                    completed.Data?.DurationMs ?? 0,
                                    new KeyValuePair<string, object?>("agent.name", completed.Data?.AgentName));
                                break;
                            case SubagentFailedEvent failed:
                                _logger.LogWarning(
                                    "Subagent failed for job {JobId}: Agent={AgentName}, DisplayName={DisplayName}, Error={Error}, Duration={DurationMs}ms, ToolCalls={ToolCalls}",
                                    jobId, failed.Data?.AgentName, failed.Data?.AgentDisplayName, failed.Data?.Error,
                                    failed.Data?.DurationMs, failed.Data?.TotalToolCalls);
                                break;
                            case ToolExecutionStartEvent toolStart:
                                _logger.LogInformation(
                                    "Copilot tool started for job {JobId}: Tool={ToolName}",
                                    jobId, toolStart.Data?.ToolName);
                                break;
                            case ToolExecutionCompleteEvent:
                                _logger.LogInformation(
                                    "Copilot tool completed for job {JobId}",
                                    jobId);
                                break;
                            case SessionErrorEvent sessionError:
                                _logger.LogWarning(
                                    "Copilot session error for job {JobId}: {Message}",
                                    jobId, sessionError.Data?.Message);
                                break;
                            // TODO: Verify AssistantUsageEvent type name at runtime — SDK may expose usage
                            // data via a different event type or as AssistantUsageData on a generic event.
                            case AssistantUsageEvent usage:
                                _logger.LogInformation(
                                    "LLM usage for job {JobId}: Model={Model}, InputTokens={InputTokens}, OutputTokens={OutputTokens}, Duration={Duration}ms, Initiator={Initiator}",
                                    jobId, usage.Data?.Model, usage.Data?.InputTokens,
                                    usage.Data?.OutputTokens, usage.Data?.Duration, usage.Data?.Initiator);
                                break;
                        }
                    });

                    // Combine the task message with the source data snapshot as a JSON block
                    // The system prompt instructs the sidecar to parse the ```json block
                    var dataJson = System.Text.Json.JsonSerializer.Serialize(sourceDataSnapshot,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
                    var fullPrompt = $"{taskMessage}\n\n```json\n{dataJson}\n```";

                    var result = await session.SendAndWaitAsync(
                        new MessageOptions { Prompt = fullPrompt },
                        timeout: TimeSpan.FromMinutes(_settings.ReportGenerationTimeoutMinutes - 1),
                        cancellationToken: attemptCts.Token);
                    var responseText = result?.Data?.Content;

                    _logger.LogInformation("Copilot session completed for job {JobId}. Response length: {Length}",
                        jobId, responseText?.Length ?? 0);

                    // Defense-in-depth code validation — primary gate is OnPostToolUse hook (ASI05)
                    ValidateGeneratedCode(jobId);

                    // Check for generated artifacts
                    using var scanActivity = ReportingTelemetry.ActivitySource.StartActivity("report.artifact_scan");
                    var artifacts = ScanForArtifacts(jobId);
                    scanActivity?.SetTag("report.artifact_count", artifacts.Count);
                    if (artifacts.Count > 0)
                    {
                        success = true;
                        using var uploadActivity = ReportingTelemetry.ActivitySource.StartActivity("report.artifact_upload");
                        uploadActivity?.SetTag("report.artifact_count", artifacts.Count);
                        await UploadArtifactsAsync(jobId, artifacts, responseText, sourceDataSnapshot);
                        _logger.LogInformation("Report job {JobId} completed with {Count} artifacts",
                            jobId, artifacts.Count);
                    }
                    else
                    {
                        _logger.LogWarning("No artifacts found after attempt {Attempt} for job {JobId}", attempt, jobId);
                        if (attempt <= MaxRetries)
                        {
                            CleanReportsDirectory();
                        }
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Per-attempt timeout fired but job-level deadline hasn't — retry
                    _logger.LogWarning(
                        "Attempt {Attempt} timed out after {Timeout}m for job {JobId}, will retry",
                        attempt, _settings.ReportGenerationTimeoutMinutes, jobId);
                    if (attempt <= MaxRetries)
                    {
                        CleanReportsDirectory();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Job-level deadline reached — propagate to caller
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Copilot session failed on attempt {Attempt} for job {JobId}", attempt, jobId);
                    if (attempt > MaxRetries)
                    {
                        await _blobStorageService.UpdateJobStatusAsync(jobId, ReportStatus.Failed,
                            $"Report generation failed after {attempt} attempts: {ex.Message}");
                    }
                }
            }

            stopwatch.Stop();
            try
            {
                if (success)
                {
                    ReportingTelemetry.ReportsGenerated.Add(1);
                    ReportingTelemetry.ReportDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
                }
                else
                {
                    ReportingTelemetry.ReportsFailed.Add(1);
                    await _blobStorageService.UpdateJobStatusAsync(jobId, ReportStatus.Failed,
                        $"No report artifacts generated after {attempt} attempts.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record metrics for job {JobId}", jobId);
            }
        }

        /// <summary>
        /// Defense-in-depth validation of generated Python scripts for dangerous code patterns (ASI05).
        /// The primary gate is the OnPostToolUse hook in CopilotService. This method provides a secondary
        /// post-hoc scan and logs warnings rather than aborting the job.
        /// </summary>
        internal void ValidateGeneratedCode(string jobId)
        {
            if (!Directory.Exists(ReportsDirectory))
                return;

            var scripts = Directory.GetFiles(ReportsDirectory, "*.py", SearchOption.TopDirectoryOnly);
            foreach (var script in scripts)
            {
                var content = File.ReadAllText(script);
                var fileName = Path.GetFileName(script);

                _logger.LogInformation("Validating generated script {Script} for job {JobId} ({Length} chars)",
                    fileName, jobId, content.Length);

                foreach (var pattern in CopilotService.DangerousCodePatterns)
                {
                    if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "Defense-in-depth: Dangerous code pattern '{Pattern}' detected in {Script} for job {JobId}",
                            pattern, fileName, jobId);
                    }
                }
            }
        }

        internal async Task UploadArtifactsAsync(
            string jobId, Dictionary<string, byte[]> artifacts, string? summary, object sourceDataSnapshot)
        {
            var pdfBytes = Array.Empty<byte>();
            var charts = new Dictionary<string, byte[]>();

            foreach (var (name, bytes) in artifacts)
            {
                if (name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    pdfBytes = bytes;
                }
                else
                {
                    charts[name] = bytes;
                }
            }

            await _blobStorageService.UploadReportAsync(jobId, pdfBytes, charts,
                summary ?? "Report generated successfully.", sourceDataSnapshot);
        }

        /// <summary>
        /// Scans /tmp/reports for artifact files. Logs detailed artifact info and flags anomalies (ASI10).
        /// </summary>
        internal Dictionary<string, byte[]> ScanForArtifacts(string jobId)
        {
            var artifacts = new Dictionary<string, byte[]>();

            if (!Directory.Exists(ReportsDirectory))
                return artifacts;

            var files = Directory.GetFiles(ReportsDirectory, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (extension is ".pdf" or ".png" or ".jpg" or ".svg")
                {
                    var fileName = Path.GetFileName(file);
                    var bytes = File.ReadAllBytes(file);

                    _logger.LogInformation(
                        "Job {JobId} produced artifact: {Name}, Size: {Size} bytes, Extension: {Extension}",
                        jobId, fileName, bytes.Length, extension);

                    // Flag anomalous artifact sizes (ASI10)
                    if (bytes.Length > _settings.MaxArtifactSizeBytes)
                    {
                        _logger.LogWarning(
                            "Anomalous artifact size detected for job {JobId}: {Name} ({Size} bytes exceeds {Max} byte limit)",
                            jobId, fileName, bytes.Length, _settings.MaxArtifactSizeBytes);
                        continue; // Skip oversized artifacts
                    }

                    artifacts[fileName] = bytes;
                }
                else if (extension is not ".py")
                {
                    // Log unexpected file types (ASI10)
                    _logger.LogWarning(
                        "Unexpected file type in reports directory for job {JobId}: {Name} (extension: {Extension})",
                        jobId, Path.GetFileName(file), extension);
                }
            }

            return artifacts;
        }

        private static void CleanReportsDirectory()
        {
            if (!Directory.Exists(ReportsDirectory))
            {
                Directory.CreateDirectory(ReportsDirectory);
                return;
            }

            foreach (var file in Directory.GetFiles(ReportsDirectory))
            {
                File.Delete(file);
            }
        }
    }
}
