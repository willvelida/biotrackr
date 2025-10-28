using System.Net;

namespace Biotrackr.Weight.Svc.IntegrationTests.Helpers
{
    /// <summary>
    /// Mock HTTP message handler for testing HTTP clients without making real network calls.
    /// Allows setting custom response handling logic for different test scenarios.
    /// </summary>
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private Func<HttpRequestMessage, HttpResponseMessage> _handler;

        /// <summary>
        /// Initializes a new instance of MockHttpMessageHandler with default 200 OK response.
        /// </summary>
        public MockHttpMessageHandler()
        {
            _handler = request => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"weight\":[]}")
            };
        }

        /// <summary>
        /// Sets custom response handler for the mock.
        /// </summary>
        /// <param name="handler">Function that takes HttpRequestMessage and returns HttpResponseMessage</param>
        public void SetResponse(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Sets a simple response with status code and content.
        /// </summary>
        /// <param name="statusCode">HTTP status code to return</param>
        /// <param name="content">Response content as string</param>
        public void SetResponse(HttpStatusCode statusCode, string content)
        {
            _handler = request => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };
        }

        /// <summary>
        /// Gets the last request that was sent through this handler.
        /// Useful for verifying request details in tests.
        /// </summary>
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_handler(request));
        }
    }
}
