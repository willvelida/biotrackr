using Azure.Communication.Email;
using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Services;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Reporting.Svc.UnitTests.Services;

public class EmailServiceShould
{
    private readonly Mock<IEmailClientWrapper> _emailClientMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;

    public EmailServiceShould()
    {
        _emailClientMock = new Mock<IEmailClientWrapper>();
        _loggerMock = new Mock<ILogger<EmailService>>();
    }

    private EmailService CreateService()
    {
        var settings = new Settings
        {
            EmailSenderAddress = "sender@example.com",
            EmailRecipientAddress = "recipient@example.com"
        };
        var options = Options.Create(settings);
        return new EmailService(_emailClientMock.Object, options, _loggerMock.Object);
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldSendEmail_WhenAllParametersValid()
    {
        // Arrange
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "Test summary", [0x01, 0x02], CancellationToken.None);

        // Assert
        _emailClientMock.Verify(x => x.SendAsync(It.Is<EmailMessage>(m =>
            m.SenderAddress == "sender@example.com" &&
            m.Content.Subject == "Your Weekly Health Summary (2025-01-01 to 2025-01-07)"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldIncludePdfAttachment_WhenPdfProvided()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync("monthly", "2025-01-01", "2025-01-31", "Summary", pdfBytes, CancellationToken.None);

        // Assert
        _emailClientMock.Verify(x => x.SendAsync(It.Is<EmailMessage>(m =>
            m.Attachments.Count == 1 &&
            m.Attachments[0].Name == "health-summary.pdf" &&
            m.Attachments[0].ContentType == "application/pdf"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("weekly", "Weekly")]
    [InlineData("monthly", "Monthly")]
    [InlineData("yearly", "Yearly")]
    public async Task SendReportEmailAsync_ShouldFormatSubjectCorrectly_WhenCadenceProvided(string cadence, string expectedTitle)
    {
        // Arrange
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync(cadence, "2025-01-01", "2025-01-07", "Summary", [0x01], CancellationToken.None);

        // Assert
        _emailClientMock.Verify(x => x.SendAsync(It.Is<EmailMessage>(m =>
            m.Content.Subject.Contains(expectedTitle)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldThrow_WhenEmailSendFails()
    {
        // Arrange
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendStatus.Failed);

        var sut = CreateService();

        // Act
        var act = () => sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "Summary", [0x01], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Failed*");
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldIncludeHtmlContent_WhenCalled()
    {
        // Arrange
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "My custom summary", [0x01], CancellationToken.None);

        // Assert
        _emailClientMock.Verify(x => x.SendAsync(It.Is<EmailMessage>(m =>
            m.Content.Html != null && m.Content.Html.Length > 0),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldUseFallbackSummary_WhenSummaryIsNull()
    {
        // Arrange
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", null, [0x01], CancellationToken.None);

        // Assert
        _emailClientMock.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
