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
                .WithOpenApi();
        }
    }
}
