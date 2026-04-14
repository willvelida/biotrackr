using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Moq;

namespace Biotrackr.Reporting.Svc.UnitTests.Services;

public class AgentTokenProviderShould
{
    private readonly Mock<ILogger<AgentTokenProvider>> _loggerMock;

    public AgentTokenProviderShould()
    {
        _loggerMock = new Mock<ILogger<AgentTokenProvider>>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task AcquireTokenForReportingApiAsync_ShouldReturnNull_WhenScopeIsEmpty(string scope)
    {
        // Arrange
        var settings = new Settings
        {
            ReportingApiScope = scope,
            AgentIdentityId = "test-identity"
        };
        var options = Options.Create(settings);
        var tokenAcquirerFactory = new Mock<ITokenAcquirerFactory>();
        var schemeProvider = new Mock<Microsoft.Identity.Web.IAuthenticationSchemeInformationProvider>();
        var credential = new MicrosoftIdentityTokenCredential(tokenAcquirerFactory.Object, schemeProvider.Object);

        var sut = new AgentTokenProvider(credential, options, _loggerMock.Object);

        // Act
        var result = await sut.AcquireTokenForReportingApiAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
