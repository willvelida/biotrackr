namespace Biotrackr.Sleep.Api.UnitTests.TestData;

/// <summary>
/// Synthetic stored documents. Values are invented; shapes follow the version 1 documents
/// written by Sleep.Svc and the C1 version 2 envelope holding raw Google Health API bodies.
/// </summary>
public static class SleepDocumentSamples
{
    public const string V1Json = """
        {
          "id": "11111111-1111-1111-1111-111111111111",
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
                  "data": [
                    { "dateTime": "2025-01-09T23:00:00.000", "level": "wake", "seconds": 300 },
                    { "dateTime": "2025-01-09T23:05:00.000", "level": "light", "seconds": 1800 }
                  ],
                  "shortData": [
                    { "dateTime": "2025-01-10T02:00:00.000", "level": "wake", "seconds": 60 }
                  ],
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

    /// <summary>
    /// The HTTP response body the API produced for <see cref="V1Json"/> before the migration
    /// (System.Text.Json web defaults), used to prove version 1 output is unchanged.
    /// </summary>
    public const string V1PreMigrationResponseJson = """
        {
          "id": "11111111-1111-1111-1111-111111111111",
          "sleep": {
            "sleep": [
              {
                "dateOfSleep": "2025-01-10",
                "duration": 28800000,
                "efficiency": 93,
                "endTime": "2025-01-10T07:00:00",
                "infoCode": 0,
                "isMainSleep": true,
                "levels": {
                  "data": [
                    { "dateTime": "2025-01-09T23:00:00", "level": "wake", "seconds": 300 },
                    { "dateTime": "2025-01-09T23:05:00", "level": "light", "seconds": 1800 }
                  ],
                  "shortData": [
                    { "dateTime": "2025-01-10T02:00:00", "level": "wake", "seconds": 60 }
                  ],
                  "summary": { "stages": null, "totalMinutesAsleep": 0, "totalSleepRecords": 0, "totalTimeInBed": 0 }
                },
                "logId": 123456789,
                "minutesAfterWakeup": 0,
                "minutesAsleep": 420,
                "minutesAwake": 60,
                "minutesToFallAsleep": 0,
                "logType": "auto_detected",
                "startTime": "2025-01-09T23:00:00",
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

    /// <summary>
    /// Version 2 document for 2026-03-15 in a UTC+11 timezone. Session one is a staged main
    /// sleep (23:30 to 07:30 local, 432 of 480 minutes asleep); session two is a classic nap
    /// (14:00 to 14:45 local, 40 of 45 minutes asleep) with no short awakenings.
    /// </summary>
    public const string V2Json = """
        {
          "id": "22222222-2222-2222-2222-222222222222",
          "date": "2026-03-15",
          "documentType": "Sleep",
          "provider": "Google",
          "schemaVersion": 2,
          "google": {
            "sleep": {
              "dataPoints": [
                {
                  "name": "users/me/dataTypes/sleep/dataPoints/synthetic-1",
                  "dataSource": { "recordingMethod": "DERIVED", "device": { "displayName": "Synthetic" }, "platform": "FITBIT" },
                  "sleep": {
                    "interval": {
                      "startTime": "2026-03-14T12:30:00Z",
                      "startUtcOffset": "39600s",
                      "endTime": "2026-03-14T20:30:00Z",
                      "endUtcOffset": "39600s"
                    },
                    "type": "STAGES",
                    "stages": [
                      { "startTime": "2026-03-14T12:30:00Z", "startUtcOffset": "39600s", "endTime": "2026-03-14T12:40:00Z", "endUtcOffset": "39600s", "type": "AWAKE", "createTime": "2026-03-14T21:00:00Z", "updateTime": "2026-03-14T21:00:00Z" },
                      { "startTime": "2026-03-14T12:40:00Z", "startUtcOffset": "39600s", "endTime": "2026-03-14T14:00:00Z", "endUtcOffset": "39600s", "type": "LIGHT", "createTime": "2026-03-14T21:00:00Z", "updateTime": "2026-03-14T21:00:00Z" },
                      { "startTime": "2026-03-14T14:00:00Z", "startUtcOffset": "39600s", "endTime": "2026-03-14T15:30:00Z", "endUtcOffset": "39600s", "type": "DEEP", "createTime": "2026-03-14T21:00:00Z", "updateTime": "2026-03-14T21:00:00Z" },
                      { "startTime": "2026-03-14T15:30:00Z", "startUtcOffset": "39600s", "endTime": "2026-03-14T16:30:00Z", "endUtcOffset": "39600s", "type": "REM", "createTime": "2026-03-14T21:00:00Z", "updateTime": "2026-03-14T21:00:00Z" }
                    ],
                    "metadata": { "stagesStatus": "SUCCEEDED", "processed": true, "mainSleep": true },
                    "summary": {
                      "minutesInSleepPeriod": "480",
                      "minutesAfterWakeUp": "3",
                      "minutesToFallAsleep": "7",
                      "minutesAsleep": "432",
                      "minutesAwake": "48",
                      "stagesSummary": [
                        { "type": "AWAKE", "minutes": "48", "count": "3" },
                        { "type": "LIGHT", "minutes": "240", "count": "10" },
                        { "type": "DEEP", "minutes": "90", "count": "3" },
                        { "type": "REM", "minutes": "102", "count": "4" }
                      ]
                    },
                    "createTime": "2026-03-14T21:00:00Z",
                    "updateTime": "2026-03-14T21:00:00Z",
                    "shortAwakenings": [
                      { "startTime": "2026-03-14T13:10:00Z", "startUtcOffset": "39600s", "endTime": "2026-03-14T13:11:30Z", "endUtcOffset": "39600s", "type": "LIGHT" }
                    ]
                  }
                },
                {
                  "name": "users/me/dataTypes/sleep/dataPoints/synthetic-2",
                  "dataSource": { "recordingMethod": "DERIVED", "platform": "FITBIT" },
                  "sleep": {
                    "interval": {
                      "startTime": "2026-03-15T03:00:00Z",
                      "startUtcOffset": "39600s",
                      "endTime": "2026-03-15T03:45:00Z",
                      "endUtcOffset": "39600s"
                    },
                    "type": "CLASSIC",
                    "stages": [
                      { "startTime": "2026-03-15T03:00:00Z", "startUtcOffset": "39600s", "endTime": "2026-03-15T03:40:00Z", "endUtcOffset": "39600s", "type": "ASLEEP" },
                      { "startTime": "2026-03-15T03:40:00Z", "startUtcOffset": "39600s", "endTime": "2026-03-15T03:45:00Z", "endUtcOffset": "39600s", "type": "RESTLESS" }
                    ],
                    "metadata": { "processed": true, "mainSleep": false },
                    "summary": {
                      "minutesInSleepPeriod": "45",
                      "minutesAfterWakeUp": "0",
                      "minutesToFallAsleep": "0",
                      "minutesAsleep": "40",
                      "minutesAwake": "5",
                      "stagesSummary": [
                        { "type": "ASLEEP", "minutes": "40", "count": "1" },
                        { "type": "RESTLESS", "minutes": "3", "count": "1" },
                        { "type": "AWAKE", "minutes": "2", "count": "1" }
                      ]
                    }
                  }
                }
              ]
            },
            "dailyHeartRateVariability": {
              "dataPoints": [
                {
                  "name": "users/me/dataTypes/daily-heart-rate-variability/dataPoints/synthetic",
                  "dailyHeartRateVariability": {
                    "date": { "year": 2026, "month": 3, "day": 15 },
                    "averageHeartRateVariabilityMilliseconds": 42.5,
                    "nonRemHeartRateBeatsPerMinute": "58",
                    "entropy": 2.1,
                    "deepSleepRootMeanSquareOfSuccessiveDifferencesMilliseconds": 51.25
                  }
                }
              ]
            },
            "dailyOxygenSaturation": {
              "dataPoints": [
                {
                  "name": "users/me/dataTypes/daily-oxygen-saturation/dataPoints/synthetic",
                  "dailyOxygenSaturation": {
                    "date": { "year": 2026, "month": 3, "day": 15 },
                    "averagePercentage": 96.5,
                    "lowerBoundPercentage": 94.0,
                    "upperBoundPercentage": 98.0,
                    "standardDeviationPercentage": 0.8
                  }
                }
              ]
            },
            "dailyRespiratoryRate": {
              "dataPoints": [
                {
                  "name": "users/me/dataTypes/daily-respiratory-rate/dataPoints/synthetic",
                  "dailyRespiratoryRate": {
                    "date": { "year": 2026, "month": 3, "day": 15 },
                    "breathsPerMinute": 14.2
                  }
                }
              ]
            },
            "dailySleepTemperatureDerivations": {
              "dataPoints": [
                {
                  "name": "users/me/dataTypes/daily-sleep-temperature-derivations/dataPoints/synthetic",
                  "dailySleepTemperatureDerivations": {
                    "date": { "year": 2026, "month": 3, "day": 15 },
                    "nightlyTemperatureCelsius": 33.1,
                    "baselineTemperatureCelsius": 33.4,
                    "relativeNightlyStddev30dCelsius": 0.3
                  }
                }
              ]
            }
          }
        }
        """;

    /// <summary>
    /// Version 2 document for a date on which Google returned nothing for every source.
    /// </summary>
    public const string V2EmptyJson = """
        {
          "id": "33333333-3333-3333-3333-333333333333",
          "date": "2026-03-16",
          "documentType": "Sleep",
          "provider": "Google",
          "schemaVersion": 2,
          "google": {
            "sleep": { "dataPoints": [] },
            "dailyHeartRateVariability": {},
            "dailyOxygenSaturation": { "dataPoints": [] },
            "dailyRespiratoryRate": {},
            "dailySleepTemperatureDerivations": { "dataPoints": [] }
          }
        }
        """;
}
