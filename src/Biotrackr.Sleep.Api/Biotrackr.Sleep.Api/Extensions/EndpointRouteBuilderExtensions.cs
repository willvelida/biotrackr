using Biotrackr.Sleep.Api.EndpointHandlers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Biotrackr.Sleep.Api.Extensions
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void RegisterSleepEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            var sleepEndpoints = endpointRouteBuilder.MapGroup("/");

            sleepEndpoints.MapGet("/", SleepHandlers.GetAllSleeps)
                .WithName("GetAllSleeps")
                .WithOpenApi()
                .WithSummary("Gets all sleep documents")
                .WithDescription("Gets all sleep documents from the database");
            sleepEndpoints.MapGet("/{date}", SleepHandlers.GetSleepByDate)
                .WithName("GetSleepByDate")
                .WithOpenApi()
                .WithSummary("Gets a sleep document by date")
                .WithDescription("Gets a sleep document by date from the database");
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
