using Biotrackr.Sleep.Svc.IntegrationTests.Fixtures;
using Biotrackr.Sleep.Svc.Repositories.Interfaces;
using Biotrackr.Sleep.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.Sleep.Svc.IntegrationTests.Contract
{
    [Collection("SleepServiceContractTests")]
    public class ServiceRegistrationTests
    {
        private readonly ContractTestFixture _fixture;

        public ServiceRegistrationTests(ContractTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void SingletonServices_ShouldReturnSameInstance()
        {
            // Arrange & Act
            var cosmosClient1 = _fixture.ServiceProvider.GetService<CosmosClient>();
            var cosmosClient2 = _fixture.ServiceProvider.GetService<CosmosClient>();

            // Assert
            cosmosClient1.Should().BeSameAs(cosmosClient2, 
                "CosmosClient should be registered as Singleton");
        }

        [Fact]
        public void ScopedServices_ShouldReturnSameInstance_WithinScope()
        {
            // Arrange & Act
            using (var scope1 = _fixture.ServiceProvider.CreateScope())
            {
                var repository1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
                var repository2 = scope1.ServiceProvider.GetService<ICosmosRepository>();

                // Assert - Same instance within scope
                repository1.Should().BeSameAs(repository2,
                    "ICosmosRepository should return same instance within scope");
            }

            // Create second scope to verify different instance
            using (var scope2 = _fixture.ServiceProvider.CreateScope())
            {
                var repository3 = scope2.ServiceProvider.GetService<ICosmosRepository>();

                using (var scope1 = _fixture.ServiceProvider.CreateScope())
                {
                    var repository1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
                    
                    // Assert - Different instance across scopes
                    repository3.Should().NotBeSameAs(repository1,
                        "ICosmosRepository should return different instance across scopes");
                }
            }
        }

        [Fact]
        public void TransientServices_ShouldReturnDifferentInstances()
        {
            // Arrange & Act
            var fitbitService1 = _fixture.ServiceProvider.GetService<IFitbitService>();
            var fitbitService2 = _fixture.ServiceProvider.GetService<IFitbitService>();

            // Assert
            fitbitService1.Should().NotBeSameAs(fitbitService2,
                "IFitbitService should be registered as Transient (via AddHttpClient)");
        }
    }
}
