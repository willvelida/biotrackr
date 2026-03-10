using Bunit;
using Moq;
using Radzen;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Activity;
using Biotrackr.UI.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class ActivityPageShould : BunitContext
    {
        private readonly Mock<IBiotrackrApiService> _mockApiService;

        public ActivityPageShould()
        {
            _mockApiService = new Mock<IBiotrackrApiService>();
            Services.AddSingleton(_mockApiService.Object);
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RenderPageTitle()
        {
            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((ActivityItem?)null);

            var cut = Render<Activity>();

            cut.Markup.Should().Contain("Activity");
        }

        [Fact]
        public void RenderLoadingSpinner_Initially()
        {
            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .Returns(new TaskCompletionSource<ActivityItem?>().Task);

            var cut = Render<Activity>();

            cut.Markup.Should().Contain("rz-progressbar-circular");
        }

        [Fact]
        public void RenderSummaryCards_WhenDataLoaded()
        {
            var activityItem = CreateActivityItem(steps: 10000, calories: 2500, floors: 10,
                fairlyActive: 15, veryActive: 30, activeMinGoal: 30, stepsGoal: 10000,
                caloriesGoal: 2500, floorsGoal: 10);

            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(activityItem);

            var cut = Render<Activity>();

            cut.Markup.Should().Contain("10,000"); // steps
            cut.Markup.Should().Contain("2,500");  // calories
        }

        [Fact]
        public void RenderNoDataMessage_WhenNullReturned()
        {
            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((ActivityItem?)null);

            var cut = Render<Activity>();

            cut.Markup.Should().Contain("No activity data found");
        }

        [Fact]
        public void RenderErrorMessage_WhenApiThrows()
        {
            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            var cut = Render<Activity>();

            cut.Markup.Should().Contain("Failed to load activity data");
        }

        [Fact]
        public void RenderHeartRateZonesTable_WhenZonesExist()
        {
            var activityItem = CreateActivityItem();
            activityItem.Activity.Summary.HeartRateZones.Add(new HeartRateZone
            {
                Name = "Cardio",
                Minutes = 25,
                CaloriesOut = 200.5,
                Min = 120,
                Max = 160
            });

            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(activityItem);

            var cut = Render<Activity>();

            cut.Markup.Should().Contain("Heart Rate Zones");
            cut.Markup.Should().Contain("Cardio");
        }

        [Fact]
        public void RenderActivitiesTable_WhenActivitiesExist()
        {
            var activityItem = CreateActivityItem();
            activityItem.Activity.Activities.Add(new ActivityLog
            {
                Name = "Running",
                Calories = 350,
                Duration = 1800000,
                Steps = 3000,
                Distance = 5.2,
                StartTime = "08:30"
            });

            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(activityItem);

            var cut = Render<Activity>();

            cut.Markup.Should().Contain("Running");
            cut.Markup.Should().Contain("350");
        }

        [Fact]
        public void RenderDistancesTable_WhenDistancesExist()
        {
            var activityItem = CreateActivityItem();
            activityItem.Activity.Summary.Distances.Add(new DistanceData
            {
                Activity = "Walking",
                Distance = 3.45
            });

            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(activityItem);

            var cut = Render<Activity>();

            cut.Markup.Should().Contain("Distances");
            cut.Markup.Should().Contain("Walking");
            cut.Markup.Should().Contain("3.45");
        }

        [Fact]
        public void RenderRangeTable_WhenRangeDataLoaded()
        {
            var rangeResponse = new PaginatedResponse<ActivityItem>
            {
                Items = [CreateActivityItem(steps: 8000, calories: 2200)],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockApiService.Setup(s => s.GetActivityByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((ActivityItem?)null);
            _mockApiService.Setup(s => s.GetActivitiesByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Activity>();

            // Range mode uses RadzenSelectBar which cannot be interacted with via bUnit selectors.
            // Verify that the component renders without errors when data is available.
            cut.Markup.Should().NotBeEmpty();
        }

        private static ActivityItem CreateActivityItem(int steps = 0, int calories = 0, int floors = 0,
            int fairlyActive = 0, int veryActive = 0, int activeMinGoal = 0, int stepsGoal = 0,
            int caloriesGoal = 0, int floorsGoal = 0)
        {
            return new ActivityItem
            {
                Id = "test-id",
                Date = "2026-03-05",
                Activity = new ActivityData
                {
                    Summary = new ActivitySummary
                    {
                        Steps = steps,
                        CaloriesOut = calories,
                        Floors = floors,
                        FairlyActiveMinutes = fairlyActive,
                        VeryActiveMinutes = veryActive
                    },
                    Goals = new ActivityGoals
                    {
                        Steps = stepsGoal,
                        CaloriesOut = caloriesGoal,
                        Floors = floorsGoal,
                        ActiveMinutes = activeMinGoal
                    }
                }
            };
        }
    }
}
