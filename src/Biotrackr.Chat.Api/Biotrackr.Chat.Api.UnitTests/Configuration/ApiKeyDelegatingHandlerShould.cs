using System.Net;
using Biotrackr.Chat.Api.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Chat.Api.UnitTests.Configuration
{
    public class ApiKeyDelegatingHandlerShould
    {
        #region Test Helpers

        private static ApiKeyDelegatingHandler CreateHandler(string? subscriptionKey)
        {
            var settings = new Settings { ApiSubscriptionKey = subscriptionKey };
            var optionsMock = new Mock<IOptions<Settings>>();
            optionsMock.Setup(o => o.Value).Returns(settings);

            var handler = new ApiKeyDelegatingHandler(optionsMock.Object)
            {
                InnerHandler = new TestInnerHandler()
            };

            return handler;
        }

        private static async Task<HttpRequestMessage> SendRequest(ApiKeyDelegatingHandler handler)
        {
            var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://test.api.com/food/2026-01-01");
            await invoker.SendAsync(request, CancellationToken.None);
            return request;
        }

        private class TestInnerHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }

        #endregion

        [Fact]
        public async Task SendAsync_ShouldAddSubscriptionKeyHeader_WhenKeyIsConfigured()
        {
            // Arrange
            var handler = CreateHandler("test-subscription-key-123");

            // Act
            var request = await SendRequest(handler);

            // Assert
            request.Headers.Contains("Ocp-Apim-Subscription-Key").Should().BeTrue();
            request.Headers.GetValues("Ocp-Apim-Subscription-Key").First().Should().Be("test-subscription-key-123");
        }

        [Fact]
        public async Task SendAsync_ShouldNotAddHeader_WhenKeyIsNull()
        {
            // Arrange
            var handler = CreateHandler(null);

            // Act
            var request = await SendRequest(handler);

            // Assert
            request.Headers.Contains("Ocp-Apim-Subscription-Key").Should().BeFalse();
        }

        [Fact]
        public async Task SendAsync_ShouldNotAddHeader_WhenKeyIsEmpty()
        {
            // Arrange
            var handler = CreateHandler("");

            // Act
            var request = await SendRequest(handler);

            // Assert
            request.Headers.Contains("Ocp-Apim-Subscription-Key").Should().BeFalse();
        }

        [Fact]
        public async Task SendAsync_ShouldNotAddHeader_WhenKeyIsWhitespace()
        {
            // Arrange
            var handler = CreateHandler("   ");

            // Act
            var request = await SendRequest(handler);

            // Assert
            request.Headers.Contains("Ocp-Apim-Subscription-Key").Should().BeFalse();
        }

        [Fact]
        public async Task SendAsync_ShouldPassRequestToInnerHandler()
        {
            // Arrange
            var settings = new Settings { ApiSubscriptionKey = "key" };
            var optionsMock = new Mock<IOptions<Settings>>();
            optionsMock.Setup(o => o.Value).Returns(settings);

            var innerHandler = new TestInnerHandler();
            var handler = new ApiKeyDelegatingHandler(optionsMock.Object)
            {
                InnerHandler = innerHandler
            };

            var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://test.api.com/activity");

            // Act
            var response = await invoker.SendAsync(request, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            innerHandler.LastRequest.Should().BeSameAs(request);
        }
    }
}
