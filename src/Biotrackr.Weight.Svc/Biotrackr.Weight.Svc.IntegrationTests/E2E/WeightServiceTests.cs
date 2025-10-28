using Biotrackr.Weight.Svc.IntegrationTests.Collections;
using Biotrackr.Weight.Svc.IntegrationTests.Fixtures;
using Biotrackr.Weight.Svc.IntegrationTests.Helpers;
using Biotrackr.Weight.Svc.Models;
using Biotrackr.Weight.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biotrackr.Weight.Svc.IntegrationTests.E2E
{
    /// <summary>
    /// E2E tests for WeightService integration with CosmosRepository.
    /// These tests verify that weight data is correctly mapped and persisted to Cosmos DB.
    /// </summary>
    [Collection("Integration Tests")]
    public class WeightServiceTests
    {
        private readonly IntegrationTestFixture _fixture;

        public WeightServiceTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task MapAndSaveDocument_Saves_Weight_Document_To_Cosmos()
        {
            // Arrange
            var weight = TestDataBuilder.BuildWeight(DateTime.UtcNow);
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

            using var scope = _fixture.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IWeightService>();

            // Act
            await service.MapAndSaveDocument(date, weight);

            // Assert - Query by date to find document
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.date = @date AND c.documentType = @docType")
                .WithParameter("@date", date)
                .WithParameter("@docType", "Weight");

            var iterator = _fixture.Container.GetItemQueryIterator<WeightDocument>(query);
            var documents = new List<WeightDocument>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                documents.AddRange(response);
            }

            documents.Should().HaveCount(1, "exactly one document should be saved");
            var document = documents.First();

            document.Id.Should().NotBeNullOrEmpty("document should have a unique ID");
            document.Date.Should().Be(date, "document date should match input date");
            document.Weight.Should().BeEquivalentTo(weight, "document weight should match input weight");
            document.DocumentType.Should().Be("Weight", "document type should be Weight");
        }

        [Fact]
        public async Task MapAndSaveDocument_Creates_Unique_Document_Ids()
        {
            // Arrange
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var weight1 = TestDataBuilder.BuildWeight(DateTime.UtcNow);
            var weight2 = TestDataBuilder.BuildWeight(DateTime.UtcNow);

            using var scope = _fixture.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IWeightService>();

            // Act - Save multiple documents with same date
            await service.MapAndSaveDocument(date, weight1);
            await service.MapAndSaveDocument(date, weight2);

            // Assert - Query all documents for this date
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.date = @date AND c.documentType = @docType")
                .WithParameter("@date", date)
                .WithParameter("@docType", "Weight");

            var iterator = _fixture.Container.GetItemQueryIterator<WeightDocument>(query);
            var documents = new List<WeightDocument>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                documents.AddRange(response);
            }

            documents.Should().HaveCount(2, "both documents should be saved");
            
            var ids = documents.Select(d => d.Id).ToList();
            ids.Should().OnlyHaveUniqueItems("each document should have a unique ID");
            ids.All(id => !string.IsNullOrEmpty(id)).Should().BeTrue("all IDs should be non-empty");
        }
    }
}
