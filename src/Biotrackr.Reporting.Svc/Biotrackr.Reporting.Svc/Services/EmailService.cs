using System.Globalization;
using System.Reflection;
using System.Text;
using Azure.Communication.Email;
using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Models;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using Markdig;
using Microsoft.Extensions.Options;

namespace Biotrackr.Reporting.Svc.Services;

public class EmailService : IEmailService
{
    private readonly IEmailClientWrapper _emailClient;
    private readonly Settings _settings;
    private readonly ILogger<EmailService> _logger;

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseAutoLinks()
        .UseEmphasisExtras()
        .UseTaskLists()
        .Build();

    private const string EmailSummaryCss = @"
        h1 { font-size: 24px; font-weight: bold; color: #1a1a1a; margin: 0 0 16px 0; padding: 0; }
        h2 { font-size: 20px; font-weight: bold; color: #1a1a1a; margin: 24px 0 12px 0; padding: 0; border-bottom: 1px solid #e0e0e0; padding-bottom: 8px; }
        h3 { font-size: 17px; font-weight: bold; color: #1a1a1a; margin: 20px 0 8px 0; padding: 0; }
        p { margin: 0 0 12px 0; padding: 0; font-size: 16px; line-height: 1.6; color: #333333; }
        ul, ol { margin: 0 0 12px 0; padding-left: 24px; }
        li { margin: 0 0 4px 0; font-size: 16px; line-height: 1.6; color: #333333; }
        a { color: #1a73e8; text-decoration: underline; }
        strong { font-weight: bold; }
        em { font-style: italic; }
        code { font-family: 'Courier New', Courier, monospace; font-size: 13px; background-color: #f4f4f7; padding: 2px 4px; }
        pre { font-family: 'Courier New', Courier, monospace; font-size: 13px; background-color: #f4f4f7; padding: 12px; margin: 0 0 12px 0; white-space: pre-wrap; }
        blockquote { border-left: 4px solid #dddddd; margin: 0 0 12px 0; padding: 8px 16px; color: #666666; }
        table { border-collapse: collapse; margin: 0 0 12px 0; width: 100%; }
        th { background-color: #f4f4f7; border: 1px solid #dddddd; padding: 8px 12px; text-align: left; font-weight: bold; font-size: 14px; }
        td { border: 1px solid #dddddd; padding: 8px 12px; text-align: left; font-size: 14px; }
        hr { border: none; border-top: 1px solid #dddddd; margin: 24px 0; }
    ";

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
        string? summary, byte[] pdfAttachment, List<MetricCard> metrics, CancellationToken cancellationToken)
    {
        var cadenceTitle = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cadence.ToLowerInvariant());
        var subject = BuildEmailSubject(cadence, cadenceTitle, startDate, endDate);
        var htmlBody = BuildHtmlBody(cadence, cadenceTitle, startDate, endDate, summary, metrics);

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

    private static string BuildHtmlBody(string cadence, string cadenceTitle, string startDate, string endDate, string? summary, List<MetricCard> metrics)
    {
        var template = LoadEmailTemplate();

        return template
            .Replace("{{HEADER_SUBTITLE}}", BuildHeaderSubtitle(cadence, startDate), StringComparison.Ordinal)
            .Replace("{{DATE_DISPLAY}}", BuildDateDisplay(cadence, startDate, endDate), StringComparison.Ordinal)
            .Replace("{{METRIC_CARDS}}", BuildMetricCardsHtml(metrics), StringComparison.Ordinal)
            .Replace("{{SUMMARY}}", ConvertMarkdownToEmailHtml(summary ?? "No summary available."), StringComparison.Ordinal)
            .Replace("{{YEAR}}", DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    internal static string ConvertMarkdownToEmailHtml(string markdown)
    {
        var rawHtml = Markdown.ToHtml(markdown, MarkdownPipeline);
        var styledHtml = $"<html><head><style>{EmailSummaryCss}</style></head><body>{rawHtml}</body></html>";
        var result = PreMailer.Net.PreMailer.MoveCssInline(styledHtml, removeStyleElements: true, stripIdAndClassAttributes: true);

        var bodyStart = result.Html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
        var bodyTagEnd = result.Html.IndexOf('>', bodyStart) + 1;
        var bodyEnd = result.Html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return result.Html[bodyTagEnd..bodyEnd];
    }

    internal static string BuildHeaderSubtitle(string cadence, string startDate)
    {
        if (string.Equals(cadence, "yearly", StringComparison.OrdinalIgnoreCase)
            && DateOnly.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return $"Your {date.Year} Year on Biotrackr";
        }

        var cadenceTitle = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cadence.ToLowerInvariant());
        return $"{cadenceTitle} Health Summary";
    }

    internal static string BuildDateDisplay(string cadence, string startDate, string endDate)
    {
        if (string.Equals(cadence, "yearly", StringComparison.OrdinalIgnoreCase)
            && DateOnly.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return $"{date.Year} Annual Review";
        }

        return $"{startDate} &mdash; {endDate}";
    }

    internal static string BuildEmailSubject(string cadence, string cadenceTitle, string startDate, string endDate)
    {
        if (string.Equals(cadence, "yearly", StringComparison.OrdinalIgnoreCase)
            && DateOnly.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return $"Your {date.Year} Year on Biotrackr";
        }

        return $"Your {cadenceTitle} Health Summary ({startDate} to {endDate})";
    }

    internal static string BuildMetricCardsHtml(List<MetricCard> metrics)
    {
        if (metrics is null || metrics.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<tr><td style=\"padding:0 40px 24px 40px;\">");
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
        sb.Append("<tr><td style=\"font-size:13px;font-weight:bold;color:#1a73e8;text-transform:uppercase;letter-spacing:1px;padding-bottom:12px;\">Key Metrics</td></tr>");
        sb.Append("<tr><td>");
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");

        for (var i = 0; i < metrics.Count; i++)
        {
            if (i % 3 == 0)
            {
                if (i > 0)
                {
                    sb.Append("</tr>");
                }
                sb.Append("<tr>");
            }
            sb.Append(BuildCardHtml(metrics[i]));
        }
        sb.Append("</tr>");

        sb.Append("</table>");
        sb.Append("</td></tr>");
        sb.Append("</table>");
        sb.Append("</td></tr>");

        return sb.ToString();
    }

    internal static string BuildCardHtml(MetricCard card)
    {
        var sb = new StringBuilder();
        sb.Append("<td width=\"33%\" style=\"padding:4px;\">");
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"background-color:#f0f7ff;border-radius:6px;\">");
        sb.Append("<tr><td style=\"padding:12px;text-align:center;\">");
        sb.Append($"<div style=\"font-size:20px;line-height:1;\">{card.Icon}</div>");
        sb.Append($"<div style=\"font-size:22px;font-weight:bold;color:{card.Color};padding-top:6px;\">{card.Value}</div>");

        if (!string.IsNullOrEmpty(card.Unit))
        {
            sb.Append($"<div style=\"font-size:11px;color:#666666;\">{card.Unit}</div>");
        }

        sb.Append($"<div style=\"font-size:12px;color:#333333;padding-top:4px;\">{card.Label}</div>");

        if (!string.IsNullOrEmpty(card.Subtitle))
        {
            sb.Append($"<div style=\"font-size:10px;color:#999999;font-style:italic;padding-top:2px;\">{card.Subtitle}</div>");
        }

        sb.Append("</td></tr>");
        sb.Append("</table>");
        sb.Append("</td>");

        return sb.ToString();
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
