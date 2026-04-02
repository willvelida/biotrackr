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
            var document = TestDataBuilder.BuildWeightDocument(DateTime.UtcNow);

            using var scope = _fixture.ServiceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICosmosRepository>();

            await repository.UpsertWeightDocument(document);

            try
            {
                var response = await _fixture.Container.ReadItemAsync<WeightDocument>(
                    document.Id,
                    new PartitionKey(document.DocumentType));

                response.Resource.Should().NotBeNull();
                response.Resource.Id.Should().Be(document.Id);
                response.Resource.DocumentType.Should().Be("Weight");
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
            var document = TestDataBuilder.BuildWeightDocument(DateTime.UtcNow);

            using var scope = _fixture.ServiceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICosmosRepository>();

            await repository.UpsertWeightDocument(document);

            // Modify and upsert again
            document.Weight.WeightKg = 81.5;
            await repository.UpsertWeightDocument(document);

            var response = await _fixture.Container.ReadItemAsync<WeightDocument>(
                document.Id,
                new PartitionKey(document.DocumentType));

            response.Resource.Weight.WeightKg.Should().Be(81.5, "upsert should overwrite the existing document");
        }
    }
}
