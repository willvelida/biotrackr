using Microsoft.Extensions.Options;

namespace Biotrackr.Mcp.Server.Configuration
{
    public class ApiKeyDelegatingHandler : DelegatingHandler
    {
        private const string SubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";
        private readonly string? _subscriptionKey;

        public ApiKeyDelegatingHandler(IOptions<BiotrackrApiSettings> settings)
        {
            _subscriptionKey = settings.Value.SubscriptionKey;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_subscriptionKey))
            {
                request.Headers.TryAddWithoutValidation(SubscriptionKeyHeader, _subscriptionKey);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
