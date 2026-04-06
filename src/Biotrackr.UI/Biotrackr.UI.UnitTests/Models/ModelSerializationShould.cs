using System.Text.Json;
using Biotrackr.UI.Models.Activity;
using Biotrackr.UI.Models.Food;
using Biotrackr.UI.Models.Sleep;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Models
{
    public class ModelSerializationShould
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [Fact]
        public void DeserializeActivityLog()
        {
            var json = """
                {
                    "activityId": 1,
                    "activityParentId": 2,
                    "activityParentName": "Walking",
                    "calories": 250,
                    "description": "Morning walk",
                    "duration": 1800000,
                    "hasActiveZoneMinutes": true,
                    "hasStartTime": true,
                    "isFavorite": false,
                    "lastModified": "2026-03-15T10:00:00",
                    "logId": 12345,
                    "name": "Walk",
                    "startDate": "2026-03-15",
                    "startTime": "08:00",
                    "steps": 3000
                }
                """;

            var result = JsonSerializer.Deserialize<ActivityLog>(json, Options);

            result.Should().NotBeNull();
            result!.ActivityId.Should().Be(1);
            result.ActivityParentName.Should().Be("Walking");
            result.Calories.Should().Be(250);
            result.Duration.Should().Be(1800000);
            result.HasActiveZoneMinutes.Should().BeTrue();
            result.IsFavorite.Should().BeFalse();
            result.LogId.Should().Be(12345);
            result.Steps.Should().Be(3000);
        }

        [Fact]
        public void DeserializeSleepRecord()
        {
            var json = """
                {
                    "dateOfSleep": "2026-03-15",
                    "duration": 28800000,
                    "efficiency": 92,
                    "endTime": "2026-03-15T07:00:00",
                    "infoCode": 0,
                    "isMainSleep": true,
                    "levels": { "summary": {}, "data": [] },
                    "logId": 67890,
                    "minutesAfterWakeup": 5,
                    "minutesAsleep": 420,
                    "minutesAwake": 30,
                    "minutesToFallAsleep": 10,
                    "logType": "classic",
                    "startTime": "2026-03-14T23:00:00",
                    "timeInBed": 480
                }
                """;

            var result = JsonSerializer.Deserialize<SleepRecord>(json, Options);

            result.Should().NotBeNull();
            result!.DateOfSleep.Should().Be("2026-03-15");
            result.Efficiency.Should().Be(92);
            result.IsMainSleep.Should().BeTrue();
            result.MinutesAsleep.Should().Be(420);
            result.MinutesAwake.Should().Be(30);
            result.LogId.Should().Be(67890);
            result.TimeInBed.Should().Be(480);
        }

        [Fact]
        public void DeserializeSleepLevelData()
        {
            var json = """
                {
                    "dateTime": "2026-03-14T23:00:00",
                    "level": "deep",
                    "seconds": 3600
                }
                """;

            var result = JsonSerializer.Deserialize<SleepLevelData>(json, Options);

            result.Should().NotBeNull();
            result!.Level.Should().Be("deep");
            result.Seconds.Should().Be(3600);
        }

        [Fact]
        public void DeserializeSleepDetails()
        {
            var json = """
                {
                    "count": 5,
                    "minutes": 120,
                    "thirtyDayAvgMinutes": 110
                }
                """;

            var result = JsonSerializer.Deserialize<SleepDetails>(json, Options);

            result.Should().NotBeNull();
            result!.Count.Should().Be(5);
            result.Minutes.Should().Be(120);
            result.ThirtyDayAvgMinutes.Should().Be(110);
        }

        [Fact]
        public void DeserializeFoodEntryWithNestedFood()
        {
            var json = """
                {
                    "isFavorite": true,
                    "logDate": "2026-03-15",
                    "logId": 11111,
                    "loggedFood": { "name": "Apple", "calories": 95, "brand": "Fresh" },
                    "nutritionalValues": { "calories": 95, "carbs": 25, "fat": 0.3, "fiber": 4.4, "protein": 0.5, "sodium": 1.8 }
                }
                """;

            var result = JsonSerializer.Deserialize<FoodEntry>(json, Options);

            result.Should().NotBeNull();
            result!.IsFavorite.Should().BeTrue();
            result.LogDate.Should().Be("2026-03-15");
            result.LogId.Should().Be(11111);
            result.LoggedFood.Should().NotBeNull();
            result.LoggedFood.Name.Should().Be("Apple");
        }

        [Fact]
        public void DeserializeLoggedFood()
        {
            var json = """
                {
                    "name": "Banana",
                    "calories": 105,
                    "brand": "Dole",
                    "amount": 1,
                    "foodId": 42
                }
                """;

            var result = JsonSerializer.Deserialize<LoggedFood>(json, Options);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Banana");
            result.Calories.Should().Be(105);
            result.Brand.Should().Be("Dole");
            result.Amount.Should().Be(1);
        }

        [Fact]
        public void DeserializeNutritionalValues()
        {
            var json = """
                {
                    "calories": 250,
                    "carbs": 30.5,
                    "fat": 12.0,
                    "fiber": 5.0,
                    "protein": 20.0,
                    "sodium": 500.0
                }
                """;

            var result = JsonSerializer.Deserialize<NutritionalValues>(json, Options);

            result.Should().NotBeNull();
            result!.Calories.Should().Be(250);
            result.Carbs.Should().Be(30.5);
            result.Fat.Should().Be(12.0);
            result.Protein.Should().Be(20.0);
        }

        [Fact]
        public void DeserializeFoodEntry()
        {
            var json = """
                {
                    "name": "Banana",
                    "calories": 105,
                    "brand": "Dole"
                }
                """;

            var result = JsonSerializer.Deserialize<LoggedFood>(json, Options);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Banana");
            result.Calories.Should().Be(105);
            result.Brand.Should().Be("Dole");
        }

        [Fact]
        public void DeserializeFoodUnit()
        {
            var json = """
                {
                    "id": 1,
                    "name": "cup",
                    "plural": "cups"
                }
                """;

            var result = JsonSerializer.Deserialize<FoodUnit>(json, Options);

            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("cup");
            result.Plural.Should().Be("cups");
        }
    }
}
