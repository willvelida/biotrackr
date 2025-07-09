using Biotrackr.Activity.Api.Configuration;
using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Biotrackr.Activity.Api.Repositories
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

        public async Task<ActivityDocument> GetActivitySummaryByDate(string date)
        {
            try
            {
                QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
                    .WithParameter("@date", date);

                QueryRequestOptions queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Activity")
                };

                var iterator = _container.GetItemQueryIterator<ActivityDocument>(queryDefinition, null, queryRequestOptions);
                var results = new List<ActivityDocument>();

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }

                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetActivitySummaryByDate)}: {ex.Message}");
                throw;
            }
        }

        public async Task<PaginationResponse<ActivityDocument>> GetAllActivitySummaries(PaginationRequest request)
        {
            try
            {
                _logger.LogInformation($"Getting all activity summaries with pagination: PageNumber={request.PageNumber}, PageSize={request.PageSize}");

                var totalCount = await GetTotalActivityCount();

                QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c ORDER BY c_ts DESC OFFSET @offset LIMIT @limit")
                    .WithParameter("@offset", request.Skip)
                    .WithParameter("@limit", request.PageSize);

                QueryRequestOptions queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Activity")
                };

                var iterator = _container.GetItemQueryIterator<ActivityDocument>(queryDefinition, null, queryRequestOptions);
                var results = new List<ActivityDocument>();

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }

                _logger.LogInformation($"Retrieved {results.Count} activity summaries for page: {request.PageNumber}");

                return new PaginationResponse<ActivityDocument>
                {
                    Items = results,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetAllActivitySummaries)}: {ex.Message}");
                throw;
            }
        }

        private async Task<int> GetTotalActivityCount()
        {
            try
            {
                var countQuery = new QueryDefinition("SELECT VALUE COUNT (1) FROM c");
                var queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Activity")
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
                _logger.LogError($"Exception thrown in {nameof(GetTotalActivityCount)}: {ex.Message}");
                return 0;
            }
        }
    }
}
