using Biotrackr.Vitals.Svc.Adapters;
using Biotrackr.Vitals.Svc.Models.WithingsEntities;
using FluentAssertions;

namespace Biotrackr.Vitals.Svc.UnitTests.AdapterTests
{
    public class WithingsWeightAdapterShould
    {
        private const double UserHeight = 1.88;

        [Fact]
        public void DecodeWeightCorrectly()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 80250, Type = 1, Unit = -3 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.WeightKg.Should().BeApproximately(80.25, 0.001);
        }

        [Fact]
        public void DecodeFatPercentCorrectly()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 2050, Type = 6, Unit = -2 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.Fat.Should().BeApproximately(20.50, 0.01);
        }

        [Fact]
        public void DecodeFatMassCorrectly()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 15230, Type = 8, Unit = -3 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.FatMassKg.Should().BeApproximately(15.23, 0.001);
        }

        [Fact]
        public void DecodeFatFreeMassCorrectly()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 65020, Type = 5, Unit = -3 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.FatFreeMassKg.Should().BeApproximately(65.02, 0.001);
        }

        [Fact]
        public void DecodeMuscleMassCorrectly()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 45200, Type = 76, Unit = -3 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.MuscleMassKg.Should().BeApproximately(45.2, 0.001);
        }

        [Fact]
        public void DecodeBoneMassCorrectly()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 3100, Type = 88, Unit = -3 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.BoneMassKg.Should().BeApproximately(3.1, 0.001);
        }

        [Fact]
        public void DecodeWaterMassCorrectly()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 48900, Type = 77, Unit = -3 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.WaterMassKg.Should().BeApproximately(48.9, 0.001);
        }

        [Fact]
        public void DecodeVisceralFatCorrectly()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 10, Type = 170, Unit = 0 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.VisceralFatIndex.Should().Be(10);
        }

        [Fact]
        public void ConvertUnixTimestampToDate()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 80250, Type = 1, Unit = -3 });
            grp.Date = new DateTimeOffset(2024, 4, 1, 7, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds();

            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.Date.Should().Be("2024-04-01");
        }

        [Fact]
        public void ConvertUnixTimestampToTime()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 80250, Type = 1, Unit = -3 });
            grp.Date = new DateTimeOffset(2024, 4, 1, 7, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds();

            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.Time.Should().Be("07:30:00");
        }

        [Fact]
        public void SetSourceToWithings()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 80250, Type = 1, Unit = -3 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.Source.Should().Be("Withings");
        }

        [Fact]
        public void MapGrpIdToLogId()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 80250, Type = 1, Unit = -3 });
            grp.GrpId = 123456789;

            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.LogId.Should().Be(123456789L);
        }

        [Fact]
        public void HandleMissingOptionalMeasures()
        {
            // Only weight (type=1) present — body comp fields should be null
            var grp = CreateMeasureGroup(new Measure { Value = 80250, Type = 1, Unit = -3 });

            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.WeightKg.Should().BeApproximately(80.25, 0.001);
            result.FatMassKg.Should().BeNull();
            result.FatFreeMassKg.Should().BeNull();
            result.MuscleMassKg.Should().BeNull();
            result.BoneMassKg.Should().BeNull();
            result.WaterMassKg.Should().BeNull();
            result.VisceralFatIndex.Should().BeNull();
        }

        [Fact]
        public void HandleZeroExponent()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 10, Type = 170, Unit = 0 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.VisceralFatIndex.Should().Be(10);
        }

        [Fact]
        public void HandlePositiveExponent()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 8, Type = 1, Unit = 1 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.WeightKg.Should().BeApproximately(80.0, 0.001);
        }

        [Fact]
        public void HandleLargeNegativeExponent()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 802500, Type = 1, Unit = -4 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.WeightKg.Should().BeApproximately(80.25, 0.001);
        }

        [Fact]
        public void HandleEmptyMeasuresList()
        {
            var grp = new MeasureGroup
            {
                GrpId = 1,
                Date = new DateTimeOffset(2024, 4, 1, 7, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                Measures = []
            };

            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);
            result.WeightKg.Should().Be(0);
            result.Fat.Should().Be(0);
            result.FatMassKg.Should().BeNull();
        }

        [Fact]
        public void CalculateBmiFromHeightAndWeight()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 80250, Type = 1, Unit = -3 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, UserHeight);

            // BMI = 80.25 / (1.88 * 1.88) = 80.25 / 3.5344 = 22.7
            result.Bmi.Should().BeApproximately(22.7, 0.1);
        }

        [Fact]
        public void SetBmiToZeroWhenHeightIsZero()
        {
            var grp = CreateMeasureGroup(new Measure { Value = 80250, Type = 1, Unit = -3 });
            var result = WithingsWeightAdapter.FromMeasureGroup(grp, 0);
            result.Bmi.Should().Be(0);
        }

        private static MeasureGroup CreateMeasureGroup(params Measure[] measures)
        {
            return new MeasureGroup
            {
                GrpId = 1,
                Attrib = 0,
                Date = new DateTimeOffset(2024, 4, 1, 7, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                Created = new DateTimeOffset(2024, 4, 1, 7, 30, 50, TimeSpan.Zero).ToUnixTimeSeconds(),
                Category = 1,
                DeviceId = "test-device",
                Measures = measures.ToList()
            };
        }
    }
}
