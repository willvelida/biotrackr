using Biotrackr.Activity.Api.EndpointHandlers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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

        public static void RegisterHealthCheckEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            var healthEndpoints = endpointRouteBuilder.MapGroup("/health");

            healthEndpoints.MapHealthChecks("/liveness", new HealthCheckOptions
            {
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
            });
        }
    }
}
