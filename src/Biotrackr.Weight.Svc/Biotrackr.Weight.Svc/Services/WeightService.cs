using Biotrackr.Weight.Svc.Models;
using Biotrackr.Weight.Svc.Repositories.Interfaces;
using Biotrackr.Weight.Svc.Services.Interfaces;
using ent = Biotrackr.Weight.Svc.Models.Entities;

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

        public async Task MapAndSaveDocument(string date, ent.Weight weight)
        {
            try
            {
                WeightDocument weightDocument = new WeightDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = date,
                    Weight = weight,
                    DocumentType = "Weight"
                };

                await _cosmosRepository.CreateWeightDocument(weightDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(MapAndSaveDocument)}: {ex.Message}");
                throw;
            }
        }
    }
}
