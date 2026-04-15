using Azure.Communication.Email;
using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Models;
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

    private static List<MetricCard> CreateTestMetrics() =>
    [
        new MetricCard { Label = "Total Steps", Value = "50,000", Unit = "steps", Icon = "\U0001F6B6", Color = "#1a73e8" },
        new MetricCard { Label = "Avg Sleep", Value = "7h 30m", Unit = "", Icon = "\U0001F634", Color = "#673ab7" }
    ];

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
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "## Summary\n\n**Hello** world", [0x01, 0x02], CreateTestMetrics(), CancellationToken.None);

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
        await sut.SendReportEmailAsync("monthly", "2025-01-01", "2025-01-31", "Summary", pdfBytes, CreateTestMetrics(), CancellationToken.None);

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
    [InlineData("yearly", "Year on Biotrackr")]
    public async Task SendReportEmailAsync_ShouldFormatSubjectCorrectly_WhenCadenceProvided(string cadence, string expectedTitle)
    {
        // Arrange
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync(cadence, "2025-01-01", "2025-01-07", "Summary", [0x01], CreateTestMetrics(), CancellationToken.None);

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
        var act = () => sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "Summary", [0x01], CreateTestMetrics(), CancellationToken.None);

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
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "My custom summary", [0x01], CreateTestMetrics(), CancellationToken.None);

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
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", null, [0x01], CreateTestMetrics(), CancellationToken.None);

        // Assert
        _emailClientMock.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldRenderMarkdownHeaders_WhenSummaryContainsMarkdown()
    {
        // Arrange
        EmailMessage? capturedMessage = null;
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "## Activity Summary\n\nSome text", [0x01], CreateTestMetrics(), CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Content.Html.Should().Contain("<h2");
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldRenderMarkdownLists_WhenSummaryContainsBullets()
    {
        // Arrange
        EmailMessage? capturedMessage = null;
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "- Item 1\n- Item 2", [0x01], CreateTestMetrics(), CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Content.Html.Should().Contain("<ul").And.Contain("<li");
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldRenderMarkdownBold_WhenSummaryContainsBold()
    {
        // Arrange
        EmailMessage? capturedMessage = null;
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "**bold text**", [0x01], CreateTestMetrics(), CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Content.Html.Should().Contain("<strong");
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldRenderMetricCards_WhenMetricsProvided()
    {
        // Arrange
        EmailMessage? capturedMessage = null;
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "Summary", [0x01], CreateTestMetrics(), CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Content.Html.Should().Contain("50,000").And.Contain("Total Steps");
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldOmitMetricCards_WhenMetricsEmpty()
    {
        // Arrange
        EmailMessage? capturedMessage = null;
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "Summary", [0x01], [], CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Content.Html.Should().NotContain("Key Metrics");
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldUseYearlyBranding_WhenCadenceIsYearly()
    {
        // Arrange
        EmailMessage? capturedMessage = null;
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync("yearly", "2024-12-28", "2025-12-27", "Summary", [0x01], CreateTestMetrics(), CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Content.Subject.Should().Contain("Year on Biotrackr");
    }

    [Fact]
    public async Task SendReportEmailAsync_ShouldRenderSubtitle_WhenMetricHasSubtitle()
    {
        // Arrange
        var metrics = new List<MetricCard>
        {
            new() { Label = "Weight", Value = "75.2", Unit = "kg", Icon = "\u2696\uFE0F", Color = "#e8453c", Subtitle = "(1 reading)" }
        };

        EmailMessage? capturedMessage = null;
        _emailClientMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(EmailSendStatus.Succeeded);

        var sut = CreateService();

        // Act
        await sut.SendReportEmailAsync("weekly", "2025-01-01", "2025-01-07", "Summary", [0x01], metrics, CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Content.Html.Should().Contain("(1 reading)");
    }
}
