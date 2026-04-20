using System.Diagnostics.CodeAnalysis;
using Biotrackr.Reporting.Api.Services;
using Biotrackr.Reporting.Api.Telemetry;

namespace Biotrackr.Reporting.Api.Endpoints;

[ExcludeFromCodeCoverage]
public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this WebApplication app)
    {
        app.MapPut("/api/reports/{jobId}/review", HandleSubmitReview)
            .RequireAuthorization("ChatApiAgent");
    }

    internal static async Task<IResult> HandleSubmitReview(
        string jobId,
        SubmitReviewRequest request,
        IBlobStorageService blobStorageService,
        ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return Results.BadRequest(new { error = "jobId is required" });
        }

        try
        {
            await blobStorageService.UpdateReviewResultAsync(
                jobId,
                request.Approved,
                request.Concerns,
                request.ValidatedSummary);

            ReportingTelemetry.ReportsReviewed.Add(1);
            logger.LogInformation("Report {JobId} reviewed: Approved={Approved}, Concerns={ConcernCount}",
                jobId, request.Approved, request.Concerns.Count);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not in generated status"))
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }
}

public class SubmitReviewRequest
{
    public bool Approved { get; set; }
    public List<string> Concerns { get; set; } = [];
    public string ValidatedSummary { get; set; } = string.Empty;
}
