using Biotrackr.Vitals.Svc.Models;

namespace Biotrackr.Vitals.Svc.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task UpsertVitalsDocument(VitalsDocument vitalsDocument);
        Task<VitalsDocument?> GetVitalsDocumentByDate(string date);
    }
}
