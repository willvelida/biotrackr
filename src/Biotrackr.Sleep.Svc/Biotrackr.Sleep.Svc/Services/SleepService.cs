using Biotrackr.Sleep.Svc.Models;
using Biotrackr.Sleep.Svc.Models.FitbitEntities;
using Biotrackr.Sleep.Svc.Repositories.Interfaces;
using Biotrackr.Sleep.Svc.Services.Interfaces;

namespace Biotrackr.Sleep.Svc.Services
{
    public class SleepService : ISleepService
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly ILogger<SleepService> _logger;

        public SleepService(ICosmosRepository cosmosRepository, ILogger<SleepService> logger)
        {
            _cosmosRepository = cosmosRepository;
            _logger = logger;
        }

        public async Task MapAndSaveDocument(string date, SleepResponse sleepResponse)
        {
            try
            {
                SleepDocument sleepDocument = new SleepDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    SleepResponse = sleepResponse,
                    Date = date,
                    DocumentType = "Sleep"
                };

                await _cosmosRepository.CreateSleepDocument(sleepDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(MapAndSaveDocument)}: {ex.Message}");
                throw;
            }
        }
    }
}
