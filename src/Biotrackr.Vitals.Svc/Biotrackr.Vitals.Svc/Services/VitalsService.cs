using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Repositories.Interfaces;
using Biotrackr.Vitals.Svc.Services.Interfaces;

namespace Biotrackr.Vitals.Svc.Services
{
    public class VitalsService : IVitalsService
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly ILogger<VitalsService> _logger;

        public VitalsService(ICosmosRepository cosmosRepository, ILogger<VitalsService> logger)
        {
            _cosmosRepository = cosmosRepository;
            _logger = logger;
        }

        public async Task UpsertVitalsDocument(VitalsDocument vitalsDocument)
        {
            try
            {
                var existingDocument = await _cosmosRepository.GetVitalsDocumentByDate(vitalsDocument.Date);

                if (existingDocument is not null)
                {
                    vitalsDocument.Id = existingDocument.Id;
                    _logger.LogInformation($"Updating existing VitalsDocument for date {vitalsDocument.Date} with Id {existingDocument.Id}");
                }
                else
                {
                    vitalsDocument.Id = Guid.NewGuid().ToString();
                    _logger.LogInformation($"Creating new VitalsDocument for date {vitalsDocument.Date} with Id {vitalsDocument.Id}");
                }

                vitalsDocument.DocumentType = "Vitals";

                await _cosmosRepository.UpsertVitalsDocument(vitalsDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(UpsertVitalsDocument)}: {ex.Message}");
                throw;
            }
        }
    }
}
