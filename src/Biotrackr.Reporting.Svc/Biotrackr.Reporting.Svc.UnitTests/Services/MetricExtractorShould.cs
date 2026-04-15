using System.Text.Json;
using Biotrackr.Reporting.Svc.Models;
using Biotrackr.Reporting.Svc.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Reporting.Svc.UnitTests.Services;

public class MetricExtractorShould
{
    private readonly Mock<ILogger<MetricExtractor>> _loggerMock;

    public MetricExtractorShould()
    {
        _loggerMock = new Mock<ILogger<MetricExtractor>>();
    }

    private MetricExtractor CreateService()
    {
        return new MetricExtractor(_loggerMock.Object);
    }

    [Fact]
    public void ExtractMetrics_ShouldReturnActivityMetrics_WhenActivityDataPresent()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildActivityJson(
                (10000, 30, 15, 65),
                (8000, 20, 10, 60)),
            Sleep = BuildEmptyJson(),
            Food = BuildEmptyJson(),
            Vitals = BuildEmptyJson()
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(c => c.Label == "Total Steps" && c.Value == "18,000");
        result.Should().Contain(c => c.Label == "Active Minutes" && c.Value == "75");
        result.Should().Contain(c => c.Label == "Avg Resting HR" && c.Value == "62");
    }

    [Fact]
    public void ExtractMetrics_ShouldReturnSleepMetrics_WhenSleepDataPresent()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildEmptyJson(),
            Sleep = BuildSleepJson(462, 380),
            Food = BuildEmptyJson(),
            Vitals = BuildEmptyJson()
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(c => c.Label == "Avg Sleep" && c.Value == "7h 1m");
    }

    [Fact]
    public void ExtractMetrics_ShouldReturnFoodMetrics_WhenFoodDataPresent()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildEmptyJson(),
            Sleep = BuildEmptyJson(),
            Food = BuildFoodJson(1800, 2100, 1900, 2000, 1700, 1850, 2050),
            Vitals = BuildEmptyJson()
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(c => c.Label == "Avg Calories" && c.Icon == "🔥");
        result.First().Subtitle.Should().BeNull();
    }

    [Fact]
    public void ExtractMetrics_ShouldReturnVitalsMetrics_WhenWeightDataPresent()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildEmptyJson(),
            Sleep = BuildEmptyJson(),
            Food = BuildEmptyJson(),
            Vitals = BuildVitalsJson(82.5, 82.3, 82.1)
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(c => c.Label == "Latest Weight" && c.Value == "82.1" && c.Unit == "kg");
        result.First().Subtitle.Should().Be("(3 readings)");
    }

    [Fact]
    public void ExtractMetrics_ShouldReturnEmptyList_WhenSnapshotIsEmpty()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildEmptyJson(),
            Sleep = BuildEmptyJson(),
            Food = BuildEmptyJson(),
            Vitals = BuildEmptyJson()
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractMetrics_ShouldReturnPartialMetrics_WhenOnlyActivityPresent()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildActivityJson((5000, 10, 5, 55)),
            Sleep = string.Empty,
            Food = string.Empty,
            Vitals = string.Empty
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(c => c.Label == "Total Steps" || c.Label == "Active Minutes" || c.Label == "Avg Resting HR");
    }

    [Fact]
    public void ExtractMetrics_ShouldFilterZeroHeartRate_WhenRestingHrIsZero()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildActivityJson(
                (10000, 30, 15, 65),
                (8000, 20, 10, 0),
                (9000, 25, 12, 0)),
            Sleep = BuildEmptyJson(),
            Food = BuildEmptyJson(),
            Vitals = BuildEmptyJson()
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        var hrCard = result.FirstOrDefault(c => c.Label == "Avg Resting HR");
        hrCard.Should().NotBeNull();
        hrCard!.Value.Should().Be("65");
    }

    [Fact]
    public void ExtractMetrics_ShouldReturnLatestWeight_WhenMultipleReadingsExist()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildEmptyJson(),
            Sleep = BuildEmptyJson(),
            Food = BuildEmptyJson(),
            Vitals = BuildVitalsJson(83.0, 82.5)
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().HaveCount(1);
        result.First().Value.Should().Be("82.5");
    }

    [Fact]
    public void ExtractMetrics_ShouldFormatSleepAsDuration_WhenMinutesProvided()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildEmptyJson(),
            Sleep = BuildSleepJson(462),
            Food = BuildEmptyJson(),
            Vitals = BuildEmptyJson()
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().HaveCount(1);
        result.First().Value.Should().Be("7h 42m");
    }

    [Fact]
    public void ExtractMetrics_ShouldHandleMissingNestedProperties_WhenJsonIncomplete()
    {
        // Arrange
        var incompleteJson = JsonSerializer.Serialize(new
        {
            items = new object[]
            {
                new { activity = new { } },
                new { other = "data" }
            },
            totalCount = 2
        });
        var snapshot = new HealthDataSnapshot
        {
            Activity = incompleteJson,
            Sleep = BuildEmptyJson(),
            Food = BuildEmptyJson(),
            Vitals = BuildEmptyJson()
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractMetrics_ShouldSumActiveMinutes_WhenFairlyAndVeryActive()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildActivityJson(
                (1000, 30, 15, 60),
                (1000, 30, 15, 60),
                (1000, 30, 15, 60),
                (1000, 30, 15, 60),
                (1000, 30, 15, 60),
                (1000, 30, 15, 60),
                (1000, 30, 15, 60)),
            Sleep = BuildEmptyJson(),
            Food = BuildEmptyJson(),
            Vitals = BuildEmptyJson()
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        var activeCard = result.FirstOrDefault(c => c.Label == "Active Minutes");
        activeCard.Should().NotBeNull();
        activeCard!.Value.Should().Be("315");
    }

    [Fact]
    public void ExtractMetrics_ShouldSetReadingCountSubtitle_WhenSingleWeightReading()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildEmptyJson(),
            Sleep = BuildEmptyJson(),
            Food = BuildEmptyJson(),
            Vitals = BuildVitalsJson(80.0)
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().HaveCount(1);
        result.First().Subtitle.Should().Be("(1 reading)");
    }

    [Fact]
    public void ExtractMetrics_ShouldSetDaysLoggedSubtitle_WhenFoodDataSparse()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildEmptyJson(),
            Sleep = BuildEmptyJson(),
            Food = BuildFoodJson(1800, 2000, 1900),
            Vitals = BuildEmptyJson()
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().HaveCount(1);
        result.First().Subtitle.Should().Be("(3 days logged)");
    }

    [Fact]
    public void ExtractMetrics_ShouldReturnEmptyList_WhenJsonIsNull()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = null!,
            Sleep = null!,
            Food = null!,
            Vitals = null!
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractMetrics_ShouldReturnAllDomainMetrics_WhenAllDataPresent()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildActivityJson((10000, 30, 15, 65)),
            Sleep = BuildSleepJson(420),
            Food = BuildFoodJson(2000, 1800, 2200, 1900, 2100),
            Vitals = BuildVitalsJson(82.0)
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().HaveCount(6);
        result.Should().Contain(c => c.Label == "Total Steps");
        result.Should().Contain(c => c.Label == "Active Minutes");
        result.Should().Contain(c => c.Label == "Avg Resting HR");
        result.Should().Contain(c => c.Label == "Avg Sleep");
        result.Should().Contain(c => c.Label == "Avg Calories");
        result.Should().Contain(c => c.Label == "Latest Weight");
    }

    [Fact]
    public void ExtractMetrics_ShouldHandleMalformedJson_WhenJsonIsInvalid()
    {
        // Arrange
        var snapshot = new HealthDataSnapshot
        {
            Activity = "not valid json",
            Sleep = "{malformed",
            Food = BuildEmptyJson(),
            Vitals = BuildEmptyJson()
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractMetrics_ShouldSkipNullWeightItems_WhenVitalsHaveNullWeight()
    {
        // Arrange
        var vitalsJson = JsonSerializer.Serialize(new
        {
            items = new object[]
            {
                new { weight = new { weight = 82.5 } },
                new { weight = (object?)null },
                new { weight = new { weight = 81.0 } }
            },
            totalCount = 3
        });
        var snapshot = new HealthDataSnapshot
        {
            Activity = BuildEmptyJson(),
            Sleep = BuildEmptyJson(),
            Food = BuildEmptyJson(),
            Vitals = vitalsJson
        };
        var sut = CreateService();

        // Act
        var result = sut.ExtractMetrics(snapshot);

        // Assert
        result.Should().HaveCount(1);
        result.First().Value.Should().Be("81.0");
        result.First().Subtitle.Should().Be("(2 readings)");
    }

    private static string BuildActivityJson(params (int steps, int fairlyActive, int veryActive, int restingHr)[] days)
    {
        var items = days.Select(d => new
        {
            activity = new
            {
                summary = new
                {
                    steps = d.steps,
                    fairlyActiveMinutes = d.fairlyActive,
                    veryActiveMinutes = d.veryActive,
                    restingHeartRate = d.restingHr
                }
            }
        }).ToArray();

        return JsonSerializer.Serialize(new { items, totalCount = items.Length });
    }

    private static string BuildSleepJson(params int[] minutesAsleep)
    {
        var items = minutesAsleep.Select(m => new
        {
            sleep = new
            {
                summary = new
                {
                    totalMinutesAsleep = m
                }
            }
        }).ToArray();

        return JsonSerializer.Serialize(new { items, totalCount = items.Length });
    }

    private static string BuildFoodJson(params double[] calories)
    {
        var items = calories.Select(c => new
        {
            food = new
            {
                summary = new
                {
                    calories = c
                }
            }
        }).ToArray();

        return JsonSerializer.Serialize(new { items, totalCount = items.Length });
    }

    private static string BuildVitalsJson(params double[] weights)
    {
        var items = weights.Select(w => new
        {
            weight = new
            {
                weight = w
            }
        }).ToArray();

        return JsonSerializer.Serialize(new { items, totalCount = items.Length });
    }

    private static string BuildEmptyJson()
    {
        return JsonSerializer.Serialize(new { items = Array.Empty<object>(), totalCount = 0 });
    }
}
