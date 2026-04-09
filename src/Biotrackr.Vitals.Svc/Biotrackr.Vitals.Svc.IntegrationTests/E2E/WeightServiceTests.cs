using Biotrackr.Vitals.Svc.IntegrationTests.Collections;
using Biotrackr.Vitals.Svc.IntegrationTests.Fixtures;
using Biotrackr.Vitals.Svc.IntegrationTests.Helpers;
using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biotrackr.Vitals.Svc.IntegrationTests.E2E
{
    [Collection("Integration Tests")]
    public class VitalsServiceTests
    {
        private readonly IntegrationTestFixture _fixture;

        public VitalsServiceTests(IntegrationTestFixture fixture)
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
        public async Task UpsertVitalsDocument_Saves_Withings_Vitals_Document_To_Cosmos()
        {
            await ClearContainerAsync();
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var vitalsDoc = TestDataBuilder.BuildVitalsDocument(DateTime.UtcNow);
            vitalsDoc.Date = date;

            using var scope = _fixture.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IVitalsService>();

            await service.UpsertVitalsDocument(vitalsDoc);

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.date = @date AND c.documentType = @docType")
                .WithParameter("@date", date)
                .WithParameter("@docType", "Vitals");

            var iterator = _fixture.Container.GetItemQueryIterator<VitalsDocument>(query);
            var documents = new List<VitalsDocument>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                documents.AddRange(response);
            }

            documents.Should().HaveCount(1);
            var document = documents.First();
            document.Provider.Should().Be("Withings");
            document.DocumentType.Should().Be("Vitals");
            document.Weight.WeightKg.Should().Be(80.25);
            document.Weight.MuscleMassKg.Should().Be(45.2);
            document.Weight.BoneMassKg.Should().Be(3.1);
        }

        [Fact]
        public async Task UpsertVitalsDocument_Upserts_Same_Document_Without_Duplicates()
        {
            await ClearContainerAsync();
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var vitalsDoc = TestDataBuilder.BuildVitalsDocument(DateTime.UtcNow);
            vitalsDoc.Date = date;

            using var scope = _fixture.ServiceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IVitalsService>();

            // Save twice with the same date
            await service.UpsertVitalsDocument(vitalsDoc);
            await service.UpsertVitalsDocument(vitalsDoc);

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = @docType")
                .WithParameter("@docType", "Vitals");

            var iterator = _fixture.Container.GetItemQueryIterator<VitalsDocument>(query);
            var documents = new List<VitalsDocument>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                documents.AddRange(response);
            }

            documents.Should().HaveCount(1, "upsert should not create duplicates for same date");
        }
    }
}
