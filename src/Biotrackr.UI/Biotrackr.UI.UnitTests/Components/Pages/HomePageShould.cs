using Bunit;
using Moq;
using Radzen;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Activity;
using Biotrackr.UI.Models.Food;
using Biotrackr.UI.Models.Sleep;
using Biotrackr.UI.Models.Weight;
using Biotrackr.UI.Services;
using Biotrackr.UI.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class HomePageShould : BunitContext
    {
        private readonly Mock<IBiotrackrApiService> _mockApiService;

        public HomePageShould()
        {
            _mockApiService = new Mock<IBiotrackrApiService>();
            Services.AddSingleton(_mockApiService.Object);
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
            JSInterop.SetupRadzenChartInterop();
        }

        [Fact]
        public void RenderDashboardTitle()
        {
            SetupEmptyApiResponses();

            var cut = Render<Home>();

            cut.Markup.Should().Contain("Dashboard");
        }

        [Fact]
        public void RenderSummaryCards_WhenAllDataLoaded()
        {
            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new ActivityItem
                {
                    Activity = new ActivityData
                    {
                        Summary = new ActivitySummary { Steps = 12000, CaloriesOut = 2800, Floors = 8, FairlyActiveMinutes = 20, VeryActiveMinutes = 35, RestingHeartRate = 62 },
                        Goals = new ActivityGoals { Steps = 10000, CaloriesOut = 2500, Floors = 10, ActiveMinutes = 30 }
                    }
                });
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new FoodItem { Food = new FoodData { Summary = new FoodSummary { Calories = 2100 }, Goals = new FoodGoals { Calories = 2500 } } });
            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new SleepItem { Sleep = new SleepData { Summary = new SleepSummary { TotalMinutesAsleep = 420 } } });
            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new WeightItem { Weight = new WeightData { Weight = 80.5, Bmi = 24.5 } });

            var cut = Render<Home>();

            cut.Markup.Should().Contain("12,000"); // steps
            cut.Markup.Should().Contain("2,800");  // calories burned
            cut.Markup.Should().Contain("2,100");  // calories consumed
            cut.Markup.Should().Contain("7h 0m");  // sleep
            cut.Markup.Should().Contain("80.5 kg"); // weight
        }

        [Fact]
        public void RenderDefaultValues_WhenDataIsNull()
        {
            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((ActivityItem?)null);
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((FoodItem?)null);
            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((SleepItem?)null);
            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((WeightItem?)null);

            var cut = Render<Home>();

            // Should show per-section empty state alerts
            cut.Markup.Should().Contain("No activity data available");
            cut.Markup.Should().Contain("No sleep data available");
            cut.Markup.Should().Contain("No weight data available");
            cut.Markup.Should().Contain("No food data available");
        }

        [Fact]
        public void RenderEmptyStates_WhenApiThrows()
        {
            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));
            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));
            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            var cut = Render<Home>();

            // Per-section loading handles errors — shows error alert messages
            cut.Markup.Should().Contain("Failed to load activity data. Please try again later.");
            cut.Markup.Should().Contain("Failed to load sleep data. Please try again later.");
            cut.Markup.Should().Contain("Failed to load weight data. Please try again later.");
            cut.Markup.Should().Contain("Failed to load food data. Please try again later.");
        }

        [Fact]
        public void RenderGoalText_WhenGoalsExist()
        {
            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new ActivityItem
                {
                    Activity = new ActivityData
                    {
                        Summary = new ActivitySummary { CaloriesOut = 2000 },
                        Goals = new ActivityGoals { CaloriesOut = 2500 }
                    }
                });
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new FoodItem { Food = new FoodData { Summary = new FoodSummary(), Goals = new FoodGoals() } });
            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new SleepItem());
            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new WeightItem());

            var cut = Render<Home>();

            cut.Markup.Should().Contain("Goal: 2,500");
        }

        [Fact]
        public void RenderBmiText_WhenWeightHasBmi()
        {
            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new ActivityItem());
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new FoodItem());
            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new SleepItem());
            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new WeightItem { Weight = new WeightData { Weight = 75, Bmi = 23.1 } });

            var cut = Render<Home>();

            cut.Markup.Should().Contain("BMI");
            cut.Markup.Should().Contain("23.1");
        }

        [Fact]
        public void RenderDomainSections_WhenAllDataLoaded()
        {
            SetupEmptyApiResponses();
            SetupEmptyRangeResponses();

            var cut = Render<Home>();

            cut.Markup.Should().Contain("Activity");
            cut.Markup.Should().Contain("Sleep");
            cut.Markup.Should().Contain("Weight");
            cut.Markup.Should().Contain("Food");
        }

        [Fact]
        public void RenderSummaryCards_WithSparklines_WhenRangeDataAvailable()
        {
            SetupEmptyApiResponses();
            _mockApiService.Setup(s => s.GetActivitiesByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<ActivityItem>
                {
                    Items =
                    [
                        new ActivityItem { Date = "2026-03-01", Activity = new ActivityData { Summary = new ActivitySummary { Steps = 8000, CaloriesOut = 2200, FairlyActiveMinutes = 10, VeryActiveMinutes = 20, Floors = 5, RestingHeartRate = 62 }, Goals = new ActivityGoals() } },
                        new ActivityItem { Date = "2026-03-02", Activity = new ActivityData { Summary = new ActivitySummary { Steps = 9500, CaloriesOut = 2400, FairlyActiveMinutes = 15, VeryActiveMinutes = 25, Floors = 7, RestingHeartRate = 60 }, Goals = new ActivityGoals() } }
                    ],
                    TotalCount = 2
                });
            SetupEmptyRangeResponses(skipActivity: true);

            var cut = Render<Home>();

            // SummaryCard renders sparklines via RadzenSparkline (which extends RadzenChart)
            cut.Markup.Should().Contain("rz-chart");
        }

        [Fact]
        public void RenderDefaultValues_WhenDomainApiFailsSilently()
        {
            // Single-day data loads normally
            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new ActivityItem
                {
                    Activity = new ActivityData
                    {
                        Summary = new ActivitySummary { Steps = 5000 },
                        Goals = new ActivityGoals()
                    }
                });
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new FoodItem());
            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new SleepItem());
            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new WeightItem());

            // Range API throws — trend data should silently degrade
            _mockApiService.Setup(s => s.GetActivitiesByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new HttpRequestException("API error"));
            SetupEmptyRangeResponses(skipActivity: true);

            var cut = Render<Home>();

            // Dashboard still renders with single-day data
            cut.Markup.Should().Contain("5,000");
            cut.Markup.Should().Contain("Activity");
        }

        [Fact]
        public void FetchSevenDayRangeData_OnLoad()
        {
            SetupEmptyApiResponses();
            SetupEmptyRangeResponses();

            var cut = Render<Home>();

            _mockApiService.Verify(s => s.GetActivitiesByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            _mockApiService.Verify(s => s.GetSleepByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            _mockApiService.Verify(s => s.GetWeightByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            _mockApiService.Verify(s => s.GetFoodLogsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        private void SetupEmptyApiResponses()
        {
            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new ActivityItem());
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new FoodItem());
            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new SleepItem());
            _mockApiService.Setup(s => s.GetWeightByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(new WeightItem());
        }

        private void SetupEmptyRangeResponses(bool skipActivity = false, bool skipSleep = false, bool skipWeight = false, bool skipFood = false)
        {
            if (!skipActivity)
                _mockApiService.Setup(s => s.GetActivitiesByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(new PaginatedResponse<ActivityItem> { Items = [], TotalCount = 0 });
            if (!skipSleep)
                _mockApiService.Setup(s => s.GetSleepByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(new PaginatedResponse<SleepItem> { Items = [], TotalCount = 0 });
            if (!skipWeight)
                _mockApiService.Setup(s => s.GetWeightByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(new PaginatedResponse<WeightItem> { Items = [], TotalCount = 0 });
            if (!skipFood)
                _mockApiService.Setup(s => s.GetFoodLogsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(new PaginatedResponse<FoodItem> { Items = [], TotalCount = 0 });
        }
    }
}
