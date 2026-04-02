using Biotrackr.Auth.Svc.Models;
using Biotrackr.Auth.Svc.Services.Interfaces;

namespace Biotrackr.Auth.Svc
{
    public class WithingsAuthWorker : BackgroundService
    {
        private readonly IWithingsRefreshTokenService _withingsRefreshTokenService;
        private readonly ILogger<WithingsAuthWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public WithingsAuthWorker(IWithingsRefreshTokenService withingsRefreshTokenService, ILogger<WithingsAuthWorker> logger, IHostApplicationLifetime appLifetime)
        {
            _withingsRefreshTokenService = withingsRefreshTokenService;
            _logger = logger;
            _appLifetime = appLifetime;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"Attempting to refresh Withings Tokens: {DateTime.Now}");
                WithingsTokenResponse withingsTokenResponse = await _withingsRefreshTokenService.RefreshTokens();
                _logger.LogInformation($"Withings Tokens refresh successful. Saving to Secret Store: {DateTime.Now}");
                await _withingsRefreshTokenService.SaveTokens(withingsTokenResponse);
                _logger.LogInformation($"Withings Tokens saved successfully: {DateTime.Now}");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown: {ex.Message}");
                return 1;
            }
            finally
            {
                _appLifetime.StopApplication();
            }
        }
    }
}
