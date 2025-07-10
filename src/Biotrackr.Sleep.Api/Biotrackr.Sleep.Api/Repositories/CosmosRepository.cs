using Biotrackr.Sleep.Api.Configuration;
using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Biotrackr.Sleep.Api.Repositories
{
    public class CosmosRepository : ICosmosRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly Settings _settings;
        private readonly ILogger<CosmosRepository> _logger;

        public CosmosRepository(CosmosClient cosmosClient, IOptions<Settings> options, ILogger<CosmosRepository> logger)
        {
            _cosmosClient = cosmosClient;
            _settings = options.Value;
            _container = _cosmosClient.GetContainer(_settings.DatabaseName, _settings.ContainerName);
            _logger = logger;
        }

        public async Task<PaginationResponse<SleepDocument>> GetAllSleepDocuments(PaginationRequest request)
        {
            try
            {
                _logger.LogInformation($"Fetching all sleep documents with pagination: PageNumber={request.PageNumber}, PageSize={request.PageSize}");

                var totalSleepCount = await GetTotalSleepCount();

                QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c ORDER BY c._ts DESC OFFSET @offset LIMIT @limit")
                    .WithParameter("@offset", request.Skip)
                    .WithParameter("@limit", request.PageSize);

                QueryRequestOptions queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Sleep")
                };

                var iterator = _container.GetItemQueryIterator<SleepDocument>(queryDefinition, requestOptions: queryRequestOptions);
                var results = new List<SleepDocument>();

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                _logger.LogInformation($"Fetched {results.Count} sleep documents out of {totalSleepCount} total records.");

                return new PaginationResponse<SleepDocument>
                {
                    Items = results,
                    TotalCount = totalSleepCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetAllSleepDocuments)}: {ex.Message}");
                throw;
            }
        }

        public async Task<SleepDocument> GetSleepSummaryByDate(string date)
        {
            try
            {
                QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
                    .WithParameter("@date", date);
                QueryRequestOptions queryRequestOptions = new QueryRequestOptions()
                {
                    PartitionKey = new PartitionKey("Sleep")
                };

                var iterator = _container.GetItemQueryIterator<SleepDocument>(queryDefinition, requestOptions: queryRequestOptions);
                List<SleepDocument> results = new List<SleepDocument>();

                while (iterator.HasMoreResults)
                {
                    FeedResponse<SleepDocument> response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }

                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetSleepSummaryByDate)}: {ex.Message}");
                throw;
            }
        }

        private async Task<int> GetTotalSleepCount()
        {
            try
            {
                var countQuery = new QueryDefinition("SELECT VALUE COUNT (1) FROM c");
                var queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Sleep")
                };

                var iterator = _container.GetItemQueryIterator<int>(countQuery, requestOptions: queryRequestOptions);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetTotalSleepCount)}: {ex.Message}");
                return 0;
            }
        }
    }
}
