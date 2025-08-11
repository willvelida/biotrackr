using Biotrackr.Food.Svc.Models;
using Biotrackr.Food.Svc.Models.FitbitEntities;
using Biotrackr.Food.Svc.Repositories.Interfaces;
using Biotrackr.Food.Svc.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Services
{
    public class FoodService : IFoodService
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly ILogger<FoodService> _logger;

        public FoodService(ICosmosRepository cosmosRepository, ILogger<FoodService> logger)
        {
            _cosmosRepository = cosmosRepository ?? throw new ArgumentNullException(nameof(cosmosRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task MapAndSaveDocument(string date, FoodResponse foodResponse)
        {
            try
            {
                FoodDocument foodDocument = new FoodDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    DocumentType = "Food",
                    Date = date,
                    Food = foodResponse
                };

                await _cosmosRepository.CreateFoodDocument(foodDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(MapAndSaveDocument)}: {ex.Message}");
                throw;
            }
        }
    }
}
