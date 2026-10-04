using System.Text.Json.Serialization;

namespace Biotrackr.Sleep.Api.Models
{
    /// <summary>
    /// Google-only sleep data for a date (C2 read contract). Null on version 1 documents;
    /// each member is null when Google returned nothing for its source.
    /// </summary>
    public sealed record SleepEnrichment
    {
        [JsonPropertyName("heartRateVariability")]
        public HeartRateVariabilityEnrichment? HeartRateVariability { get; init; }

        [JsonPropertyName("oxygenSaturation")]
        public OxygenSaturationEnrichment? OxygenSaturation { get; init; }

        [JsonPropertyName("respiratoryRate")]
        public RespiratoryRateEnrichment? RespiratoryRate { get; init; }

        [JsonPropertyName("skinTemperature")]
        public SkinTemperatureEnrichment? SkinTemperature { get; init; }

        [JsonPropertyName("shortAwakenings")]
        public List<ShortAwakening>? ShortAwakenings { get; init; }
    }

    public sealed record HeartRateVariabilityEnrichment
    {
        [JsonPropertyName("averageMs")]
        public double? AverageMs { get; init; }

        [JsonPropertyName("deepSleepRmssdMs")]
        public double? DeepSleepRmssdMs { get; init; }
    }

    public sealed record OxygenSaturationEnrichment
    {
        [JsonPropertyName("averagePercent")]
        public double? AveragePercent { get; init; }

        [JsonPropertyName("lowerBoundPercent")]
        public double? LowerBoundPercent { get; init; }

        [JsonPropertyName("upperBoundPercent")]
        public double? UpperBoundPercent { get; init; }
    }

    public sealed record RespiratoryRateEnrichment
    {
        [JsonPropertyName("breathsPerMinute")]
        public double? BreathsPerMinute { get; init; }
    }

    public sealed record SkinTemperatureEnrichment
    {
        [JsonPropertyName("nightlyCelsius")]
        public double? NightlyCelsius { get; init; }

        [JsonPropertyName("baselineCelsius")]
        public double? BaselineCelsius { get; init; }

        /// <summary>Nightly minus baseline; null when either is missing.</summary>
        [JsonPropertyName("deviationCelsius")]
        public double? DeviationCelsius { get; init; }

        /// <summary>Standard deviation of relative nightly temperature over the past 30 days.</summary>
        [JsonPropertyName("variabilityCelsius")]
        public double? VariabilityCelsius { get; init; }
    }

    public sealed record ShortAwakening
    {
        /// <summary>Local time (no offset), matching <c>levels.data[].dateTime</c>.</summary>
        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; init; }

        /// <summary>Local time (no offset).</summary>
        [JsonPropertyName("endTime")]
        public DateTime EndTime { get; init; }

        [JsonPropertyName("seconds")]
        public int Seconds { get; init; }
    }
}
