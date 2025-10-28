using Biotrackr.Weight.Svc.IntegrationTests.Collections;
using Biotrackr.Weight.Svc.IntegrationTests.Fixtures;
using Biotrackr.Weight.Svc.IntegrationTests.Helpers;
using Biotrackr.Weight.Svc.Models;
using Biotrackr.Weight.Svc.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biotrackr.Weight.Svc.IntegrationTests.E2E
{
    /// <summary>
    /// E2E tests for CosmosRepository database operations.
    /// These tests verify that documents are correctly persisted to Cosmos DB with proper partition keys.
    /// </summary>
    [Collection("Integration Tests")]
    public class CosmosRepositoryTests
    {
        private readonly IntegrationTestFixture _fixture;

        public CosmosRepositoryTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateDocument_Persists_To_Cosmos_With_Correct_PartitionKey()
        {
            // Arrange
            var document = TestDataBuilder.BuildWeightDocument(DateTime.UtcNow);

            using var scope = _fixture.ServiceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICosmosRepository>();

            // Act
            await repository.CreateWeightDocument(document);

            // Assert - Retrieve document directly by ID and partition key
            try
            {
                var response = await _fixture.Container.ReadItemAsync<WeightDocument>(
                    document.Id,
                    new PartitionKey(document.DocumentType));

                response.Resource.Should().NotBeNull("document should be retrievable");
                response.Resource.Id.Should().Be(document.Id, "document ID should match");
                response.Resource.DocumentType.Should().Be("Weight", "partition key should be 'Weight'");
                response.Resource.Date.Should().Be(document.Date, "date should match");
                response.Resource.Weight.Should().BeEquivalentTo(document.Weight, "weight data should match");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Assert.Fail($"Document with ID {document.Id} was not found in Cosmos DB");
            }
        }

        [Fact]
        public async Task CreateDocument_Handles_Duplicate_Id_Gracefully()
        {
            // Arrange
            var document = TestDataBuilder.BuildWeightDocument(DateTime.UtcNow);

            using var scope = _fixture.ServiceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICosmosRepository>();

            // Act - Create document first time
            await repository.CreateWeightDocument(document);

            // Act & Assert - Attempt to create same document again (same ID)
            var act = async () => await repository.CreateWeightDocument(document);

            // Cosmos DB should throw CosmosException with Conflict status code
            await act.Should().ThrowAsync<CosmosException>(
                "duplicate document IDs should cause a conflict")
                .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.Conflict,
                "the exception should indicate a conflict");
        }
    }
}
