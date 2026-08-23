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
        public void ServiceProvider_ShouldResolveAllRequiredServices_WhenHostIsBuilt()
        {
            // Arrange
            var serviceProvider = _fixture.ServiceProvider;

            // Act
            var secretClient = serviceProvider.GetService<SecretClient>();
            var refreshTokenService = serviceProvider.GetService<IRefreshTokenService>();
            var authWorker = serviceProvider.GetServices<IHostedService>().OfType<AuthWorker>().FirstOrDefault();

            // Assert
            secretClient.Should().NotBeNull("SecretClient should be registered");
            refreshTokenService.Should().NotBeNull("IRefreshTokenService should be registered");
            authWorker.Should().NotBeNull("AuthWorker should be registered as IHostedService");
        }

        [Fact]
        public void GetService_ShouldReturnSameSecretClientInstance_WhenResolvedMultipleTimes()
        {
            // Arrange
            var serviceProvider = _fixture.ServiceProvider;

            // Act
            var secretClient1 = serviceProvider.GetService<SecretClient>();
            var secretClient2 = serviceProvider.GetService<SecretClient>();

            // Assert
            secretClient1.Should().NotBeNull();
            secretClient2.Should().NotBeNull();
            secretClient1.Should().BeSameAs(secretClient2, "SecretClient should be registered as Singleton");
        }

        [Fact]
        public void GetService_ShouldReturnDifferentRefreshTokenServiceInstances_WhenResolvedMultipleTimes()
        {
            // Arrange
            // Services registered with AddHttpClient are Transient by default
            var serviceProvider = _fixture.ServiceProvider;

            // Act
            var refreshTokenService1 = serviceProvider.GetService<IRefreshTokenService>();
            var refreshTokenService2 = serviceProvider.GetService<IRefreshTokenService>();

            // Assert
            refreshTokenService1.Should().NotBeNull();
            refreshTokenService2.Should().NotBeNull();
            refreshTokenService1.Should().NotBeSameAs(refreshTokenService2, 
                "IRefreshTokenService should be registered as Transient (via AddHttpClient)");
        }

        [Fact]
        public void GetServices_ShouldReturnSingleRefreshTokenServiceRegistration_WhenResolved()
        {
            // Arrange
            var serviceProvider = _fixture.ServiceProvider;

            // Act
            var registrations = serviceProvider.GetServices<IRefreshTokenService>().ToList();

            // Assert
            registrations.Should().HaveCount(1, 
                "IRefreshTokenService should have exactly one registration (no duplicate AddScoped + AddHttpClient)");
        }

        [Fact]
        public void GetService_ShouldResolveRefreshTokenService_WhenHttpClientResilienceHandlerIsRegistered()
        {
            // Arrange
            var serviceProvider = _fixture.ServiceProvider;

            // Act
            // The service is registered with AddHttpClient().AddStandardResilienceHandler(), so
            // resolving it proves the HttpClient factory pipeline is configured correctly.
            // The actual resilience handler behaviour is tested in E2E tests.
            var refreshTokenService = serviceProvider.GetService<IRefreshTokenService>();

            // Assert
            refreshTokenService.Should().NotBeNull("IRefreshTokenService should be registered");
        }
    }
}
