namespace Biotrackr.Food.Svc.IntegrationTests.Contract;

/// <summary>
/// Contract tests verifying the application can start successfully with valid configuration.
/// </summary>
[Collection("ContractTests")]
public class ProgramStartupTests
{
    private readonly ContractTestFixture _fixture;

    public ProgramStartupTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Application_ShouldResolveConfiguration_Successfully()
    {
        // Act
        var configuration = _fixture.ServiceProvider.GetService<IConfiguration>();

        // Assert
        configuration.Should().NotBeNull();
        configuration["Biotrackr:DatabaseName"].Should().Be("biotrackr-test");
        configuration["Biotrackr:ContainerName"].Should().Be("food-test");
    }

    [Fact]
    public void Application_ShouldResolveAllRequiredServices_Successfully()
    {
        // Arrange & Act
        var cosmosRepository = _fixture.ServiceProvider.GetService<ICosmosRepository>();
        var foodService = _fixture.ServiceProvider.GetService<IFoodService>();
        var fitbitService = _fixture.ServiceProvider.GetService<IFitbitService>();

        // Assert
        cosmosRepository.Should().NotBeNull();
        foodService.Should().NotBeNull();
        fitbitService.Should().NotBeNull();
    }
}
