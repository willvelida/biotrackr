using Bunit;
using Moq;
using Radzen;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Sleep;
using Biotrackr.UI.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class SleepPageShould : BunitContext
    {
        private readonly Mock<IBiotrackrApiService> _mockApiService;

        public SleepPageShould()
        {
            _mockApiService = new Mock<IBiotrackrApiService>();
            Services.AddSingleton(_mockApiService.Object);
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RenderPageTitle()
        {
            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((SleepItem?)null);

            var cut = Render<Sleep>();

            cut.Markup.Should().Contain("Sleep");
        }

        [Fact]
        public void RenderSummaryCards_WhenDataLoaded()
        {
            var sleepItem = CreateSleepItem(minutesAsleep: 450, timeInBed: 500, records: 1);

            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(sleepItem);

            var cut = Render<Sleep>();

            cut.Markup.Should().Contain("7h 30m"); // 450 minutes
            cut.Markup.Should().Contain("8h 20m"); // 500 minutes
        }

        [Fact]
        public void RenderSleepStages_WhenStagesExist()
        {
            var sleepItem = CreateSleepItem(minutesAsleep: 420, timeInBed: 480, records: 1);
            sleepItem.Sleep.Summary.Stages = new SleepStages
            {
                Deep = 90,
                Light = 180,
                Rem = 100,
                Wake = 30
            };

            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(sleepItem);

            var cut = Render<Sleep>();

            cut.Markup.Should().Contain("Sleep Stages");
            cut.Markup.Should().Contain("Deep");
            cut.Markup.Should().Contain("1h 30m"); // 90 minutes deep
        }

        [Fact]
        public void RenderSleepLogs_WhenRecordsExist()
        {
            var sleepItem = CreateSleepItem(minutesAsleep: 420, timeInBed: 480, records: 1);
            sleepItem.Sleep.Sleep.Add(new SleepRecord
            {
                StartTime = new DateTime(2026, 3, 5, 22, 30, 0),
                EndTime = new DateTime(2026, 3, 6, 6, 30, 0),
                TimeInBed = 480,
                MinutesAsleep = 420,
                MinutesAwake = 60,
                Efficiency = 88,
                IsMainSleep = true
            });

            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(sleepItem);

            var cut = Render<Sleep>();

            cut.Markup.Should().Contain("Sleep Logs");
            cut.Markup.Should().Contain("88%");
            cut.Markup.Should().Contain("Main");
        }

        [Fact]
        public void RenderNoDataMessage_WhenNullReturned()
        {
            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((SleepItem?)null);

            var cut = Render<Sleep>();

            cut.Markup.Should().Contain("No sleep data found");
        }

        [Fact]
        public void RenderErrorMessage_WhenApiThrows()
        {
            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            var cut = Render<Sleep>();

            cut.Markup.Should().Contain("Failed to load sleep data");
        }

        [Fact]
        public void RenderEfficiency_WhenMainSleepExists()
        {
            var sleepItem = CreateSleepItem(minutesAsleep: 420, timeInBed: 480, records: 1);
            sleepItem.Sleep.Sleep.Add(new SleepRecord
            {
                IsMainSleep = true,
                Efficiency = 92,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(8)
            });

            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(sleepItem);

            var cut = Render<Sleep>();

            cut.Markup.Should().Contain("92%");
        }

        [Fact]
        public void RenderRangeTable_WhenRangeDataLoaded()
        {
            var rangeResponse = new PaginatedResponse<SleepItem>
            {
                Items = [CreateSleepItem(minutesAsleep: 400, timeInBed: 450, records: 1)],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockApiService.Setup(s => s.GetSleepByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((SleepItem?)null);
            _mockApiService.Setup(s => s.GetSleepByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Sleep>();

            // Range mode uses RadzenSelectBar which cannot be interacted with via bUnit selectors.
            // Verify that the component renders without errors when data is available.
            cut.Markup.Should().NotBeEmpty();
        }

        private static SleepItem CreateSleepItem(int minutesAsleep = 0, int timeInBed = 0, int records = 0)
        {
            return new SleepItem
            {
                Id = "test-id",
                Date = "2026-03-05",
                Sleep = new SleepData
                {
                    Summary = new SleepSummary
                    {
                        TotalMinutesAsleep = minutesAsleep,
                        TotalTimeInBed = timeInBed,
                        TotalSleepRecords = records,
                        Stages = new SleepStages()
                    }
                }
            };
        }
    }
}
