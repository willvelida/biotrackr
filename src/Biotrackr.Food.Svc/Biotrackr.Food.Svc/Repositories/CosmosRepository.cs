using Biotrackr.Food.Svc.Configuration;
using Biotrackr.Food.Svc.Models;
using Biotrackr.Food.Svc.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Repositories
{
    public class CosmosRepository : ICosmosRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly Settings _settings;
        private readonly ILogger<CosmosRepository> _logger;

        public CosmosRepository(CosmosClient cosmosClient, IOptions<Settings> settings, ILogger<CosmosRepository> logger)
        {
            _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));

            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(_settings.DatabaseName))
                throw new ArgumentNullException("DatabaseName cannot be null or empty");

            if (string.IsNullOrEmpty(_settings.ContainerName))
                throw new ArgumentNullException("ContainerName cannot be null or empty");

            _container = _cosmosClient.GetContainer(_settings.DatabaseName, _settings.ContainerName);
        }

        public async Task CreateFoodDocument(FoodDocument foodDocument)
        {
            try
            {
                ItemRequestOptions itemRequestOptions = new ItemRequestOptions
                {
                    EnableContentResponseOnWrite = false
                };

                await _container.CreateItemAsync(foodDocument, new PartitionKey(foodDocument.DocumentType), itemRequestOptions);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception thrown in CreateActivityDocument: {ex.Message}");
                throw;
            }
        }
    }
}
