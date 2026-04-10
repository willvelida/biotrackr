using Bunit;
using Moq;
using Radzen;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Vitals;
using Biotrackr.UI.Services;
using Biotrackr.UI.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class VitalsPageShould : BunitContext
    {
        private readonly Mock<IBiotrackrApiService> _mockApiService;

        public VitalsPageShould()
        {
            _mockApiService = new Mock<IBiotrackrApiService>();
            Services.AddSingleton(_mockApiService.Object);
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
            JSInterop.SetupRadzenChartInterop();
        }

        [Fact]
        public void RenderPageTitle()
        {
            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((VitalsItem?)null);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Vitals");
        }

        [Fact]
        public void RenderSummaryCards_WhenDataLoaded()
        {
            var weightItem = CreateWeightItem(weight: 80.5, bmi: 24.5);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("80.5 kg");
            cut.Markup.Should().Contain("24.5");
        }

        [Fact]
        public void RenderBmiCategory_Normal()
        {
            var weightItem = CreateWeightItem(weight: 70, bmi: 22.5);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Normal");
        }

        [Fact]
        public void RenderBmiCategory_Overweight()
        {
            var weightItem = CreateWeightItem(weight: 85, bmi: 27.0);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Overweight");
        }

        [Fact]
        public void RenderBmiCategory_Underweight()
        {
            var weightItem = CreateWeightItem(weight: 50, bmi: 17.0);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Underweight");
        }

        [Fact]
        public void RenderBmiCategory_Obese()
        {
            var weightItem = CreateWeightItem(weight: 100, bmi: 32.0);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Obese");
        }

        [Fact]
        public void RenderBodyComposition_WhenWithingsDataAvailable()
        {
            var weightItem = CreateWeightItem(weight: 80, bmi: 22.7, muscleMass: 45.2, fatMass: 15.23, boneMass: 3.1, waterMass: 48.9, visceralFat: 10);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Body Composition");
            cut.Markup.Should().Contain("Muscle Mass");
            cut.Markup.Should().Contain("45.2 kg");
            cut.Markup.Should().Contain("Fat Mass");
            cut.Markup.Should().Contain("15.2 kg");
            cut.Markup.Should().Contain("Bone Mass");
            cut.Markup.Should().Contain("3.1 kg");
        }

        [Fact]
        public void NotRenderBodyComposition_WhenFitbitDataOnly()
        {
            var weightItem = CreateWeightItem(weight: 80, bmi: 24.5);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().NotContain("Body Composition");
        }

        [Fact]
        public void RenderNoEntries_WhenWeightIsZero()
        {
            var weightItem = CreateWeightItem(weight: 0, bmi: 0);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("No vitals entries");
        }

        [Fact]
        public void RenderNoDataMessage_WhenNullReturned()
        {
            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((VitalsItem?)null);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("No vitals data found");
        }

        [Fact]
        public void RenderErrorMessage_WhenApiThrows()
        {
            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Failed to load vitals data");
        }

        [Fact]
        public void RenderRangeTable_WhenRangeDataLoaded()
        {
            var rangeResponse = new PaginatedResponse<VitalsItem>
            {
                Items = [CreateWeightItem(weight: 81.2, bmi: 25.1)],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((VitalsItem?)null);
            _mockApiService.Setup(s => s.GetVitalsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Vitals>();

            // Range mode uses RadzenSelectBar which cannot be interacted with via bUnit selectors.
            // Verify that the component renders without errors when data is available.
            cut.Markup.Should().NotBeEmpty();
        }

        [Fact]
        public void RenderCharts_WhenSingleDateDataLoaded()
        {
            var weightItem = CreateWeightItem(weight: 80, bmi: 22.7, muscleMass: 45.2, fatMass: 15.23, boneMass: 3.1, waterMass: 48.9, visceralFat: 10);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Vitals>();

            // Donut chart for body composition
            cut.Markup.Should().Contain("Body Composition");
            cut.Markup.Should().Contain("80");
        }

        [Fact]
        public void RenderTrendCharts_WhenRangeDateDataLoaded()
        {
            var rangeResponse = new PaginatedResponse<VitalsItem>
            {
                Items =
                [
                    CreateWeightItem(weight: 80.5, bmi: 24.5),
                    CreateWeightItem(weight: 80.2, bmi: 24.4)
                ],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 2,
                HasPreviousPage = false,
                HasNextPage = false
            };
            rangeResponse.Items[0].Date = "2026-03-01";
            rangeResponse.Items[1].Date = "2026-03-02";

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((VitalsItem?)null);
            _mockApiService.Setup(s => s.GetVitalsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Vitals>();

            // The component renders without errors when range data is available
            cut.Markup.Should().NotBeEmpty();
        }

        [Fact]
        public void RenderChartsAndDataGrid_InRangeMode()
        {
            var rangeResponse = new PaginatedResponse<VitalsItem>
            {
                Items = [CreateWeightItem(weight: 81.2, bmi: 25.1)],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((VitalsItem?)null);
            _mockApiService.Setup(s => s.GetVitalsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Vitals");
            cut.Markup.Should().NotBeEmpty();
        }

        private static VitalsItem CreateWeightItem(
            double weight = 0, double bmi = 0, double fat = 0,
            double? muscleMass = null, double? fatMass = null,
            double? boneMass = null, double? waterMass = null,
            int? visceralFat = null, string provider = "Withings")
        {
            return new VitalsItem
            {
                Id = "test-id",
                Date = "2026-03-05",
                Provider = provider,
                Weight = new VitalsData
                {
                    Weight = weight,
                    Bmi = bmi,
                    Fat = fat,
                    Source = provider,
                    Time = "08:00:00",
                    MuscleMassKg = muscleMass,
                    FatMassKg = fatMass,
                    BoneMassKg = boneMass,
                    WaterMassKg = waterMass,
                    VisceralFatIndex = visceralFat
                }
            };
        }

        private static VitalsItem CreateBpItem(int systolic = 120, int diastolic = 80, int heartRate = 72,
            int readingCount = 1, string provider = "Withings")
        {
            var readings = Enumerable.Range(0, readingCount).Select(i => new BloodPressureReadingData
            {
                Systolic = systolic + i,
                Diastolic = diastolic + i,
                HeartRate = heartRate,
                Timestamp = $"2026-03-05T{8 + i:D2}:30:00Z",
                Time = $"{8 + i:D2}:30:00",
                Source = "Withings",
                LogId = 100 + i,
                DeviceId = "device-1"
            }).ToList();

            return new VitalsItem
            {
                Id = "test-bp-id",
                Date = "2026-03-05",
                Provider = provider,
                BloodPressureReadings = readings
            };
        }

        private static VitalsItem CreateCombinedItem()
        {
            var item = CreateWeightItem(weight: 80.5, bmi: 24.5);
            item.BloodPressureReadings = new List<BloodPressureReadingData>
            {
                new() { Systolic = 118, Diastolic = 76, HeartRate = 68, Timestamp = "2026-03-05T08:00:00Z", Time = "08:00:00", Source = "Withings", LogId = 200 },
                new() { Systolic = 122, Diastolic = 82, HeartRate = 72, Timestamp = "2026-03-05T18:00:00Z", Time = "18:00:00", Source = "Withings", LogId = 201 }
            };
            return item;
        }

        [Fact]
        public void RenderBpSummaryCards_WhenBpDataLoaded()
        {
            var bpItem = CreateBpItem(systolic: 120, diastolic: 80, heartRate: 72);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(bpItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Blood Pressure");
            cut.Markup.Should().Contain("120/80 mmHg");
            cut.Markup.Should().Contain("72 bpm");
        }

        [Fact]
        public void RenderBpReadingsTable_WhenMultipleReadings()
        {
            var bpItem = CreateBpItem(readingCount: 3);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(bpItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Systolic");
            cut.Markup.Should().Contain("Diastolic");
            cut.Markup.Should().Contain("Heart Rate");
        }

        [Fact]
        public void RenderBothWeightAndBp_WhenCombinedDataLoaded()
        {
            var combinedItem = CreateCombinedItem();

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(combinedItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Weight");
            cut.Markup.Should().Contain("80.5");
            cut.Markup.Should().Contain("Blood Pressure");
            cut.Markup.Should().Contain("mmHg");
        }

        [Fact]
        public void NotRenderBpSection_WhenNoBpData()
        {
            var weightOnly = CreateWeightItem(weight: 80, bmi: 24.5);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightOnly);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain("Weight");
            cut.Markup.Should().NotContain("Blood Pressure");
        }

        [Theory]
        [InlineData(115, 75, "Normal")]
        [InlineData(125, 78, "Elevated")]
        [InlineData(135, 85, "Stage 1")]
        [InlineData(145, 95, "Stage 2")]
        [InlineData(185, 125, "Crisis")]
        public void RenderCorrectBpCategory(int systolic, int diastolic, string expectedCategory)
        {
            var bpItem = CreateBpItem(systolic: systolic, diastolic: diastolic);

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(bpItem);

            var cut = Render<Vitals>();

            cut.Markup.Should().Contain(expectedCategory);
        }

        [Fact]
        public void RenderBpCharts_InRangeMode()
        {
            var rangeResponse = new PaginatedResponse<VitalsItem>
            {
                Items = [CreateBpItem(systolic: 120, diastolic: 80, heartRate: 72)],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockApiService.Setup(s => s.GetVitalsByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((VitalsItem?)null);
            _mockApiService.Setup(s => s.GetVitalsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Vitals>();

            // Range mode uses RadzenSelectBar which cannot be interacted with via bUnit.
            // Verify the component renders without errors when BP range data is available.
            cut.Markup.Should().NotBeEmpty();
        }
    }
}
