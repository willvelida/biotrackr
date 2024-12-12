using Biotrackr.Activity.Svc.Models.FitbitEntities;

namespace Biotrackr.Activity.Svc.Models
{
    public class ActivityDocument
    {
        public string Id { get; set; }
        public ActivityResponse Activity { get; set; }
        public string Date { get; set; }
    }
}
