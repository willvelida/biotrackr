using Biotrackr.Sleep.Api.Models;

namespace Biotrackr.Sleep.Api.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task<SleepStoredDocument> GetSleepSummaryByDate(string date);
        Task<PaginationResponse<SleepStoredDocument>> GetAllSleepDocuments(PaginationRequest request);
        Task<PaginationResponse<SleepStoredDocument>> GetSleepDocumentsByDateRange(string startDate, string endDate, PaginationRequest request);
    }
}
