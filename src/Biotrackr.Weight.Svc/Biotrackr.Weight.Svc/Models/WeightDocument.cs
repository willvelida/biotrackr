namespace Biotrackr.Weight.Svc.Models
{
    public class WeightDocument
    {
        public string Id { get; set; }
        public WeightMeasurement Weight { get; set; }
        public string Date { get; set; }
        public string DocumentType { get; set; }
        public string Provider { get; set; } = "Withings";
    }
}
