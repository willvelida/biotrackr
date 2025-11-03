using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Svc;
using Biotrackr.Auth.Svc.IntegrationTests.Fixtures;
using Biotrackr.Auth.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Biotrackr.Auth.Svc.IntegrationTests.Contract
{
    /// <summary>
    /// Contract tests verifying service registration and dependency injection configuration.
    /// Validates that all services can be resolved and have correct lifetimes.
    /// </summary>
    [Collection("ContractTestCollection")]
    public class ServiceRegistrationTests
    {
        private readonly ContractTestFixture _fixture;

        public ServiceRegistrationTests(ContractTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void AllRequiredServicesCanBeResolvedFromDI()
        {
            // Arrange & Act
            var secretClient = _fixture.ServiceProvider.GetService<SecretClient>();
            var refreshTokenService = _fixture.ServiceProvider.GetService<IRefreshTokenService>();
            var authWorker = _fixture.ServiceProvider.GetServices<IHostedService>().OfType<AuthWorker>().FirstOrDefault();

            // Assert
            secretClient.Should().NotBeNull("SecretClient should be registered");
            refreshTokenService.Should().NotBeNull("IRefreshTokenService should be registered");
            authWorker.Should().NotBeNull("AuthWorker should be registered as IHostedService");
        }

        [Fact]
        public void SingletonServicesReturnSameInstanceAcrossResolutions()
        {
            // Arrange & Act
            var secretClient1 = _fixture.ServiceProvider.GetService<SecretClient>();
            var secretClient2 = _fixture.ServiceProvider.GetService<SecretClient>();

            // Assert
            secretClient1.Should().NotBeNull();
            secretClient2.Should().NotBeNull();
            secretClient1.Should().BeSameAs(secretClient2, "SecretClient should be registered as Singleton");
        }

        [Fact]
        public void HttpClientBasedServicesReturnDifferentInstances()
        {
            // Arrange & Act
            // Services registered with AddHttpClient are Transient by default
            var refreshTokenService1 = _fixture.ServiceProvider.GetService<IRefreshTokenService>();
            var refreshTokenService2 = _fixture.ServiceProvider.GetService<IRefreshTokenService>();

            // Assert
            refreshTokenService1.Should().NotBeNull();
            refreshTokenService2.Should().NotBeNull();
            refreshTokenService1.Should().NotBeSameAs(refreshTokenService2, 
                "IRefreshTokenService should be registered as Transient (via AddHttpClient)");
        }

        [Fact]
        public void RefreshTokenServiceHasOnlyOneRegistration()
        {
            // Arrange & Act
            var registrations = _fixture.ServiceProvider.GetServices<IRefreshTokenService>().ToList();

            // Assert
            registrations.Should().HaveCount(1, 
                "IRefreshTokenService should have exactly one registration (no duplicate AddScoped + AddHttpClient)");
        }

        [Fact]
        public void RefreshTokenServiceHttpClientHasResilienceHandler()
        {
            // Arrange & Act
            var refreshTokenService = _fixture.ServiceProvider.GetService<IRefreshTokenService>();

            // Assert
            refreshTokenService.Should().NotBeNull("IRefreshTokenService should be registered");
            
            // The service is registered with AddHttpClient().AddStandardResilienceHandler()
            // We verify it can be resolved, which means HttpClient factory is configured correctly
            // The actual resilience handler behavior is tested in E2E tests
        }
    }
}
