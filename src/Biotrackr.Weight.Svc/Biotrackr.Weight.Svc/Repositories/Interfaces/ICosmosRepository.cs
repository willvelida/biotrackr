using Biotrackr.Weight.Svc.Models;

namespace Biotrackr.Weight.Svc.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task CreateWeightDocument(WeightDocument weightDocument);
    }
}
