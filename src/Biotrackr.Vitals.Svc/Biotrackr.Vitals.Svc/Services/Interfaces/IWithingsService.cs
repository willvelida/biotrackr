using Biotrackr.Vitals.Svc.Models.WithingsEntities;

namespace Biotrackr.Vitals.Svc.Services.Interfaces
{
    public interface IWithingsService
    {
        Task<WithingsMeasureResponse> GetMeasurements(string startDate, string endDate);
    }
}
