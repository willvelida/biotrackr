namespace Biotrackr.Reporting.Svc.Services.Interfaces;

public interface IMcpClientFactory
{
    Task<IMcpToolCaller> CreateClientAsync(CancellationToken cancellationToken);
}
