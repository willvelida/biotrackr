using Biotrackr.Weight.Api.Models;

namespace Biotrackr.Weight.Api.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task<List<WeightDocument>> GetAllWeightDocuments();
        Task<WeightDocument> GetWeightDocumentByDate(string date);
    }
}
