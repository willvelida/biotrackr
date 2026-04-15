using Biotrackr.Reporting.Svc.Models;

namespace Biotrackr.Reporting.Svc.Services.Interfaces;

public interface IEmailService
{
    Task SendReportEmailAsync(string cadence, string startDate, string endDate,
        string? summary, byte[] pdfAttachment, List<MetricCard> metrics,
        CancellationToken cancellationToken);
}
