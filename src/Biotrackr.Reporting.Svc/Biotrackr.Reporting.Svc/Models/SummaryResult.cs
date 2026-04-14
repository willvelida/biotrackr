namespace Biotrackr.Reporting.Svc.Models;

public class SummaryResult
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? PdfUrl { get; set; }
    public string? Error { get; set; }
}
