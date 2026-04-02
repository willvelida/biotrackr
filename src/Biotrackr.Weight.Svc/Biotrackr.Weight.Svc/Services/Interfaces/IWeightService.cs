using Biotrackr.Weight.Svc.Models;

namespace Biotrackr.Weight.Svc.Services.Interfaces
{
    public interface IWeightService
    {
        Task MapAndSaveDocument(string date, WeightMeasurement weight, string provider);
    }
}
