using System.Text.Json;
using Biotrackr.Reporting.Svc.Models;
using FluentAssertions;

namespace Biotrackr.Reporting.Svc.UnitTests.Models;

public class ModelsShould
{
    [Fact]
    public void GenerateReportRequest_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var request = new GenerateReportRequest();

        // Assert
        request.ReportType.Should().BeEmpty();
        request.StartDate.Should().BeEmpty();
        request.EndDate.Should().BeEmpty();
        request.TaskMessage.Should().BeEmpty();
        request.SourceDataSnapshot.Should().BeNull();
    }

    [Fact]
    public void GenerateReportRequest_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var request = new GenerateReportRequest
        {
            ReportType = "weekly_summary",
            StartDate = "2026-01-01",
            EndDate = "2026-01-07",
            TaskMessage = "Generate weekly report",
            SourceDataSnapshot = JsonSerializer.Deserialize<JsonElement>("{\"steps\": 5000}")
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<GenerateReportRequest>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ReportType.Should().Be("weekly_summary");
        deserialized.StartDate.Should().Be("2026-01-01");
        deserialized.EndDate.Should().Be("2026-01-07");
        deserialized.TaskMessage.Should().Be("Generate weekly report");
        deserialized.SourceDataSnapshot.Should().NotBeNull();
    }

    [Fact]
    public void HealthDataSnapshot_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var snapshot = new HealthDataSnapshot();

        // Assert
        snapshot.Activity.Should().BeEmpty();
        snapshot.Food.Should().BeEmpty();
        snapshot.Sleep.Should().BeEmpty();
        snapshot.Vitals.Should().BeEmpty();
    }

    [Fact]
    public void HealthDataSnapshot_ShouldSetProperties()
    {
        // Arrange & Act
        var snapshot = new HealthDataSnapshot
        {
            Activity = "{\"steps\": 10000}",
            Food = "{\"calories\": 2000}",
            Sleep = "{\"hours\": 8}",
            Vitals = "{\"weight\": 75.5}"
        };

        // Assert
        snapshot.Activity.Should().Be("{\"steps\": 10000}");
        snapshot.Food.Should().Be("{\"calories\": 2000}");
        snapshot.Sleep.Should().Be("{\"hours\": 8}");
        snapshot.Vitals.Should().Be("{\"weight\": 75.5}");
    }

    [Fact]
    public void MetricCard_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var card = new MetricCard();

        // Assert
        card.Label.Should().BeEmpty();
        card.Value.Should().BeEmpty();
        card.Unit.Should().BeEmpty();
        card.Icon.Should().BeEmpty();
        card.Color.Should().BeEmpty();
        card.Subtitle.Should().BeNull();
    }

    [Fact]
    public void MetricCard_ShouldSetAllProperties()
    {
        // Arrange & Act
        var card = new MetricCard
        {
            Label = "Steps",
            Value = "10000",
            Unit = "steps",
            Icon = "walking",
            Color = "#4CAF50",
            Subtitle = "Daily average"
        };

        // Assert
        card.Label.Should().Be("Steps");
        card.Value.Should().Be("10000");
        card.Unit.Should().Be("steps");
        card.Icon.Should().Be("walking");
        card.Color.Should().Be("#4CAF50");
        card.Subtitle.Should().Be("Daily average");
    }

    [Fact]
    public void SummaryResult_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var result = new SummaryResult();

        // Assert
        result.JobId.Should().BeEmpty();
        result.Status.Should().BeEmpty();
        result.Summary.Should().BeNull();
        result.PdfUrl.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void SummaryResult_ShouldSetAllProperties()
    {
        // Arrange & Act
        var result = new SummaryResult
        {
            JobId = "job-123",
            Status = "completed",
            Summary = "Weekly health summary",
            PdfUrl = "https://example.com/report.pdf",
            Error = null
        };

        // Assert
        result.JobId.Should().Be("job-123");
        result.Status.Should().Be("completed");
        result.Summary.Should().Be("Weekly health summary");
        result.PdfUrl.Should().Be("https://example.com/report.pdf");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ReportJobResult_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var jobResult = new ReportJobResult
        {
            JobId = "abc-123",
            Status = "generating",
            Message = "Report generation started"
        };

        // Act
        var json = JsonSerializer.Serialize(jobResult);
        var deserialized = JsonSerializer.Deserialize<ReportJobResult>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.JobId.Should().Be("abc-123");
        deserialized.Status.Should().Be("generating");
        deserialized.Message.Should().Be("Report generation started");
    }

    [Fact]
    public void ReportStatusResponse_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var response = new ReportStatusResponse();

        // Assert
        response.Metadata.Should().NotBeNull();
        response.ArtifactUrls.Should().NotBeNull();
        response.ArtifactUrls.Should().BeEmpty();
    }

    [Fact]
    public void ReportMetadata_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var metadata = new ReportMetadata
        {
            JobId = "job-456",
            Status = "generated",
            ReportType = "weekly_summary",
            Summary = "Good week overall",
            Error = null
        };

        // Act
        var json = JsonSerializer.Serialize(metadata);
        var deserialized = JsonSerializer.Deserialize<ReportMetadata>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.JobId.Should().Be("job-456");
        deserialized.Status.Should().Be("generated");
        deserialized.ReportType.Should().Be("weekly_summary");
        deserialized.Summary.Should().Be("Good week overall");
        deserialized.Error.Should().BeNull();
    }

    [Fact]
    public void ReportStatusResponse_ShouldSerializeWithArtifacts()
    {
        // Arrange
        var response = new ReportStatusResponse
        {
            Metadata = new ReportMetadata
            {
                JobId = "job-789",
                Status = "generated",
                ReportType = "monthly_summary"
            },
            ArtifactUrls = new Dictionary<string, string>
            {
                ["report.pdf"] = "https://example.com/report.pdf",
                ["chart.png"] = "https://example.com/chart.png"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<ReportStatusResponse>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Metadata.JobId.Should().Be("job-789");
        deserialized.ArtifactUrls.Should().HaveCount(2);
        deserialized.ArtifactUrls["report.pdf"].Should().Be("https://example.com/report.pdf");
    }
}
