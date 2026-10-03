using System.Text.Json;
using System.Text.Json.Nodes;
using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Models.FitbitEntities;
using Biotrackr.Sleep.Api.Services;
using Biotrackr.Sleep.Api.UnitTests.TestData;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace Biotrackr.Sleep.Api.UnitTests.ServiceTests;

public class SleepDocumentTranslatorShould
{
    private readonly SleepDocumentTranslator _translator = new();

    [Fact]
    public void Translate_ShouldThrowArgumentNullException_WhenStoredDocumentIsNull()
    {
        // Arrange
        SleepStoredDocument storedDocument = null!;

        // Act
        var act = () => _translator.Translate(storedDocument);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Translate_ShouldReturnFitbitVersion1WithoutEnrichment_WhenSchemaVersionIsMissing()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V1Json);

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            Id = stored.Id,
            Date = stored.Date,
            DocumentType = stored.DocumentType,
            Provider = "Fitbit",
            SchemaVersion = 1,
            SleepEnrichment = (SleepEnrichment?)null
        });
    }

    [Fact]
    public void Translate_ShouldPassThroughStoredSleep_WhenDocumentIsVersion1()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V1Json);

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Sleep.Should().BeSameAs(stored.Sleep);
    }

    [Fact]
    public void Translate_ShouldReturnFitbitProvider_WhenSchemaVersionIs1()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V1Json);
        stored.SchemaVersion = 1;
        stored.Provider = "Fitbit";

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Provider.Should().Be("Fitbit");
    }

    [Fact]
    public void Translate_ShouldMatchPreMigrationResponseExceptAdditions_WhenDocumentIsVersion1()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V1Json);

        // Act
        var actual = JsonSerializer.SerializeToNode(_translator.Translate(stored), new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();

        // Assert
        var withoutAdditions = actual.DeepClone().AsObject();
        withoutAdditions.Remove("provider");
        withoutAdditions.Remove("schemaVersion");
        withoutAdditions.Remove("sleepEnrichment");
        JsonNode.DeepEquals(withoutAdditions, JsonNode.Parse(SleepDocumentSamples.V1PreMigrationResponseJson)).Should().BeTrue(
            $"AGENT FIX: AC 15/17 require version 1 fields to equal the pre-migration body. Actual: {actual.ToJsonString()}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Translate_ShouldThrowInvalidOperationException_WhenSchemaVersionIsUnsupported(int schemaVersion)
    {
        // Arrange
        var stored = new SleepStoredDocument { Id = "x", Date = "2026-03-15", SchemaVersion = schemaVersion };

        // Act
        var act = () => _translator.Translate(stored);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"*unsupported schemaVersion {schemaVersion}*");
    }

    [Fact]
    public void Translate_ShouldSetVersion2Envelope_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            Id = "22222222-2222-2222-2222-222222222222",
            Date = "2026-03-15",
            DocumentType = "Sleep",
            Provider = "Google",
            SchemaVersion = 2
        });
    }

    [Fact]
    public void Translate_ShouldDefaultProviderToGoogle_WhenVersion2ProviderIsMissing()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);
        stored.Provider = null;

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Provider.Should().Be("Google");
    }

    [Fact]
    public void Translate_ShouldReturnOneSessionPerGoogleSleepDataPoint_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Sleep.Sleep.Should().HaveCount(2);
    }

    [Fact]
    public void Translate_ShouldConvertSessionTimesToLocalWallClock_WhenUtcOffsetIsPositive()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var main = _translator.Translate(stored).Sleep.Sleep[0];

        // Assert
        main.StartTime.Should().Be(new DateTime(2026, 3, 14, 23, 30, 0, DateTimeKind.Unspecified));
        main.EndTime.Should().Be(new DateTime(2026, 3, 15, 7, 30, 0, DateTimeKind.Unspecified));
        main.EndTime.Kind.Should().Be(DateTimeKind.Unspecified, "AGENT FIX: local times must serialise without an offset, like Fitbit-era responses.");
    }

    [Fact]
    public void Translate_ShouldConvertSessionTimesToLocalWallClock_WhenUtcOffsetIsNegative()
    {
        // Arrange
        var stored = BuildVersion2(Session(start: "2026-03-15T03:00:00Z", end: "2026-03-15T11:00:00Z", offset: "-18000s"));

        // Act
        var session = _translator.Translate(stored).Sleep.Sleep.Single();

        // Assert
        session.StartTime.Should().Be(new DateTime(2026, 3, 14, 22, 0, 0));
    }

    [Fact]
    public void Translate_ShouldSetDateOfSleepToLocalEndDate_WhenSessionCrossesMidnight()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var main = _translator.Translate(stored).Sleep.Sleep[0];

        // Assert
        main.DateOfSleep.Should().Be("2026-03-15");
    }

    [Fact]
    public void Translate_ShouldMapStageIntervalsToLevelsData_WhenSessionHasStages()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var data = _translator.Translate(stored).Sleep.Sleep[0].Levels.Data;

        // Assert
        data.Should().BeEquivalentTo(new[]
        {
            new SleepData { DateTime = new DateTime(2026, 3, 14, 23, 30, 0), Level = "wake", Seconds = 600 },
            new SleepData { DateTime = new DateTime(2026, 3, 14, 23, 40, 0), Level = "light", Seconds = 4800 },
            new SleepData { DateTime = new DateTime(2026, 3, 15, 1, 0, 0), Level = "deep", Seconds = 5400 },
            new SleepData { DateTime = new DateTime(2026, 3, 15, 2, 30, 0), Level = "rem", Seconds = 3600 }
        }, options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData("AWAKE", "wake")]
    [InlineData("LIGHT", "light")]
    [InlineData("DEEP", "deep")]
    [InlineData("REM", "rem")]
    [InlineData("ASLEEP", "asleep")]
    [InlineData("RESTLESS", "restless")]
    [InlineData("SLEEP_STAGE_TYPE_UNSPECIFIED", "sleep_stage_type_unspecified")]
    public void Translate_ShouldMapGoogleStageTypeToFitbitLevel_WhenStageIsPresent(string stageType, string expectedLevel)
    {
        // Arrange
        var stored = BuildVersion2(Session(stages: $$"""[{ "startTime": "2026-03-14T13:00:00Z", "startUtcOffset": "0s", "endTime": "2026-03-14T13:01:00Z", "endUtcOffset": "0s", "type": "{{stageType}}" }]"""));

        // Act
        var level = _translator.Translate(stored).Sleep.Sleep.Single().Levels.Data.Single().Level;

        // Assert
        level.Should().Be(expectedLevel);
    }

    [Fact]
    public void Translate_ShouldMapShortAwakeningsToLevelsShortData_WhenSessionHasShortAwakenings()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var shortData = _translator.Translate(stored).Sleep.Sleep[0].Levels.ShortData;

        // Assert
        shortData.Should().BeEquivalentTo(new[]
        {
            new SleepData { DateTime = new DateTime(2026, 3, 15, 0, 10, 0), Level = "light", Seconds = 90 }
        });
    }

    [Fact]
    public void Translate_ShouldReturnEmptyShortData_WhenSessionHasNoShortAwakenings()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var nap = _translator.Translate(stored).Sleep.Sleep[1];

        // Assert
        nap.Levels.ShortData.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("432", "480", 90)]
    [InlineData("40", "45", 89)]
    [InlineData("1", "8", 13)]
    [InlineData("3", "8", 38)]
    [InlineData("0", "0", 0)]
    public void Translate_ShouldRoundEfficiencyToNearestInteger_WhenComputingFromMinutes(string minutesAsleep, string minutesInSleepPeriod, int expected)
    {
        // Arrange
        var stored = BuildVersion2(Session(summary: $$"""{ "minutesAsleep": "{{minutesAsleep}}", "minutesInSleepPeriod": "{{minutesInSleepPeriod}}" }"""));

        // Act
        var efficiency = _translator.Translate(stored).Sleep.Sleep.Single().Efficiency;

        // Assert
        efficiency.Should().Be(expected, "AGENT FIX: AC 21 is round(minutesAsleep / minutesInSleepPeriod * 100), midpoints away from zero.");
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void Translate_ShouldSetIsMainSleepFromMetadata_WhenDocumentIsVersion2(int sessionIndex, bool expected)
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var session = _translator.Translate(stored).Sleep.Sleep[sessionIndex];

        // Assert
        session.IsMainSleep.Should().Be(expected);
    }

    [Fact]
    public void Translate_ShouldSetIsMainSleepFalse_WhenMetadataIsMissing()
    {
        // Arrange
        var stored = BuildVersion2(Session());

        // Act
        var session = _translator.Translate(stored).Sleep.Sleep.Single();

        // Assert
        session.IsMainSleep.Should().BeFalse();
    }

    [Fact]
    public void Translate_ShouldReturnNullForFitbitOnlyFields_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var main = _translator.Translate(stored).Sleep.Sleep[0];

        // Assert
        main.Should().BeEquivalentTo(new { LogId = (long?)null, InfoCode = (int?)null, LogType = (string?)null });
    }

    [Theory]
    [InlineData(0, "stages")]
    [InlineData(1, "classic")]
    public void Translate_ShouldLowerCaseSessionType_WhenDocumentIsVersion2(int sessionIndex, string expected)
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var session = _translator.Translate(stored).Sleep.Sleep[sessionIndex];

        // Assert
        session.Type.Should().Be(expected);
    }

    [Fact]
    public void Translate_ShouldMapSessionSummaryMinutes_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var main = _translator.Translate(stored).Sleep.Sleep[0];

        // Assert
        main.Should().BeEquivalentTo(new
        {
            Duration = 28_800_000,
            MinutesAsleep = 432,
            MinutesAwake = 48,
            MinutesToFallAsleep = 7,
            MinutesAfterWakeup = 3,
            TimeInBed = 480
        });
    }

    [Fact]
    public void Translate_ShouldPopulateSessionLevelsSummaryFromStagesSummary_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var summary = _translator.Translate(stored).Sleep.Sleep[0].Levels.Summary;

        // Assert
        summary.Should().BeEquivalentTo(new Summary
        {
            Stages = new Stages { Deep = 90, Light = 240, Rem = 102, Wake = 48 },
            TotalMinutesAsleep = 432,
            TotalSleepRecords = 1,
            TotalTimeInBed = 480
        });
    }

    [Fact]
    public void Translate_ShouldSumDailySummaryAcrossSessions_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var summary = _translator.Translate(stored).Sleep.Summary;

        // Assert
        summary.Should().BeEquivalentTo(new Summary
        {
            Stages = new Stages { Deep = 90, Light = 240, Rem = 102, Wake = 50 },
            TotalMinutesAsleep = 472,
            TotalSleepRecords = 2,
            TotalTimeInBed = 525
        });
    }

    [Fact]
    public void Translate_ShouldMapHeartRateVariability_WhenSourceHasDataPoint()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var hrv = _translator.Translate(stored).SleepEnrichment!.HeartRateVariability;

        // Assert
        hrv.Should().Be(new HeartRateVariabilityEnrichment { AverageMs = 42.5, DeepSleepRmssdMs = 51.25 });
    }

    [Fact]
    public void Translate_ShouldLeaveOptionalHeartRateVariabilityFieldsNull_WhenGoogleOmitsThem()
    {
        // Arrange
        var stored = BuildVersion2(
            sessionsJson: string.Empty,
            extraSources: """
                "dailyHeartRateVariability": { "dataPoints": [ { "dailyHeartRateVariability": { "date": { "year": 2026, "month": 3, "day": 15 }, "entropy": 1.5 } } ] }
                """);

        // Act
        var hrv = _translator.Translate(stored).SleepEnrichment!.HeartRateVariability;

        // Assert
        hrv.Should().Be(new HeartRateVariabilityEnrichment { AverageMs = null, DeepSleepRmssdMs = null });
    }

    [Fact]
    public void Translate_ShouldMapOxygenSaturation_WhenSourceHasDataPoint()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var spo2 = _translator.Translate(stored).SleepEnrichment!.OxygenSaturation;

        // Assert
        spo2.Should().Be(new OxygenSaturationEnrichment { AveragePercent = 96.5, LowerBoundPercent = 94.0, UpperBoundPercent = 98.0 });
    }

    [Fact]
    public void Translate_ShouldMapRespiratoryRate_WhenSourceHasDataPoint()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var respiratoryRate = _translator.Translate(stored).SleepEnrichment!.RespiratoryRate;

        // Assert
        respiratoryRate.Should().Be(new RespiratoryRateEnrichment { BreathsPerMinute = 14.2 });
    }

    [Fact]
    public void Translate_ShouldMapSkinTemperature_WhenSourceHasDataPoint()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var temperature = _translator.Translate(stored).SleepEnrichment!.SkinTemperature;

        // Assert
        temperature.Should().Be(new SkinTemperatureEnrichment { NightlyCelsius = 33.1, BaselineCelsius = 33.4, DeviationCelsius = 0.3 });
    }

    [Fact]
    public void Translate_ShouldCollectShortAwakeningsIntoEnrichment_WhenAnySessionHasThem()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);

        // Act
        var shortAwakenings = _translator.Translate(stored).SleepEnrichment!.ShortAwakenings;

        // Assert
        shortAwakenings.Should().BeEquivalentTo(new[]
        {
            new ShortAwakening { StartTime = new DateTime(2026, 3, 15, 0, 10, 0), EndTime = new DateTime(2026, 3, 15, 0, 11, 30), Seconds = 90 }
        });
    }

    [Fact]
    public void Translate_ShouldReturnEnrichmentWithEveryMemberNull_WhenAllGoogleSourcesAreEmpty()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2EmptyJson);

        // Act
        var enrichment = _translator.Translate(stored).SleepEnrichment;

        // Assert
        enrichment.Should().Be(new SleepEnrichment(), "AGENT FIX: C2 requires each sleepEnrichment member to be null when its Google source is empty.");
    }

    [Fact]
    public void Translate_ShouldReturnNoSessionsAndZeroSummary_WhenGoogleSleepIsEmpty()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2EmptyJson);

        // Act
        var sleep = _translator.Translate(stored).Sleep;

        // Assert
        sleep.Should().BeEquivalentTo(new SleepResponse
        {
            Sleep = [],
            Summary = new Summary { Stages = new Stages(), TotalMinutesAsleep = 0, TotalSleepRecords = 0, TotalTimeInBed = 0 }
        });
    }

    [Fact]
    public void Translate_ShouldTreatMissingGooglePayloadAsEmpty_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = new SleepStoredDocument { Id = "x", Date = "2026-03-16", DocumentType = "Sleep", Provider = "Google", SchemaVersion = 2, Google = null };

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Sleep.Sleep.Should().BeEmpty();
    }

    [Fact]
    public void Translate_ShouldReadTimestamps_WhenGooglePayloadKeepsIsoStrings()
    {
        // Arrange
        using var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(SleepDocumentSamples.V2Json)) { DateParseHandling = Newtonsoft.Json.DateParseHandling.None };
        var raw = JObject.Load(reader);
        var stored = new SleepStoredDocument
        {
            Id = "x",
            Date = "2026-03-15",
            DocumentType = "Sleep",
            SchemaVersion = 2,
            Google = (JObject)raw["google"]!
        };

        // Act
        var main = _translator.Translate(stored).Sleep.Sleep[0];

        // Assert
        main.StartTime.Should().Be(new DateTime(2026, 3, 14, 23, 30, 0));
    }

    [Fact]
    public void Translate_ShouldThrowInvalidOperationException_WhenSessionIntervalIsMissing()
    {
        // Arrange
        var stored = BuildVersion2("""{ "sleep": { "type": "STAGES" } }""");

        // Act
        var act = () => _translator.Translate(stored);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*interval is missing*");
    }

    [Theory]
    [InlineData("\"39600\"")]
    [InlineData("\"abcs\"")]
    [InlineData("null")]
    public void Translate_ShouldThrowInvalidOperationException_WhenUtcOffsetIsMalformed(string offsetJson)
    {
        // Arrange
        var stored = BuildVersion2($$"""{ "sleep": { "interval": { "startTime": "2026-03-15T03:00:00Z", "startUtcOffset": {{offsetJson}}, "endTime": "2026-03-15T04:00:00Z", "endUtcOffset": "0s" } } }""");

        // Act
        var act = () => _translator.Translate(stored);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*startUtcOffset*");
    }

    [Fact]
    public void Translate_ShouldThrowInvalidOperationException_WhenTimestampIsMissing()
    {
        // Arrange
        var stored = BuildVersion2("""{ "sleep": { "interval": { "startUtcOffset": "0s", "endTime": "2026-03-15T04:00:00Z", "endUtcOffset": "0s" } } }""");

        // Act
        var act = () => _translator.Translate(stored);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*startTime*");
    }

    private static string Session(
        string start = "2026-03-14T13:00:00Z",
        string end = "2026-03-14T21:00:00Z",
        string offset = "0s",
        string stages = "[]",
        string summary = """{ "minutesAsleep": "400", "minutesInSleepPeriod": "480" }""") => $$"""
        {
          "sleep": {
            "interval": { "startTime": "{{start}}", "startUtcOffset": "{{offset}}", "endTime": "{{end}}", "endUtcOffset": "{{offset}}" },
            "type": "STAGES",
            "stages": {{stages}},
            "summary": {{summary}}
          }
        }
        """;

    private static SleepStoredDocument BuildVersion2(string sessionsJson, string? extraSources = null)
    {
        var sources = $$"""
            "sleep": { "dataPoints": [ {{sessionsJson}} ] }{{(extraSources is null ? string.Empty : "," + extraSources)}}
            """;

        return CosmosJson.Deserialize<SleepStoredDocument>($$"""
            {
              "id": "44444444-4444-4444-4444-444444444444",
              "date": "2026-03-15",
              "documentType": "Sleep",
              "provider": "Google",
              "schemaVersion": 2,
              "google": { {{sources}} }
            }
            """);
    }
}
