using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace Biotrackr.Auth.Svc.IntegrationTests.Helpers
{
    /// <summary>
    /// Fluent builder for configuring HttpMessageHandler mocks.
    /// Simplifies setting up HTTP responses in tests.
    /// </summary>
    public class MockHttpMessageHandlerBuilder
    {
        private readonly Mock<HttpMessageHandler> _mockHandler;
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        private string _content = string.Empty;
        private Exception? _exception;

        public MockHttpMessageHandlerBuilder(Mock<HttpMessageHandler> mockHandler)
        {
            _mockHandler = mockHandler;
        }

        /// <summary>
        /// Sets the HTTP status code for the response.
        /// </summary>
        public MockHttpMessageHandlerBuilder WithStatusCode(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            return this;
        }

        /// <summary>
        /// Sets the response content as a serialized JSON object.
        /// </summary>
        public MockHttpMessageHandlerBuilder WithJsonContent<T>(T content)
        {
            _content = JsonSerializer.Serialize(content);
            return this;
        }

        /// <summary>
        /// Sets the response content as a plain string.
        /// </summary>
        public MockHttpMessageHandlerBuilder WithStringContent(string content)
        {
            _content = content;
            return this;
        }

        /// <summary>
        /// Configures the mock to throw an exception.
        /// </summary>
        public MockHttpMessageHandlerBuilder WithException(Exception exception)
        {
            _exception = exception;
            return this;
        }

        /// <summary>
        /// Builds and applies the mock configuration.
        /// </summary>
        public void Build()
        {
            if (_exception != null)
            {
                _mockHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ThrowsAsync(_exception);
            }
            else
            {
                _mockHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = _statusCode,
                        Content = new StringContent(_content)
                    });
            }
        }

        /// <summary>
        /// Creates a new builder instance for the given mock handler.
        /// </summary>
        public static MockHttpMessageHandlerBuilder For(Mock<HttpMessageHandler> mockHandler)
        {
            return new MockHttpMessageHandlerBuilder(mockHandler);
        }
    }
}
