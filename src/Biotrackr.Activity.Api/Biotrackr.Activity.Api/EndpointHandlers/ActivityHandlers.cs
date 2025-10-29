using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Biotrackr.Activity.Api.EndpointHandlers
{
    public static class ActivityHandlers
    {
        public static async Task<Results<BadRequest, NotFound, Ok<ActivityDocument>>> GetActivityByDate(
            ICosmosRepository cosmosRepository,
            string date)
        {
            // Validate date format
            if (!DateOnly.TryParse(date, out _))
            {
                return TypedResults.BadRequest();
            }

            var activity = await cosmosRepository.GetActivitySummaryByDate(date);
            if (activity == null)
            {
                return TypedResults.NotFound();
            }
            return TypedResults.Ok(activity);
        }

        public static async Task<Ok<PaginationResponse<ActivityDocument>>> GetAllActivities(
            ICosmosRepository cosmosRepository,
            int? pageNumber = null,
            int? pageSize = null)
        {
            var paginationRequest = new PaginationRequest
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 20
            };

            var activities = await cosmosRepository.GetAllActivitySummaries(paginationRequest);
            return TypedResults.Ok(activities);
        }

        public static async Task<Results<BadRequest, Ok<PaginationResponse<ActivityDocument>>>> GetActivitiesByDateRange(
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

            var activityDocuments = await cosmosRepository.GetActivitiesByDateRange(startDate, endDate, paginationRequest);
            return TypedResults.Ok(activityDocuments);
        }
    }
}
