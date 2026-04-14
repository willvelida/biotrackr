namespace Biotrackr.Reporting.Svc.Services.Interfaces;

public interface IAgentTokenProvider
{
    Task<string?> AcquireTokenForReportingApiAsync(CancellationToken cancellationToken);
}
