using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Biotrackr.Reporting.Svc.Services;

public class SummaryService : ISummaryService
{
    private readonly IHealthDataService _healthDataService;
    private readonly IReportingApiService _reportingApiService;
    private readonly IEmailService _emailService;
    private readonly IMetricExtractor _metricExtractor;
    private readonly Settings _settings;
    private readonly ILogger<SummaryService> _logger;

    public SummaryService(
        IHealthDataService healthDataService,
        IReportingApiService reportingApiService,
        IEmailService emailService,
        IMetricExtractor metricExtractor,
        IOptions<Settings> settings,
        ILogger<SummaryService> logger)
    {
        _healthDataService = healthDataService;
        _reportingApiService = reportingApiService;
        _emailService = emailService;
        _metricExtractor = metricExtractor;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task GenerateAndSendSummaryAsync(CancellationToken cancellationToken)
    {
        var cadence = _settings.SummaryCadence;
        _logger.LogInformation("Starting {Cadence} health summary generation", cadence);

        var (startDate, endDate) = CalculateDateRange(cadence);
        _logger.LogInformation("Date range calculated: {StartDate} to {EndDate}", startDate, endDate);

        var snapshot = await _healthDataService.FetchHealthDataAsync(startDate, endDate, cancellationToken);
        _logger.LogInformation("Health data fetched for all domains");

        var reportType = MapCadenceToReportType(cadence);
        var taskMessage = BuildTaskMessage(cadence, startDate, endDate);

        var result = await _reportingApiService.GenerateReportAsync(reportType, startDate, endDate, taskMessage, snapshot, cancellationToken);
        _logger.LogInformation("Report generated with job {JobId}, status: {Status}", result.JobId, result.Status);

        byte[] pdfBytes = [];
        if (!string.IsNullOrEmpty(result.PdfUrl))
        {
            pdfBytes = await _reportingApiService.DownloadArtifactAsync(result.PdfUrl, cancellationToken);
            _logger.LogInformation("PDF artifact downloaded ({Size} bytes)", pdfBytes.Length);
        }

        var metrics = _metricExtractor.ExtractMetrics(snapshot);

        await _emailService.SendReportEmailAsync(cadence, startDate, endDate, result.Summary, pdfBytes, metrics, cancellationToken);
        _logger.LogInformation("{Cadence} health summary email sent successfully", cadence);
    }

    public static (string startDate, string endDate) CalculateDateRange(string cadence)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return cadence.ToLowerInvariant() switch
        {
            "weekly" => (
                today.AddDays(-(int)today.DayOfWeek - 7).ToString("yyyy-MM-dd"),
                today.AddDays(-(int)today.DayOfWeek - 1).ToString("yyyy-MM-dd")
            ),
            "monthly" => (
                new DateOnly(today.Year, today.Month, 1).AddMonths(-1).ToString("yyyy-MM-dd"),
                new DateOnly(today.Year, today.Month, 1).AddDays(-1).ToString("yyyy-MM-dd")
            ),
            "yearly" => (
                new DateOnly(today.Year - 1, 12, 28).ToString("yyyy-MM-dd"),
                new DateOnly(today.Year, 12, 27).ToString("yyyy-MM-dd")
            ),
            _ => throw new ArgumentException($"Unknown cadence: {cadence}")
        };
    }

    private static string MapCadenceToReportType(string cadence)
    {
        return cadence.ToLowerInvariant() switch
        {
            "weekly" => "weekly_summary",
            "monthly" => "monthly_summary",
            "yearly" => "trend_analysis",
            _ => throw new ArgumentException($"Unknown cadence: {cadence}")
        };
    }

    private static string BuildTaskMessage(string cadence, string startDate, string endDate)
    {
        return cadence.ToLowerInvariant() switch
        {
            "weekly" => $"Generate a comprehensive weekly health and fitness summary for {startDate} to {endDate}. Include activity trends, sleep quality patterns, nutrition highlights, and vitals overview. Highlight achievements and areas for improvement.",
            "monthly" => $"Generate a detailed monthly health and fitness report for {startDate} to {endDate}. Include trend analysis across all domains, goal progress, weekly comparisons, and actionable recommendations for next month.",
            "yearly" => $"Generate a comprehensive annual health review from {startDate} to {endDate}. Include year-over-year trends, seasonal patterns, major achievements, monthly breakdowns, and goal-setting recommendations for the coming year.",
            _ => throw new ArgumentException($"Unknown cadence: {cadence}")
        };
    }
}
