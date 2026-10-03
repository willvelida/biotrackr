using Biotrackr.Food.Api.Models;
using Biotrackr.Food.Api.Repositories.Interfaces;
using Biotrackr.Food.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Biotrackr.Food.Api.EndpointHandlers;

public static class FoodHandlers
{
    public static async Task<Results<BadRequest, NotFound, Ok<FoodDocument>>> GetFoodLogByDate(
        ICosmosRepository cosmosRepository,
        IFoodDocumentTranslator translator,
        string date)
    {
        // Validate date format
        if (!DateOnly.TryParse(date, out _))
        {
            return TypedResults.BadRequest();
        }

        var foodLog = await cosmosRepository.GetFoodLogByDateAsync(date);
        if (foodLog == null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(translator.Translate(foodLog));
    }

    public static async Task<Ok<PaginationResponse<FoodDocument>>> GetAllFoodLogs(
        ICosmosRepository cosmosRepository,
        IFoodDocumentTranslator translator,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var paginationRequest = new PaginationRequest
        {
            PageNumber = pageNumber ?? 1,
            PageSize = pageSize ?? 20
        };

        var foodLogs = await cosmosRepository.GetAllFoodLogsAsync(paginationRequest.PageNumber, paginationRequest.PageSize);
        var totalCount = await cosmosRepository.GetTotalFoodLogsCountAsync();

        var response = new PaginationResponse<FoodDocument>
        {
            Items = foodLogs.ConvertAll(translator.Translate),
            TotalCount = totalCount,
            PageNumber = paginationRequest.PageNumber,
            PageSize = paginationRequest.PageSize
        };

        return TypedResults.Ok(response);
    }

    public static async Task<Results<BadRequest, Ok<PaginationResponse<FoodDocument>>>> GetFoodLogsByDateRange(
        ICosmosRepository cosmosRepository,
        IFoodDocumentTranslator translator,
        string startDate,
        string endDate,
        int? pageNumber = null,
        int? pageSize = null)
    {
        // Validate date formats
        if (!DateOnly.TryParse(startDate, out var parsedStartDate) ||
            !DateOnly.TryParse(endDate, out var parsedEndDate))
        {
            return TypedResults.BadRequest();
        }

        // Validate date range (start date should be before or equal to end date)
        if (parsedStartDate > parsedEndDate)
        {
            return TypedResults.BadRequest();
        }

        var paginationRequest = new PaginationRequest
        {
            PageNumber = pageNumber ?? 1,
            PageSize = pageSize ?? 20
        };

        var foodLogs = await cosmosRepository.GetFoodLogsByDateRangeAsync(startDate, endDate, paginationRequest.PageNumber, paginationRequest.PageSize);
        var totalCount = await cosmosRepository.GetFoodLogsCountByDateRangeAsync(startDate, endDate);

        var response = new PaginationResponse<FoodDocument>
        {
            Items = foodLogs.ConvertAll(translator.Translate),
            TotalCount = totalCount,
            PageNumber = paginationRequest.PageNumber,
            PageSize = paginationRequest.PageSize
        };

        return TypedResults.Ok(response);
    }
}
