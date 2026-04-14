using System.Text.Json;
using Biotrackr.Reporting.Svc.Services;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Reporting.Svc.UnitTests.Services;

public class HealthDataServiceShould
{
    private readonly Mock<IMcpClientFactory> _mcpClientFactoryMock;
    private readonly Mock<IMcpToolCaller> _mcpToolCallerMock;
    private readonly Mock<ILogger<HealthDataService>> _loggerMock;

    public HealthDataServiceShould()
    {
        _mcpClientFactoryMock = new Mock<IMcpClientFactory>();
        _mcpToolCallerMock = new Mock<IMcpToolCaller>();
        _loggerMock = new Mock<ILogger<HealthDataService>>();

        _mcpClientFactoryMock
            .Setup(x => x.CreateClientAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mcpToolCallerMock.Object);
    }

    private HealthDataService CreateService() =>
        new HealthDataService(_mcpClientFactoryMock.Object, _loggerMock.Object);

    private static string BuildPageResponse(object[] items, bool hasNextPage = false)
    {
        var response = new { items, hasNextPage };
        return JsonSerializer.Serialize(response);
    }

    [Fact]
    public async Task FetchHealthDataAsync_ShouldReturnSnapshot_WhenAllDomainsReturnData()
    {
        // Arrange
        var activityResponse = BuildPageResponse([new { date = "2024-01-01", steps = 5000 }]);
        var foodResponse = BuildPageResponse([new { date = "2024-01-01", calories = 2000 }]);
        var sleepResponse = BuildPageResponse([new { date = "2024-01-01", duration = 480 }]);
        var vitalsResponse = BuildPageResponse([new { date = "2024-01-01", weight = 75.5 }]);

        _mcpToolCallerMock.Setup(x => x.CallToolAsync("GetActivityByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activityResponse);
        _mcpToolCallerMock.Setup(x => x.CallToolAsync("GetFoodByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(foodResponse);
        _mcpToolCallerMock.Setup(x => x.CallToolAsync("GetSleepByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sleepResponse);
        _mcpToolCallerMock.Setup(x => x.CallToolAsync("GetVitalsByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vitalsResponse);

        var service = CreateService();

        // Act
        var result = await service.FetchHealthDataAsync("2024-01-01", "2024-01-07", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Activity.Should().Contain("steps");
        result.Food.Should().Contain("calories");
        result.Sleep.Should().Contain("duration");
        result.Vitals.Should().Contain("weight");
    }

    [Fact]
    public async Task FetchHealthDataAsync_ShouldHandlePagination_WhenMultiplePagesExist()
    {
        // Arrange
        var page1 = BuildPageResponse([new { date = "2024-01-01", steps = 5000 }], hasNextPage: true);
        var page2 = BuildPageResponse([new { date = "2024-01-02", steps = 6000 }], hasNextPage: false);

        var callCount = 0;
        _mcpToolCallerMock
            .Setup(x => x.CallToolAsync("GetActivityByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? page1 : page2;
            });

        // Single-page responses for other domains
        var singlePageResponse = BuildPageResponse([new { value = 1 }]);
        _mcpToolCallerMock.Setup(x => x.CallToolAsync("GetFoodByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(singlePageResponse);
        _mcpToolCallerMock.Setup(x => x.CallToolAsync("GetSleepByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(singlePageResponse);
        _mcpToolCallerMock.Setup(x => x.CallToolAsync("GetVitalsByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(singlePageResponse);

        var service = CreateService();

        // Act
        var result = await service.FetchHealthDataAsync("2024-01-01", "2024-01-07", CancellationToken.None);

        // Assert
        result.Activity.Should().Contain("5000");
        result.Activity.Should().Contain("6000");

        using var doc = JsonDocument.Parse(result.Activity);
        var items = doc.RootElement.GetProperty("items");
        items.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task FetchHealthDataAsync_ShouldHandleEmptyResponse_WhenToolReturnsNull()
    {
        // Arrange
        _mcpToolCallerMock
            .Setup(x => x.CallToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var service = CreateService();

        // Act
        var result = await service.FetchHealthDataAsync("2024-01-01", "2024-01-07", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Activity.Should().Contain("\"totalCount\":0");
        result.Food.Should().Contain("\"totalCount\":0");
        result.Sleep.Should().Contain("\"totalCount\":0");
        result.Vitals.Should().Contain("\"totalCount\":0");
    }

    [Fact]
    public async Task FetchHealthDataAsync_ShouldCallAllFourDomainTools()
    {
        // Arrange
        var response = BuildPageResponse([new { value = 1 }]);
        _mcpToolCallerMock
            .Setup(x => x.CallToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var service = CreateService();

        // Act
        await service.FetchHealthDataAsync("2024-01-01", "2024-01-07", CancellationToken.None);

        // Assert
        _mcpToolCallerMock.Verify(x => x.CallToolAsync("GetActivityByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mcpToolCallerMock.Verify(x => x.CallToolAsync("GetFoodByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mcpToolCallerMock.Verify(x => x.CallToolAsync("GetSleepByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mcpToolCallerMock.Verify(x => x.CallToolAsync("GetVitalsByDateRange", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FetchHealthDataAsync_ShouldThrow_WhenFactoryFails()
    {
        // Arrange
        _mcpClientFactoryMock
            .Setup(x => x.CreateClientAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("MCP connection failed"));

        var service = CreateService();

        // Act
        var act = () => service.FetchHealthDataAsync("2024-01-01", "2024-01-07", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("MCP connection failed");
    }
}
