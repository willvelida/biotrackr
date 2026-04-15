using Biotrackr.Reporting.Svc.Models;

namespace Biotrackr.Reporting.Svc.Services.Interfaces;

public interface IMetricExtractor
{
    List<MetricCard> ExtractMetrics(HealthDataSnapshot snapshot);
}
