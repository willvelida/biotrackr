using Biotrackr.Vitals.Api.Models;

namespace Biotrackr.Vitals.Api.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task<PaginationResponse<VitalsDocument>> GetAllVitalsDocuments(PaginationRequest request);
        Task<VitalsDocument> GetVitalsDocumentByDate(string date);
        Task<PaginationResponse<VitalsDocument>> GetVitalsByDateRange(string startDate, string endDate, PaginationRequest request);
    }
}
