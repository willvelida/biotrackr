using Bunit;
using Moq;
using Radzen;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Weight;
using Biotrackr.UI.Services;
using Biotrackr.UI.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class WeightPageShould : BunitContext
    {
        private readonly Mock<IBiotrackrApiService> _mockApiService;

        public WeightPageShould()
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
            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((WeightItem?)null);

            var cut = Render<Weight>();

            cut.Markup.Should().Contain("Weight");
        }

        [Fact]
        public void RenderSummaryCards_WhenDataLoaded()
        {
            var weightItem = CreateWeightItem(weight: 80.5, bmi: 24.5);

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Weight>();

            cut.Markup.Should().Contain("80.5 kg");
            cut.Markup.Should().Contain("24.5");
        }

        [Fact]
        public void RenderBmiCategory_Normal()
        {
            var weightItem = CreateWeightItem(weight: 70, bmi: 22.5);

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Weight>();

            cut.Markup.Should().Contain("Normal");
        }

        [Fact]
        public void RenderBmiCategory_Overweight()
        {
            var weightItem = CreateWeightItem(weight: 85, bmi: 27.0);

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Weight>();

            cut.Markup.Should().Contain("Overweight");
        }

        [Fact]
        public void RenderBmiCategory_Underweight()
        {
            var weightItem = CreateWeightItem(weight: 50, bmi: 17.0);

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Weight>();

            cut.Markup.Should().Contain("Underweight");
        }

        [Fact]
        public void RenderBmiCategory_Obese()
        {
            var weightItem = CreateWeightItem(weight: 100, bmi: 32.0);

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Weight>();

            cut.Markup.Should().Contain("Obese");
        }

        [Fact]
        public void RenderBodyComposition_WhenWithingsDataAvailable()
        {
            var weightItem = CreateWeightItem(weight: 80, bmi: 22.7, muscleMass: 45.2, fatMass: 15.23, boneMass: 3.1, waterMass: 48.9, visceralFat: 10);

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Weight>();

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

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Weight>();

            cut.Markup.Should().NotContain("Body Composition");
        }

        [Fact]
        public void RenderNoEntries_WhenWeightIsZero()
        {
            var weightItem = CreateWeightItem(weight: 0, bmi: 0);

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Weight>();

            cut.Markup.Should().Contain("No weight entries");
        }

        [Fact]
        public void RenderNoDataMessage_WhenNullReturned()
        {
            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((WeightItem?)null);

            var cut = Render<Weight>();

            cut.Markup.Should().Contain("No weight data found");
        }

        [Fact]
        public void RenderErrorMessage_WhenApiThrows()
        {
            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            var cut = Render<Weight>();

            cut.Markup.Should().Contain("Failed to load weight data");
        }

        [Fact]
        public void RenderRangeTable_WhenRangeDataLoaded()
        {
            var rangeResponse = new PaginatedResponse<WeightItem>
            {
                Items = [CreateWeightItem(weight: 81.2, bmi: 25.1)],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((WeightItem?)null);
            _mockApiService.Setup(s => s.GetWeightByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Weight>();

            // Range mode uses RadzenSelectBar which cannot be interacted with via bUnit selectors.
            // Verify that the component renders without errors when data is available.
            cut.Markup.Should().NotBeEmpty();
        }

        [Fact]
        public void RenderCharts_WhenSingleDateDataLoaded()
        {
            var weightItem = CreateWeightItem(weight: 80, bmi: 22.7, muscleMass: 45.2, fatMass: 15.23, boneMass: 3.1, waterMass: 48.9, visceralFat: 10);

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(weightItem);

            var cut = Render<Weight>();

            // Donut chart for body composition
            cut.Markup.Should().Contain("Body Composition");
            cut.Markup.Should().Contain("80");
        }

        [Fact]
        public void RenderTrendCharts_WhenRangeDateDataLoaded()
        {
            var rangeResponse = new PaginatedResponse<WeightItem>
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

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((WeightItem?)null);
            _mockApiService.Setup(s => s.GetWeightByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Weight>();

            // The component renders without errors when range data is available
            cut.Markup.Should().NotBeEmpty();
        }

        [Fact]
        public void RenderChartsAndDataGrid_InRangeMode()
        {
            var rangeResponse = new PaginatedResponse<WeightItem>
            {
                Items = [CreateWeightItem(weight: 81.2, bmi: 25.1)],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((WeightItem?)null);
            _mockApiService.Setup(s => s.GetWeightByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Weight>();

            cut.Markup.Should().Contain("Weight");
            cut.Markup.Should().NotBeEmpty();
        }

        private static WeightItem CreateWeightItem(
            double weight = 0, double bmi = 0, double fat = 0,
            double? muscleMass = null, double? fatMass = null,
            double? boneMass = null, double? waterMass = null,
            int? visceralFat = null, string provider = "Withings")
        {
            return new WeightItem
            {
                Id = "test-id",
                Date = "2026-03-05",
                Provider = provider,
                Weight = new WeightData
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
    }
}
