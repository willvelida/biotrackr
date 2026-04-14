using System.Net;
using Biotrackr.Reporting.Svc.Services;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Reporting.Svc.UnitTests.Services;

public class AgentIdentityTokenHandlerShould
{
    private readonly Mock<IAgentTokenProvider> _tokenProviderMock;
    private readonly Mock<ILogger<AgentIdentityTokenHandler>> _loggerMock;

    public AgentIdentityTokenHandlerShould()
    {
        _tokenProviderMock = new Mock<IAgentTokenProvider>();
        _loggerMock = new Mock<ILogger<AgentIdentityTokenHandler>>();
    }

    private static HttpMessageInvoker CreateInvoker(AgentIdentityTokenHandler handler)
    {
        handler.InnerHandler = new TestHandler();
        return new HttpMessageInvoker(handler);
    }

    [Fact]
    public async Task SendAsync_ShouldAttachBearerToken_WhenTokenAcquired()
    {
        // Arrange
        _tokenProviderMock
            .Setup(x => x.AcquireTokenForReportingApiAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-token-abc");

        var handler = new AgentIdentityTokenHandler(_tokenProviderMock.Object, _loggerMock.Object);
        var invoker = CreateInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://reporting.example.com/api/reports");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be("test-token-abc");
    }

    [Fact]
    public async Task SendAsync_ShouldNotAttachToken_WhenTokenIsNull()
    {
        // Arrange
        _tokenProviderMock
            .Setup(x => x.AcquireTokenForReportingApiAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var handler = new AgentIdentityTokenHandler(_tokenProviderMock.Object, _loggerMock.Object);
        var invoker = CreateInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://reporting.example.com/api/reports");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_ShouldProceed_WhenTokenAcquisitionFails()
    {
        // Arrange
        _tokenProviderMock
            .Setup(x => x.AcquireTokenForReportingApiAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Token acquisition failed"));

        var handler = new AgentIdentityTokenHandler(_tokenProviderMock.Object, _loggerMock.Object);
        var invoker = CreateInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://reporting.example.com/api/reports");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_ShouldDelegateToInnerHandler_WhenCalled()
    {
        // Arrange
        _tokenProviderMock
            .Setup(x => x.AcquireTokenForReportingApiAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("some-token");

        var handler = new AgentIdentityTokenHandler(_tokenProviderMock.Object, _loggerMock.Object);
        var invoker = CreateInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://reporting.example.com/api/reports");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private class TestHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
