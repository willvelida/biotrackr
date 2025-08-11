using Biotrackr.Food.Svc.Models;
using Biotrackr.Food.Svc.Models.FitbitEntities;
using Biotrackr.Food.Svc.Repositories.Interfaces;
using Biotrackr.Food.Svc.Services;
using Microsoft.Extensions.Logging;

namespace Biotrackr.Food.Svc.UnitTests.ServiceTests
{
    public class FoodServiceShould
    {
        private readonly Mock<ICosmosRepository> _mockCosmosRepository;
        private readonly Mock<ILogger<FoodService>> _mockLogger;
        private readonly FoodService _foodService;

        public FoodServiceShould()
        {
            _mockCosmosRepository = new Mock<ICosmosRepository>();
            _mockLogger = new Mock<ILogger<FoodService>>();
            _foodService = new FoodService(_mockCosmosRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act
            var service = new FoodService(_mockCosmosRepository.Object, _mockLogger.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullCosmosRepository_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FoodService(null!, _mockLogger.Object));

            exception.ParamName.Should().Be("cosmosRepository");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FoodService(_mockCosmosRepository.Object, null!));

            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldMapAndSaveDocument()
        {
            // Arrange
            var date = "2023-10-01";
            var fixture = new Fixture();
            var foodResponse = fixture.Create<FoodResponse>();

            _mockCosmosRepository.Setup(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> foodServiceAction = async () => await _foodService.MapAndSaveDocument(date, foodResponse);

            // Assert
            await foodServiceAction.Should().NotThrowAsync<Exception>();
            _mockCosmosRepository.Verify(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()), Times.Once);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldCreateCorrectFoodDocument()
        {
            // Arrange
            var date = "2023-10-01";
            var foodResponse = new FoodResponse();
            FoodDocument? capturedDocument = null;

            _mockCosmosRepository.Setup(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()))
                .Callback<FoodDocument>(doc => capturedDocument = doc)
                .Returns(Task.CompletedTask);

            // Act
            await _foodService.MapAndSaveDocument(date, foodResponse);

            // Assert
            capturedDocument.Should().NotBeNull();
            capturedDocument!.Id.Should().NotBeNullOrEmpty();
            capturedDocument.DocumentType.Should().Be("Food");
            capturedDocument.Date.Should().Be(date);
            capturedDocument.Food.Should().Be(foodResponse);

            // Verify the ID is a valid GUID
            IsValidGuid(capturedDocument.Id).Should().BeTrue();
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldThrowExceptionWhenFails()
        {
            // Arrange
            var date = "2023-10-01";
            var fixture = new Fixture();
            var foodResponse = fixture.Create<FoodResponse>();

            _mockCosmosRepository.Setup(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            Func<Task> foodServiceAction = async () => await _foodService.MapAndSaveDocument(date, foodResponse);

            // Assert
            await foodServiceAction.Should().ThrowAsync<Exception>();
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in MapAndSaveDocument: Test exception"));
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldLogErrorAndRethrow_WhenRepositoryThrowsException()
        {
            // Arrange
            var date = "2023-10-01";
            var foodResponse = new FoodResponse();
            var expectedMessage = "Database connection failed";
            var expectedException = new Exception(expectedMessage);

            _mockCosmosRepository.Setup(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()))
                .ThrowsAsync(expectedException);

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _foodService.MapAndSaveDocument(date, foodResponse));

            // Assert
            exception.Should().Be(expectedException);
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in MapAndSaveDocument: {expectedMessage}"), Times.Once);
        }

        [Theory]
        [InlineData(typeof(TimeoutException), "Request timeout")]
        [InlineData(typeof(ArgumentException), "Invalid argument")]
        [InlineData(typeof(InvalidOperationException), "Invalid operation")]
        public async Task MapAndSaveDocument_ShouldHandleDifferentExceptionTypes(Type exceptionType, string message)
        {
            // Arrange
            var date = "2023-10-01";
            var foodResponse = new FoodResponse();
            var exception = Activator.CreateInstance(exceptionType, message) as Exception;

            _mockCosmosRepository.Setup(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()))
                .ThrowsAsync(exception!);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync(exceptionType, () =>
                _foodService.MapAndSaveDocument(date, foodResponse));

            thrownException.Message.Should().Be(message);
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in MapAndSaveDocument: {message}"), Times.Once);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldNotLogError_WhenSuccessful()
        {
            // Arrange
            var date = "2023-10-01";
            var foodResponse = new FoodResponse();

            _mockCosmosRepository.Setup(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()))
                .Returns(Task.CompletedTask);

            // Act
            await _foodService.MapAndSaveDocument(date, foodResponse);

            // Assert
            _mockLogger.VerifyLog(logger => logger.LogError(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("2023-01-01")]
        [InlineData("2024-02-29")] // Leap year
        [InlineData("2023-12-31")]
        [InlineData("1900-01-01")]
        [InlineData("2099-12-31")]
        public async Task MapAndSaveDocument_ShouldHandleDifferentDateFormats(string date)
        {
            // Arrange
            var foodResponse = new FoodResponse();
            FoodDocument? capturedDocument = null;

            _mockCosmosRepository.Setup(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()))
                .Callback<FoodDocument>(doc => capturedDocument = doc)
                .Returns(Task.CompletedTask);

            // Act
            await _foodService.MapAndSaveDocument(date, foodResponse);

            // Assert
            capturedDocument.Should().NotBeNull();
            capturedDocument!.Date.Should().Be(date);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldGenerateUniqueIdsForMultipleCalls()
        {
            // Arrange
            var date = "2023-10-01";
            var foodResponse = new FoodResponse();
            var capturedIds = new List<string>();

            _mockCosmosRepository.Setup(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()))
                .Callback<FoodDocument>(doc => capturedIds.Add(doc.Id!))
                .Returns(Task.CompletedTask);

            // Act
            await _foodService.MapAndSaveDocument(date, foodResponse);
            await _foodService.MapAndSaveDocument(date, foodResponse);
            await _foodService.MapAndSaveDocument(date, foodResponse);

            // Assert
            capturedIds.Should().HaveCount(3);
            capturedIds.Should().OnlyHaveUniqueItems();
            capturedIds.Should().OnlyContain(id => IsValidGuid(id));
        }

        [Fact]
        public async Task MapAndSaveDocument_WithNullFoodResponse_ShouldStillCreateDocument()
        {
            // Arrange
            var date = "2023-10-01";
            FoodResponse? foodResponse = null;
            FoodDocument? capturedDocument = null;

            _mockCosmosRepository.Setup(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()))
                .Callback<FoodDocument>(doc => capturedDocument = doc)
                .Returns(Task.CompletedTask);

            // Act
            await _foodService.MapAndSaveDocument(date, foodResponse!);

            // Assert
            capturedDocument.Should().NotBeNull();
            capturedDocument!.Food.Should().BeNull();
            capturedDocument.DocumentType.Should().Be("Food");
            capturedDocument.Date.Should().Be(date);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task MapAndSaveDocument_WithInvalidDate_ShouldStillCreateDocument(string? date)
        {
            // Arrange
            var foodResponse = new FoodResponse();
            FoodDocument? capturedDocument = null;

            _mockCosmosRepository.Setup(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()))
                .Callback<FoodDocument>(doc => capturedDocument = doc)
                .Returns(Task.CompletedTask);

            // Act
            await _foodService.MapAndSaveDocument(date!, foodResponse);

            // Assert
            capturedDocument.Should().NotBeNull();
            capturedDocument!.Date.Should().Be(date);
            capturedDocument.DocumentType.Should().Be("Food");
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldHandleCosmosExceptions()
        {
            // Arrange
            var date = "2023-10-01";
            var foodResponse = new FoodResponse();
            var cosmosException = new CosmosException("Cosmos error", System.Net.HttpStatusCode.InternalServerError, 0, "test", 0);

            _mockCosmosRepository.Setup(x => x.CreateFoodDocument(It.IsAny<FoodDocument>()))
                .ThrowsAsync(cosmosException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<CosmosException>(() =>
                _foodService.MapAndSaveDocument(date, foodResponse));

            exception.Should().Be(cosmosException);
            _mockLogger.VerifyLog(logger => logger.LogError("Exception thrown in MapAndSaveDocument: Cosmos error"), Times.Once);
        }

        private static bool IsValidGuid(string? value)
        {
            return Guid.TryParse(value, out _);
        }
    }
}
