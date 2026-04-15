using System.Text.Json;
using Biotrackr.Reporting.Svc.Models;
using Biotrackr.Reporting.Svc.Services.Interfaces;

namespace Biotrackr.Reporting.Svc.Services;

public class MetricExtractor : IMetricExtractor
{
    private readonly ILogger<MetricExtractor> _logger;

    public MetricExtractor(ILogger<MetricExtractor> logger)
    {
        _logger = logger;
    }

    public List<MetricCard> ExtractMetrics(HealthDataSnapshot snapshot)
    {
        var metrics = new List<MetricCard>();

        metrics.AddRange(ExtractActivityMetrics(snapshot.Activity));
        metrics.AddRange(ExtractSleepMetrics(snapshot.Sleep));
        metrics.AddRange(ExtractFoodMetrics(snapshot.Food));
        metrics.AddRange(ExtractVitalsMetrics(snapshot.Vitals));

        return metrics;
    }

    private List<MetricCard> ExtractActivityMetrics(string activityJson)
    {
        var cards = new List<MetricCard>();

        if (string.IsNullOrWhiteSpace(activityJson))
            return cards;

        try
        {
            using var doc = JsonDocument.Parse(activityJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return cards;

            long totalSteps = 0;
            int totalFairlyActive = 0;
            int totalVeryActive = 0;
            var restingHrValues = new List<int>();

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("activity", out var activity))
                    continue;

                if (!activity.TryGetProperty("summary", out var summary))
                    continue;

                if (summary.TryGetProperty("steps", out var steps))
                    totalSteps += steps.GetInt64();

                if (summary.TryGetProperty("fairlyActiveMinutes", out var fairly))
                    totalFairlyActive += fairly.GetInt32();

                if (summary.TryGetProperty("veryActiveMinutes", out var very))
                    totalVeryActive += very.GetInt32();

                if (summary.TryGetProperty("restingHeartRate", out var hr))
                {
                    var hrValue = hr.GetInt32();
                    if (hrValue > 0)
                        restingHrValues.Add(hrValue);
                }
            }

            if (totalSteps > 0)
            {
                cards.Add(new MetricCard
                {
                    Label = "Total Steps",
                    Value = totalSteps.ToString("N0"),
                    Unit = "steps",
                    Icon = "🚶",
                    Color = "#1a73e8"
                });
            }

            var totalActiveMinutes = totalFairlyActive + totalVeryActive;
            if (totalActiveMinutes > 0)
            {
                cards.Add(new MetricCard
                {
                    Label = "Active Minutes",
                    Value = totalActiveMinutes.ToString(),
                    Unit = "min",
                    Icon = "⚡",
                    Color = "#34a853"
                });
            }

            if (restingHrValues.Count > 0)
            {
                var avgHr = (int)Math.Round(restingHrValues.Average());
                cards.Add(new MetricCard
                {
                    Label = "Avg Resting HR",
                    Value = avgHr.ToString(),
                    Unit = "bpm",
                    Icon = "❤️",
                    Color = "#ea4335"
                });
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse activity JSON for metric extraction");
        }

        return cards;
    }

    private List<MetricCard> ExtractSleepMetrics(string sleepJson)
    {
        var cards = new List<MetricCard>();

        if (string.IsNullOrWhiteSpace(sleepJson))
            return cards;

        try
        {
            using var doc = JsonDocument.Parse(sleepJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return cards;

            var sleepMinutes = new List<int>();

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("sleep", out var sleep))
                    continue;

                if (!sleep.TryGetProperty("summary", out var summary))
                    continue;

                if (summary.TryGetProperty("totalMinutesAsleep", out var minutes))
                    sleepMinutes.Add(minutes.GetInt32());
            }

            if (sleepMinutes.Count > 0)
            {
                var avgMinutes = (int)Math.Round(sleepMinutes.Average());
                cards.Add(new MetricCard
                {
                    Label = "Avg Sleep",
                    Value = FormatSleepDuration(avgMinutes),
                    Unit = "",
                    Icon = "😴",
                    Color = "#673ab7"
                });
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse sleep JSON for metric extraction");
        }

        return cards;
    }

    private List<MetricCard> ExtractFoodMetrics(string foodJson)
    {
        var cards = new List<MetricCard>();

        if (string.IsNullOrWhiteSpace(foodJson))
            return cards;

        try
        {
            using var doc = JsonDocument.Parse(foodJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return cards;

            var calorieValues = new List<double>();

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("food", out var food))
                    continue;

                if (!food.TryGetProperty("summary", out var summary))
                    continue;

                if (summary.TryGetProperty("calories", out var calories))
                    calorieValues.Add(calories.GetDouble());
            }

            if (calorieValues.Count > 0)
            {
                var avgCalories = (int)Math.Round(calorieValues.Average());
                var card = new MetricCard
                {
                    Label = "Avg Calories",
                    Value = avgCalories.ToString("N0"),
                    Unit = "kcal",
                    Icon = "🔥",
                    Color = "#ff6d00"
                };

                if (calorieValues.Count < 5)
                    card.Subtitle = $"({calorieValues.Count} days logged)";

                cards.Add(card);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse food JSON for metric extraction");
        }

        return cards;
    }

    private List<MetricCard> ExtractVitalsMetrics(string vitalsJson)
    {
        var cards = new List<MetricCard>();

        if (string.IsNullOrWhiteSpace(vitalsJson))
            return cards;

        try
        {
            using var doc = JsonDocument.Parse(vitalsJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return cards;

            double? latestWeight = null;
            int readingCount = 0;

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("weight", out var weight) || weight.ValueKind == JsonValueKind.Null)
                    continue;

                if (weight.TryGetProperty("weight", out var weightValue))
                {
                    latestWeight = weightValue.GetDouble();
                    readingCount++;
                }
            }

            if (latestWeight.HasValue)
            {
                cards.Add(new MetricCard
                {
                    Label = "Latest Weight",
                    Value = latestWeight.Value.ToString("N1"),
                    Unit = "kg",
                    Icon = "⚖️",
                    Color = "#6b7280",
                    Subtitle = FormatReadingCount(readingCount)
                });
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse vitals JSON for metric extraction");
        }

        return cards;
    }

    private static string FormatSleepDuration(int totalMinutes)
    {
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return $"{hours}h {minutes}m";
    }

    private static string FormatReadingCount(int count)
    {
        return count == 1 ? "(1 reading)" : $"({count} readings)";
    }
}
