using Biotrackr.Sleep.Svc.Models.FitbitEntities;

namespace Biotrackr.Sleep.Svc.Services.Interfaces
{
    public interface ISleepService
    {
        Task MapAndSaveDocument(string date, SleepResponse sleepResponse);
    }
}
