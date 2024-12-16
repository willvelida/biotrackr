using ent = Biotrackr.Weight.Svc.Models.Entities;

namespace Biotrackr.Weight.Svc.Models
{
    public class WeightDocument
    {
        public string Id { get; set; }
        public ent.Weight Weight { get; set; }
        public string Date { get; set; }
        public string DocumentType { get; set; }
    }
}
