using Biotrackr.Food.Api.Configuration;
using Biotrackr.Food.Api.Models;
using Biotrackr.Food.Api.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Biotrackr.Food.Api.Repositories;

public class CosmosRepository : ICosmosRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly Settings _settings;
    private readonly ILogger<CosmosRepository> _logger;

    public CosmosRepository(
        CosmosClient cosmosClient,
        IOptions<Settings> settings,
        ILogger<CosmosRepository> logger)
    {
        _cosmosClient = cosmosClient;
        _settings = settings.Value;
        _container = _cosmosClient.GetContainer(_settings.DatabaseName, _settings.ContainerName);
        _logger = logger;
    }

    public async Task<List<FoodStoredDocument>> GetAllFoodLogsAsync(int pageNumber, int pageSize)
    {
        try
        {
            _logger.LogInformation("Fetching all food logs with pagination: PageNumber={PageNumber}, PageSize={PageSize}", 
                pageNumber, pageSize);

            int skip = (pageNumber - 1) * pageSize;

            var queryDefinition = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = @documentType ORDER BY c.date DESC OFFSET @offset LIMIT @limit")
                .WithParameter("@documentType", "Food")
                .WithParameter("@offset", skip)
                .WithParameter("@limit", pageSize);

            var queryRequestOptions = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey("Food")
            };

            var iterator = _container.GetItemQueryIterator<FoodStoredDocument>(queryDefinition, requestOptions: queryRequestOptions);
            var results = new List<FoodStoredDocument>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            _logger.LogInformation("Found {Count} food logs (page {PageNumber})", results.Count, pageNumber);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown in {MethodName}", nameof(GetAllFoodLogsAsync));
            throw;
        }
    }

    public async Task<FoodStoredDocument?> GetFoodLogByDateAsync(string date)
    {
        try
        {
            _logger.LogInformation("Fetching food log for date: {Date}", date);

            var queryDefinition = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = @documentType AND c.date = @date")
                .WithParameter("@documentType", "Food")
                .WithParameter("@date", date);

            var queryRequestOptions = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey("Food")
            };

            var iterator = _container.GetItemQueryIterator<FoodStoredDocument>(queryDefinition, requestOptions: queryRequestOptions);
            var results = new List<FoodStoredDocument>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown in {MethodName}", nameof(GetFoodLogByDateAsync));
            throw;
        }
    }

    public async Task<List<FoodStoredDocument>> GetFoodLogsByDateRangeAsync(string startDate, string endDate, int pageNumber, int pageSize)
    {
        try
        {
            _logger.LogInformation("Fetching food logs between {StartDate} and {EndDate} with pagination: PageNumber={PageNumber}, PageSize={PageSize}",
                startDate, endDate, pageNumber, pageSize);

            int skip = (pageNumber - 1) * pageSize;

            var queryDefinition = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = @documentType AND c.date >= @startDate AND c.date <= @endDate ORDER BY c.date ASC OFFSET @offset LIMIT @limit")
                .WithParameter("@documentType", "Food")
                .WithParameter("@startDate", startDate)
                .WithParameter("@endDate", endDate)
                .WithParameter("@offset", skip)
                .WithParameter("@limit", pageSize);

            var queryRequestOptions = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey("Food")
            };

            var iterator = _container.GetItemQueryIterator<FoodStoredDocument>(queryDefinition, requestOptions: queryRequestOptions);
            var results = new List<FoodStoredDocument>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            _logger.LogInformation("Found {Count} food logs in date range (page {PageNumber})", results.Count, pageNumber);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown in {MethodName}", nameof(GetFoodLogsByDateRangeAsync));
            throw;
        }
    }

    public async Task<int> GetTotalFoodLogsCountAsync()
    {
        var queryDefinition = new QueryDefinition(
            "SELECT VALUE COUNT(1) FROM c WHERE c.documentType = @documentType")
            .WithParameter("@documentType", "Food");

        var queryRequestOptions = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey("Food")
        };

        var iterator = _container.GetItemQueryIterator<int>(queryDefinition, requestOptions: queryRequestOptions);

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }

        return 0;
    }

    public async Task<int> GetFoodLogsCountByDateRangeAsync(string startDate, string endDate)
    {
        var queryDefinition = new QueryDefinition(
            "SELECT VALUE COUNT(1) FROM c WHERE c.documentType = @documentType AND c.date >= @startDate AND c.date <= @endDate")
            .WithParameter("@documentType", "Food")
            .WithParameter("@startDate", startDate)
            .WithParameter("@endDate", endDate);

        var queryRequestOptions = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey("Food")
        };

        var iterator = _container.GetItemQueryIterator<int>(queryDefinition, requestOptions: queryRequestOptions);

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }

        return 0;
    }
}
