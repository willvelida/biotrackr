using Biotrackr.Activity.Svc.Models;

namespace Biotrackr.Activity.Svc.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task CreateActivityDocument(ActivityDocument activityDocument);
    }
}
