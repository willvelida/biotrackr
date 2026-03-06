using Bunit;
using Moq;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Food;
using Biotrackr.UI.Services;
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
        }

        [Fact]
        public void RenderPageTitle()
        {
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((FoodItem?)null);

            var cut = Render<Food>();

            cut.Find("h1").TextContent.Should().Be("Food");
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
        public async Task RenderRangeTable_WhenRangeDataLoaded()
        {
            _mockApiService.Setup(s => s.GetFoodLogByDateAsync(It.IsAny<string>()))
                .ReturnsAsync((FoodItem?)null);

            var rangeResponse = new PaginatedResponse<FoodItem>
            {
                Items = [CreateFoodItem(calories: 1800, protein: 90, carbs: 200, fat: 60, fiber: 20)],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockApiService.Setup(s => s.GetFoodLogsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(rangeResponse);

            var cut = Render<Food>();

            cut.Find("select").Change("range");
            cut.Find("button.btn-primary").Click();

            cut.Markup.Should().Contain("Food Records");
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
