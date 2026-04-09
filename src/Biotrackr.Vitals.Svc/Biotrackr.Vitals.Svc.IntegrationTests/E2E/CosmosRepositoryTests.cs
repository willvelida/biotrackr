using Biotrackr.Vitals.Svc.IntegrationTests.Collections;
using Biotrackr.Vitals.Svc.IntegrationTests.Fixtures;
using Biotrackr.Vitals.Svc.IntegrationTests.Helpers;
using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biotrackr.Vitals.Svc.IntegrationTests.E2E
{
    [Collection("Integration Tests")]
    public class CosmosRepositoryTests
    {
        private readonly IntegrationTestFixture _fixture;

        public CosmosRepositoryTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task UpsertDocument_Persists_To_Cosmos_With_Correct_PartitionKey()
        {
            var document = TestDataBuilder.BuildVitalsDocument(DateTime.UtcNow);

            using var scope = _fixture.ServiceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICosmosRepository>();

            await repository.UpsertVitalsDocument(document);

            try
            {
                var response = await _fixture.Container.ReadItemAsync<VitalsDocument>(
                    document.Id,
                    new PartitionKey(document.DocumentType));

                response.Resource.Should().NotBeNull();
                response.Resource.Id.Should().Be(document.Id);
                response.Resource.DocumentType.Should().Be("Vitals");
                response.Resource.Provider.Should().Be("Withings");
                response.Resource.Weight.WeightKg.Should().Be(80.25);
                response.Resource.Weight.MuscleMassKg.Should().Be(45.2);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Assert.Fail($"Document with ID {document.Id} was not found in Cosmos DB");
            }
        }

        [Fact]
        public async Task UpsertDocument_Overwrites_Existing_Document()
        {
            var document = TestDataBuilder.BuildVitalsDocument(DateTime.UtcNow);

            using var scope = _fixture.ServiceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICosmosRepository>();

            await repository.UpsertVitalsDocument(document);

            // Modify and upsert again
            document.Weight.WeightKg = 81.5;
            await repository.UpsertVitalsDocument(document);

            var response = await _fixture.Container.ReadItemAsync<VitalsDocument>(
                document.Id,
                new PartitionKey(document.DocumentType));

            response.Resource.Weight.WeightKg.Should().Be(81.5, "upsert should overwrite the existing document");
        }
    }
}
