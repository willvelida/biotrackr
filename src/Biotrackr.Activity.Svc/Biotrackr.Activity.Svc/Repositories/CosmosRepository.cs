using Biotrackr.Activity.Svc.Configuration;
using Biotrackr.Activity.Svc.Models;
using Biotrackr.Activity.Svc.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Biotrackr.Activity.Svc.Repositories
{
    public class CosmosRepository : ICosmosRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly Settings _settings;
        private readonly ILogger<CosmosRepository> _logger;

        public CosmosRepository(CosmosClient cosmosClient, IOptions<Settings> settings, ILogger<CosmosRepository> logger)
        {
            _cosmosClient = cosmosClient;
            _settings = settings.Value;
            _logger = logger;
            _container = _cosmosClient.GetContainer(_settings.DatabaseName, _settings.ActivityContainer);
        }

        public async Task CreateActivityDocument(ActivityDocument activityDocument)
        {
            try
            {
                ItemRequestOptions itemRequestOptions = new ItemRequestOptions
                {
                    EnableContentResponseOnWrite = false
                };

                await _container.CreateItemAsync(activityDocument, new PartitionKey(activityDocument.Date), itemRequestOptions);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception thrown in CreateActivityDocument: {ex.Message}");
                throw;
            }
        }
    }
}
