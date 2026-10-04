namespace Biotrackr.Sleep.Api.IntegrationTests.Helpers;

/// <summary>
/// Synthetic raw documents seeded byte-for-byte into the emulator: a Fitbit-era version 1 document
/// (no schemaVersion) and a C1 version 2 document holding unmodified Google Health API bodies.
/// </summary>
public static class SleepDocumentSamples
{
    public const string V1Date = "2025-01-10";
    public const string V2Date = "2026-03-15";

    public const string V1Json = """
        {
          "id": "e2e-sleep-v1",
          "sleep": {
            "sleep": [
              {
                "dateOfSleep": "2025-01-10",
                "duration": 28800000,
                "efficiency": 93,
                "endTime": "2025-01-10T07:00:00.000",
                "infoCode": 0,
                "isMainSleep": true,
                "levels": {
                  "data": [ { "dateTime": "2025-01-09T23:00:00.000", "level": "wake", "seconds": 300 } ],
                  "shortData": [],
                  "summary": { "stages": null, "totalMinutesAsleep": 0, "totalSleepRecords": 0, "totalTimeInBed": 0 }
                },
                "logId": 123456789,
                "minutesAfterWakeup": 0,
                "minutesAsleep": 420,
                "minutesAwake": 60,
                "minutesToFallAsleep": 0,
                "logType": "auto_detected",
                "startTime": "2025-01-09T23:00:00.000",
                "timeInBed": 480,
                "type": "stages"
              }
            ],
            "summary": {
              "stages": { "deep": 80, "light": 250, "rem": 90, "wake": 60 },
              "totalMinutesAsleep": 420,
              "totalSleepRecords": 1,
              "totalTimeInBed": 480
            }
          },
          "date": "2025-01-10",
          "documentType": "Sleep"
        }
        """;

    public const string V2Json = """
        {
          "id": "e2e-sleep-v2",
          "date": "2026-03-15",
          "documentType": "Sleep",
          "provider": "Google",
          "schemaVersion": 2,
          "google": {
            "sleep": {
              "dataPoints": [
                {
                  "name": "users/me/dataTypes/sleep/dataPoints/synthetic-1",
                  "dataSource": { "recordingMethod": "DERIVED", "platform": "FITBIT" },
                  "sleep": {
                    "interval": { "startTime": "2026-03-14T12:30:00Z", "startUtcOffset": "39600s", "endTime": "2026-03-14T20:30:00Z", "endUtcOffset": "39600s" },
                    "type": "STAGES",
                    "stages": [
                      { "startTime": "2026-03-14T12:30:00Z", "startUtcOffset": "39600s", "endTime": "2026-03-14T12:40:00Z", "endUtcOffset": "39600s", "type": "AWAKE" },
                      { "startTime": "2026-03-14T12:40:00Z", "startUtcOffset": "39600s", "endTime": "2026-03-14T20:30:00Z", "endUtcOffset": "39600s", "type": "LIGHT" }
                    ],
                    "metadata": { "stagesStatus": "SUCCEEDED", "processed": true, "mainSleep": true },
                    "summary": {
                      "minutesInSleepPeriod": "480",
                      "minutesAfterWakeUp": "0",
                      "minutesToFallAsleep": "0",
                      "minutesAsleep": "432",
                      "minutesAwake": "48",
                      "stagesSummary": [ { "type": "AWAKE", "minutes": "48", "count": "1" }, { "type": "LIGHT", "minutes": "432", "count": "1" } ]
                    },
                    "shortAwakenings": [
                      { "startTime": "2026-03-14T13:10:00Z", "startUtcOffset": "39600s", "endTime": "2026-03-14T13:11:30Z", "endUtcOffset": "39600s", "type": "LIGHT" }
                    ]
                  }
                }
              ]
            },
            "dailyHeartRateVariability": { "dataPoints": [ { "dailyHeartRateVariability": { "date": { "year": 2026, "month": 3, "day": 15 }, "averageHeartRateVariabilityMilliseconds": 42.5 } } ] },
            "dailyOxygenSaturation": { "dataPoints": [] },
            "dailyRespiratoryRate": { "dataPoints": [ { "dailyRespiratoryRate": { "date": { "year": 2026, "month": 3, "day": 15 }, "breathsPerMinute": 14.2 } } ] },
            "dailySleepTemperatureDerivations": {}
          }
        }
        """;
}
