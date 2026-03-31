using System.Net;
using Biotrackr.Chat.Api.Handlers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Chat.Api.UnitTests.Handlers
{
    public class AnthropicMetricsHandlerShould
    {
        private readonly Mock<ILogger<AnthropicMetricsHandler>> _loggerMock;

        public AnthropicMetricsHandlerShould()
        {
            _loggerMock = new Mock<ILogger<AnthropicMetricsHandler>>();
        }

        [Fact]
        public async Task ReturnResponse_WhenRequestSucceeds()
        {
            // Arrange
            var innerHandler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var handler = new AnthropicMetricsHandler(_loggerMock.Object)
            {
                InnerHandler = innerHandler
            };
            var invoker = new HttpMessageInvoker(handler);

            // Act
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"), CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LogWarning_WhenRateLimitHit()
        {
            // Arrange
            var innerHandler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
            var handler = new AnthropicMetricsHandler(_loggerMock.Object)
            {
                InnerHandler = innerHandler
            };
            var invoker = new HttpMessageInvoker(handler);

            // Act
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"), CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anthropic rate limit hit (429)")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CaptureRateLimitHeaders_WhenPresent()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            responseMessage.Headers.Add("anthropic-ratelimit-input-tokens-limit", "30000");
            responseMessage.Headers.Add("anthropic-ratelimit-input-tokens-remaining", "28500");
            responseMessage.Headers.Add("anthropic-ratelimit-output-tokens-remaining", "7500");
            responseMessage.Headers.Add("anthropic-ratelimit-requests-remaining", "48");

            var innerHandler = new FakeHandler(responseMessage);
            var handler = new AnthropicMetricsHandler(_loggerMock.Object)
            {
                InnerHandler = innerHandler
            };
            var invoker = new HttpMessageInvoker(handler);

            // Act
            await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"), CancellationToken.None);

            // Assert — verify Debug logs emitted for each header
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("InputTokensLimit")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("InputTokensRemaining")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OutputTokensRemaining")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("RequestsRemaining")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task NotLogHeaders_WhenHeadersNotPresent()
        {
            // Arrange
            var innerHandler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var handler = new AnthropicMetricsHandler(_loggerMock.Object)
            {
                InnerHandler = innerHandler
            };
            var invoker = new HttpMessageInvoker(handler);

            // Act
            await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"), CancellationToken.None);

            // Assert — no Debug logs for rate limit headers
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task NotLogHeader_WhenHeaderValueIsNotNumeric()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            responseMessage.Headers.Add("anthropic-ratelimit-input-tokens-limit", "not-a-number");

            var innerHandler = new FakeHandler(responseMessage);
            var handler = new AnthropicMetricsHandler(_loggerMock.Object)
            {
                InnerHandler = innerHandler
            };
            var invoker = new HttpMessageInvoker(handler);

            // Act
            await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"), CancellationToken.None);

            // Assert — non-numeric value should not produce a Debug log
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public FakeHandler(HttpResponseMessage response) => _response = response;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_response);
        }
    }
}
