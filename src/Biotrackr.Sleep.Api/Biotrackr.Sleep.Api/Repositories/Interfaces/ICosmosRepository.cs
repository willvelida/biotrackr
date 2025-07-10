using Biotrackr.Sleep.Api.Models;

namespace Biotrackr.Sleep.Api.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task<SleepDocument> GetSleepSummaryByDate(string date);
        Task<PaginationResponse<SleepDocument>> GetAllSleepDocuments(PaginationRequest request);
    }
}
