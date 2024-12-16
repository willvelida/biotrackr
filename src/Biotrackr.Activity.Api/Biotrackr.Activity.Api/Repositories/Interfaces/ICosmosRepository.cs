using Biotrackr.Activity.Api.Models;

namespace Biotrackr.Activity.Api.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task<ActivityDocument> GetActivitySummaryByDate(string date);
    }
}
