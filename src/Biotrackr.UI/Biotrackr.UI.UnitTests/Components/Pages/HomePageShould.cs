using Bunit;
using Moq;
using Radzen;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Models.Activity;
using Biotrackr.UI.Models.Food;
using Biotrackr.UI.Models.Sleep;
using Biotrackr.UI.Models.Weight;
using Biotrackr.UI.Services;
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

            // Should show "--" placeholders for missing data
            cut.Markup.Should().Contain("--");
        }

        [Fact]
        public void RenderErrorMessage_WhenApiThrows()
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

            cut.Markup.Should().Contain("Unable to load dashboard data");
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

            cut.Markup.Should().Contain("BMI: 23.1");
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
    }
}
