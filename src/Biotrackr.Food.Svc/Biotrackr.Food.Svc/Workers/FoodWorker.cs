using Biotrackr.Food.Svc.Services.Interfaces;
using DnsClient.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Workers
{
    public class FoodWorker : BackgroundService
    {
        private readonly IFitbitService _fitbitService;
        private readonly IFoodService _foodService;
        private readonly ILogger<FoodWorker> _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public FoodWorker(IFitbitService fitbitService, IFoodService foodService, ILogger<FoodWorker> logger, IHostApplicationLifetime appLifetime)
        {
            _fitbitService = fitbitService ?? throw new ArgumentNullException(nameof(fitbitService));
            _foodService = foodService ?? throw new ArgumentNullException(nameof(foodService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
        }

        protected override async Task<int> ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"{nameof(FoodWorker)} executed at: {DateTime.Now}");

                var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                _logger.LogInformation($"Fetching food data for date: {date}");
                var foodResponse = await _fitbitService.GetFoodResponse(date);

                _logger.LogInformation($"Mapping and saving food document for date: {date}");
                await _foodService.MapAndSaveDocument(date, foodResponse);

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(FoodWorker)}: {ex.Message}");
                return 1;
            }
            finally
            {
                _appLifetime.StopApplication();
            }
        }
    }
}
