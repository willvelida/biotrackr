using Biotrackr.Activity.Api.EndpointHandlers;

namespace Biotrackr.Activity.Api.Extensions
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void RegisterActivityEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            var activityEndpoints = endpointRouteBuilder.MapGroup("/activity");

            activityEndpoints.MapGet("/{date}", ActivityHandlers.GetActivityByDate)
                .WithName("GetActivityByDate")
                .WithOpenApi()
                .WithSummary("Get an Activity Summary by providing a date")
                .WithDescription("You can get a specific activity summary via this endpoint by providing the date in the following format (YYYY-MM-DD)");
        }
    }
}
