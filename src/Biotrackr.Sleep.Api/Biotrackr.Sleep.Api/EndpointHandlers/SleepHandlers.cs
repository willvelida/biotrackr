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

        public static async Task<Ok<List<SleepDocument>>> GetAllSleeps(
            ICosmosRepository cosmosRepository)
        {
            var sleeps = await cosmosRepository.GetAllSleepDocuments();
            return TypedResults.Ok(sleeps);
        }
    }
}
