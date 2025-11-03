namespace Biotrackr.Food.Svc.IntegrationTests.Contract;

/// <summary>
/// Contract tests verifying service lifetime registrations follow correct patterns.
/// </summary>
[Collection("ContractTests")]
public class ServiceRegistrationTests
{
    private readonly ContractTestFixture _fixture;

    public ServiceRegistrationTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void CosmosClient_ShouldBe_RegisteredAsSingleton()
    {
        // Arrange & Act
        var cosmosClient1 = _fixture.ServiceProvider.GetService<CosmosClient>();
        var cosmosClient2 = _fixture.ServiceProvider.GetService<CosmosClient>();

        // Assert
        cosmosClient1.Should().NotBeNull();
        cosmosClient2.Should().NotBeNull();
        cosmosClient1.Should().BeSameAs(cosmosClient2, "CosmosClient should be registered as singleton");
    }

    [Fact]
    public void SecretClient_ShouldBe_RegisteredAsSingleton()
    {
        // Arrange & Act
        var secretClient1 = _fixture.ServiceProvider.GetService<SecretClient>();
        var secretClient2 = _fixture.ServiceProvider.GetService<SecretClient>();

        // Assert
        secretClient1.Should().NotBeNull();
        secretClient2.Should().NotBeNull();
        secretClient1.Should().BeSameAs(secretClient2, "SecretClient should be registered as singleton");
    }

    [Fact]
    public void CosmosRepository_ShouldBe_RegisteredAsScoped()
    {
        // Arrange & Act - Create two scopes and resolve service in each
        using var scope1 = _fixture.ServiceProvider.CreateScope();
        using var scope2 = _fixture.ServiceProvider.CreateScope();

        var repository1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
        var repository2 = scope1.ServiceProvider.GetService<ICosmosRepository>();
        var repository3 = scope2.ServiceProvider.GetService<ICosmosRepository>();

        // Assert
        repository1.Should().NotBeNull();
        repository2.Should().NotBeNull();
        repository3.Should().NotBeNull();

        repository1.Should().BeSameAs(repository2, "same scope should return same instance");
        repository1.Should().NotBeSameAs(repository3, "different scopes should return different instances");
    }

    [Fact]
    public void FoodService_ShouldBe_RegisteredAsScoped()
    {
        // Arrange & Act - Create two scopes and resolve service in each
        using var scope1 = _fixture.ServiceProvider.CreateScope();
        using var scope2 = _fixture.ServiceProvider.CreateScope();

        var service1 = scope1.ServiceProvider.GetService<IFoodService>();
        var service2 = scope1.ServiceProvider.GetService<IFoodService>();
        var service3 = scope2.ServiceProvider.GetService<IFoodService>();

        // Assert
        service1.Should().NotBeNull();
        service2.Should().NotBeNull();
        service3.Should().NotBeNull();

        service1.Should().BeSameAs(service2, "same scope should return same instance");
        service1.Should().NotBeSameAs(service3, "different scopes should return different instances");
    }

    [Fact]
    public void FitbitService_ShouldBe_RegisteredAsTransient()
    {
        // Arrange & Act - Resolve service multiple times
        using var scope = _fixture.ServiceProvider.CreateScope();
        var service1 = scope.ServiceProvider.GetService<IFitbitService>();
        var service2 = scope.ServiceProvider.GetService<IFitbitService>();

        // Assert
        service1.Should().NotBeNull();
        service2.Should().NotBeNull();
        service1.Should().NotBeSameAs(service2, "FitbitService should be transient (managed by HttpClientFactory)");
    }
}
