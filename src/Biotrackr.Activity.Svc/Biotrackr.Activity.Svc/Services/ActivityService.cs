using Biotrackr.Activity.Svc.Models;
using Biotrackr.Activity.Svc.Models.FitbitEntities;
using Biotrackr.Activity.Svc.Repositories.Interfaces;
using Biotrackr.Activity.Svc.Services.Interfaces;

namespace Biotrackr.Activity.Svc.Services
{
    public class ActivityService : IActivityService
    {
        private readonly ICosmosRepository _cosmosDbRepository;
        private readonly ILogger<ActivityService> _logger;

        public ActivityService(ICosmosRepository cosmosDbRepository, ILogger<ActivityService> logger)
        {
            _cosmosDbRepository = cosmosDbRepository;
            _logger = logger;
        }

        public async Task MapAndSaveDocument(string date, ActivityResponse activityResponse)
        {
            try
            {
                ActivityDocument activityDocument = new ActivityDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = date,
                    Activity = activityResponse
                };

                await _cosmosDbRepository.CreateActivityDocument(activityDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in MapAndSaveDocument: {ex.Message}");
                throw;
            }
        }
    }
}
