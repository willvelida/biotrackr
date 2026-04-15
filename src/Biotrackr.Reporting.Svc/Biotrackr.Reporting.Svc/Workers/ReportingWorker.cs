using Biotrackr.Reporting.Svc.Services.Interfaces;

namespace Biotrackr.Reporting.Svc.Workers;

public class ReportingWorker : BackgroundService
{
    private readonly ISummaryService _summaryService;
    private readonly ILogger<ReportingWorker> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    public ReportingWorker(
        ISummaryService summaryService,
        ILogger<ReportingWorker> logger,
        IHostApplicationLifetime appLifetime)
    {
        _summaryService = summaryService;
        _logger = logger;
        _appLifetime = appLifetime;
    }

    protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("{Worker} executed at: {Time}", nameof(ReportingWorker), DateTime.UtcNow);
            await _summaryService.GenerateAndSendSummaryAsync(stoppingToken);
            _logger.LogInformation("{Worker} completed successfully", nameof(ReportingWorker));
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown in {Worker}", nameof(ReportingWorker));
            return 1;
        }
        finally
        {
            _appLifetime.StopApplication();
        }
    }
}
