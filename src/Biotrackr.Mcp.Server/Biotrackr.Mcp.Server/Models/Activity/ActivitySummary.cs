using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Activity
{
    public class ActivitySummary
    {
        [JsonIgnore]
        [JsonPropertyName("activeScore")]
        public int ActiveScore { get; set; }

        [JsonPropertyName("activityCalories")]
        public int ActivityCalories { get; set; }

        [JsonIgnore]
        [JsonPropertyName("calorieEstimationMu")]
        public int CalorieEstimationMu { get; set; }

        [JsonPropertyName("caloriesBMR")]
        public int CaloriesBMR { get; set; }

        [JsonPropertyName("caloriesOut")]
        public int CaloriesOut { get; set; }

        [JsonIgnore]
        [JsonPropertyName("caloriesOutUnestimated")]
        public int CaloriesOutUnestimated { get; set; }

        [JsonPropertyName("distances")]
        public List<DistanceData> Distances { get; set; } = [];

        [JsonPropertyName("elevation")]
        public double Elevation { get; set; }

        [JsonPropertyName("fairlyActiveMinutes")]
        public int FairlyActiveMinutes { get; set; }

        [JsonPropertyName("floors")]
        public int Floors { get; set; }

        [JsonPropertyName("heartRateZones")]
        public List<HeartRateZone> HeartRateZones { get; set; } = [];

        [JsonPropertyName("lightlyActiveMinutes")]
        public int LightlyActiveMinutes { get; set; }

        [JsonIgnore]
        [JsonPropertyName("marginalCalories")]
        public int MarginalCalories { get; set; }

        [JsonPropertyName("restingHeartRate")]
        public int RestingHeartRate { get; set; }

        [JsonPropertyName("sedentaryMinutes")]
        public int SedentaryMinutes { get; set; }

        [JsonPropertyName("steps")]
        public int Steps { get; set; }

        [JsonIgnore]
        [JsonPropertyName("useEstimation")]
        public bool UseEstimation { get; set; }

        [JsonPropertyName("veryActiveMinutes")]
        public int VeryActiveMinutes { get; set; }

    }
}
