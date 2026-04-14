using System.Diagnostics.CodeAnalysis;
using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Biotrackr.Reporting.Svc.Services;

[ExcludeFromCodeCoverage]
public class McpClientFactory : IMcpClientFactory
{
    private readonly Settings _settings;
    private readonly ILoggerFactory _loggerFactory;

    public McpClientFactory(IOptions<Settings> settings, ILoggerFactory loggerFactory)
    {
        _settings = settings.Value;
        _loggerFactory = loggerFactory;
    }

    public async Task<IMcpToolCaller> CreateClientAsync(CancellationToken cancellationToken)
    {
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(_settings.McpServerUrl),
            TransportMode = HttpTransportMode.AutoDetect,
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Ocp-Apim-Subscription-Key"] = _settings.ApiSubscriptionKey,
                ["X-Api-Key"] = _settings.McpServerApiKey
            }
        };

        var transport = new HttpClientTransport(transportOptions, _loggerFactory);
        var mcpClient = await McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: cancellationToken);
        return new McpToolCaller(mcpClient);
    }
}
