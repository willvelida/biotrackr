using Biotrackr.Weight.Svc.Services.Interfaces;

namespace Biotrackr.Weight.Svc.Workers
{
    public class WeightWorker : BackgroundService
    {
        private readonly IFitbitService _fitbitService;
        private readonly IWeightService _weightService;
        private readonly ILogger<WeightWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public WeightWorker(IFitbitService fibitService, IWeightService weightService, ILogger<WeightWorker> logger, IHostApplicationLifetime appLifetime)
        {
            _fitbitService = fibitService;
            _weightService = weightService;
            _logger = logger;
            _appLifetime = appLifetime;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"{nameof(WeightWorker)} executed at: {DateTime.Now}");

                var startDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                var endDate = DateTime.Now.ToString("yyyy-MM-dd");

                var weightResponse = await _fitbitService.GetWeightLogs(startDate, endDate);

                foreach (var weight in weightResponse.Weight)
                {
                    await _weightService.MapAndSaveDocument(weight.Date, weight);
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(WeightWorker)}: {ex.Message}");
                return 1;
            }
            finally
            {
                _appLifetime.StopApplication();
            }
        }
    }
}
