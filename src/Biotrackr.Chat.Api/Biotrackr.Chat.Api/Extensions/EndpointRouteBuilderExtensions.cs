using Biotrackr.Chat.Api.Handlers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Biotrackr.Chat.Api.Extensions
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void RegisterChatEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            var conversationEndpoints = endpointRouteBuilder.MapGroup("/conversations");

            conversationEndpoints.MapGet("/", ChatHandlers.GetConversations)
                .WithName("GetConversations")
                .WithOpenApi()
                .WithSummary("Get all conversations")
                .WithDescription("Returns a paginated list of chat conversations, ordered by most recent.");

            conversationEndpoints.MapGet("/{sessionId}", ChatHandlers.GetConversation)
                .WithName("GetConversation")
                .WithOpenApi()
                .WithSummary("Get a conversation by session ID")
                .WithDescription("Returns the full conversation document including all messages.");

            conversationEndpoints.MapDelete("/{sessionId}", ChatHandlers.DeleteConversation)
                .WithName("DeleteConversation")
                .WithOpenApi()
                .WithSummary("Delete a conversation")
                .WithDescription("Permanently deletes a conversation and all its messages.");

            var reportEndpoints = endpointRouteBuilder.MapGroup("/reports");

            reportEndpoints.MapGet("/{jobId}/status", ChatHandlers.GetReportStatus)
                .WithName("GetReportStatus")
                .WithOpenApi()
                .WithSummary("Get report generation status")
                .WithDescription("Proxies to Reporting.Api to get the current status of a report generation job.");
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
