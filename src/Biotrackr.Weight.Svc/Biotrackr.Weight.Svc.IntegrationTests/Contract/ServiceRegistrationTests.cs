using Biotrackr.Weight.Svc.IntegrationTests.Collections;
using Biotrackr.Weight.Svc.IntegrationTests.Fixtures;
using Biotrackr.Weight.Svc.Repositories.Interfaces;
using Biotrackr.Weight.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biotrackr.Weight.Svc.IntegrationTests.Contract
{
    /// <summary>
    /// Contract tests for service registration and lifetime configuration.
    /// These tests verify that services are registered with the correct lifetime scopes.
    /// </summary>
    [Collection("Contract Tests")]
    public class ServiceRegistrationTests
    {
        private readonly ContractTestFixture _fixture;

        public ServiceRegistrationTests(ContractTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void CosmosRepository_Is_Registered_As_Scoped()
        {
            // Arrange
            var serviceProvider = _fixture.ServiceProvider;

            // Act - Same scope
            using (var scope1 = serviceProvider.CreateScope())
            {
                var repo1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
                var repo2 = scope1.ServiceProvider.GetService<ICosmosRepository>();

                // Assert - Same instance in same scope
                repo1.Should().BeSameAs(repo2, "scoped services should return the same instance within a scope");
            }

            // Act - Different scopes
            ICosmosRepository? repoScope1;
            ICosmosRepository? repoScope2;

            using (var scope1 = serviceProvider.CreateScope())
            {
                repoScope1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
            }

            using (var scope2 = serviceProvider.CreateScope())
            {
                repoScope2 = scope2.ServiceProvider.GetService<ICosmosRepository>();
            }

            // Assert - Different instances across scopes
            repoScope1.Should().NotBeSameAs(repoScope2, "scoped services should return different instances across scopes");
        }

        [Fact]
        public void WeightService_Is_Registered_As_Scoped()
        {
            // Arrange
            var serviceProvider = _fixture.ServiceProvider;

            // Act - Same scope
            using (var scope1 = serviceProvider.CreateScope())
            {
                var service1 = scope1.ServiceProvider.GetService<IWeightService>();
                var service2 = scope1.ServiceProvider.GetService<IWeightService>();

                // Assert - Same instance in same scope
                service1.Should().BeSameAs(service2, "scoped services should return the same instance within a scope");
            }

            // Act - Different scopes
            IWeightService? serviceScope1;
            IWeightService? serviceScope2;

            using (var scope1 = serviceProvider.CreateScope())
            {
                serviceScope1 = scope1.ServiceProvider.GetService<IWeightService>();
            }

            using (var scope2 = serviceProvider.CreateScope())
            {
                serviceScope2 = scope2.ServiceProvider.GetService<IWeightService>();
            }

            // Assert - Different instances across scopes
            serviceScope1.Should().NotBeSameAs(serviceScope2, "scoped services should return different instances across scopes");
        }

        [Fact]
        public void FitbitService_Is_Registered_As_Transient()
        {
            // Arrange
            var serviceProvider = _fixture.ServiceProvider;

            // Act - Same scope
            using (var scope1 = serviceProvider.CreateScope())
            {
                var service1 = scope1.ServiceProvider.GetService<IFitbitService>();
                var service2 = scope1.ServiceProvider.GetService<IFitbitService>();

                // Assert - Different instances even in same scope (transient)
                service1.Should().NotBeSameAs(service2, "transient services should return different instances even within the same scope");
            }

            // Act - Different scopes
            IFitbitService? serviceScope1;
            IFitbitService? serviceScope2;

            using (var scope1 = serviceProvider.CreateScope())
            {
                serviceScope1 = scope1.ServiceProvider.GetService<IFitbitService>();
            }

            using (var scope2 = serviceProvider.CreateScope())
            {
                serviceScope2 = scope2.ServiceProvider.GetService<IFitbitService>();
            }

            // Assert - Different instances across scopes (transient)
            serviceScope1.Should().NotBeSameAs(serviceScope2, "transient services should return different instances across scopes");
        }
    }
}
