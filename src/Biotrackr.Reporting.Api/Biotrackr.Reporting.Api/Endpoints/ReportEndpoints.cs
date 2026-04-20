using Biotrackr.Reporting.Api.Models;
using Biotrackr.Reporting.Api.Services;

namespace Biotrackr.Reporting.Api.Endpoints
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class ReportEndpoints
    {
        public static void MapReportEndpoints(this WebApplication app)
        {
            app.MapGet("/api/reports", async (
                IBlobStorageService blobStorageService,
                ILogger<Program> logger,
                string? reportType,
                string? startDate,
                string? endDate) =>
            {
                if (reportType is not null && !ReportType.IsValid(reportType))
                {
                    logger.LogWarning("Invalid report type requested: {ReportType}", reportType);
                    return Results.BadRequest(new { error = $"Invalid report type: {reportType}" });
                }

                var reports = await blobStorageService.ListReportsAsync(reportType, startDate, endDate);
                logger.LogInformation("Listed {Count} reports (type={ReportType}, start={StartDate}, end={EndDate})",
                    reports?.Count() ?? 0, reportType, startDate, endDate);
                return Results.Ok(reports);
            }).RequireAuthorization("ChatApiAgent");

            app.MapGet("/api/reports/{jobId}", async (
                string jobId,
                IBlobStorageService blobStorageService,
                ILogger<Program> logger) =>
            {
                var metadata = await blobStorageService.GetMetadataAsync(jobId);
                if (metadata is null)
                {
                    logger.LogWarning("Report job not found: {JobId}", jobId);
                    return Results.NotFound(new { error = $"Report job {jobId} not found" });
                }

                logger.LogInformation("Retrieved report job {JobId}, Status={Status}, Artifacts={ArtifactCount}",
                    jobId, metadata.Status, metadata.Artifacts.Count);

                // Generate fresh SAS URLs for artifacts if report is generated
                if (metadata.Status is ReportStatus.Generated or ReportStatus.Reviewed && metadata.Artifacts.Count > 0)
                {
                    var blobPath = metadata.BlobPath
                        ?? $"{metadata.DateRange.Start}_{metadata.DateRange.End}/{metadata.ReportType}";
                    var artifactUrls = new Dictionary<string, string>();

                    foreach (var artifact in metadata.Artifacts)
                    {
                        var fullPath = $"{blobPath}/{artifact}";
                        var sasUrl = await blobStorageService.GetReportSasUrlAsync(fullPath);
                        artifactUrls[artifact] = sasUrl;
                    }

                    return Results.Ok(new { metadata, artifactUrls });
                }

                return Results.Ok(new { metadata, artifactUrls = new Dictionary<string, string>() });
            }).RequireAuthorization("ChatApiAgent");
        }
    }
}
