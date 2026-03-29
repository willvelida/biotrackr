using Azure.Core;
using Biotrackr.Chat.Api.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace Biotrackr.Chat.Api.Services
{
    /// <summary>
    /// Acquires agent identity tokens for inter-service authentication (ASI07).
    /// Uses the autonomous app flow: managed identity → FIC → agent identity → resource token.
    /// </summary>
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
}
