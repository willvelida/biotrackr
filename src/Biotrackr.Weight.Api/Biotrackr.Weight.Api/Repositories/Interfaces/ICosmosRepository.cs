using Biotrackr.Weight.Api.Models;

namespace Biotrackr.Weight.Api.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task<PaginationResponse<WeightDocument>> GetAllWeightDocuments(PaginationRequest request);
        Task<WeightDocument> GetWeightDocumentByDate(string date);
    }
}
