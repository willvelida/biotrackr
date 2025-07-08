using Biotrackr.Activity.Api.Models.FitbitEntities;

namespace Biotrackr.Activity.Api.Models
{
    // Activity Document
    public class ActivityDocument
    {
        public string Id { get; set; }
        public ActivityResponse Activity { get; set; }
        public string Date { get; set; }
        public string DocumentType { get; set; }
    }
}
