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
    }
}
