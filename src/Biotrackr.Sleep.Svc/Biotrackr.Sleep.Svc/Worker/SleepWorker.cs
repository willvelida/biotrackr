using Biotrackr.Sleep.Svc.Models.FitbitEntities;
using Biotrackr.Sleep.Svc.Services.Interfaces;
using System.Globalization;

namespace Biotrackr.Sleep.Svc.Worker
{
    public class SleepWorker : BackgroundService
    {
        private readonly IFitbitService _fitbitService;
        private readonly ISleepService _sleepService;
        private readonly ILogger<SleepWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public SleepWorker(IFitbitService fitbitService, ISleepService sleepService, ILogger<SleepWorker> logger, IHostApplicationLifetime appLifeTime)
        {
            _fitbitService = fitbitService;
            _sleepService = sleepService;
            _logger = logger;
            _appLifetime = appLifeTime;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"{nameof(SleepWorker)} executed at: {DateTime.Now}");

                var backfillStartDate = Environment.GetEnvironmentVariable("BackfillStartDate");
                var backfillEndDate = Environment.GetEnvironmentVariable("BackfillEndDate");

                if (!string.IsNullOrEmpty(backfillStartDate) && !string.IsNullOrEmpty(backfillEndDate))
                {
                    await ExecuteBackfill(backfillStartDate, backfillEndDate, stoppingToken);
                }
                else
                {
                    var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                    _logger.LogInformation($"Getting sleep data for {date}");
                    var sleepResponse = await _fitbitService.GetSleepResponse(date);

                    _logger.LogInformation($"Mapping and saving document for {date}");
                    await _sleepService.MapAndSaveDocument(date, sleepResponse);
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(SleepWorker)}: {ex.Message}");
                return 1;
            }
            finally
            {
                _appLifetime.StopApplication();
            }
        }

        private async Task ExecuteBackfill(string startDateStr, string endDateStr, CancellationToken stoppingToken)
        {
            var start = DateTime.ParseExact(startDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var end = DateTime.ParseExact(endDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var chunkSize = 100;
            var chunkNumber = 0;
            var totalChunks = (int)Math.Ceiling((end - start).TotalDays / chunkSize);

            _logger.LogInformation("Starting backfill from {StartDate} to {EndDate} ({TotalChunks} chunks)",
                startDateStr, endDateStr, totalChunks);

            var current = start;
            while (current <= end)
            {
                stoppingToken.ThrowIfCancellationRequested();

                var chunkEnd = current.AddDays(chunkSize - 1);
                if (chunkEnd > end) chunkEnd = end;
                chunkNumber++;

                var chunkStartStr = current.ToString("yyyy-MM-dd");
                var chunkEndStr = chunkEnd.ToString("yyyy-MM-dd");

                _logger.LogInformation("Processing chunk {ChunkNumber}/{TotalChunks}: {Start} to {End}",
                    chunkNumber, totalChunks, chunkStartStr, chunkEndStr);

                var sleepResponse = await _fitbitService.GetSleepResponseByDateRange(chunkStartStr, chunkEndStr);

                if (sleepResponse?.Sleep != null)
                {
                    var groupedByDate = sleepResponse.Sleep
                        .GroupBy(s => s.DateOfSleep)
                        .ToList();

                    foreach (var group in groupedByDate)
                    {
                        try
                        {
                            _logger.LogInformation("Saving sleep data for {Date} ({EntryCount} entries)",
                                group.Key, group.Count());

                            var dateResponse = new SleepResponse
                            {
                                Sleep = group.ToList(),
                                Summary = null
                            };
                            await _sleepService.MapAndSaveDocument(group.Key, dateResponse);

                            _logger.LogInformation("Successfully saved sleep data for {Date}", group.Key);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Failed to save sleep data for {Date}: {Error}", group.Key, ex.Message);
                        }
                    }

                    _logger.LogInformation("Saved {Count} dates for chunk {ChunkNumber}",
                        groupedByDate.Count, chunkNumber);
                }

                current = chunkEnd.AddDays(1);

                if (current <= end)
                {
                    await Task.Delay(500, stoppingToken);
                }
            }

            _logger.LogInformation("Backfill complete. Processed {TotalChunks} chunks.", totalChunks);
        }
    }
}
