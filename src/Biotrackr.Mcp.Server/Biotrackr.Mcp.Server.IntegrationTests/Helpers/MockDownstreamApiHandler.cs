using System.Net;

namespace Biotrackr.Mcp.Server.IntegrationTests.Helpers
{
    /// <summary>
    /// A mock HTTP message handler that returns configurable responses
    /// for downstream API calls during integration tests.
    /// </summary>
    public class MockDownstreamApiHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, HttpResponseMessage> _responses = new(StringComparer.OrdinalIgnoreCase);
        private readonly HttpResponseMessage _defaultResponse;

        public List<HttpRequestMessage> ReceivedRequests { get; } = [];

        public MockDownstreamApiHandler()
        {
            _defaultResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
        }

        /// <summary>
        /// Register a response for a specific endpoint path prefix.
        /// </summary>
        public MockDownstreamApiHandler WithResponse(string pathPrefix, HttpResponseMessage response)
        {
            _responses[pathPrefix] = response;
            return this;
        }

        /// <summary>
        /// Register a response for a specific endpoint path prefix with JSON content.
        /// </summary>
        public MockDownstreamApiHandler WithJsonResponse(string pathPrefix, string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responses[pathPrefix] = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ReceivedRequests.Add(request);

            var path = request.RequestUri?.PathAndQuery ?? string.Empty;

            foreach (var kvp in _responses)
            {
                if (path.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(kvp.Value);
                }
            }

            return Task.FromResult(_defaultResponse);
        }
    }
}
