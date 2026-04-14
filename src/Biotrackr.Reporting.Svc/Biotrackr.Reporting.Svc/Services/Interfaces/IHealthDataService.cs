using Biotrackr.Reporting.Svc.Models;

namespace Biotrackr.Reporting.Svc.Services.Interfaces;

public interface IHealthDataService
{
    Task<HealthDataSnapshot> FetchHealthDataAsync(string startDate, string endDate, CancellationToken cancellationToken);
}
