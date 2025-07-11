using Biotrackr.Weight.Api.Configuration;
using Biotrackr.Weight.Api.Models;
using Biotrackr.Weight.Api.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Biotrackr.Weight.Api.Repositories
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

        public async Task<PaginationResponse<WeightDocument>> GetAllWeightDocuments(PaginationRequest paginationRequest)
        {
            try
            {
                _logger.LogInformation($"Fetching all weight documents with pagination: PageNumber={paginationRequest.PageNumber}, PageSize={paginationRequest.PageSize}");

                var totalCount = await GetTotalWeightCount();

                QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c ORDER BY c._ts DESC OFFSET @offset LIMIT @limit")
                    .WithParameter("@offset", paginationRequest.Skip)
                    .WithParameter("@limit", paginationRequest.PageSize);
                QueryRequestOptions queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Weight")
                };

                var iterator = _container.GetItemQueryIterator<WeightDocument>(queryDefinition, requestOptions: queryRequestOptions);
                var results = new List<WeightDocument>();

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                return new PaginationResponse<WeightDocument>
                {
                    Items = results,
                    TotalCount = totalCount,
                    PageNumber = paginationRequest.PageNumber,
                    PageSize = paginationRequest.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetAllWeightDocuments)}: {ex.Message}");
                throw;
            }
        }

        public async Task<WeightDocument> GetWeightDocumentByDate(string date)
        {
            try
            {
                QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
                    .WithParameter("@date", date);
                QueryRequestOptions queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Weight")
                };

                var iterator = _container.GetItemQueryIterator<WeightDocument>(queryDefinition, requestOptions: queryRequestOptions);
                var results = new List<WeightDocument>();

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetWeightDocumentByDate)}: {ex.Message}");
                throw;
            }
        }

        private async Task<int> GetTotalWeightCount()
        {
            try
            {
                QueryDefinition queryDefinition = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
                QueryRequestOptions queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Weight")
                };

                var iterator = _container.GetItemQueryIterator<int>(queryDefinition, requestOptions: queryRequestOptions);

                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetTotalWeightCount)}: {ex.Message}");
                return 0;
            }
        }
    }
}
