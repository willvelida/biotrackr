using AutoFixture;
using Biotrackr.Weight.Api.EndpointHandlers;
using Biotrackr.Weight.Api.Models;
using Biotrackr.Weight.Api.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Biotrackr.Weight.Api.UnitTests.EndpointHandlerTests
{
    public class WeightHandlersShould
    {
        private readonly Mock<ICosmosRepository> _cosmosRepositoryMock;

        public WeightHandlersShould()
        {
            _cosmosRepositoryMock = new Mock<ICosmosRepository>();
        }

        [Fact]
        public async Task GetAllWeights_ShouldReturnListOfWeightDocuments()
        {
            // Arrange
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>().ToList();
            _cosmosRepositoryMock.Setup(c => c.GetAllWeightDocuments()).ReturnsAsync(weightDocuments);
            // Act
            var result = await WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object);
            // Assert
            result.Should().BeOfType<Ok<List<WeightDocument>>>();
            result.Value.Should().BeEquivalentTo(weightDocuments);
        }

        [Fact]
        public async Task GetWeightByDate_ShouldReturnOk_WhenWeightIsFound()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var weightDocument = fixture.Create<WeightDocument>();
            weightDocument.Date = date;

            _cosmosRepositoryMock.Setup(c => c.GetWeightDocumentByDate(date)).ReturnsAsync(weightDocument);

            // Act
            var result = await WeightHandlers.GetWeightByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<Ok<WeightDocument>>();
        }

        [Fact]
        public async Task GetWeightByDate_ShouldReturnNotFound_WhenWeightIsNotFound()
        {
            // Arrange
            var date = "2022-01-01";
            _cosmosRepositoryMock.Setup(c => c.GetWeightDocumentByDate(date)).ReturnsAsync((WeightDocument)null);

            // Act
            var result = await WeightHandlers.GetWeightByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<NotFound>();
        }
    }
}
