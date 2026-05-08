using FluentAssertions;
using System.Text.Json;
using Biotrackr.Mcp.Server.Models.Vitals;

namespace Biotrackr.Mcp.Server.UnitTests.Models;

public class VitalsModelsShould
{
    [Fact]
    public void VitalsData_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var data = new VitalsData
        {
            Bmi = 24.5,
            Date = "2024-01-15",
            Fat = 18.2,
            LogId = "log123",
            Source = "Withings",
            Time = "08:30:00",
            Weight = 75.5,
            FatMassKg = 13.7,
            FatFreeMassKg = 61.8,
            MuscleMassKg = 55.2,
            BoneMassKg = 3.1,
            WaterMassKg = 42.0,
            VisceralFatIndex = 5
        };

        // Act
        var json = JsonSerializer.Serialize(data);
        var deserialized = JsonSerializer.Deserialize<VitalsData>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Bmi.Should().Be(24.5);
        deserialized.Date.Should().Be("2024-01-15");
        deserialized.Fat.Should().Be(18.2);
        deserialized.Time.Should().Be("08:30:00");
        deserialized.Weight.Should().Be(75.5);
        deserialized.FatMassKg.Should().Be(13.7);
        deserialized.FatFreeMassKg.Should().Be(61.8);
        deserialized.MuscleMassKg.Should().Be(55.2);
        deserialized.BoneMassKg.Should().Be(3.1);
        deserialized.WaterMassKg.Should().Be(42.0);
        deserialized.VisceralFatIndex.Should().Be(5);
    }

    [Fact]
    public void BloodPressureReadingData_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var reading = new BloodPressureReadingData
        {
            Systolic = 120,
            Diastolic = 80,
            HeartRate = 72,
            Timestamp = "2024-01-15T08:30:00Z",
            Time = "08:30:00",
            Source = "Withings",
            LogId = 98765,
            DeviceId = "device-123"
        };

        // Act
        var json = JsonSerializer.Serialize(reading);
        var deserialized = JsonSerializer.Deserialize<BloodPressureReadingData>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Systolic.Should().Be(120);
        deserialized.Diastolic.Should().Be(80);
        deserialized.HeartRate.Should().Be(72);
        deserialized.Timestamp.Should().Be("2024-01-15T08:30:00Z");
        deserialized.Time.Should().Be("08:30:00");
        deserialized.Source.Should().Be("Withings");
        deserialized.LogId.Should().Be(98765);
        deserialized.DeviceId.Should().Be("device-123");
    }
}
