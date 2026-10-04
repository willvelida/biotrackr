using System.Text.Json;
using System.Text.Json.Nodes;
using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.Services;
using Biotrackr.Activity.Api.UnitTests.TestData;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace Biotrackr.Activity.Api.UnitTests.ServiceTests;

public class ActivityDocumentTranslatorShould
{
    private readonly ActivityDocumentTranslator _translator = new();

    private static ActivityStoredDocument Version2() =>
        ActivityDocumentSamples.DeserializeLikeCosmos<ActivityStoredDocument>(ActivityDocumentSamples.Version2Document);

    private static ActivityStoredDocument Version2With(Action<JObject> mutateGoogle)
    {
        var document = Version2();
        mutateGoogle(document.Google!);
        return document;
    }

    [Fact]
    public void Translate_ShouldThrowArgumentNullException_WhenDocumentIsNull()
    {
        // Arrange
        ActivityStoredDocument document = null!;

        // Act
        var act = () => _translator.Translate(document);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Translate_ShouldThrowInvalidOperationException_WhenSchemaVersionIsUnsupported()
    {
        // Arrange
        var document = new ActivityStoredDocument { Id = "id-3", SchemaVersion = 3 };

        // Act
        var act = () => _translator.Translate(document);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*id-3*schemaVersion 3*");
    }

    [Fact]
    public void Translate_ShouldPassVersion1Through_WhenSchemaVersionIsMissing()
    {
        // Arrange
        var stored = ActivityDocumentSamples.DeserializeLikeCosmos<ActivityStoredDocument>(ActivityDocumentSamples.Version1Document);

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            Id = ActivityDocumentSamples.Version1Id,
            Date = "2024-03-05",
            DocumentType = "Activity",
            Provider = "Fitbit",
            SchemaVersion = 1,
            ActivityEnrichment = (ActivityEnrichment?)null
        }, "AGENT FIX: version 1 documents return provider Fitbit, schemaVersion 1, null enrichment (AC 17, AC 27)");
        result.Activity.Should().BeSameAs(stored.Activity);
    }

    [Fact]
    public void Translate_ShouldPassVersion1Through_WhenSchemaVersionIsOne()
    {
        // Arrange
        var stored = new ActivityStoredDocument { Id = "id-1", SchemaVersion = 1, Provider = "Fitbit" };

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Provider.Should().Be("Fitbit");
        result.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void Translate_ShouldEmitVersion1JsonUnchanged_WhenSerialisedForHttp()
    {
        // Arrange
        var stored = ActivityDocumentSamples.DeserializeLikeCosmos<ActivityStoredDocument>(ActivityDocumentSamples.Version1Document);

        // Act
        var json = JsonNode.Parse(JsonSerializer.Serialize(_translator.Translate(stored), JsonSerializerOptions.Web))!;

        // Assert
        JsonNode.DeepEquals(json["activity"], JsonNode.Parse(ActivityDocumentSamples.Version1ActivityJson)).Should().BeTrue(
            $"AGENT FIX: every pre-migration field must keep its name and value (AC 15, AC 17). Got: {json["activity"]!.ToJsonString()}");
    }

    [Fact]
    public void Translate_ShouldKeepVersion1ZeroZoneBoundsAsZero_WhenSerialisedForHttp()
    {
        // Arrange
        var stored = ActivityDocumentSamples.DeserializeLikeCosmos<ActivityStoredDocument>(ActivityDocumentSamples.Version1Document);
        stored.Activity!.summary.heartRateZones[0].min = 0;
        stored.Activity.summary.heartRateZones[0].max = 0;

        // Act
        var json = JsonNode.Parse(JsonSerializer.Serialize(_translator.Translate(stored), JsonSerializerOptions.Web))!;

        // Assert
        var zone = json["activity"]!["summary"]!["heartRateZones"]![0]!;
        zone["min"]!.GetValue<int>().Should().Be(0, "AGENT FIX: nullable min/max must not change v1 output (AC 15, AC 17)");
        zone["max"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public void Translate_ShouldStampGoogleVersion2_WhenSchemaVersionIsTwo()
    {
        // Arrange
        var stored = Version2();

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            Id = "6a3d2f10-1c1e-4c0e-9d7a-6e2b8f1a0002",
            Date = ActivityDocumentSamples.Version2Date,
            DocumentType = "Activity",
            Provider = "Google",
            SchemaVersion = 2
        });
    }

    [Fact]
    public void Translate_ShouldSetGoalsToNull_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Activity.goals.Should().BeNull("AGENT FIX: C2 maps activity.goals to null for version 2");
    }

    [Fact]
    public void Translate_ShouldSetUnsupportedSummaryFieldsToNull_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var summary = _translator.Translate(stored).Activity.summary;

        // Assert
        summary.Should().BeEquivalentTo(new
        {
            activeScore = (int?)null,
            calorieEstimationMu = (int?)null,
            caloriesOutUnestimated = (int?)null,
            marginalCalories = (int?)null,
            useEstimation = (bool?)null
        }, "AGENT FIX: C2 maps these summary fields to null for version 2");
    }

    [Fact]
    public void Translate_ShouldMapDirectSummaryValues_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var summary = _translator.Translate(stored).Activity.summary;

        // Assert
        summary.Should().BeEquivalentTo(new
        {
            steps = 8432,
            caloriesOut = 2457,
            activityCalories = 812,
            floors = 12,
            restingHeartRate = 58,
            lightlyActiveMinutes = 184,
            fairlyActiveMinutes = 22,
            veryActiveMinutes = 17,
            sedentaryMinutes = 691
        });
    }

    [Fact]
    public void Translate_ShouldDeriveCaloriesBmrAsTotalMinusActive_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var summary = _translator.Translate(stored).Activity.summary;

        // Assert
        summary.caloriesBMR.Should().Be(1645, "AGENT FIX: caloriesBMR = round(2456.7) - round(812.4) (AC 22)");
        summary.caloriesBMR.Should().Be(summary.caloriesOut - summary.activityCalories);
    }

    [Fact]
    public void Translate_ShouldMapDistanceToOneTotalEntryInKilometres_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var distances = _translator.Translate(stored).Activity.summary.distances;

        // Assert
        distances.Should().ContainSingle();
        distances[0].activity.Should().Be("total");
        distances[0].distance.Should().BeApproximately(6.215, 1e-9, "AGENT FIX: 6215000 mm is 6.215 km");
    }

    [Fact]
    public void Translate_ShouldMapElevationToMetresFromAltitudeGain_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var elevation = _translator.Translate(stored).Activity.summary.elevation;

        // Assert
        elevation.Should().BeApproximately(36.576, 1e-9, "AGENT FIX: 36576 mm altitude gain is 36.576 m");
    }

    [Fact]
    public void Translate_ShouldNameGoogleHeartRateZonesWithoutOutOfRange_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var zones = _translator.Translate(stored).Activity.summary.heartRateZones;

        // Assert
        zones.Select(z => z.name).Should().Equal(["Light", "Moderate", "Vigorous", "Peak"],
            "AGENT FIX: C2 zones are Light, Moderate, Vigorous, Peak with no 'Out of Range' entry");
    }

    [Fact]
    public void Translate_ShouldMergeZoneBoundariesMinutesAndCalories_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var zones = _translator.Translate(stored).Activity.summary.heartRateZones;

        // Assert
        zones.Should().BeEquivalentTo(new[]
        {
            new { name = "Light", min = 96, max = 115, minutes = 90, caloriesOut = 410.5 },
            new { name = "Moderate", min = 116, max = 134, minutes = 22, caloriesOut = 150.25 },
            new { name = "Vigorous", min = 135, max = 159, minutes = 10, caloriesOut = 98.0 },
            new { name = "Peak", min = 160, max = 220, minutes = 0, caloriesOut = 0.0 }
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Translate_ShouldOnlyEmitZonesGoogleReturned_WhenBoundariesAreMissing()
    {
        // Arrange
        var stored = Version2With(google => google["dailyHeartRateZones"] = new JObject());

        // Act
        var zones = _translator.Translate(stored).Activity.summary.heartRateZones;

        // Assert
        zones.Should().BeEquivalentTo(new[]
        {
            new { name = "Light", min = (int?)null, max = (int?)null, minutes = 90 },
            new { name = "Moderate", min = (int?)null, max = (int?)null, minutes = 22 },
            new { name = "Vigorous", min = (int?)null, max = (int?)null, minutes = 10 }
        }, options => options.WithStrictOrdering(),
            "AGENT FIX: zone min/max are null, not 0, when Google returned no boundaries (C2)");
    }

    [Fact]
    public void Translate_ShouldKeepZoneWithZeroMinutesAndNullBounds_WhenOnlyCaloriesExist()
    {
        // Arrange
        var stored = Version2With(google =>
        {
            google["dailyHeartRateZones"] = new JObject();
            google["timeInHeartRateZone"] = new JObject();
        });

        // Act
        var light = _translator.Translate(stored).Activity.summary.heartRateZones.First();

        // Assert
        light.Should().BeEquivalentTo(new { name = "Light", min = (int?)null, max = (int?)null, minutes = 0, caloriesOut = 410.5 });
    }

    [Fact]
    public void Translate_ShouldSkipUnknownZoneTypes_WhenGoogleReturnsThem()
    {
        // Arrange
        var stored = Version2With(google =>
            ((JArray)google.SelectToken("dailyHeartRateZones.dataPoints[0].dailyHeartRateZones.heartRateZones")!)
                .Add(JObject.Parse("""{ "heartRateZoneType": "HEART_RATE_ZONE_TYPE_UNSPECIFIED", "minBeatsPerMinute": "1" }""")));

        // Act
        var zones = _translator.Translate(stored).Activity.summary.heartRateZones;

        // Assert
        zones.Should().HaveCount(4);
    }

    [Fact]
    public void Translate_ShouldMapActiveZoneMinutesWithTotal_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var azm = _translator.Translate(stored).ActivityEnrichment!.ActiveZoneMinutes;

        // Assert
        azm.Should().BeEquivalentTo(new ActiveZoneMinutes { FatBurn = 14, Cardio = 18, Peak = 4, Total = 36 });
    }

    [Fact]
    public void Translate_ShouldMapExerciseToWorkout_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var workouts = _translator.Translate(stored).ActivityEnrichment!.Workouts;

        // Assert
        workouts.Should().ContainSingle().Which.Should().BeEquivalentTo(new Workout
        {
            Name = "Morning Walk",
            ExerciseType = "WALKING",
            StartTime = "2026-10-20T08:30:00",
            EndTime = "2026-10-20T09:15:00",
            DurationMinutes = 43,
            Calories = 216,
            Steps = 5120,
            DistanceKm = 3.82,
            AverageHeartRate = 112,
            ActiveZoneMinutes = 9
        }, options => options.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 1e-9)).WhenTypeIs<double>(),
            "AGENT FIX: workout times are UTC + startUtcOffset (39600s) as local ISO-8601 without offset");
    }

    [Fact]
    public void Translate_ShouldMapExerciseToActivitiesWithNullIds_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var activities = _translator.Translate(stored).Activity.activities;

        // Assert
        activities.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            activityId = (int?)null,
            activityParentId = (int?)null,
            logId = (long?)null,
            activityParentName = "Morning Walk",
            name = "Morning Walk",
            calories = 216,
            duration = 2580000,
            hasActiveZoneMinutes = true,
            hasStartTime = true,
            isFavorite = false,
            lastModified = new DateTime(2026, 10, 19, 22, 20, 0, DateTimeKind.Utc),
            startDate = "2026-10-20",
            startTime = "08:30",
            steps = 5120,
            description = (string?)null
        }, "AGENT FIX: C2 maps activities[].logId, activityId, activityParentId to null");
    }

    [Fact]
    public void Translate_ShouldUseIntervalLength_WhenActiveDurationIsMissing()
    {
        // Arrange
        var stored = Version2With(google =>
            ((JObject)google.SelectToken("exercise.dataPoints[0].exercise")!).Remove("activeDuration"));

        // Act
        var workout = _translator.Translate(stored).ActivityEnrichment!.Workouts.Single();

        // Assert
        workout.DurationMinutes.Should().Be(45, "AGENT FIX: 21:30Z to 22:15Z is 45 minutes");
    }

    [Fact]
    public void Translate_ShouldParseTimestamps_WhenTheyAreStoredAsStrings()
    {
        // Arrange
        var stored = Version2With(google =>
        {
            var interval = (JObject)google.SelectToken("exercise.dataPoints[0].exercise.interval")!;
            interval["startTime"] = new JValue("2026-10-19T21:30:00Z");
            interval["endTime"] = new JValue("2026-10-19T22:15:00Z");
        });

        // Act
        var workout = _translator.Translate(stored).ActivityEnrichment!.Workouts.Single();

        // Assert
        workout.StartTime.Should().Be("2026-10-20T08:30:00");
        workout.EndTime.Should().Be("2026-10-20T09:15:00");
    }

    [Fact]
    public void Translate_ShouldParseTimestamps_WhenTheyAreStoredAsDateTimeOffsets()
    {
        // Arrange
        var stored = Version2With(google =>
            google.SelectToken("exercise.dataPoints[0].exercise.interval")!["startTime"] =
                new JValue(new DateTimeOffset(2026, 10, 19, 21, 30, 0, TimeSpan.Zero)));

        // Act
        var workout = _translator.Translate(stored).ActivityEnrichment!.Workouts.Single();

        // Assert
        workout.StartTime.Should().Be("2026-10-20T08:30:00");
    }

    [Fact]
    public void Translate_ShouldLeaveWorkoutTimesNull_WhenIntervalIsMissing()
    {
        // Arrange
        var stored = Version2With(google =>
        {
            var exercise = (JObject)google.SelectToken("exercise.dataPoints[0].exercise")!;
            exercise.Remove("interval");
            exercise.Remove("metricsSummary");
        });

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.ActivityEnrichment!.Workouts.Single().Should().BeEquivalentTo(new
        {
            StartTime = (string?)null,
            EndTime = (string?)null,
            Calories = (int?)null,
            Steps = (int?)null,
            DistanceKm = (double?)null,
            AverageHeartRate = (int?)null,
            ActiveZoneMinutes = (int?)null
        }, "AGENT FIX: workout metrics Google omits are null, not 0 (C2)");
        result.Activity.activities.Single().Should().BeEquivalentTo(new { calories = 0, steps = 0, distance = (double?)0d },
            "AGENT FIX: activities[] keeps 0 for omitted metrics; only workouts[] is nullable");
        result.Activity.activities.Single().hasStartTime.Should().BeFalse();
        result.Activity.activities.Single().hasActiveZoneMinutes.Should().BeFalse();
    }

    [Fact]
    public void Translate_ShouldNullOnlyTheOmittedWorkoutMetric_WhenHeartRateIsMissing()
    {
        // Arrange
        var stored = Version2With(google =>
            ((JObject)google.SelectToken("exercise.dataPoints[0].exercise.metricsSummary")!).Remove("averageHeartRateBeatsPerMinute"));

        // Act
        var workout = _translator.Translate(stored).ActivityEnrichment!.Workouts.Single();

        // Assert
        workout.Should().BeEquivalentTo(new { AverageHeartRate = (int?)null, Calories = (int?)216, Steps = (int?)5120, ActiveZoneMinutes = (int?)9 });
    }

    [Fact]
    public void Translate_ShouldSerialiseOmittedWorkoutMetricsAsJsonNull_WhenGoogleOmitsThem()
    {
        // Arrange
        var stored = Version2With(google =>
            ((JObject)google.SelectToken("exercise.dataPoints[0].exercise")!).Remove("metricsSummary"));

        // Act
        var json = JsonNode.Parse(JsonSerializer.Serialize(_translator.Translate(stored), JsonSerializerOptions.Web))!;

        // Assert
        var workout = json["activityEnrichment"]!["workouts"]![0]!.AsObject();
        workout.Where(p => p.Value is null).Select(p => p.Key).Should().Contain(
            ["calories", "steps", "distanceKm", "averageHeartRate", "activeZoneMinutes"],
            "AGENT FIX: omitted workout metrics are emitted as JSON null (C2)");
    }

    [Fact]
    public void Translate_ShouldSkipDataPoints_WhenTheyHaveNoExerciseObject()
    {
        // Arrange
        var stored = Version2With(google =>
            ((JArray)google.SelectToken("exercise.dataPoints")!).Add(JObject.Parse("""{ "name": "orphan" }""")));

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.ActivityEnrichment!.Workouts.Should().ContainSingle();
    }

    [Fact]
    public void Translate_ShouldAcceptDurationStrings_WhenActiveMinutesUseThem()
    {
        // Arrange
        var stored = Version2With(google =>
            google.SelectToken("activeMinutes.rollupDataPoints[0].activeMinutes.activeMinutesRollupByActivityLevel[0]")!
                ["activeMinutesSum"] = "1830s");

        // Act
        var summary = _translator.Translate(stored).Activity.summary;

        // Assert
        summary.lightlyActiveMinutes.Should().Be(30, "AGENT FIX: 1830s is 30 whole minutes");
    }

    [Fact]
    public void Translate_ShouldReturnEmptyValues_WhenGoogleReturnedNoDataPoints()
    {
        // Arrange
        var stored = ActivityDocumentSamples.DeserializeLikeCosmos<ActivityStoredDocument>(ActivityDocumentSamples.Version2EmptyDocument);

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Activity.summary.Should().BeEquivalentTo(new
        {
            steps = 0,
            caloriesOut = 0,
            activityCalories = 0,
            caloriesBMR = 0,
            floors = 0,
            elevation = 0.0,
            restingHeartRate = 0,
            sedentaryMinutes = 0,
            lightlyActiveMinutes = 0,
            fairlyActiveMinutes = 0,
            veryActiveMinutes = 0
        });
        result.Activity.summary.heartRateZones.Should().BeEmpty();
        result.Activity.summary.distances.Should().ContainSingle().Which.distance.Should().Be(0);
        result.Activity.activities.Should().BeEmpty();
        result.ActivityEnrichment!.ActiveZoneMinutes.Should().BeNull("AGENT FIX: no AZM rollup means null, not zeros");
        result.ActivityEnrichment.Workouts.Should().BeEmpty();
    }

    [Fact]
    public void Translate_ShouldReturnEmptyValues_WhenGooglePayloadIsMissing()
    {
        // Arrange
        var stored = new ActivityStoredDocument { Id = "id-2", SchemaVersion = 2, Provider = "Google", Google = null };

        // Act
        var result = _translator.Translate(stored);

        // Assert
        result.Activity.summary.steps.Should().Be(0);
        result.ActivityEnrichment!.Workouts.Should().BeEmpty();
    }

    [Fact]
    public void Translate_ShouldTolerateNullSourceValues_WhenGoogleStoredJsonNull()
    {
        // Arrange
        var stored = Version2With(google =>
        {
            google["steps"] = JValue.CreateNull();
            google.SelectToken("floors.rollupDataPoints[0].floors")!["countSum"] = JValue.CreateNull();
        });

        // Act
        var summary = _translator.Translate(stored).Activity.summary;

        // Assert
        summary.steps.Should().Be(0);
        summary.floors.Should().Be(0);
    }

    [Fact]
    public void Translate_ShouldProduceHttpSerialisableJsonWithoutNewtonsoftTypes_WhenDocumentIsVersion2()
    {
        // Arrange
        var stored = Version2();

        // Act
        var json = JsonNode.Parse(JsonSerializer.Serialize(_translator.Translate(stored), JsonSerializerOptions.Web))!;

        // Assert
        json.AsObject().Select(p => p.Key).Should().BeEquivalentTo(
            ["id", "activity", "date", "documentType", "provider", "schemaVersion", "activityEnrichment"],
            "AGENT FIX: never return the stored document or its 'google' JObject over HTTP (D-03)");
        json["activity"]!["goals"].Should().BeNull();
        json["activity"]!["summary"]!["activeScore"].Should().BeNull();
    }
}
