using Biotrackr.Activity.Api.EndpointHandlers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Biotrackr.Activity.Api.Extensions
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void RegisterActivityEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            var activityEndpoints = endpointRouteBuilder.MapGroup("/");

            activityEndpoints.MapGet("/", ActivityHandlers.GetAllActivities)
                .WithName("GetAllActivities")
                .WithOpenApi()
                .WithSummary("Get all Activity Summaries")
                .WithDescription("You can get all activity summaries via this endpoint");

            activityEndpoints.MapGet("/{date}", ActivityHandlers.GetActivityByDate)
                .WithName("GetActivityByDate")
                .WithOpenApi()
                .WithSummary("Get an Activity Summary by providing a date")
                .WithDescription("You can get a specific activity summary via this endpoint by providing the date in the following format (YYYY-MM-DD)");

            activityEndpoints.MapGet("/range/{startDate}/{endDate}", ActivityHandlers.GetActivitiesByDateRange)
                .WithName("GetActivitiesByDateRange")
                .WithOpenApi()
                .WithSummary("Gets activity documents within a date range with pagination")
                .WithDescription("Gets paginated activity documents between the specified start and end dates (inclusive). Supports optional pageNumber and pageSize query parameters.");
        }

        public static void RegisterHealthCheckEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            var healthEndpoints = endpointRouteBuilder.MapGroup("/healthz");

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
