using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Biotrackr.Reporting.Api.Telemetry;

internal static class ReportingTelemetry
{
    public const string SourceName = "Biotrackr.Reporting.Api";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    public static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> ReportsGenerated = Meter.CreateCounter<long>(
        "reporting.reports.generated", description: "Total reports generated");

    public static readonly Counter<long> ReportsFailed = Meter.CreateCounter<long>(
        "reporting.reports.failed", description: "Total reports failed");

    public static readonly Histogram<double> ReportDuration = Meter.CreateHistogram<double>(
        "reporting.reports.duration_ms", "ms", "Report generation duration");

    public static readonly Histogram<double> SubagentDuration = Meter.CreateHistogram<double>(
        "reporting.subagent.duration_ms", "ms", "Per-subagent execution duration");

    public static readonly UpDownCounter<long> ConcurrentJobs = Meter.CreateUpDownCounter<long>(
        "reporting.jobs.concurrent", description: "Current concurrent report jobs");
}
