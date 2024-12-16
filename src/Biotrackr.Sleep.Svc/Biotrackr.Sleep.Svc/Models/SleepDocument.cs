using Biotrackr.Sleep.Svc.Models.FitbitEntities;

namespace Biotrackr.Sleep.Svc.Models
{
    public class SleepDocument
    {
        public string Id { get; set; }
        public SleepResponse Sleep { get; set; }
        public string Date { get; set; }
        public string DocumentType { get; set; }
    }
}
