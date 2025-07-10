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
    }
}
