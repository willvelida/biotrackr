using Biotrackr.Activity.Api.Models.FitbitEntities;
using System.Diagnostics.CodeAnalysis;

namespace Biotrackr.Activity.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class ActivityDocument
    {
        public string Id { get; set; }
        public ActivityResponse Activity { get; set; }
        public string Date { get; set; }
        public string DocumentType { get; set; }
    }
}
