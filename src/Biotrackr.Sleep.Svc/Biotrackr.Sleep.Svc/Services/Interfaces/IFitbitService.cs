using Biotrackr.Sleep.Svc.Models.FitbitEntities;

namespace Biotrackr.Sleep.Svc.Services.Interfaces
{
    public interface IFitbitService
    {
        Task<SleepResponse> GetSleepResponse(string date);
    }
}
