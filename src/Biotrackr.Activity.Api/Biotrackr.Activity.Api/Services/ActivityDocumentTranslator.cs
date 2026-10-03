using System.Globalization;
using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.Models.FitbitEntities;
using Biotrackr.Activity.Api.Services.Interfaces;
using Newtonsoft.Json.Linq;
using FitbitActivity = Biotrackr.Activity.Api.Models.FitbitEntities.Activity;

namespace Biotrackr.Activity.Api.Services;

/// <summary>
/// Stateless translator from stored activity documents to the response model (plan contracts C1 and C2).
/// Google int64 values arrive as JSON strings and durations as "123s"; both are parsed here.
/// </summary>
public class ActivityDocumentTranslator : IActivityDocumentTranslator
{
    private const string FitbitProvider = "Fitbit";
    private const string GoogleProvider = "Google";
    private const double MillimetresPerKilometre = 1_000_000d;
    private const double MillimetresPerMetre = 1_000d;
    private const string LocalDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

    // Ordered as Google ranks them; Fitbit's "Out of Range" zone has no Google equivalent and is never emitted.
    private static readonly (string GoogleType, string Name)[] HeartRateZones =
    [
        ("LIGHT", "Light"),
        ("MODERATE", "Moderate"),
        ("VIGOROUS", "Vigorous"),
        ("PEAK", "Peak")
    ];

    public ActivityDocument Translate(ActivityStoredDocument storedDocument)
    {
        ArgumentNullException.ThrowIfNull(storedDocument);

        return storedDocument.SchemaVersion switch
        {
            null or 1 => TranslateVersion1(storedDocument),
            2 => TranslateVersion2(storedDocument),
            _ => throw new InvalidOperationException(
                $"Activity document '{storedDocument.Id}' has unsupported schemaVersion {storedDocument.SchemaVersion}.")
        };
    }

    private static ActivityDocument TranslateVersion1(ActivityStoredDocument storedDocument) => new()
    {
        Id = storedDocument.Id,
        Date = storedDocument.Date,
        DocumentType = storedDocument.DocumentType,
        Activity = storedDocument.Activity!,
        Provider = FitbitProvider,
        SchemaVersion = 1,
        ActivityEnrichment = null
    };

    private static ActivityDocument TranslateVersion2(ActivityStoredDocument storedDocument)
    {
        var google = storedDocument.Google;
        var exercises = ReadExercises(google);

        return new ActivityDocument
        {
            Id = storedDocument.Id,
            Date = storedDocument.Date,
            DocumentType = storedDocument.DocumentType,
            Provider = GoogleProvider,
            SchemaVersion = 2,
            Activity = new ActivityResponse
            {
                activities = exercises.Select(ToFitbitActivity).ToList(),
                goals = null,
                summary = BuildSummary(google)
            },
            ActivityEnrichment = new ActivityEnrichment
            {
                ActiveZoneMinutes = BuildActiveZoneMinutes(google),
                Workouts = exercises.Select(ToWorkout).ToList()
            }
        };
    }

    private static Summary BuildSummary(JObject? google)
    {
        var caloriesOut = RoundToInt(ReadNumber(RollupValue(google, "totalCalories")?["kcalSum"]));
        var activityCalories = RoundToInt(ReadNumber(RollupValue(google, "activeEnergyBurned")?["kcalSum"]));
        var activeMinutesByLevel = ReadActiveMinutesByLevel(google);

        return new Summary
        {
            activeScore = null,
            activityCalories = activityCalories,
            calorieEstimationMu = null,
            // Derived from the rounded values so caloriesOut = caloriesBMR + activityCalories holds exactly (AC 22).
            caloriesBMR = caloriesOut - activityCalories,
            caloriesOut = caloriesOut,
            caloriesOutUnestimated = null,
            distances =
            [
                new Distance
                {
                    activity = "total",
                    distance = ReadNumber(RollupValue(google, "distance")?["millimetersSum"]) / MillimetresPerKilometre
                }
            ],
            elevation = ReadNumber(RollupValue(google, "altitude")?["gainMillimetersSum"]) / MillimetresPerMetre,
            fairlyActiveMinutes = activeMinutesByLevel.GetValueOrDefault("MODERATE"),
            floors = RoundToInt(ReadNumber(RollupValue(google, "floors")?["countSum"])),
            heartRateZones = BuildHeartRateZones(google),
            lightlyActiveMinutes = activeMinutesByLevel.GetValueOrDefault("LIGHT"),
            marginalCalories = null,
            restingHeartRate = RoundToInt(ReadNumber(ReadRestingHeartRate(google))),
            sedentaryMinutes = ToWholeMinutes(ReadDurationSeconds(RollupValue(google, "sedentaryPeriod")?["durationSum"])),
            steps = RoundToInt(ReadNumber(RollupValue(google, "steps")?["countSum"])),
            useEstimation = null,
            veryActiveMinutes = activeMinutesByLevel.GetValueOrDefault("VIGOROUS")
        };
    }

    private static Dictionary<string, int> ReadActiveMinutesByLevel(JObject? google)
    {
        var levels = new Dictionary<string, int>(StringComparer.Ordinal);
        var rollup = RollupValue(google, "activeMinutes")?["activeMinutesRollupByActivityLevel"] as JArray;
        foreach (var level in rollup?.OfType<JObject>() ?? [])
        {
            if ((string?)level["activityLevel"] is { } name)
            {
                levels[name] = ReadMinutes(level["activeMinutesSum"]);
            }
        }

        return levels;
    }

    private static JToken? ReadRestingHeartRate(JObject? google) =>
        DataPoints(google, "dailyRestingHeartRate")
            .Select(point => (point["dailyRestingHeartRate"] as JObject)?["beatsPerMinute"])
            .FirstOrDefault(value => value is not null);

    // O(z) with z <= 4 zones: each source is indexed once by zone type, then merged in Google's order.
    private static List<HeartRateZone> BuildHeartRateZones(JObject? google)
    {
        var boundaries = new Dictionary<string, JObject>(StringComparer.Ordinal);
        var boundaryZones = DataPoints(google, "dailyHeartRateZones")
            .Select(point => (point["dailyHeartRateZones"] as JObject)?["heartRateZones"] as JArray)
            .FirstOrDefault(zones => zones is not null);
        foreach (var zone in boundaryZones?.OfType<JObject>() ?? [])
        {
            if ((string?)zone["heartRateZoneType"] is { } type)
            {
                boundaries.TryAdd(type, zone);
            }
        }

        var minutes = IndexByZone(
            RollupValue(google, "timeInHeartRateZone")?["timeInHeartRateZones"],
            zone => ToWholeMinutes(ReadDurationSeconds(zone["duration"])));
        var calories = IndexByZone(
            RollupValue(google, "caloriesInHeartRateZone")?["caloriesInHeartRateZones"],
            zone => ReadNumber(zone["kcal"]));

        var result = new List<HeartRateZone>(HeartRateZones.Length);
        foreach (var (googleType, name) in HeartRateZones)
        {
            var hasBoundary = boundaries.TryGetValue(googleType, out var boundary);
            var hasMinutes = minutes.TryGetValue(googleType, out var zoneMinutes);
            var hasCalories = calories.TryGetValue(googleType, out var zoneCalories);
            if (!hasBoundary && !hasMinutes && !hasCalories)
            {
                continue;
            }

            result.Add(new HeartRateZone
            {
                name = name,
                min = RoundToInt(ReadNumber(boundary?["minBeatsPerMinute"])),
                max = RoundToInt(ReadNumber(boundary?["maxBeatsPerMinute"])),
                minutes = zoneMinutes,
                caloriesOut = zoneCalories
            });
        }

        return result;
    }

    private static Dictionary<string, T> IndexByZone<T>(JToken? zones, Func<JObject, T> read)
    {
        var index = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var zone in (zones as JArray)?.OfType<JObject>() ?? [])
        {
            if ((string?)zone["heartRateZone"] is { } type)
            {
                index.TryAdd(type, read(zone));
            }
        }

        return index;
    }

    private static ActiveZoneMinutes? BuildActiveZoneMinutes(JObject? google)
    {
        var rollup = RollupValue(google, "activeZoneMinutes");
        if (rollup is null)
        {
            return null;
        }

        var fatBurn = RoundToInt(ReadNumber(rollup["sumInFatBurnHeartZone"]));
        var cardio = RoundToInt(ReadNumber(rollup["sumInCardioHeartZone"]));
        var peak = RoundToInt(ReadNumber(rollup["sumInPeakHeartZone"]));

        return new ActiveZoneMinutes { FatBurn = fatBurn, Cardio = cardio, Peak = peak, Total = fatBurn + cardio + peak };
    }

    private static List<ExerciseSession> ReadExercises(JObject? google)
    {
        var sessions = new List<ExerciseSession>();
        foreach (var point in DataPoints(google, "exercise"))
        {
            if (point["exercise"] is not JObject exercise)
            {
                continue;
            }

            var interval = exercise["interval"] as JObject;
            var start = ReadTimestamp(interval?["startTime"]);
            var end = ReadTimestamp(interval?["endTime"]);
            var metrics = exercise["metricsSummary"] as JObject;

            var durationSeconds = exercise["activeDuration"] is { Type: not JTokenType.Null } activeDuration
                ? ReadDurationSeconds(activeDuration)
                : start is not null && end is not null ? (end.Value - start.Value).TotalSeconds : 0d;

            sessions.Add(new ExerciseSession(
                DisplayName: (string?)exercise["displayName"],
                ExerciseType: (string?)exercise["exerciseType"],
                LocalStart: ToLocal(start, interval?["startUtcOffset"]),
                LocalEnd: ToLocal(end, interval?["endUtcOffset"]),
                DurationSeconds: durationSeconds,
                Calories: RoundToInt(ReadNumber(metrics?["caloriesKcal"])),
                Steps: RoundToInt(ReadNumber(metrics?["steps"])),
                DistanceKm: ReadNumber(metrics?["distanceMillimeters"]) / MillimetresPerKilometre,
                AverageHeartRate: RoundToInt(ReadNumber(metrics?["averageHeartRateBeatsPerMinute"])),
                ActiveZoneMinutes: metrics?["activeZoneMinutes"] is { Type: not JTokenType.Null } azm ? RoundToInt(ReadNumber(azm)) : null,
                LastModified: ReadTimestamp(exercise["updateTime"])?.UtcDateTime ?? default));
        }

        return sessions;
    }

    private static Workout ToWorkout(ExerciseSession session) => new()
    {
        Name = session.DisplayName,
        ExerciseType = session.ExerciseType,
        StartTime = session.LocalStart?.ToString(LocalDateTimeFormat, CultureInfo.InvariantCulture),
        EndTime = session.LocalEnd?.ToString(LocalDateTimeFormat, CultureInfo.InvariantCulture),
        DurationMinutes = ToWholeMinutes(session.DurationSeconds),
        Calories = session.Calories,
        Steps = session.Steps,
        DistanceKm = session.DistanceKm,
        AverageHeartRate = session.AverageHeartRate,
        ActiveZoneMinutes = session.ActiveZoneMinutes ?? 0
    };

    private static FitbitActivity ToFitbitActivity(ExerciseSession session) => new()
    {
        activityId = null,
        activityParentId = null,
        activityParentName = session.DisplayName,
        calories = session.Calories,
        description = null,
        distance = session.DistanceKm,
        duration = (int)Math.Round(session.DurationSeconds * 1000d, MidpointRounding.AwayFromZero),
        hasActiveZoneMinutes = session.ActiveZoneMinutes is not null,
        hasStartTime = session.LocalStart is not null,
        isFavorite = false,
        lastModified = session.LastModified,
        logId = null,
        name = session.DisplayName,
        startDate = session.LocalStart?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        startTime = session.LocalStart?.ToString("HH:mm", CultureInfo.InvariantCulture),
        steps = session.Steps
    };

    private static JObject? RollupValue(JObject? google, string sourceKey) =>
        (((google?[sourceKey] as JObject)?["rollupDataPoints"] as JArray)?.OfType<JObject>() ?? [])
            .Select(point => point[sourceKey] as JObject)
            .FirstOrDefault(value => value is not null);

    private static IEnumerable<JObject> DataPoints(JObject? google, string sourceKey) =>
        ((google?[sourceKey] as JObject)?["dataPoints"] as JArray)?.OfType<JObject>() ?? [];

    private static double ReadNumber(JToken? token) =>
        token is null || token.Type == JTokenType.Null ? 0d : (double)token;

    private static int RoundToInt(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    // Google durations are seconds with an "s" suffix, for example "41490s" or "3.5s".
    private static double ReadDurationSeconds(JToken? token)
    {
        if (token is null || token.Type == JTokenType.Null)
        {
            return 0d;
        }

        var text = ((string?)token ?? string.Empty).Trim();
        if (text.EndsWith('s'))
        {
            text = text[..^1];
        }

        return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    // activeMinutesSum is an int64 count of minutes; a duration string is also accepted defensively.
    private static int ReadMinutes(JToken? token) =>
        token is { Type: JTokenType.String } && ((string?)token)!.Trim().EndsWith('s')
            ? ToWholeMinutes(ReadDurationSeconds(token))
            : RoundToInt(ReadNumber(token));

    private static int ToWholeMinutes(double seconds) => (int)Math.Floor(seconds / 60d);

    // The Cosmos Newtonsoft serializer parses ISO-8601 strings into Date tokens, so both forms are handled.
    private static DateTimeOffset? ReadTimestamp(JToken? token)
    {
        switch (token)
        {
            case JValue { Value: DateTimeOffset offset }:
                return offset;
            case JValue { Value: DateTime dateTime }:
                var utc = dateTime.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                    : dateTime.ToUniversalTime();
                return new DateTimeOffset(utc);
            case JValue { Type: JTokenType.String } value:
                return DateTimeOffset.Parse((string)value!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            default:
                return null;
        }
    }

    private static DateTime? ToLocal(DateTimeOffset? instant, JToken? utcOffset) =>
        instant is null
            ? null
            : DateTime.SpecifyKind(instant.Value.UtcDateTime.AddSeconds(ReadDurationSeconds(utcOffset)), DateTimeKind.Unspecified);

    private sealed record ExerciseSession(
        string? DisplayName,
        string? ExerciseType,
        DateTime? LocalStart,
        DateTime? LocalEnd,
        double DurationSeconds,
        int Calories,
        int Steps,
        double DistanceKm,
        int AverageHeartRate,
        int? ActiveZoneMinutes,
        DateTime LastModified);
}
