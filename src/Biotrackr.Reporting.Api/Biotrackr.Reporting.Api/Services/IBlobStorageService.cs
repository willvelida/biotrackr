using Biotrackr.Reporting.Api.Models;

namespace Biotrackr.Reporting.Api.Services
{
    public interface IBlobStorageService
    {
        Task<string> CreateJobAsync(string reportType, string startDate, string endDate);
        Task UploadReportAsync(string jobId, byte[] pdfBytes, Dictionary<string, byte[]> charts, string summary, object sourceDataSnapshot);
        Task UpdateJobStatusAsync(string jobId, string status, string? error = null);
        Task UpdateReviewResultAsync(string jobId, bool approved, List<string> concerns, string validatedSummary);
        Task<ReportMetadata?> GetMetadataAsync(string jobId);
        Task<string> GetReportSasUrlAsync(string blobPath);
        Task<List<ReportMetadata>> ListReportsAsync(string? reportType = null, string? startDate = null, string? endDate = null);
    }
}
