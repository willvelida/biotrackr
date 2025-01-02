using ent = Biotrackr.Weight.Api.Models.FitbitEntities;

namespace Biotrackr.Weight.Api.Models
{
    public class WeightDocument
    {
        public string Id { get; set; }
        public ent.Weight Weight { get; set; }
        public string Date { get; set; }
        public string DocumentType { get; set; }
    }
}
