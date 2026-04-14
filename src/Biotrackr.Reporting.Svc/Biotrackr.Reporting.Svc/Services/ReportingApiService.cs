using System.Net.Http.Json;
using System.Text.Json;
using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Models;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Biotrackr.Reporting.Svc.Services;

public class ReportingApiService : IReportingApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Settings _settings;
    private readonly ILogger<ReportingApiService> _logger;

    public ReportingApiService(
        IHttpClientFactory httpClientFactory,
        IOptions<Settings> settings,
        ILogger<ReportingApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<SummaryResult> GenerateReportAsync(string reportType, string startDate, string endDate,
        string taskMessage, HealthDataSnapshot snapshot, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("ReportingApi");

        var request = new GenerateReportRequest
        {
            ReportType = reportType,
            StartDate = startDate,
            EndDate = endDate,
            TaskMessage = taskMessage,
            SourceDataSnapshot = snapshot
        };

        _logger.LogInformation("Submitting report generation request for {ReportType} ({StartDate} to {EndDate})", reportType, startDate, endDate);

        var response = await client.PostAsJsonAsync("/api/reports/generate", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jobResult = await response.Content.ReadFromJsonAsync<ReportJobResult>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize report job result");

        _logger.LogInformation("Report generation started with job {JobId}", jobResult.JobId);

        return await PollForCompletionAsync(client, jobResult.JobId, cancellationToken);
    }

    public async Task<byte[]> DownloadArtifactAsync(string sasUrl, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Downloading report artifact from SAS URL");

        var client = _httpClientFactory.CreateClient("ArtifactDownload");
        var pdfBytes = await client.GetByteArrayAsync(sasUrl, cancellationToken);

        _logger.LogInformation("Downloaded report artifact ({Size} bytes)", pdfBytes.Length);

        return pdfBytes;
    }

    private async Task<SummaryResult> PollForCompletionAsync(HttpClient client, string jobId, CancellationToken cancellationToken)
    {
        var pollInterval = TimeSpan.FromSeconds(_settings.ReportPollIntervalSeconds);
        var timeout = TimeSpan.FromMinutes(_settings.ReportTimeoutMinutes);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(pollInterval, cancellationToken);

            _logger.LogInformation("Polling report status for job {JobId}", jobId);

            var response = await client.GetAsync($"/api/reports/{jobId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var statusResponse = await response.Content.ReadFromJsonAsync<ReportStatusResponse>(cancellationToken)
                ?? throw new InvalidOperationException($"Failed to deserialize report status for job {jobId}");

            var status = statusResponse.Metadata.Status;

            if (status is "generated" or "reviewed")
            {
                _logger.LogInformation("Report job {JobId} completed with status {Status}", jobId, status);

                string? pdfUrl = null;
                if (statusResponse.ArtifactUrls.TryGetValue("report.pdf", out var url))
                {
                    pdfUrl = url;
                }

                return new SummaryResult
                {
                    JobId = jobId,
                    Status = status,
                    Summary = statusResponse.Metadata.Summary,
                    PdfUrl = pdfUrl
                };
            }

            if (status is "failed")
            {
                throw new InvalidOperationException($"Report generation failed for job {jobId}: {statusResponse.Metadata.Error}");
            }
        }

        throw new TimeoutException($"Report generation timed out after {_settings.ReportTimeoutMinutes} minutes for job {jobId}");
    }
}
