using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Activity
{
    public class ActivityLog
    {
        [JsonPropertyName("activityId")]
        public int ActivityId { get; set; }

        [JsonPropertyName("activityParentId")]
        public int ActivityParentId { get; set; }

        [JsonPropertyName("activityParentName")]
        public string ActivityParentName { get; set; } = string.Empty;

        [JsonPropertyName("calories")]
        public int Calories { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("duration")]
        public long Duration { get; set; }

        [JsonPropertyName("hasActiveZoneMinutes")]
        public bool HasActiveZoneMinutes { get; set; }

        [JsonPropertyName("hasStartTime")]
        public bool HasStartTime { get; set; }

        [JsonPropertyName("isFavorite")]
        public bool IsFavorite { get; set; }

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; }

        [JsonPropertyName("logId")]
        public long LogId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("startDate")]
        public string StartDate { get; set; } = string.Empty;

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; } = string.Empty;

        [JsonPropertyName("steps")]
        public int Steps { get; set; }

        [JsonPropertyName("distance")]
        public double? Distance { get; set; }
    }
}
