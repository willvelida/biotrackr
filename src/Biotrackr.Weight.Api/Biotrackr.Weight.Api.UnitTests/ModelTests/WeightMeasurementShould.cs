using Biotrackr.Weight.Api.Models;
using FluentAssertions;
using System.Text.Json;

namespace Biotrackr.Weight.Api.UnitTests.ModelTests
{
    public class WeightMeasurementShould
    {
        [Fact]
        public void DeserializeWithBodyCompFields()
        {
            var json = """
            {
                "bmi": 22.7,
                "date": "2026-04-01",
                "fat": 20.5,
                "logId": 123456789,
                "source": "Withings",
                "time": "07:30:00",
                "weight": 80.25,
                "fatMassKg": 15.23,
                "fatFreeMassKg": 65.02,
                "muscleMassKg": 45.2,
                "boneMassKg": 3.1,
                "waterMassKg": 48.9,
                "visceralFatIndex": 10
            }
            """;

            var result = JsonSerializer.Deserialize<WeightMeasurement>(json);

            result.Should().NotBeNull();
            result!.Bmi.Should().Be(22.7);
            result.WeightKg.Should().Be(80.25);
            result.Fat.Should().Be(20.5);
            result.FatMassKg.Should().Be(15.23);
            result.FatFreeMassKg.Should().Be(65.02);
            result.MuscleMassKg.Should().Be(45.2);
            result.BoneMassKg.Should().Be(3.1);
            result.WaterMassKg.Should().Be(48.9);
            result.VisceralFatIndex.Should().Be(10);
            result.Source.Should().Be("Withings");
        }

        [Fact]
        public void DeserializeLegacyFitbitDocumentBackwardCompatible()
        {
            // Existing Fitbit Cosmos documents have no body comp fields
            var json = """
            {
                "bmi": 24.1,
                "date": "2026-03-25",
                "fat": 18.5,
                "logId": 1234567890,
                "source": "Aria",
                "time": "07:30:00",
                "weight": 82.3
            }
            """;

            var result = JsonSerializer.Deserialize<WeightMeasurement>(json);

            result.Should().NotBeNull();
            result!.Bmi.Should().Be(24.1);
            result.WeightKg.Should().Be(82.3);
            result.Fat.Should().Be(18.5);
            result.Source.Should().Be("Aria");
            result.FatMassKg.Should().BeNull();
            result.FatFreeMassKg.Should().BeNull();
            result.MuscleMassKg.Should().BeNull();
            result.BoneMassKg.Should().BeNull();
            result.WaterMassKg.Should().BeNull();
            result.VisceralFatIndex.Should().BeNull();
        }

        [Fact]
        public void DeserializeWeightDocumentWithProvider()
        {
            var json = """
            {
                "id": "123456789",
                "weight": {
                    "bmi": 22.7,
                    "date": "2026-04-01",
                    "fat": 20.5,
                    "weight": 80.25,
                    "source": "Withings",
                    "time": "07:30:00",
                    "muscleMassKg": 45.2
                },
                "date": "2026-04-01",
                "documentType": "Weight",
                "provider": "Withings"
            }
            """;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<WeightDocument>(json, options);

            result.Should().NotBeNull();
            result!.Provider.Should().Be("Withings");
            result.Weight.Should().NotBeNull();
            result.Weight.MuscleMassKg.Should().Be(45.2);
        }

        [Fact]
        public void DeserializeLegacyWeightDocumentWithoutProvider()
        {
            var json = """
            {
                "id": "some-guid",
                "weight": {
                    "bmi": 24.1,
                    "date": "2026-03-25",
                    "fat": 18.5,
                    "weight": 82.3,
                    "source": "Aria",
                    "time": "07:30:00"
                },
                "date": "2026-03-25",
                "documentType": "Weight"
            }
            """;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<WeightDocument>(json, options);

            result.Should().NotBeNull();
            result!.Provider.Should().BeNull();
            result.Weight.Should().NotBeNull();
            result.Weight.WeightKg.Should().Be(82.3);
            result.Weight.FatMassKg.Should().BeNull();
        }

        [Fact]
        public void SerializeWithNullBodyCompFieldsOmitted()
        {
            var measurement = new WeightMeasurement
            {
                Bmi = 24.1,
                Date = "2026-03-25",
                Fat = 18.5,
                WeightKg = 82.3,
                Source = "Fitbit",
                Time = "07:30:00"
            };

            var json = JsonSerializer.Serialize(measurement);

            json.Should().Contain("\"weight\":82.3");
            json.Should().Contain("\"bmi\":24.1");
            // Nullable fields serialize as null when not set
            json.Should().Contain("\"fatMassKg\":null");
        }
    }
}
