using System.Diagnostics.CodeAnalysis;

namespace Biotrackr.Reporting.Svc.Configuration;

[ExcludeFromCodeCoverage]
public class Settings
{
    public string SummaryCadence { get; set; } = string.Empty;
    public string McpServerUrl { get; set; } = string.Empty;
    public string McpServerApiKey { get; set; } = string.Empty;
    public string ReportingSvcApiSubscriptionKey { get; set; } = string.Empty;
    public string ReportingApiUrl { get; set; } = string.Empty;
    public string ReportingApiScope { get; set; } = string.Empty;
    public string ReportingSvcAgentIdentityId { get; set; } = string.Empty;
    public string EmailSenderAddress { get; set; } = string.Empty;
    public string EmailRecipientAddress { get; set; } = string.Empty;
    public string AcsEndpoint { get; set; } = string.Empty;
    public int ReportPollIntervalSeconds { get; set; } = 15;
    public int ReportTimeoutMinutes { get; set; } = 20;
}
