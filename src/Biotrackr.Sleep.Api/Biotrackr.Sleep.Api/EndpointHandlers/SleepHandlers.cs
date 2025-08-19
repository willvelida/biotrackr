using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Biotrackr.Sleep.Api.EndpointHandlers
{
    public static class SleepHandlers
    {
        public static async Task<Results<NotFound, Ok<SleepDocument>>> GetSleepByDate(
            ICosmosRepository cosmosRepository,
            string date)
        {
            var sleep = await cosmosRepository.GetSleepSummaryByDate(date);
            if (sleep == null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(sleep);
        }

        public static async Task<Ok<PaginationResponse<SleepDocument>>> GetAllSleeps(
            ICosmosRepository cosmosRepository,
            int? pageNumber = null,
            int? pageSize = null)
        {
            var paginationRequest = new PaginationRequest
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 20
            };

            var sleeps = await cosmosRepository.GetAllSleepDocuments(paginationRequest);
            return TypedResults.Ok(sleeps);
        }

        public static async Task<Results<BadRequest, Ok<PaginationResponse<SleepDocument>>>> GetSleepsByDateRange(
            ICosmosRepository cosmosRepository,
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

            var activityDocuments = await cosmosRepository.GetSleepDocumentsByDateRange(startDate, endDate, paginationRequest);
            return TypedResults.Ok(activityDocuments);
        }
    }
}
