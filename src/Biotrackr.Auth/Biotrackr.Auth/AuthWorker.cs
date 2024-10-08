using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Auth
{
    public class AuthWorker : BackgroundService
    {
        private readonly ILogger<AuthWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public AuthWorker(ILogger<AuthWorker> logger, IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _appLifetime = appLifetime;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"Hello there! Biotrackr.Auth running at {DateTime.Now}");
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
