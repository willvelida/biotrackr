using System.Diagnostics.CodeAnalysis;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Biotrackr.Reporting.Svc.Services;

[ExcludeFromCodeCoverage]
public class McpToolCaller : IMcpToolCaller
{
    private readonly McpClient _mcpClient;

    public McpToolCaller(McpClient mcpClient) => _mcpClient = mcpClient;

    public async Task<string?> CallToolAsync(string toolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var result = await _mcpClient.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);
        return result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
    }

    public async ValueTask DisposeAsync() => await _mcpClient.DisposeAsync();
}
