using System.Net;
using Biotrackr.Reporting.Api.IntegrationTests.Fixtures;
using Biotrackr.Reporting.Api.Models;
using FluentAssertions;
using Moq;

namespace Biotrackr.Reporting.Api.IntegrationTests.Contract;

/// <summary>
/// Integration tests for GET /api/reports and GET /api/reports/{jobId} endpoints.
/// Verifies routing, query parameters, authentication, and service integration.
/// </summary>
public class ReportRetrievalTests : IClassFixture<ReportingApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ReportingApiWebApplicationFactory _factory;

    public ReportRetrievalTests(ReportingApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListReports_ShouldReturn200WithResults()
    {
        _factory.MockBlobStorageService
            .Setup(s => s.ListReportsAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(
            [
                new ReportMetadata
                {
                    JobId = "job-1",
                    ReportType = "weekly_summary",
                    Status = ReportStatus.Generated,
                    DateRange = new ReportDateRange { Start = "2026-03-01", End = "2026-03-07" }
                }
            ]);

        var response = await _client.GetAsync("/api/reports");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("job-1");
        content.Should().Contain("weekly_summary");
    }

    [Fact]
    public async Task ListReports_WithInvalidReportType_ShouldReturn400()
    {
        var response = await _client.GetAsync("/api/reports?reportType=invalid_type");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid report type");
    }

    [Fact]
    public async Task GetReport_ExistingJob_ShouldReturn200WithMetadata()
    {
        _factory.MockBlobStorageService
            .Setup(s => s.GetMetadataAsync("job-123"))
            .ReturnsAsync(new ReportMetadata
            {
                JobId = "job-123",
                ReportType = "weekly_summary",
                Status = ReportStatus.Generating,
                DateRange = new ReportDateRange { Start = "2026-03-01", End = "2026-03-07" }
            });

        var response = await _client.GetAsync("/api/reports/job-123");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("job-123");
        content.Should().Contain("generating");
    }

    [Fact]
    public async Task GetReport_NonExistentJob_ShouldReturn404()
    {
        _factory.MockBlobStorageService
            .Setup(s => s.GetMetadataAsync("nonexistent"))
            .ReturnsAsync((ReportMetadata?)null);

        var response = await _client.GetAsync("/api/reports/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task GetReport_GeneratedWithArtifacts_ShouldReturnSasUrls()
    {
        _factory.MockBlobStorageService
            .Setup(s => s.GetMetadataAsync("job-with-artifacts"))
            .ReturnsAsync(new ReportMetadata
            {
                JobId = "job-with-artifacts",
                ReportType = "weekly_summary",
                Status = ReportStatus.Generated,
                DateRange = new ReportDateRange { Start = "2026-03-01", End = "2026-03-07" },
                Artifacts = ["report.pdf", "chart.png"]
            });

        _factory.MockBlobStorageService
            .Setup(s => s.GetReportSasUrlAsync(It.IsAny<string>()))
            .ReturnsAsync("https://storage.blob.core.windows.net/reports/test?sv=2024&sig=test");

        var response = await _client.GetAsync("/api/reports/job-with-artifacts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("report.pdf");
        content.Should().Contain("chart.png");
        content.Should().Contain("sig=test");
    }
}
