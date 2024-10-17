using Biotrackr.FitbitApi.Configuration;
using Biotrackr.FitbitApi.Services.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biotrackr.FitbitApi.Workers
{
    public class FitbitActivityWorker : BackgroundService
    {
        private readonly IFitbitService _fitbitService;
        private readonly IQueueService _queueService;
        private readonly ILogger<FitbitActivityWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly Settings _options;
        private readonly TelemetryClient _telemetryClient;

        public FitbitActivityWorker(IFitbitService fitbitService, IQueueService queueService, ILogger<FitbitActivityWorker> logger, IHostApplicationLifetime appLifetime, IOptions<Settings> options, TelemetryClient telemetryClient)
        {
            _fitbitService = fitbitService;
            _queueService = queueService;
            _logger = logger;
            _appLifetime = appLifetime;
            _options = options.Value;
            _telemetryClient = telemetryClient;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"{nameof(FitbitActivityWorker)} executed at: {DateTime.Now}");

                using (_telemetryClient.StartOperation<RequestTelemetry>("GetDailyActivityResponse"))
                {
                    var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                    var activityResponse = await _fitbitService.GetActivityResponse(date);

                    await _queueService.SendRecordToQueue(activityResponse, _options.ActivityQueueName);

                    _telemetryClient.TrackEvent("ActivityRecordSentToQueue");
                }
             
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(FitbitActivityWorker)}: {ex.Message}");
                return 1;
            }
            finally
            {
                _appLifetime.StopApplication();
            }
        }
    }
}
