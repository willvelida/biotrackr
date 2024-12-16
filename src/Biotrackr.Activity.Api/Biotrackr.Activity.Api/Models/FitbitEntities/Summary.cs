using System.Diagnostics.CodeAnalysis;

namespace Biotrackr.Activity.Api.Models.FitbitEntities
{
    [ExcludeFromCodeCoverage]
    public class Summary
    {
        public int activeScore { get; set; }
        public int activityCalories { get; set; }
        public int calorieEstimationMu { get; set; }
        public int caloriesBMR { get; set; }
        public int caloriesOut { get; set; }
        public int caloriesOutUnestimated { get; set; }
        public List<Distance> distances { get; set; }
        public double elevation { get; set; }
        public int fairlyActiveMinutes { get; set; }
        public int floors { get; set; }
        public List<HeartRateZone> heartRateZones { get; set; }
        public int lightlyActiveMinutes { get; set; }
        public int marginalCalories { get; set; }
        public int restingHeartRate { get; set; }
        public int sedentaryMinutes { get; set; }
        public int steps { get; set; }
        public bool useEstimation { get; set; }
        public int veryActiveMinutes { get; set; }
    }
}
