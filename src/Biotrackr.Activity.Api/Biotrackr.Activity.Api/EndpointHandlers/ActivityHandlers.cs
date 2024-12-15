using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Biotrackr.Activity.Api.EndpointHandlers
{
    public static class ActivityHandlers
    {
        public static async Task<Results<NotFound, Ok<ActivityDocument>>> GetActivityByDate(
            ICosmosRepository cosmosRepository,
            string date)
        {
            var activity = await cosmosRepository.GetActivitySummaryByDate(date);
            if (activity == null)
            {
                return TypedResults.NotFound();
            }
            return TypedResults.Ok(activity);
        }
    }
}
