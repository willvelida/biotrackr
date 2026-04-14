using Azure.Core;
using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace Biotrackr.Reporting.Svc.Services;

public class AgentTokenProvider : IAgentTokenProvider
{
    private readonly MicrosoftIdentityTokenCredential _credential;
    private readonly Settings _settings;
    private readonly ILogger<AgentTokenProvider> _logger;

    public AgentTokenProvider(
        MicrosoftIdentityTokenCredential credential,
        IOptions<Settings> settings,
        ILogger<AgentTokenProvider> logger)
    {
        _credential = credential;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string?> AcquireTokenForReportingApiAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ReportingApiScope))
        {
            return null;
        }

        _credential.Options.WithAgentIdentity(_settings.AgentIdentityId);
        _credential.Options.RequestAppToken = true;

        var tokenRequestContext = new TokenRequestContext([_settings.ReportingApiScope]);
        var accessToken = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);

        return accessToken.Token;
    }
}
