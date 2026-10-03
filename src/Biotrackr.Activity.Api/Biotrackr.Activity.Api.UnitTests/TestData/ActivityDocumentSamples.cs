using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Biotrackr.Activity.Api.UnitTests.TestData;

/// <summary>
/// Synthetic Cosmos documents for both schema versions. The version 2 sample follows plan contract C1 and
/// the masked Google Health API v4 response shapes; every value is invented.
/// </summary>
public static class ActivityDocumentSamples
{
    // Mirrors the Cosmos SDK serializer built from CosmosSerializationOptions { PropertyNamingPolicy = CamelCase }.
    public static readonly JsonSerializerSettings CosmosSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Include,
        MaxDepth = 64
    };

    public static T DeserializeLikeCosmos<T>(string json) =>
        JsonConvert.DeserializeObject<T>(json, CosmosSettings)!;

    /// <summary>
    /// The "activity" object of <see cref="Version1Document"/>, written exactly as System.Text.Json with web
    /// defaults emits it today, so tests can compare the API output to it field by field.
    /// </summary>
    public const string Version1ActivityJson = """
        {
          "activities": [
            {
              "activityId": 90013,
              "activityParentId": 90013,
              "activityParentName": "Walk",
              "calories": 212,
              "description": "Walking less than 2 mph, strolling very slowly",
              "duration": 2048000,
              "hasActiveZoneMinutes": true,
              "hasStartTime": true,
              "isFavorite": false,
              "lastModified": "2024-03-05T08:47:16",
              "logId": 61234567890,
              "name": "Walk",
              "startDate": "2024-03-05",
              "startTime": "07:58",
              "steps": 3125,
              "distance": 2.375
            }
          ],
          "goals": {
            "activeMinutes": 30,
            "caloriesOut": 2600,
            "distance": 8.05,
            "floors": 10,
            "steps": 10000
          },
          "summary": {
            "activeScore": -1,
            "activityCalories": 1034,
            "calorieEstimationMu": 2413,
            "caloriesBMR": 1688,
            "caloriesOut": 2533,
            "caloriesOutUnestimated": 2533,
            "distances": [
              { "activity": "total", "distance": 7.25 },
              { "activity": "tracker", "distance": 7.25 },
              { "activity": "Walk", "distance": 2.375 }
            ],
            "elevation": 21.5,
            "fairlyActiveMinutes": 21,
            "floors": 7,
            "heartRateZones": [
              { "caloriesOut": 1460.5, "max": 96, "min": 30, "minutes": 1210, "name": "Out of Range" },
              { "caloriesOut": 870.25, "max": 134, "min": 96, "minutes": 190, "name": "Fat Burn" },
              { "caloriesOut": 70.5, "max": 163, "min": 134, "minutes": 9, "name": "Cardio" },
              { "caloriesOut": 0, "max": 220, "min": 163, "minutes": 0, "name": "Peak" }
            ],
            "lightlyActiveMinutes": 242,
            "marginalCalories": 652,
            "restingHeartRate": 61,
            "sedentaryMinutes": 655,
            "steps": 10432,
            "useEstimation": true,
            "veryActiveMinutes": 14
          }
        }
        """;

    public const string Version1Id = "6a3d2f10-1c1e-4c0e-9d7a-6e2b8f1a0001";

    public static string Version1Document => $$"""
        {
          "id": "{{Version1Id}}",
          "date": "2024-03-05",
          "documentType": "Activity",
          "activity": {{Version1ActivityJson}},
          "_rid": "abc==",
          "_ts": 1709625600
        }
        """;

    public const string Version2Date = "2026-10-20";

    public const string Version2Document = """
        {
          "id": "6a3d2f10-1c1e-4c0e-9d7a-6e2b8f1a0002",
          "date": "2026-10-20",
          "documentType": "Activity",
          "provider": "Google",
          "schemaVersion": 2,
          "google": {
            "steps": { "rollupDataPoints": [ {
              "civilStartTime": { "date": { "year": 2026, "month": 10, "day": 20 }, "time": {} },
              "civilEndTime": { "date": { "year": 2026, "month": 10, "day": 21 }, "time": {} },
              "steps": { "countSum": "8432" } } ] },
            "totalCalories": { "rollupDataPoints": [ { "totalCalories": { "kcalSum": 2456.7 } } ] },
            "activeEnergyBurned": { "rollupDataPoints": [ { "activeEnergyBurned": { "kcalSum": 812.4 } } ] },
            "distance": { "rollupDataPoints": [ { "distance": { "millimetersSum": "6215000" } } ] },
            "floors": { "rollupDataPoints": [ { "floors": { "countSum": "12" } } ] },
            "altitude": { "rollupDataPoints": [ { "altitude": { "gainMillimetersSum": "36576" } } ] },
            "activeMinutes": { "rollupDataPoints": [ { "activeMinutes": { "activeMinutesRollupByActivityLevel": [
              { "activityLevel": "LIGHT", "activeMinutesSum": "184" },
              { "activityLevel": "MODERATE", "activeMinutesSum": "22" },
              { "activityLevel": "VIGOROUS", "activeMinutesSum": "17" } ] } } ] },
            "sedentaryPeriod": { "rollupDataPoints": [ { "sedentaryPeriod": { "durationSum": "41490s" } } ] },
            "activeZoneMinutes": { "rollupDataPoints": [ { "activeZoneMinutes": {
              "sumInCardioHeartZone": "18", "sumInPeakHeartZone": "4", "sumInFatBurnHeartZone": "14" } } ] },
            "timeInHeartRateZone": { "rollupDataPoints": [ { "timeInHeartRateZone": { "timeInHeartRateZones": [
              { "heartRateZone": "LIGHT", "duration": "5430s" },
              { "heartRateZone": "MODERATE", "duration": "1320s" },
              { "heartRateZone": "VIGOROUS", "duration": "600s" } ] } } ] },
            "caloriesInHeartRateZone": { "rollupDataPoints": [ { "caloriesInHeartRateZone": { "caloriesInHeartRateZones": [
              { "heartRateZone": "LIGHT", "kcal": 410.5 },
              { "heartRateZone": "MODERATE", "kcal": 150.25 },
              { "heartRateZone": "VIGOROUS", "kcal": 98 } ] } } ] },
            "dailyRestingHeartRate": { "dataPoints": [ {
              "dataSource": { "recordingMethod": "DERIVED", "platform": "FITBIT" },
              "dailyRestingHeartRate": { "date": { "year": 2026, "month": 10, "day": 20 }, "beatsPerMinute": "58" } } ] },
            "dailyHeartRateZones": { "dataPoints": [ {
              "dataSource": { "recordingMethod": "DERIVED", "platform": "FITBIT" },
              "dailyHeartRateZones": { "date": { "year": 2026, "month": 10, "day": 20 }, "heartRateZones": [
                { "heartRateZoneType": "LIGHT", "minBeatsPerMinute": "96", "maxBeatsPerMinute": "115" },
                { "heartRateZoneType": "MODERATE", "minBeatsPerMinute": "116", "maxBeatsPerMinute": "134" },
                { "heartRateZoneType": "VIGOROUS", "minBeatsPerMinute": "135", "maxBeatsPerMinute": "159" },
                { "heartRateZoneType": "PEAK", "minBeatsPerMinute": "160", "maxBeatsPerMinute": "220" } ] } } ] },
            "exercise": { "dataPoints": [ {
              "name": "users/me/dataTypes/exercise/dataPoints/synthetic-1",
              "dataSource": { "recordingMethod": "ACTIVELY_MEASURED", "platform": "FITBIT" },
              "exercise": {
                "interval": { "startTime": "2026-10-19T21:30:00Z", "startUtcOffset": "39600s",
                              "endTime": "2026-10-19T22:15:00Z", "endUtcOffset": "39600s" },
                "exerciseType": "WALKING",
                "metricsSummary": { "caloriesKcal": 215.6, "distanceMillimeters": 3820000, "steps": "5120",
                  "averageHeartRateBeatsPerMinute": "112", "activeZoneMinutes": "9",
                  "heartRateZoneDurations": { "lightTime": "1500s", "moderateTime": "540s", "vigorousTime": "0s", "peakTime": "0s" } },
                "exerciseMetadata": {},
                "displayName": "Morning Walk",
                "activeDuration": "2580s",
                "updateTime": "2026-10-19T22:20:00Z",
                "createTime": "2026-10-19T22:16:00Z" } } ] }
          }
        }
        """;

    /// <summary>A version 2 document where Google returned nothing for every source (device not worn).</summary>
    public const string Version2EmptyDocument = """
        {
          "id": "6a3d2f10-1c1e-4c0e-9d7a-6e2b8f1a0003",
          "date": "2026-10-21",
          "documentType": "Activity",
          "provider": "Google",
          "schemaVersion": 2,
          "google": {
            "steps": { "rollupDataPoints": [] },
            "totalCalories": { "rollupDataPoints": [] },
            "activeEnergyBurned": { "rollupDataPoints": [] },
            "distance": { "rollupDataPoints": [] },
            "floors": { "rollupDataPoints": [] },
            "altitude": { "rollupDataPoints": [] },
            "activeMinutes": { "rollupDataPoints": [] },
            "sedentaryPeriod": { "rollupDataPoints": [] },
            "activeZoneMinutes": { "rollupDataPoints": [] },
            "timeInHeartRateZone": { "rollupDataPoints": [] },
            "caloriesInHeartRateZone": { "rollupDataPoints": [] },
            "dailyRestingHeartRate": {},
            "dailyHeartRateZones": { "dataPoints": [] },
            "exercise": { "dataPoints": [] }
          }
        }
        """;
}
