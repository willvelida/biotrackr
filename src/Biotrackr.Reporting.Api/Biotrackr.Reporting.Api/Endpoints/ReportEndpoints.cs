using Biotrackr.Reporting.Api.Models;
using Biotrackr.Reporting.Api.Services;

namespace Biotrackr.Reporting.Api.Endpoints
{
    public static class ReportEndpoints
    {
        public static void MapReportEndpoints(this WebApplication app)
        {
            app.MapGet("/api/reports", async (
                IBlobStorageService blobStorageService,
                string? reportType,
                string? startDate,
                string? endDate) =>
            {
                if (reportType is not null && !ReportType.IsValid(reportType))
                {
                    return Results.BadRequest(new { error = $"Invalid report type: {reportType}" });
                }

                var reports = await blobStorageService.ListReportsAsync(reportType, startDate, endDate);
                return Results.Ok(reports);
            }).RequireAuthorization("ChatApiAgent");

            app.MapGet("/api/reports/{jobId}", async (
                string jobId,
                IBlobStorageService blobStorageService) =>
            {
                var metadata = await blobStorageService.GetMetadataAsync(jobId);
                if (metadata is null)
                {
                    return Results.NotFound(new { error = $"Report job {jobId} not found" });
                }

                // Generate fresh SAS URLs for artifacts if report is generated
                if (metadata.Status is ReportStatus.Generated or ReportStatus.Reviewed && metadata.Artifacts.Count > 0)
                {
                    var blobPath = $"{metadata.DateRange.Start}_{metadata.DateRange.End}/{metadata.ReportType}";
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
