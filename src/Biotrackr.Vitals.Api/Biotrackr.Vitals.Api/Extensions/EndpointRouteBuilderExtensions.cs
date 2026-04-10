using Biotrackr.Vitals.Api.EndpointHandlers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Biotrackr.Vitals.Api.Extensions
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void RegisterVitalsEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            var vitalsEndpoints = endpointRouteBuilder.MapGroup("/");

            vitalsEndpoints.MapGet("/", VitalsHandlers.GetAllVitals)
                .WithName("GetAllVitals")
                .WithOpenApi()
                .WithSummary("Gets all vitals documents")
                .WithDescription("Gets all vitals documents from the database");

            vitalsEndpoints.MapGet("/{date}", VitalsHandlers.GetVitalsByDate)
                .WithName("GetVitalsByDate")
                .WithOpenApi()
                .WithSummary("Gets a vitals document by date")
                .WithDescription("Gets a vitals document from the database by date");

            vitalsEndpoints.MapGet("/range/{startDate}/{endDate}", VitalsHandlers.GetVitalsByDateRange)
                .WithName("GetVitalsByDateRange")
                .WithOpenApi()
                .WithSummary("Gets vitals documents within a date range with pagination")
                .WithDescription("Gets paginated vitals documents between the specified start and end dates (inclusive). Supports optional pageNumber and pageSize query parameters.");
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
