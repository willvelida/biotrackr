using Biotrackr.Activity.Svc.Services.Interfaces;

// Rebuild to restore daily sync container image after backfill

namespace Biotrackr.Activity.Svc.Workers
{
    public class ActivityWorker : BackgroundService
    {
        private readonly IFitbitService _fitbitService;
        private readonly IActivityService _activityService;
        private readonly ILogger<ActivityWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly TimeProvider _timeProvider;

        public ActivityWorker(IFitbitService fitbitService, IActivityService activityService, ILogger<ActivityWorker> logger, IHostApplicationLifetime appLifetime, TimeProvider timeProvider)
        {
            _fitbitService = fitbitService;
            _activityService = activityService;
            _logger = logger;
            _appLifetime = appLifetime;
            _timeProvider = timeProvider;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // GetLocalNow preserves the local-time semantics of the DateTime.Now this replaced.
                var now = _timeProvider.GetLocalNow();

                _logger.LogInformation($"{nameof(ActivityWorker)} executed at: {now.DateTime}");

                var date = now.AddDays(-1).ToString("yyyy-MM-dd");

                _logger.LogInformation($"Getting activity response for date: {date}");
                var activityResponse = await _fitbitService.GetActivityResponse(date);

                _logger.LogInformation($"Mapping and saving document for date: {date}");
                await _activityService.MapAndSaveDocument(date, activityResponse);

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
    }
}
