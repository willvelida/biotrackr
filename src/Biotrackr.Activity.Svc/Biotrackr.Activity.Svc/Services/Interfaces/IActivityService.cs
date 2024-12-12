using Biotrackr.Activity.Svc.Models.FitbitEntities;

namespace Biotrackr.Activity.Svc.Services.Interfaces
{
    public interface IActivityService
    {
        Task MapAndSaveDocument(string date, ActivityResponse activityResponse);
    }
}
