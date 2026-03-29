using Biotrackr.Chat.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Chat.Api.UnitTests.Services
{
    public class AgentIdentityTokenHandlerShould
    {
        private readonly Mock<IAgentTokenProvider> _tokenProvider;
        private readonly Mock<ILogger<AgentIdentityTokenHandler>> _logger;

        public AgentIdentityTokenHandlerShould()
        {
            _tokenProvider = new Mock<IAgentTokenProvider>();
            _logger = new Mock<ILogger<AgentIdentityTokenHandler>>();
        }

        [Fact]
        public async Task AttachBearerTokenWhenTokenAcquired()
        {
            _tokenProvider.Setup(p => p.AcquireTokenForReportingApiAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("test-access-token");

            var handler = new AgentIdentityTokenHandler(_tokenProvider.Object, _logger.Object)
            {
                InnerHandler = new FakeInnerHandler()
            };

            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://test.example.com/api/reports/generate");

            await client.SendAsync(request);

            request.Headers.Authorization.Should().NotBeNull();
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be("test-access-token");
        }

        [Fact]
        public async Task NotAttachAuthHeaderWhenTokenIsNull()
        {
            _tokenProvider.Setup(p => p.AcquireTokenForReportingApiAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            var handler = new AgentIdentityTokenHandler(_tokenProvider.Object, _logger.Object)
            {
                InnerHandler = new FakeInnerHandler()
            };

            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://test.example.com/api/reports");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            request.Headers.Authorization.Should().BeNull();
        }

        [Fact]
        public async Task PassThroughRequestOnTokenAcquisitionFailure()
        {
            _tokenProvider.Setup(p => p.AcquireTokenForReportingApiAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Token acquisition failed"));

            var handler = new AgentIdentityTokenHandler(_tokenProvider.Object, _logger.Object)
            {
                InnerHandler = new FakeInnerHandler()
            };

            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://test.example.com/api/reports");

            var response = await client.SendAsync(request);

            // Should still complete the request (graceful fallback)
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        private sealed class FakeInnerHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }
    }
}
