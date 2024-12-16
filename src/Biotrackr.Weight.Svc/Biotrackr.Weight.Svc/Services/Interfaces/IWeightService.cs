using ent = Biotrackr.Weight.Svc.Models.Entities;

namespace Biotrackr.Weight.Svc.Services.Interfaces
{
    public interface IWeightService
    {
        Task MapAndSaveDocument(string date, ent.Weight weight);
    }
}
