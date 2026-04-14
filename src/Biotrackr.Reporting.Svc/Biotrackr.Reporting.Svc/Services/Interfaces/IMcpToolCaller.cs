namespace Biotrackr.Reporting.Svc.Services.Interfaces;

public interface IMcpToolCaller : IAsyncDisposable
{
    Task<string?> CallToolAsync(string toolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken);
}
