using System.Diagnostics.CodeAnalysis;

namespace Biotrackr.Activity.Svc.Models.FitbitEntities
{
    [ExcludeFromCodeCoverage]
    public class Goals
    {
        public int activeMinutes { get; set; }
        public int caloriesOut { get; set; }
        public double distance { get; set; }
        public int floors { get; set; }
        public int steps { get; set; }
    }
}
