using System.Globalization;
using System.Reflection;
using Azure.Communication.Email;
using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Biotrackr.Reporting.Svc.Services;

public class EmailService : IEmailService
{
    private readonly IEmailClientWrapper _emailClient;
    private readonly Settings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailClientWrapper emailClient,
        IOptions<Settings> settings,
        ILogger<EmailService> logger)
    {
        _emailClient = emailClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendReportEmailAsync(string cadence, string startDate, string endDate,
        string? summary, byte[] pdfAttachment, CancellationToken cancellationToken)
    {
        var cadenceTitle = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cadence.ToLowerInvariant());
        var subject = $"Your {cadenceTitle} Health Summary ({startDate} to {endDate})";
        var htmlBody = BuildHtmlBody(cadence, cadenceTitle, startDate, endDate, summary);

        _logger.LogInformation("Sending {Cadence} health summary email for {StartDate} to {EndDate}", cadence, startDate, endDate);

        var message = new EmailMessage(
            senderAddress: _settings.EmailSenderAddress,
            recipientAddress: _settings.EmailRecipientAddress,
            content: new EmailContent(subject) { Html = htmlBody });

        message.Attachments.Add(new EmailAttachment(
            "health-summary.pdf",
            "application/pdf",
            new BinaryData(pdfAttachment)));

        var result = await _emailClient.SendAsync(message, cancellationToken);

        if (result != EmailSendStatus.Succeeded)
        {
            throw new InvalidOperationException($"Email send failed with status: {result}");
        }

        _logger.LogInformation("Health summary email sent successfully");
    }

    private static string BuildHtmlBody(string cadence, string cadenceTitle, string startDate, string endDate, string? summary)
    {
        var template = LoadEmailTemplate();

        return template
            .Replace("{{CADENCE}}", cadence, StringComparison.Ordinal)
            .Replace("{{CADENCE_TITLE}}", cadenceTitle, StringComparison.Ordinal)
            .Replace("{{START_DATE}}", startDate, StringComparison.Ordinal)
            .Replace("{{END_DATE}}", endDate, StringComparison.Ordinal)
            .Replace("{{SUMMARY}}", summary ?? "No summary available.", StringComparison.Ordinal)
            .Replace("{{YEAR}}", DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private static string LoadEmailTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Biotrackr.Reporting.Svc.Templates.email-template.html";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
