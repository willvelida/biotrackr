using Biotrackr.Weight.Svc.Models;
using Biotrackr.Weight.Svc.Repositories.Interfaces;
using Biotrackr.Weight.Svc.Services.Interfaces;

namespace Biotrackr.Weight.Svc.Services
{
    public class WeightService : IWeightService
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly ILogger<WeightService> _logger;

        public WeightService(ICosmosRepository cosmosRepository, ILogger<WeightService> logger)
        {
            _cosmosRepository = cosmosRepository;
            _logger = logger;
        }

        public async Task MapAndSaveDocument(string date, WeightMeasurement weight, string provider)
        {
            try
            {
                WeightDocument weightDocument = new WeightDocument
                {
                    Id = weight.LogId?.ToString() ?? Guid.NewGuid().ToString(),
                    Date = date,
                    Weight = weight,
                    DocumentType = "Weight",
                    Provider = provider
                };

                await _cosmosRepository.UpsertWeightDocument(weightDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(MapAndSaveDocument)}: {ex.Message}");
                throw;
            }
        }
    }
}
