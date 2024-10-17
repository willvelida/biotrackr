using Biotrackr.FitbitApi.FitbitEntities;

namespace Biotrackr.FitbitApi.Services.Interfaces
{
    public interface IFitbitService
    {
        Task<ActivityResponse> GetActivityResponse(string date);
    }
}
