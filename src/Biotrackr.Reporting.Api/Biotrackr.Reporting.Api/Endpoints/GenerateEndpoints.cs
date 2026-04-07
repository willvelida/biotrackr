using Biotrackr.Reporting.Api.Models;
using Biotrackr.Reporting.Api.Services;
using Biotrackr.Reporting.Api.Validation;

namespace Biotrackr.Reporting.Api.Endpoints
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class GenerateEndpoints
    {
        public static void MapGenerateEndpoints(this WebApplication app)
        {
            app.MapPost("/api/reports/generate", async (
                GenerateReportRequest request,
                IReportGenerationService reportGenerationService,
                ILogger<Program> logger) =>
            {
                var validation = ReportRequestValidator.Validate(request);
                if (!validation.IsValid)
                {
                    if (validation.WarningMessage is not null)
                    {
                        logger.LogWarning(validation.WarningMessage);
                    }
                    return Results.BadRequest(new { error = validation.ErrorMessage });
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
