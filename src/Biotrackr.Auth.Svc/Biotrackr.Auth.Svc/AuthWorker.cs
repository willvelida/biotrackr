using Biotrackr.Auth.Svc.Models;
using Biotrackr.Auth.Svc.Services.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Biotrackr.Auth.Svc
{
    public class AuthWorker : BackgroundService
    {
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<AuthWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public AuthWorker(IRefreshTokenService refreshTokenService, ILogger<AuthWorker> logger, IHostApplicationLifetime appLifetime)
        {
            _refreshTokenService = refreshTokenService;
            _logger = logger;
            _appLifetime = appLifetime;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"Attempting to refresh FitBit Tokens: {DateTime.Now}");
                RefreshTokenResponse refreshTokenResponse = await _refreshTokenService.RefreshTokens();
                _logger.LogInformation($"FitBit Tokens refresh successful. Saving to Secret Store: {DateTime.Now}");
                await _refreshTokenService.SaveTokens(refreshTokenResponse);
                _logger.LogInformation($"FitBit Tokens saved successfully: {DateTime.Now}");
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
