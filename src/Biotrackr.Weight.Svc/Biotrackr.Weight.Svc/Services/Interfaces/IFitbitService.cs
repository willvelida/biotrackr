using Biotrackr.Weight.Svc.Models.Entities;

namespace Biotrackr.Weight.Svc.Services.Interfaces
{
    public interface IFitbitService
    {
        Task<WeightResponse> GetWeightLogs(string startDate, string endDate);
    }
}
