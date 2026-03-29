namespace Biotrackr.Chat.Api.Services
{
    /// <summary>
    /// DelegatingHandler that attaches an agent identity bearer token to
    /// outbound HTTP requests for inter-service authentication (ASI07).
    /// Uses the autonomous app flow: managed identity → FIC → agent identity → resource token.
    /// </summary>
    public class AgentIdentityTokenHandler : DelegatingHandler
    {
        private readonly IAgentTokenProvider _tokenProvider;
        private readonly ILogger<AgentIdentityTokenHandler> _logger;

        public AgentIdentityTokenHandler(
            IAgentTokenProvider tokenProvider,
            ILogger<AgentIdentityTokenHandler> logger)
        {
            _tokenProvider = tokenProvider;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                var token = await _tokenProvider.AcquireTokenForReportingApiAsync(cancellationToken);
                if (token is not null)
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire agent identity token for Reporting.Api");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
