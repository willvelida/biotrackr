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
    [Collection("Integration Tests")]
    public class WeightServiceTests
    {
        private readonly IntegrationTestFixture _fixture;

        public WeightServiceTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task ClearContainerAsync()
        {
            var query = new QueryDefinition("SELECT c.id, c.documentType FROM c");
            var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    await _fixture.Container.DeleteItemAsync<dynamic>(
                        item.id.ToString(),
                        new PartitionKey(item.documentType.ToString()));
                }
            }
        }

        [Fact]
        public async Task MapAndSaveDocument_Saves_Withings_Weight_Document_To_Cosmos()
        {
            await ClearContainerAsync();
            var weight = TestDataBuilder.BuildWeightMeasurement(DateTime.UtcNow);
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

            using var scope = _fixture.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IWeightService>();

            await service.MapAndSaveDocument(date, weight, "Withings");

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

            documents.Should().HaveCount(1);
            var document = documents.First();
            document.Provider.Should().Be("Withings");
            document.Weight.WeightKg.Should().Be(80.25);
            document.Weight.MuscleMassKg.Should().Be(45.2);
            document.Weight.BoneMassKg.Should().Be(3.1);
        }

        [Fact]
        public async Task MapAndSaveDocument_Upserts_Same_Document_Without_Duplicates()
        {
            await ClearContainerAsync();
            var weight = TestDataBuilder.BuildWeightMeasurement(DateTime.UtcNow);
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

            using var scope = _fixture.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IWeightService>();

            // Save twice with the same LogId
            await service.MapAndSaveDocument(date, weight, "Withings");
            await service.MapAndSaveDocument(date, weight, "Withings");

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = @docType")
                .WithParameter("@docType", "Weight");

            var iterator = _fixture.Container.GetItemQueryIterator<WeightDocument>(query);
            var documents = new List<WeightDocument>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                documents.AddRange(response);
            }

            documents.Should().HaveCount(1, "upsert should not create duplicates for same LogId");
        }
    }
}
