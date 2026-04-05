using System.Globalization;
using Biotrackr.Activity.Svc.Configuration;
using Biotrackr.Activity.Svc.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Biotrackr.Activity.Svc.Workers
{
    public class ActivityWorker : BackgroundService
    {
        private readonly IFitbitService _fitbitService;
        private readonly IActivityService _activityService;
        private readonly ILogger<ActivityWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly Settings _settings;

        public ActivityWorker(IFitbitService fitbitService, IActivityService activityService, ILogger<ActivityWorker> logger, IHostApplicationLifetime appLifetime, IOptions<Settings> settings)
        {
            _fitbitService = fitbitService;
            _activityService = activityService;
            _logger = logger;
            _appLifetime = appLifetime;
            _settings = settings.Value;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"{nameof(ActivityWorker)} executed at: {DateTime.Now}");

                if (!string.IsNullOrEmpty(_settings.StartDate) && !string.IsNullOrEmpty(_settings.EndDate))
                {
                    await ExecuteBackfill(stoppingToken);
                }
                else
                {
                    await ExecuteSingleDay();
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(ActivityWorker)}: {ex.Message}");
                return 1;
            }
            finally
            {
                _appLifetime.StopApplication();
            }
        }

        private async Task ExecuteBackfill(CancellationToken stoppingToken)
        {
            var start = DateTime.ParseExact(_settings.StartDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var end = DateTime.ParseExact(_settings.EndDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var totalDays = (int)(end - start).TotalDays + 1;
            var processedCount = 0;
            var failedDates = new List<string>();
            var requestCount = 0;
            var hourStart = DateTime.UtcNow;

            _logger.LogInformation("Starting backfill from {StartDate} to {EndDate} ({TotalDays} days)",
                _settings.StartDate, _settings.EndDate, totalDays);

            var current = start;
            while (current <= end)
            {
                stoppingToken.ThrowIfCancellationRequested();
                var date = current.ToString("yyyy-MM-dd");

                try
                {
                    // Rate limiting: pause after 140 requests per hour
                    requestCount++;
                    if (requestCount >= 140)
                    {
                        var elapsed = DateTime.UtcNow - hourStart;
                        if (elapsed < TimeSpan.FromHours(1))
                        {
                            var waitTime = TimeSpan.FromHours(1) - elapsed + TimeSpan.FromMinutes(1);
                            _logger.LogInformation("Rate limit approaching ({RequestCount} requests). Waiting {WaitMinutes:F1} minutes.",
                                requestCount, waitTime.TotalMinutes);
                            await Task.Delay(waitTime, stoppingToken);
                        }
                        requestCount = 0;
                        hourStart = DateTime.UtcNow;
                    }

                    _logger.LogInformation("Processing date {Date}", date);
                    var activityResponse = await _fitbitService.GetActivityResponse(date);
                    await _activityService.MapAndSaveDocument(date, activityResponse);
                    processedCount++;

                    if (processedCount % 50 == 0)
                    {
                        _logger.LogInformation("Progress: {Processed}/{Total} dates processed", processedCount, totalDays);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to process date {Date}: {Error}", date, ex.Message);
                    failedDates.Add(date);
                }

                current = current.AddDays(1);
            }

            _logger.LogInformation("Backfill complete. Processed: {Processed}, Failed: {Failed}, Total: {Total}",
                processedCount, failedDates.Count, totalDays);

            if (failedDates.Count > 0)
            {
                _logger.LogWarning("Failed dates: {FailedDates}", string.Join(", ", failedDates));
            }
        }

        private async Task ExecuteSingleDay()
        {
            var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            _logger.LogInformation($"Getting activity response for date: {date}");
            var activityResponse = await _fitbitService.GetActivityResponse(date);
            _logger.LogInformation($"Mapping and saving document for date: {date}");
            await _activityService.MapAndSaveDocument(date, activityResponse);
        }
    }
}
