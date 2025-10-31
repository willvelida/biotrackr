using Biotrackr.Sleep.Svc.IntegrationTests.Fixtures;
using Biotrackr.Sleep.Svc.Repositories.Interfaces;
using Biotrackr.Sleep.Svc.Services.Interfaces;
using Biotrackr.Sleep.Svc.Worker;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Biotrackr.Sleep.Svc.IntegrationTests.Contract
{
    [Collection("SleepServiceContractTests")]
    public class ProgramStartupTests
    {
        private readonly ContractTestFixture _fixture;

        public ProgramStartupTests(ContractTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Application_ShouldResolveAllServices()
        {
            // Act & Assert - Verify all services can be resolved
            var cosmosClient = _fixture.ServiceProvider.GetService<CosmosClient>();
            cosmosClient.Should().NotBeNull("CosmosClient should be registered");

            var cosmosRepository = _fixture.ServiceProvider.GetService<ICosmosRepository>();
            cosmosRepository.Should().NotBeNull("ICosmosRepository should be registered");

            var sleepService = _fixture.ServiceProvider.GetService<ISleepService>();
            sleepService.Should().NotBeNull("ISleepService should be registered");

            var fitbitService = _fixture.ServiceProvider.GetService<IFitbitService>();
            fitbitService.Should().NotBeNull("IFitbitService should be registered");

            var hostedServices = _fixture.ServiceProvider.GetServices<IHostedService>();
            hostedServices.Should().Contain(s => s.GetType() == typeof(SleepWorker), 
                "SleepWorker should be registered as IHostedService");
        }

        [Fact]
        public void Application_ShouldBuildHost_WithoutExceptions()
        {
            // Arrange & Act - Host was built in fixture constructor
            // Assert - If we got here, the host was built successfully
            _fixture.ServiceProvider.Should().NotBeNull("Service provider should be available");
        }
    }
}
