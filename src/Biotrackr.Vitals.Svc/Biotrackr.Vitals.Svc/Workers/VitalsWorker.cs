using Biotrackr.Vitals.Svc.Adapters;
using Biotrackr.Vitals.Svc.Configuration;
using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Biotrackr.Vitals.Svc.Workers
{
    public class VitalsWorker : BackgroundService
    {
        private readonly IWithingsService _withingsService;
        private readonly IVitalsService _vitalsService;
        private readonly ILogger<VitalsWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly Settings _settings;

        public VitalsWorker(IWithingsService withingsService, IVitalsService vitalsService, ILogger<VitalsWorker> logger, IHostApplicationLifetime appLifetime, IOptions<Settings> settings)
        {
            _withingsService = withingsService;
            _vitalsService = vitalsService;
            _logger = logger;
            _appLifetime = appLifetime;
            _settings = settings.Value;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"{nameof(VitalsWorker)} executed at: {DateTime.Now}");

                var startDate = DateTime.Now.AddDays(-_settings.LookbackDays).ToString("yyyy-MM-dd");
                var endDate = DateTime.Now.ToString("yyyy-MM-dd");

                var measureResponse = await _withingsService.GetMeasurements(startDate, endDate);

                var dateGroups = measureResponse.Body!.MeasureGroups
                    .GroupBy(mg => DateTimeOffset.FromUnixTimeSeconds(mg.Date).ToString("yyyy-MM-dd"));

                foreach (var dateGroup in dateGroups)
                {
                    var date = dateGroup.Key;

                    var weightGroups = dateGroup.Where(mg => mg.Measures.Any(m => m.Type == 1)).ToList();
                    var bpGroups = dateGroup.Where(mg => mg.Measures.Any(m => m.Type == 10)).ToList();

                    WeightMeasurement? weight = null;
                    if (weightGroups.Count > 0)
                    {
                        var mostRecentWeightGroup = weightGroups.OrderByDescending(mg => mg.Date).First();
                        weight = WithingsWeightAdapter.FromMeasureGroup(mostRecentWeightGroup, _settings.UserHeight);
                    }

                    List<BloodPressureReading>? bpReadings = null;
                    if (bpGroups.Count > 0)
                    {
                        bpReadings = bpGroups.Select(WithingsBloodPressureAdapter.FromMeasureGroup).ToList();
                    }

                    var vitalsDocument = new VitalsDocument
                    {
                        Date = date,
                        Weight = weight,
                        BloodPressureReadings = bpReadings,
                        Provider = "Withings"
                    };

                    await _vitalsService.UpsertVitalsDocument(vitalsDocument);
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(VitalsWorker)}: {ex.Message}");
                return 1;
            }
            finally
            {
                _appLifetime.StopApplication();
            }
        }
    }
}
