using Biotrackr.Vitals.Api.Configuration;
using Biotrackr.Vitals.Api.Models;
using Biotrackr.Vitals.Api.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Biotrackr.Vitals.Api.Repositories
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

        public async Task<PaginationResponse<VitalsDocument>> GetAllVitalsDocuments(PaginationRequest paginationRequest)
        {
            try
            {
                _logger.LogInformation($"Fetching all vitals documents with pagination: PageNumber={paginationRequest.PageNumber}, PageSize={paginationRequest.PageSize}");

                var totalCount = await GetTotalVitalsCount();

                QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c ORDER BY c._ts DESC OFFSET @offset LIMIT @limit")
                    .WithParameter("@offset", paginationRequest.Skip)
                    .WithParameter("@limit", paginationRequest.PageSize);
                QueryRequestOptions queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Vitals")
                };

                var iterator = _container.GetItemQueryIterator<VitalsDocument>(queryDefinition, requestOptions: queryRequestOptions);
                var results = new List<VitalsDocument>();

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                return new PaginationResponse<VitalsDocument>
                {
                    Items = results,
                    TotalCount = totalCount,
                    PageNumber = paginationRequest.PageNumber,
                    PageSize = paginationRequest.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetAllVitalsDocuments)}: {ex.Message}");
                throw;
            }
        }

        public async Task<VitalsDocument> GetVitalsDocumentByDate(string date)
        {
            try
            {
                QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.date = @date")
                    .WithParameter("@date", date);
                QueryRequestOptions queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Vitals")
                };

                var iterator = _container.GetItemQueryIterator<VitalsDocument>(queryDefinition, requestOptions: queryRequestOptions);
                var results = new List<VitalsDocument>();

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetVitalsDocumentByDate)}: {ex.Message}");
                throw;
            }
        }

        public async Task<PaginationResponse<VitalsDocument>> GetVitalsByDateRange(string startDate, string endDate, PaginationRequest paginationRequest)
        {
            try
            {
                _logger.LogInformation($"Fetching vitals documents between {startDate} and {endDate} with pagination: PageNumber={paginationRequest.PageNumber}, PageSize={paginationRequest.PageSize}");

                // Get total count for the date range
                var totalCount = await GetVitalsCountForDateRange(startDate, endDate);

                // Get paginated results
                QueryDefinition queryDefinition = new QueryDefinition(
                    "SELECT * FROM c WHERE c.documentType = 'Vitals' AND c.date >= @startDate AND c.date <= @endDate ORDER BY c.date ASC OFFSET @offset LIMIT @limit")
                    .WithParameter("@startDate", startDate)
                    .WithParameter("@endDate", endDate)
                    .WithParameter("@offset", paginationRequest.Skip)
                    .WithParameter("@limit", paginationRequest.PageSize);

                QueryRequestOptions queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Vitals")
                };

                var iterator = _container.GetItemQueryIterator<VitalsDocument>(queryDefinition, requestOptions: queryRequestOptions);
                var results = new List<VitalsDocument>();

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                _logger.LogInformation($"Found {results.Count} vitals documents in date range (page {paginationRequest.PageNumber})");

                return new PaginationResponse<VitalsDocument>
                {
                    Items = results,
                    TotalCount = totalCount,
                    PageNumber = paginationRequest.PageNumber,
                    PageSize = paginationRequest.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetVitalsByDateRange)}: {ex.Message}");
                throw;
            }
        }

        private async Task<int> GetTotalVitalsCount()
        {
            try
            {
                QueryDefinition queryDefinition = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
                QueryRequestOptions queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Vitals")
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
                _logger.LogError($"Exception thrown in {nameof(GetTotalVitalsCount)}: {ex.Message}");
                return 0;
            }
        }

        private async Task<int> GetVitalsCountForDateRange(string startDate, string endDate)
        {
            try
            {
                QueryDefinition queryDefinition = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.date >= @startDate AND c.date <= @endDate")
                    .WithParameter("@startDate", startDate)
                    .WithParameter("@endDate", endDate);
                QueryRequestOptions queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("Vitals")
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
                _logger.LogError($"Exception thrown in {nameof(GetVitalsCountForDateRange)}: {ex.Message}");
                return 0;
            }
        }
    }
}
