namespace Biotrackr.Chat.Api.Tools;

public interface IReportReviewerService
{
    Task<ReviewResult> ReviewReportAsync(string reportSummary, object? sourceDataSnapshot, string reportType);
}
