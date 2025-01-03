using Biotrackr.Sleep.Api.Models.FitbitEntities;

namespace Biotrackr.Sleep.Api.Models
{
    public class SleepDocument
    {
        public string Id { get; set; }
        public SleepResponse Sleep { get; set; }
        public string Date { get; set; }
        public string DocumentType { get; set; }
    }
}
