using Biotrackr.Vitals.Svc.Adapters;
using Biotrackr.Vitals.Svc.Models.WithingsEntities;
using FluentAssertions;

namespace Biotrackr.Vitals.Svc.UnitTests.AdapterTests
{
    public class WithingsBloodPressureAdapterShould
    {
        [Fact]
        public void ConvertMeasureGroupWithTypes9_10_11Correctly()
        {
            var grp = CreateBpMeasureGroup(
                systolicValue: 120, systolicUnit: 0,
                diastolicValue: 80, diastolicUnit: 0,
                heartRateValue: 72, heartRateUnit: 0);

            var result = WithingsBloodPressureAdapter.FromMeasureGroup(grp);

            result.Systolic.Should().Be(120);
            result.Diastolic.Should().Be(80);
            result.HeartRate.Should().Be(72);
        }

        [Fact]
        public void HandleMissingMeasureTypes_ReturnZeroForMissing()
        {
            // Only systolic present
            var grp = new MeasureGroup
            {
                GrpId = 1,
                Date = new DateTimeOffset(2024, 4, 1, 8, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                DeviceId = "bp-device",
                Measures = [new Measure { Value = 120, Type = 10, Unit = 0 }]
            };

            var result = WithingsBloodPressureAdapter.FromMeasureGroup(grp);

            result.Systolic.Should().Be(120);
            result.Diastolic.Should().Be(0);
            result.HeartRate.Should().Be(0);
        }

        [Fact]
        public void ApplyValueTimesPow10UnitConversion()
        {
            // Systolic = 12000 * 10^-2 = 120
            var grp = CreateBpMeasureGroup(
                systolicValue: 12000, systolicUnit: -2,
                diastolicValue: 8000, diastolicUnit: -2,
                heartRateValue: 7200, heartRateUnit: -2);

            var result = WithingsBloodPressureAdapter.FromMeasureGroup(grp);

            result.Systolic.Should().Be(120);
            result.Diastolic.Should().Be(80);
            result.HeartRate.Should().Be(72);
        }

        [Fact]
        public void SetCorrectTimestamp()
        {
            var grp = CreateBpMeasureGroup(
                systolicValue: 120, systolicUnit: 0,
                diastolicValue: 80, diastolicUnit: 0,
                heartRateValue: 72, heartRateUnit: 0);
            grp.Date = new DateTimeOffset(2024, 4, 1, 8, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds();

            var result = WithingsBloodPressureAdapter.FromMeasureGroup(grp);

            result.Timestamp.Should().Contain("2024-04-01");
            result.Time.Should().Be("08:30:00");
        }

        [Fact]
        public void SetSourceToWithings()
        {
            var grp = CreateBpMeasureGroup(
                systolicValue: 120, systolicUnit: 0,
                diastolicValue: 80, diastolicUnit: 0,
                heartRateValue: 72, heartRateUnit: 0);

            var result = WithingsBloodPressureAdapter.FromMeasureGroup(grp);

            result.Source.Should().Be("Withings");
        }

        [Fact]
        public void MapGrpIdToLogId()
        {
            var grp = CreateBpMeasureGroup(
                systolicValue: 120, systolicUnit: 0,
                diastolicValue: 80, diastolicUnit: 0,
                heartRateValue: 72, heartRateUnit: 0);
            grp.GrpId = 987654321;

            var result = WithingsBloodPressureAdapter.FromMeasureGroup(grp);

            result.LogId.Should().Be(987654321L);
        }

        [Fact]
        public void MapDeviceId()
        {
            var grp = CreateBpMeasureGroup(
                systolicValue: 120, systolicUnit: 0,
                diastolicValue: 80, diastolicUnit: 0,
                heartRateValue: 72, heartRateUnit: 0);
            grp.DeviceId = "bpm-connect-123";

            var result = WithingsBloodPressureAdapter.FromMeasureGroup(grp);

            result.DeviceId.Should().Be("bpm-connect-123");
        }

        [Fact]
        public void SetDeviceIdToNull_WhenEmpty()
        {
            var grp = CreateBpMeasureGroup(
                systolicValue: 120, systolicUnit: 0,
                diastolicValue: 80, diastolicUnit: 0,
                heartRateValue: 72, heartRateUnit: 0);
            grp.DeviceId = "";

            var result = WithingsBloodPressureAdapter.FromMeasureGroup(grp);

            result.DeviceId.Should().BeNull();
        }

        private static MeasureGroup CreateBpMeasureGroup(
            int systolicValue, int systolicUnit,
            int diastolicValue, int diastolicUnit,
            int heartRateValue, int heartRateUnit)
        {
            return new MeasureGroup
            {
                GrpId = 1,
                Attrib = 0,
                Date = new DateTimeOffset(2024, 4, 1, 8, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                Created = new DateTimeOffset(2024, 4, 1, 8, 0, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                Category = 1,
                DeviceId = "bp-device",
                Measures =
                [
                    new Measure { Value = systolicValue, Type = 10, Unit = systolicUnit },
                    new Measure { Value = diastolicValue, Type = 9, Unit = diastolicUnit },
                    new Measure { Value = heartRateValue, Type = 11, Unit = heartRateUnit }
                ]
            };
        }
    }
}
