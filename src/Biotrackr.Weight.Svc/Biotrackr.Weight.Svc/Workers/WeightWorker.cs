using Biotrackr.Weight.Svc.Adapters;
using Biotrackr.Weight.Svc.Configuration;
using Biotrackr.Weight.Svc.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Biotrackr.Weight.Svc.Workers
{
    public class WeightWorker : BackgroundService
    {
        private readonly IWithingsService _withingsService;
        private readonly IWeightService _weightService;
        private readonly ILogger<WeightWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly Settings _settings;

        public WeightWorker(IWithingsService withingsService, IWeightService weightService, ILogger<WeightWorker> logger, IHostApplicationLifetime appLifetime, IOptions<Settings> settings)
        {
            _withingsService = withingsService;
            _weightService = weightService;
            _logger = logger;
            _appLifetime = appLifetime;
            _settings = settings.Value;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"{nameof(WeightWorker)} executed at: {DateTime.Now}");

                var startDate = DateTime.Now.AddDays(-57).ToString("yyyy-MM-dd"); // TODO: Change to -2 after historical backfill
                var endDate = DateTime.Now.ToString("yyyy-MM-dd");

                var measureResponse = await _withingsService.GetMeasurements(startDate, endDate);

                foreach (var measureGroup in measureResponse.Body!.MeasureGroups)
                {
                    var weightMeasurement = WithingsWeightAdapter.FromMeasureGroup(measureGroup, _settings.UserHeight);
                    await _weightService.MapAndSaveDocument(weightMeasurement.Date, weightMeasurement, "Withings");
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
