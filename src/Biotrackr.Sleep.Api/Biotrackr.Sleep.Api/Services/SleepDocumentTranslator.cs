using System.Globalization;
using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Models.FitbitEntities;
using Biotrackr.Sleep.Api.Services.Interfaces;
using Newtonsoft.Json.Linq;
using SleepRecord = Biotrackr.Sleep.Api.Models.FitbitEntities.Sleep;

namespace Biotrackr.Sleep.Api.Services
{
    /// <summary>
    /// Stateless translator from stored sleep documents to the C2 read contract. Registered as a singleton.
    /// </summary>
    public sealed class SleepDocumentTranslator : ISleepDocumentTranslator
    {
        private const string FitbitProvider = "Fitbit";
        private const string GoogleProvider = "Google";

        private const string SleepSource = "sleep";
        private const string HeartRateVariabilitySource = "dailyHeartRateVariability";
        private const string OxygenSaturationSource = "dailyOxygenSaturation";
        private const string RespiratoryRateSource = "dailyRespiratoryRate";
        private const string SleepTemperatureSource = "dailySleepTemperatureDerivations";

        public SleepDocument Translate(SleepStoredDocument storedDocument)
        {
            ArgumentNullException.ThrowIfNull(storedDocument);

            return storedDocument.SchemaVersion switch
            {
                null or 1 => TranslateVersion1(storedDocument),
                2 => TranslateVersion2(storedDocument),
                var version => throw new InvalidOperationException(
                    $"Sleep document '{storedDocument.Id}' for {storedDocument.Date} has unsupported schemaVersion {version}.")
            };
        }

        private static SleepDocument TranslateVersion1(SleepStoredDocument storedDocument) => new()
        {
            Id = storedDocument.Id,
            Sleep = storedDocument.Sleep!,
            Date = storedDocument.Date,
            DocumentType = storedDocument.DocumentType,
            Provider = FitbitProvider,
            SchemaVersion = 1,
            SleepEnrichment = null
        };

        // O(s) time and space, where s is the total number of stages and short awakenings across sessions.
        private static SleepDocument TranslateVersion2(SleepStoredDocument storedDocument)
        {
            var google = storedDocument.Google;
            var sessions = new List<SleepRecord>();
            var shortAwakenings = new List<ShortAwakening>();

            foreach (var dataPoint in DataPoints(google, SleepSource))
            {
                if (dataPoint[SleepSource] is not JObject session)
                {
                    continue;
                }

                var sessionShortAwakenings = Intervals(session["shortAwakenings"]);
                sessions.Add(TranslateSession(session, sessionShortAwakenings));
                shortAwakenings.AddRange(sessionShortAwakenings.Select(a => new ShortAwakening
                {
                    StartTime = a.LocalStart,
                    EndTime = a.LocalEnd,
                    Seconds = a.Seconds
                }));
            }

            return new SleepDocument
            {
                Id = storedDocument.Id,
                Date = storedDocument.Date,
                DocumentType = storedDocument.DocumentType,
                Provider = storedDocument.Provider ?? GoogleProvider,
                SchemaVersion = 2,
                Sleep = new SleepResponse
                {
                    Sleep = sessions,
                    Summary = BuildDailySummary(sessions)
                },
                SleepEnrichment = new SleepEnrichment
                {
                    HeartRateVariability = FirstValue(google, HeartRateVariabilitySource) is { } hrv
                        ? new HeartRateVariabilityEnrichment
                        {
                            AverageMs = ReadDouble(hrv["averageHeartRateVariabilityMilliseconds"]),
                            DeepSleepRmssdMs = ReadDouble(hrv["deepSleepRootMeanSquareOfSuccessiveDifferencesMilliseconds"])
                        }
                        : null,
                    OxygenSaturation = FirstValue(google, OxygenSaturationSource) is { } spo2
                        ? new OxygenSaturationEnrichment
                        {
                            AveragePercent = ReadDouble(spo2["averagePercentage"]),
                            LowerBoundPercent = ReadDouble(spo2["lowerBoundPercentage"]),
                            UpperBoundPercent = ReadDouble(spo2["upperBoundPercentage"])
                        }
                        : null,
                    RespiratoryRate = FirstValue(google, RespiratoryRateSource) is { } respiratory
                        ? new RespiratoryRateEnrichment
                        {
                            BreathsPerMinute = ReadDouble(respiratory["breathsPerMinute"])
                        }
                        : null,
                    SkinTemperature = FirstValue(google, SleepTemperatureSource) is { } temperature
                        ? new SkinTemperatureEnrichment
                        {
                            NightlyCelsius = ReadDouble(temperature["nightlyTemperatureCelsius"]),
                            BaselineCelsius = ReadDouble(temperature["baselineTemperatureCelsius"]),
                            DeviationCelsius = ReadDouble(temperature["relativeNightlyStddev30dCelsius"])
                        }
                        : null,
                    ShortAwakenings = shortAwakenings.Count > 0 ? shortAwakenings : null
                }
            };
        }

        private static SleepRecord TranslateSession(JObject session, List<Interval> shortAwakenings)
        {
            var interval = ReadInterval(session["interval"]);
            var summary = session["summary"] as JObject;
            var minutesInSleepPeriod = ReadInt64(summary?["minutesInSleepPeriod"]) ?? 0;
            var minutesAsleep = ReadInt64(summary?["minutesAsleep"]) ?? 0;
            var stageMinutes = StageMinutes(summary?["stagesSummary"]);

            return new SleepRecord
            {
                DateOfSleep = interval.LocalEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Duration = ToInt32(minutesInSleepPeriod * 60_000),
                Efficiency = CalculateEfficiency(minutesAsleep, minutesInSleepPeriod),
                StartTime = interval.LocalStart,
                EndTime = interval.LocalEnd,
                InfoCode = null,
                LogId = null,
                IsMainSleep = (session["metadata"] as JObject)?["mainSleep"]?.Value<bool?>() ?? false,
                Levels = new Levels
                {
                    Data = Intervals(session["stages"]).Select(ToSleepData).ToList(),
                    ShortData = shortAwakenings.Select(ToSleepData).ToList(),
                    Summary = new Summary
                    {
                        Stages = ToStages(stageMinutes),
                        TotalMinutesAsleep = ToInt32(minutesAsleep),
                        TotalSleepRecords = 1,
                        TotalTimeInBed = ToInt32(minutesInSleepPeriod)
                    }
                },
                MinutesAfterWakeup = ToInt32(ReadInt64(summary?["minutesAfterWakeUp"]) ?? 0),
                MinutesAsleep = ToInt32(minutesAsleep),
                MinutesAwake = ToInt32(ReadInt64(summary?["minutesAwake"]) ?? 0),
                MinutesToFallAsleep = ToInt32(ReadInt64(summary?["minutesToFallAsleep"]) ?? 0),
                LogType = null,
                TimeInBed = ToInt32(minutesInSleepPeriod),
                Type = session["type"]?.Value<string>()?.ToLowerInvariant()
            };
        }

        // AC 21: round(minutesAsleep / minutesInSleepPeriod * 100). A zero-length period has no efficiency, so 0.
        private static int CalculateEfficiency(long minutesAsleep, long minutesInSleepPeriod) =>
            minutesInSleepPeriod <= 0
                ? 0
                : ToInt32((long)Math.Round(minutesAsleep * 100.0 / minutesInSleepPeriod, MidpointRounding.AwayFromZero));

        private static Summary BuildDailySummary(List<SleepRecord> sessions)
        {
            var stages = new Stages();
            var totalMinutesAsleep = 0;
            var totalTimeInBed = 0;

            foreach (var session in sessions)
            {
                var sessionStages = session.Levels.Summary.Stages;
                stages.Deep += sessionStages.Deep;
                stages.Light += sessionStages.Light;
                stages.Rem += sessionStages.Rem;
                stages.Wake += sessionStages.Wake;
                totalMinutesAsleep += session.MinutesAsleep;
                totalTimeInBed += session.TimeInBed;
            }

            return new Summary
            {
                Stages = stages,
                TotalMinutesAsleep = totalMinutesAsleep,
                TotalSleepRecords = sessions.Count,
                TotalTimeInBed = totalTimeInBed
            };
        }

        private static Dictionary<string, long> StageMinutes(JToken? stagesSummary)
        {
            var minutesByType = new Dictionary<string, long>(StringComparer.Ordinal);
            if (stagesSummary is not JArray entries)
            {
                return minutesByType;
            }

            foreach (var entry in entries.OfType<JObject>())
            {
                var type = entry["type"]?.Value<string>();
                if (type is null)
                {
                    continue;
                }

                minutesByType[type] = minutesByType.GetValueOrDefault(type) + (ReadInt64(entry["minutes"]) ?? 0);
            }

            return minutesByType;
        }

        private static Stages ToStages(Dictionary<string, long> stageMinutes) => new()
        {
            Deep = ToInt32(stageMinutes.GetValueOrDefault("DEEP")),
            Light = ToInt32(stageMinutes.GetValueOrDefault("LIGHT")),
            Rem = ToInt32(stageMinutes.GetValueOrDefault("REM")),
            Wake = ToInt32(stageMinutes.GetValueOrDefault("AWAKE"))
        };

        private static string MapLevel(string? stageType) => stageType switch
        {
            "AWAKE" => "wake",
            "LIGHT" => "light",
            "DEEP" => "deep",
            "REM" => "rem",
            "ASLEEP" => "asleep",
            "RESTLESS" => "restless",
            null => string.Empty,
            _ => stageType.ToLowerInvariant()
        };

        private static SleepData ToSleepData(Interval interval) => new()
        {
            DateTime = interval.LocalStart,
            Level = MapLevel(interval.Type),
            Seconds = interval.Seconds
        };

        private static IEnumerable<JObject> DataPoints(JObject? google, string sourceKey) =>
            (google?[sourceKey] as JObject)?["dataPoints"] is JArray dataPoints
                ? dataPoints.OfType<JObject>()
                : [];

        private static JObject? FirstValue(JObject? google, string sourceKey) =>
            DataPoints(google, sourceKey).Select(dataPoint => dataPoint[sourceKey] as JObject).FirstOrDefault(value => value is not null);

        private static List<Interval> Intervals(JToken? segments) =>
            segments is JArray array ? array.OfType<JObject>().Select(ReadInterval).ToList() : [];

        private static Interval ReadInterval(JToken? token)
        {
            if (token is not JObject segment)
            {
                throw new InvalidOperationException("Google sleep interval is missing.");
            }

            var start = ReadTimestamp(segment["startTime"], "startTime");
            var end = ReadTimestamp(segment["endTime"], "endTime");

            return new Interval(
                ToLocal(start, ReadUtcOffset(segment["startUtcOffset"], "startUtcOffset")),
                ToLocal(end, ReadUtcOffset(segment["endUtcOffset"], "endUtcOffset")),
                ToInt32((long)Math.Round((end - start).TotalSeconds)),
                segment["type"]?.Value<string>());
        }

        // Fitbit-era responses carry local wall-clock times without an offset; reproduce that shape.
        private static DateTime ToLocal(DateTimeOffset instant, TimeSpan utcOffset) =>
            DateTime.SpecifyKind(instant.UtcDateTime + utcOffset, DateTimeKind.Unspecified);

        // The Cosmos Newtonsoft serializer parses ISO strings into JTokenType.Date, so accept both forms.
        private static DateTimeOffset ReadTimestamp(JToken? token, string field) => token?.Type switch
        {
            JTokenType.Date => ((JValue)token).Value switch
            {
                DateTimeOffset offset => offset,
                DateTime { Kind: DateTimeKind.Unspecified } unspecified => new DateTimeOffset(DateTime.SpecifyKind(unspecified, DateTimeKind.Utc)),
                DateTime dateTime => new DateTimeOffset(dateTime),
                _ => throw new InvalidOperationException($"Google sleep field '{field}' is not a timestamp.")
            },
            JTokenType.String => DateTimeOffset.Parse(token.Value<string>()!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
            _ => throw new InvalidOperationException($"Google sleep field '{field}' is missing or not a timestamp.")
        };

        // Google durations are seconds with an "s" suffix, for example "39600s" or "-18000s".
        private static TimeSpan ReadUtcOffset(JToken? token, string field)
        {
            var value = token?.Type == JTokenType.String ? token.Value<string>() : null;
            if (value is null || !value.EndsWith('s') ||
                !decimal.TryParse(value.AsSpan(0, value.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            {
                throw new InvalidOperationException($"Google sleep field '{field}' is missing or not a duration.");
            }

            return TimeSpan.FromSeconds((double)seconds);
        }

        // int64 values arrive as JSON strings.
        private static long? ReadInt64(JToken? token) => token?.Type switch
        {
            JTokenType.Integer => token.Value<long>(),
            JTokenType.String => long.Parse(token.Value<string>()!, NumberStyles.Integer, CultureInfo.InvariantCulture),
            _ => null
        };

        private static double? ReadDouble(JToken? token) => token?.Type switch
        {
            JTokenType.Integer or JTokenType.Float => token.Value<double>(),
            JTokenType.String => double.Parse(token.Value<string>()!, NumberStyles.Float, CultureInfo.InvariantCulture),
            _ => null
        };

        private static int ToInt32(long value) => checked((int)value);

        private sealed record Interval(DateTime LocalStart, DateTime LocalEnd, int Seconds, string? Type);
    }
}
