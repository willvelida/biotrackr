using Biotrackr.Weight.Svc.Models.WithingsEntities;

namespace Biotrackr.Weight.Svc.Services.Interfaces
{
    public interface IWithingsService
    {
        Task<WithingsMeasureResponse> GetMeasurements(string startDate, string endDate);
    }
}
