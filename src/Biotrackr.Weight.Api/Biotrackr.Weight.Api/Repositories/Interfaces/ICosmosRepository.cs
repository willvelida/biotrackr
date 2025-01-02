using Biotrackr.Weight.Api.Models;

namespace Biotrackr.Weight.Api.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task<List<WeightDocument>> GetAllWeightDocuments();
    }
}
