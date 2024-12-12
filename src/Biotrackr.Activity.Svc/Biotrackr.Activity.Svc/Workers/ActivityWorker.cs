using Biotrackr.Activity.Svc.Services.Interfaces;

namespace Biotrackr.Activity.Svc.Workers
{
    public class ActivityWorker : BackgroundService
    {
        private readonly IFitbitService _fitbitService;
        private readonly IActivityService _activityService;
        private readonly ILogger<ActivityWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public ActivityWorker(IFitbitService fitbitService, IActivityService activityService, ILogger<ActivityWorker> logger, IHostApplicationLifetime appLifetime)
        {
            _fitbitService = fitbitService;
            _activityService = activityService;
            _logger = logger;
            _appLifetime = appLifetime;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"{nameof(ActivityWorker)} executed at: {DateTime.Now}");

                var date = DateTime.Now.ToString("yyyy-MM-dd");

                var activityResponse = await _fitbitService.GetActivityResponse(date);

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
