using Biotrackr.Activity.Svc.Models.FitbitEntities;

namespace Biotrackr.Activity.Svc.Services.Interfaces
{
    public interface IFitbitService
    {
        Task<ActivityResponse> GetActivityResponse(string date);
    }
}
