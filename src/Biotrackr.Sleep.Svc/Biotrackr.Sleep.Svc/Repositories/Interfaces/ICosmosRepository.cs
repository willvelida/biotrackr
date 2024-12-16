using Biotrackr.Sleep.Svc.Models;

namespace Biotrackr.Sleep.Svc.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task CreateSleepDocument(SleepDocument sleepDocument);
    }
}
