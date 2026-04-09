using Biotrackr.Vitals.Svc.Models;

namespace Biotrackr.Vitals.Svc.Services.Interfaces
{
    public interface IVitalsService
    {
        Task UpsertVitalsDocument(VitalsDocument vitalsDocument);
    }
}
