namespace Biotrackr.Reporting.Svc.Services.Interfaces;

public interface ISummaryService
{
    Task GenerateAndSendSummaryAsync(CancellationToken cancellationToken);
}
