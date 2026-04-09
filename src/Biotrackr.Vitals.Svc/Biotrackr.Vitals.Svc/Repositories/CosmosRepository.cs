using Biotrackr.Vitals.Svc.Configuration;
using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Biotrackr.Vitals.Svc.Repositories
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
            _container = _cosmosClient.GetContainer(_settings.DatabaseName, _settings.ContainerName);
            _logger = logger;
        }

        public async Task<VitalsDocument?> GetVitalsDocumentByDate(string date)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date AND c.documentType = 'Vitals'")
                    .WithParameter("@date", date);

                using var iterator = _container.GetItemQueryIterator<VitalsDocument>(query, requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Vitals")
                });

                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetVitalsDocumentByDate)}: {ex.Message}");
                throw;
            }
        }

        public async Task UpsertVitalsDocument(VitalsDocument vitalsDocument)
        {
            try
            {
                ItemRequestOptions itemRequestOptions = new ItemRequestOptions
                {
                    EnableContentResponseOnWrite = false
                };

                await _container.UpsertItemAsync(vitalsDocument, new PartitionKey(vitalsDocument.DocumentType), itemRequestOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(UpsertVitalsDocument)}: {ex.Message}");
                throw;
            }
        }
    }
}
