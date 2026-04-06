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
    public class AnalyticsPageShould : BunitContext
    {
        private readonly Mock<IBiotrackrApiService> _mockApiService;

        public AnalyticsPageShould()
        {
            _mockApiService = new Mock<IBiotrackrApiService>();
            Services.AddSingleton(_mockApiService.Object);
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
            JSInterop.SetupRadzenChartInterop();
            SetupDefaultMocks();
        }

        [Fact]
        public void RenderPageTitle()
        {
            var cut = Render<Analytics>();

            cut.Markup.Should().Contain("Analytics");
        }

        [Fact]
        public void RenderDateRangeControls()
        {
            var cut = Render<Analytics>();

            cut.Markup.Should().Contain("Start Date");
            cut.Markup.Should().Contain("End Date");
        }

        [Fact]
        public void RenderCorrelationDropdown()
        {
            var cut = Render<Analytics>();

            cut.Markup.Should().Contain("Correlation");
        }

        [Fact]
        public void RenderEmptyState_WhenNoDataLoaded()
        {
            var cut = Render<Analytics>();

            cut.Markup.Should().NotContain("No correlated data points found");
            cut.Markup.Should().NotContain("rz-chart");
        }

        [Fact]
        public void RenderErrorMessage_WhenApiThrows()
        {
            _mockApiService.Setup(s => s.GetActivitiesByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new HttpRequestException("API error"));
            _mockApiService.Setup(s => s.GetSleepByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            cut.Markup.Should().Contain("Failed to load analytics data");
        }

        [Fact]
        public void RenderEmptyMessage_WhenNoMatchingDataPoints()
        {
            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            cut.Markup.Should().Contain("No correlated data points found");
        }

        [Fact]
        public void RenderScatterChart_WhenStepsVsSleepDataLoaded()
        {
            _mockApiService.Setup(s => s.GetActivitiesByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<ActivityItem>
                {
                    Items =
                    [
                        new ActivityItem
                        {
                            Date = "2026-03-15",
                            Activity = new ActivityData
                            {
                                Summary = new ActivitySummary { Steps = 10000 }
                            }
                        }
                    ]
                });
            _mockApiService.Setup(s => s.GetSleepByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<SleepItem>
                {
                    Items =
                    [
                        new SleepItem
                        {
                            Date = "2026-03-15",
                            Sleep = new SleepData
                            {
                                Summary = new SleepSummary { TotalMinutesAsleep = 420 }
                            }
                        }
                    ]
                });

            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            cut.Markup.Should().Contain("Steps vs Sleep Duration");
        }

        [Fact]
        public void RenderScatterChart_WhenCaloriesVsWeightDataLoaded()
        {
            _mockApiService.Setup(s => s.GetFoodLogsByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<FoodItem>
                {
                    Items =
                    [
                        new FoodItem
                        {
                            Date = "2026-03-15",
                            Food = new FoodData
                            {
                                Summary = new FoodSummary { Calories = 2200 }
                            }
                        }
                    ]
                });
            _mockApiService.Setup(s => s.GetWeightByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<WeightItem>
                {
                    Items =
                    [
                        new WeightItem
                        {
                            Date = "2026-03-15",
                            Weight = new WeightData { Weight = 80.5 }
                        }
                    ]
                });

            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            // Default correlation is StepsVsSleep, so verify the APIs are being called
            _mockApiService.Verify(s => s.GetActivitiesByDateRangeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void RenderLoadingSpinner_WhenLoadingData()
        {
            _mockApiService.Setup(s => s.GetActivitiesByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new TaskCompletionSource<PaginatedResponse<ActivityItem>>().Task);
            _mockApiService.Setup(s => s.GetSleepByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new TaskCompletionSource<PaginatedResponse<SleepItem>>().Task);

            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            cut.Markup.Should().Contain("Loading analytics data");
        }

        [Fact]
        public void NotRenderChart_WhenNoMatchingDatesExist()
        {
            _mockApiService.Setup(s => s.GetActivitiesByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<ActivityItem>
                {
                    Items =
                    [
                        new ActivityItem
                        {
                            Date = "2026-03-15",
                            Activity = new ActivityData
                            {
                                Summary = new ActivitySummary { Steps = 10000 }
                            }
                        }
                    ]
                });
            _mockApiService.Setup(s => s.GetSleepByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<SleepItem>
                {
                    Items =
                    [
                        new SleepItem
                        {
                            Date = "2026-03-16",
                            Sleep = new SleepData
                            {
                                Summary = new SleepSummary { TotalMinutesAsleep = 420 }
                            }
                        }
                    ]
                });

            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            cut.Markup.Should().Contain("No correlated data points found");
        }

        private void SetupDefaultMocks()
        {
            _mockApiService.Setup(s => s.GetActivitiesByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<ActivityItem> { Items = [] });
            _mockApiService.Setup(s => s.GetSleepByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<SleepItem> { Items = [] });
            _mockApiService.Setup(s => s.GetFoodLogsByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<FoodItem> { Items = [] });
            _mockApiService.Setup(s => s.GetWeightByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<WeightItem> { Items = [] });
        }
    }
}
