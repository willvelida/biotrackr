using System.Globalization;
using System.Text.Json;
using Biotrackr.Reporting.Api.Models;
using Biotrackr.Reporting.Api.Services;

namespace Biotrackr.Reporting.Api.Endpoints
{
    public static class GenerateEndpoints
    {
        private const int MaxTaskMessageLength = 5000;

        // Prompt-injection detection patterns (ASI01)
        private static readonly string[] InjectionPatterns =
        [
            "ignore previous", "ignore all previous", "disregard previous",
            "system prompt", "you are now", "new instructions",
            "override instructions", "forget your instructions",
            "ignore above", "disregard above", "forget above",
            "act as", "pretend you are", "simulate being"
        ];

        public static void MapGenerateEndpoints(this WebApplication app)
        {
            app.MapPost("/api/reports/generate", async (
                GenerateReportRequest request,
                IReportGenerationService reportGenerationService,
                ILogger<Program> logger) =>
            {
                // Validate report type
                if (!ReportType.IsValid(request.ReportType))
                {
                    return Results.BadRequest(new { error = $"Invalid report type: {request.ReportType}. Valid types: {string.Join(", ", ReportType.All)}" });
                }

                // Validate dates
                if (!DateOnly.TryParseExact(request.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
                {
                    return Results.BadRequest(new { error = "Invalid startDate format. Use yyyy-MM-dd." });
                }

                if (!DateOnly.TryParseExact(request.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
                {
                    return Results.BadRequest(new { error = "Invalid endDate format. Use yyyy-MM-dd." });
                }

                if (endDate < startDate)
                {
                    return Results.BadRequest(new { error = "endDate must be after startDate." });
                }

                if ((endDate.DayNumber - startDate.DayNumber) > 365)
                {
                    return Results.BadRequest(new { error = "Date range cannot exceed 365 days." });
                }

                if (string.IsNullOrWhiteSpace(request.TaskMessage))
                {
                    return Results.BadRequest(new { error = "taskMessage is required." });
                }

                // Enforce maximum task message length (ASI01)
                if (request.TaskMessage.Length > MaxTaskMessageLength)
                {
                    return Results.BadRequest(new { error = $"taskMessage must not exceed {MaxTaskMessageLength} characters." });
                }

                // Prompt-injection detection (ASI01)
                var lowerMessage = request.TaskMessage.ToLowerInvariant();
                if (InjectionPatterns.Any(pattern => lowerMessage.Contains(pattern)))
                {
                    logger.LogWarning("Potential prompt injection detected in taskMessage");
                    return Results.BadRequest(new { error = "taskMessage contains disallowed content." });
                }

                // Validate source data snapshot is provided (enables reviewer cross-validation)
                if (request.SourceDataSnapshot is null || IsEmptySnapshot(request.SourceDataSnapshot))
                {
                    logger.LogWarning("Report generation request missing sourceDataSnapshot");
                    return Results.BadRequest(new { error = "sourceDataSnapshot is required. Health data must be fetched and provided for report generation." });
                }

                var result = await reportGenerationService.StartReportGenerationAsync(
                    request.ReportType,
                    request.StartDate,
                    request.EndDate,
                    request.TaskMessage,
                    request.SourceDataSnapshot ?? new object());

                if (result.Status == ReportStatus.Failed)
                {
                    return Results.StatusCode(503);
                }

                logger.LogInformation("Report generation started. Job {JobId}", result.JobId);

                return Results.Accepted(value: result);
            }).RequireAuthorization("ChatApiAgent");
        }

        /// <summary>
        /// Checks whether the source data snapshot is effectively empty (null, empty object, or empty string).
        /// </summary>
        internal static bool IsEmptySnapshot(object snapshot)
        {
            if (snapshot is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Null or JsonValueKind.Undefined => true,
                    JsonValueKind.Object => element.EnumerateObject().MoveNext() is false,
                    JsonValueKind.Array => element.GetArrayLength() == 0,
                    JsonValueKind.String => string.IsNullOrWhiteSpace(element.GetString()),
                    _ => false
                };
            }

            var json = snapshot.ToString();
            return string.IsNullOrWhiteSpace(json) || json == "{}";
        }
    }

    public class GenerateReportRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string TaskMessage { get; set; } = string.Empty;
        public object? SourceDataSnapshot { get; set; }
    }
}
