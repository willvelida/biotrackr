using System.Net;
using System.Net.Http.Json;
using Biotrackr.Reporting.Api.Endpoints;
using Biotrackr.Reporting.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Moq;

namespace Biotrackr.Reporting.Api.IntegrationTests.Contract;

/// <summary>
/// Contract tests for PUT /api/reports/{jobId}/review endpoint.
/// Verifies routing, error handling, authentication, and service integration.
/// </summary>
public class SubmitReviewTests : IClassFixture<ReportingApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ReportingApiWebApplicationFactory _factory;

    public SubmitReviewTests(ReportingApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SubmitReview_ShouldReturnNoContent_WhenJobIsGenerated()
    {
        // Arrange
        var jobId = "contract-test-job-1";
        var request = new SubmitReviewRequest
        {
            Approved = true,
            Concerns = ["minor note about step count"],
            ValidatedSummary = "Weekly summary is accurate"
        };

        _factory.MockBlobStorageService
            .Setup(s => s.UpdateReviewResultAsync(jobId, request.Approved, request.Concerns, request.ValidatedSummary))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/reports/{jobId}/review", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SubmitReview_ShouldReturnNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var jobId = "nonexistent-job";
        var request = new SubmitReviewRequest
        {
            Approved = true,
            Concerns = [],
            ValidatedSummary = "Summary"
        };

        _factory.MockBlobStorageService
            .Setup(s => s.UpdateReviewResultAsync(jobId, It.IsAny<bool>(), It.IsAny<List<string>>(), It.IsAny<string>()))
            .ThrowsAsync(new KeyNotFoundException($"Job {jobId} not found"));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/reports/{jobId}/review", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task SubmitReview_ShouldReturnConflict_WhenJobIsGenerating()
    {
        // Arrange
        var jobId = "generating-job";
        var request = new SubmitReviewRequest
        {
            Approved = true,
            Concerns = [],
            ValidatedSummary = "Summary"
        };

        _factory.MockBlobStorageService
            .Setup(s => s.UpdateReviewResultAsync(jobId, It.IsAny<bool>(), It.IsAny<List<string>>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException($"Job {jobId} is not in generated status (current: generating)"));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/reports/{jobId}/review", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not in generated status");
    }

    [Fact]
    public async Task SubmitReview_ShouldReturnConflict_WhenJobIsFailed()
    {
        // Arrange
        var jobId = "failed-job";
        var request = new SubmitReviewRequest
        {
            Approved = false,
            Concerns = ["job failed"],
            ValidatedSummary = "N/A"
        };

        _factory.MockBlobStorageService
            .Setup(s => s.UpdateReviewResultAsync(jobId, It.IsAny<bool>(), It.IsAny<List<string>>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException($"Job {jobId} is not in generated status (current: failed)"));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/reports/{jobId}/review", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SubmitReview_ShouldReturnConflict_WhenJobIsAlreadyReviewed()
    {
        // Arrange
        var jobId = "reviewed-job";
        var request = new SubmitReviewRequest
        {
            Approved = true,
            Concerns = [],
            ValidatedSummary = "Already reviewed"
        };

        _factory.MockBlobStorageService
            .Setup(s => s.UpdateReviewResultAsync(jobId, It.IsAny<bool>(), It.IsAny<List<string>>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException($"Job {jobId} is not in generated status (current: reviewed)"));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/reports/{jobId}/review", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SubmitReview_ShouldHandleWhitespaceJobId()
    {
        // Arrange — whitespace jobId may be caught by routing or handler
        var request = new SubmitReviewRequest
        {
            Approved = true,
            Concerns = [],
            ValidatedSummary = "Summary"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/reports/%20/review", request);

        // Assert — expect 400 from handler's whitespace check
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
