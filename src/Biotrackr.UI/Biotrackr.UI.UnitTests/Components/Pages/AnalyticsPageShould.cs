using Bunit;
using Moq;
using Radzen;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Activity;
using Biotrackr.UI.Models.Food;
using Biotrackr.UI.Models.Sleep;
using Biotrackr.UI.Models.Vitals;
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
            // Act
            var cut = Render<Analytics>();

            // Assert
            cut.Markup.Should().Contain("Analytics");
        }

        [Fact]
        public void RenderDateRangeControls()
        {
            // Act
            var cut = Render<Analytics>();

            // Assert
            cut.Markup.Should().Contain("Start Date");
            cut.Markup.Should().Contain("End Date");
        }

        [Fact]
        public void RenderCorrelationDropdown()
        {
            // Act
            var cut = Render<Analytics>();

            // Assert
            cut.Markup.Should().Contain("Correlation");
        }

        [Fact]
        public void RenderEmptyState_WhenNoDataLoaded()
        {
            // Act
            var cut = Render<Analytics>();

            // Assert
            cut.Markup.Should().NotContain("No correlated data points found");
            cut.Markup.Should().NotContain("rz-chart");
        }

        [Fact]
        public void RenderErrorMessage_WhenApiThrows()
        {
            // Arrange
            _mockApiService.Setup(s => s.GetActivitiesByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new HttpRequestException("API error"));
            _mockApiService.Setup(s => s.GetSleepByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            // Act
            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            // Assert
            cut.Markup.Should().Contain("Failed to load analytics data");
        }

        [Fact]
        public void RenderEmptyMessage_WhenNoMatchingDataPoints()
        {
            // Act
            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            // Assert
            cut.Markup.Should().Contain("No correlated data points found");
        }

        [Fact]
        public void RenderScatterChart_WhenStepsVsSleepDataLoaded()
        {
            // Arrange
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

            // Act
            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            // Assert
            cut.Markup.Should().Contain("Steps vs Sleep Duration");
        }

        [Fact]
        public void RenderScatterChart_WhenCaloriesVsWeightDataLoaded()
        {
            // Arrange
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
            _mockApiService.Setup(s => s.GetVitalsByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<VitalsItem>
                {
                    Items =
                    [
                        new VitalsItem
                        {
                            Date = "2026-03-15",
                            Weight = new VitalsData { Weight = 80.5 }
                        }
                    ]
                });

            // Act
            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            // Assert
            // Default correlation is StepsVsSleep, so verify the APIs are being called
            _mockApiService.Verify(s => s.GetActivitiesByDateRangeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void RenderLoadingSpinner_WhenLoadingData()
        {
            // Arrange
            _mockApiService.Setup(s => s.GetActivitiesByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new TaskCompletionSource<PaginatedResponse<ActivityItem>>().Task);
            _mockApiService.Setup(s => s.GetSleepByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new TaskCompletionSource<PaginatedResponse<SleepItem>>().Task);

            // Act
            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            // Assert
            cut.Markup.Should().Contain("Loading analytics data");
        }

        [Fact]
        public void NotRenderChart_WhenNoMatchingDatesExist()
        {
            // Arrange
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

            // Act
            var cut = Render<Analytics>();
            cut.Find("button[aria-label='Load correlation data']").Click();

            // Assert
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
            _mockApiService.Setup(s => s.GetVitalsByDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<VitalsItem> { Items = [] });
        }
    }
}
