using Biotrackr.Weight.Api.EndpointHandlers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Biotrackr.Weight.Api.Extensions
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void RegisterWeightEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            var weightEndpoints = endpointRouteBuilder.MapGroup("/");

            weightEndpoints.MapGet("/", WeightHandlers.GetAllWeights)
                .WithName("GetAllWeights")
                .WithOpenApi()
                .WithSummary("Gets all weight documents")
                .WithDescription("Gets all weight documents from the database");

            weightEndpoints.MapGet("/{date}", WeightHandlers.GetWeightByDate)
                .WithName("GetWeightByDate")
                .WithOpenApi()
                .WithSummary("Gets a weight document by date")
                .WithDescription("Gets a weight document from the database by date");

            weightEndpoints.MapGet("/range/{startDate}/{endDate}", WeightHandlers.GetWeightsByDateRange)
                .WithName("GetWeightsByDateRange")
                .WithOpenApi()
                .WithSummary("Gets weight documents within a date range with pagination")
                .WithDescription("Gets paginated weight documents between the specified start and end dates (inclusive). Supports optional pageNumber and pageSize query parameters.");
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
