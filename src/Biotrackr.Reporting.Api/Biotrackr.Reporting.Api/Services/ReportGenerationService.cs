using System.Text.Json;
using Biotrackr.Reporting.Api.Configuration;
using Biotrackr.Reporting.Api.Models;
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
        private const int MaxRetries = 2;

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

            // Create job in Blob Storage
            var jobId = await _blobStorageService.CreateJobAsync(reportType, startDate, endDate);
            _logger.LogInformation("Created report job {JobId} for {ReportType} ({StartDate} to {EndDate})",
                jobId, reportType, startDate, endDate);

            // Run generation in background with timeout (ASI08)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(
                        TimeSpan.FromMinutes(_settings.ReportGenerationTimeoutMinutes));
                    await GenerateReportAsync(jobId, taskMessage, sourceDataSnapshot, cts.Token);
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

                try
                {
                    // Create a Copilot CLI session and send the task message directly via the SDK
                    await using var client = _copilotService.Client;
                    await client.StartAsync(cancellationToken);

                    var sessionConfig = _copilotService.CreateSessionConfig();
                    await using var session = await client.CreateSessionAsync(sessionConfig, cancellationToken);

                    // Track tool execution and session events for observability
                    var eventSubscription = session.On(evt =>
                    {
                        switch (evt)
                        {
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
                        cancellationToken: cancellationToken);
                    var responseText = result?.Data?.Content;

                    _logger.LogInformation("Copilot session completed for job {JobId}. Response length: {Length}",
                        jobId, responseText?.Length ?? 0);

                    // Dispose event subscription after session completes
                    eventSubscription?.Dispose();

                    // Defense-in-depth code validation — primary gate is OnPostToolUse hook (ASI05)
                    ValidateGeneratedCode(jobId);

                    // Check for generated artifacts
                    var artifacts = ScanForArtifacts(jobId);
                    if (artifacts.Count > 0)
                    {
                        success = true;
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
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Copilot session failed on attempt {Attempt} for job {JobId}", attempt, jobId);
                    if (attempt > MaxRetries)
                    {
                        await _blobStorageService.UpdateJobStatusAsync(jobId, ReportStatus.Failed,
                            $"Report generation failed after {attempt} attempts: {ex.Message}");
                        return;
                    }
                }
            }

            if (!success)
            {
                await _blobStorageService.UpdateJobStatusAsync(jobId, ReportStatus.Failed,
                    $"No report artifacts generated after {attempt} attempts.");
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
