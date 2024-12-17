using System.Diagnostics.CodeAnalysis;

namespace Biotrackr.Activity.Svc.Models.FitbitEntities
{
    [ExcludeFromCodeCoverage]
    public class ActivityResponse
    {
        public List<Activity>? activities { get; set; }
        public Goals? goals { get; set; }
        public Summary? summary { get; set; }
    }
}
