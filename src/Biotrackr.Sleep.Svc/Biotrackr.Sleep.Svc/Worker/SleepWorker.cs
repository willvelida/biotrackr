using Biotrackr.Sleep.Svc.Services.Interfaces;

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

                var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                _logger.LogInformation($"Getting sleep data for {date}");
                var sleepResponse = await _fitbitService.GetSleepResponse(date);

                _logger.LogInformation($"Mapping and saving document for {date}");
                await _sleepService.MapAndSaveDocument(date, sleepResponse);

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
    }
}
