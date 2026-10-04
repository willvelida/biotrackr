using Biotrackr.Activity.Api.Models;

namespace Biotrackr.Activity.Api.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task<ActivityStoredDocument> GetActivitySummaryByDate(string date);
        Task<PaginationResponse<ActivityStoredDocument>> GetAllActivitySummaries(PaginationRequest request);
        Task<PaginationResponse<ActivityStoredDocument>> GetActivitiesByDateRange(string startDate, string endDate, PaginationRequest request);
    }
}
