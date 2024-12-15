using System.Diagnostics.CodeAnalysis;

namespace Biotrackr.Activity.Api.Models.FitbitEntities
{
    [ExcludeFromCodeCoverage]
    public class Distance
    {
        public string activity { get; set; }
        public double distance { get; set; }
    }
}
