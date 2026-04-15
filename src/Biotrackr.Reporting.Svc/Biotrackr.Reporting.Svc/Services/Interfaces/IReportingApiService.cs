using Biotrackr.Reporting.Svc.Models;

namespace Biotrackr.Reporting.Svc.Services.Interfaces;

public interface IReportingApiService
{
    Task<SummaryResult> GenerateReportAsync(string reportType, string startDate, string endDate,
        string taskMessage, HealthDataSnapshot snapshot, CancellationToken cancellationToken);
    Task<byte[]> DownloadArtifactAsync(string sasUrl, CancellationToken cancellationToken);
}
