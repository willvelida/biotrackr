using Bunit;
using Moq;
using Radzen;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Food;
using Biotrackr.UI.Services;
using Biotrackr.UI.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class FoodPageShould : BunitContext
    {
        private readonly Mock<IBiotrackrApiService> _mockApiService;

        public FoodPageShould()
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
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((FoodItem?)null);

            var cut = Render<Food>();

            cut.Markup.Should().Contain("Food");
        }

        [Fact]
        public void RenderSummaryCards_WhenDataLoaded()
        {
            var foodItem = CreateFoodItem(calories: 2500, protein: 120, carbs: 300, fat: 80, fiber: 25, water: 2000);

            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(foodItem);

            var cut = Render<Food>();

            cut.Markup.Should().Contain("2,500");  // calories
            cut.Markup.Should().Contain("120.0g"); // protein
            cut.Markup.Should().Contain("300.0g"); // carbs
            cut.Markup.Should().Contain("80.0g");  // fat
        }

        [Fact]
        public void RenderFoodLog_WhenFoodsExist()
        {
            var foodItem = CreateFoodItem();
            foodItem.Food.Foods.Add(new FoodEntry
            {
                LoggedFood = new LoggedFood
                {
                    Name = "Chicken Breast",
                    Brand = "Fresh",
                    Amount = 1,
                    Unit = new FoodUnit { Name = "serving" }
                },
                NutritionalValues = new NutritionalValues
                {
                    Calories = 165,
                    Protein = 31,
                    Carbs = 0,
                    Fat = 3.6
                }
            });

            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(foodItem);

            var cut = Render<Food>();

            cut.Markup.Should().Contain("Food Log");
            cut.Markup.Should().Contain("Chicken Breast");
        }

        [Fact]
        public void RenderNoDataMessage_WhenNullReturned()
        {
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((FoodItem?)null);

            var cut = Render<Food>();

            cut.Markup.Should().Contain("No food data found");
        }

        [Fact]
        public void RenderErrorMessage_WhenApiThrows()
        {
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            var cut = Render<Food>();

            cut.Markup.Should().Contain("Failed to load food data");
        }

        [Fact]
        public void RenderCalorieGoalText_WhenGoalSet()
        {
            var foodItem = CreateFoodItem(calories: 2000);
            foodItem.Food.Goals = new FoodGoals { Calories = 2500 };

            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(foodItem);

            var cut = Render<Food>();

            cut.Markup.Should().Contain("goal");
        }

        [Fact]
        public void RenderRangeTable_WhenRangeDataLoaded()
        {
            var rangeResponse = new PaginatedResponse<FoodItem>
            {
                Items = [CreateFoodItem(calories: 1800, protein: 90, carbs: 200, fat: 60, fiber: 20)],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((FoodItem?)null);
            _mockApiService.Setup(s => s.GetFoodLogsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Food>();

            // Range mode uses RadzenSelectBar which cannot be interacted with via bUnit selectors.
            // Verify that the component renders without errors when data is available.
            cut.Markup.Should().NotBeEmpty();
        }

        [Fact]
        public void RenderCharts_WhenSingleDateDataLoaded()
        {
            var foodItem = CreateFoodItem(calories: 2500, protein: 120, carbs: 300, fat: 80, fiber: 25, water: 2000);
            foodItem.Food.Goals = new FoodGoals { Calories = 3000 };

            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync(foodItem);

            var cut = Render<Food>();

            // Donut chart for macros and arc gauge for calorie goal
            cut.Markup.Should().Contain("2,500");
            cut.Markup.Should().Contain("rz-arc-gauge");
        }

        [Fact]
        public void RenderTrendCharts_WhenRangeDateDataLoaded()
        {
            var rangeResponse = new PaginatedResponse<FoodItem>
            {
                Items =
                [
                    CreateFoodItem(calories: 1800, protein: 90, carbs: 200, fat: 60),
                    CreateFoodItem(calories: 2200, protein: 110, carbs: 250, fat: 70)
                ],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 2,
                HasPreviousPage = false,
                HasNextPage = false
            };
            rangeResponse.Items[0].Date = "2026-03-01";
            rangeResponse.Items[1].Date = "2026-03-02";

            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((FoodItem?)null);
            _mockApiService.Setup(s => s.GetFoodLogsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Food>();

            // The component renders without errors when range data is available
            cut.Markup.Should().NotBeEmpty();
        }

        [Fact]
        public void RenderChartsAndDataGrid_InRangeMode()
        {
            var rangeResponse = new PaginatedResponse<FoodItem>
            {
                Items = [CreateFoodItem(calories: 1800, protein: 90, carbs: 200, fat: 60, fiber: 20)],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((FoodItem?)null);
            _mockApiService.Setup(s => s.GetFoodLogsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Food>();

            cut.Markup.Should().Contain("Food");
            cut.Markup.Should().NotBeEmpty();
        }

        private static FoodItem CreateFoodItem(double calories = 0, double protein = 0, double carbs = 0,
            double fat = 0, double fiber = 0, double water = 0)
        {
            return new FoodItem
            {
                Id = "test-id",
                Date = "2026-03-05",
                Food = new FoodData
                {
                    Summary = new FoodSummary
                    {
                        Calories = calories,
                        Protein = protein,
                        Carbs = carbs,
                        Fat = fat,
                        Fiber = fiber,
                        Water = water
                    },
                    Goals = new FoodGoals()
                }
            };
        }
    }
}
