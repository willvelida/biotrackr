using Biotrackr.Reporting.Api.Endpoints;
using Biotrackr.Reporting.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Reporting.Api.UnitTests.Endpoints;

public class ReviewEndpointsShould
{
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
    private readonly Mock<ILogger<Program>> _loggerMock;

    public ReviewEndpointsShould()
    {
        _blobStorageServiceMock = new Mock<IBlobStorageService>();
        _loggerMock = new Mock<ILogger<Program>>();
    }

    [Fact]
    public async Task HandleSubmitReview_ShouldReturnNoContent_WhenReviewIsSuccessful()
    {
        // Arrange
        var jobId = "test-job-123";
        var request = new SubmitReviewRequest
        {
            Approved = true,
            Concerns = ["minor note"],
            ValidatedSummary = "All checks passed"
        };

        _blobStorageServiceMock
            .Setup(s => s.UpdateReviewResultAsync(jobId, request.Approved, request.Concerns, request.ValidatedSummary))
            .Returns(Task.CompletedTask);

        // Act
        var result = await ReviewEndpoints.HandleSubmitReview(
            jobId, request, _blobStorageServiceMock.Object, _loggerMock.Object);

        // Assert
        result.Should().BeOfType<NoContent>();
        _blobStorageServiceMock.Verify(
            s => s.UpdateReviewResultAsync(jobId, true, request.Concerns, request.ValidatedSummary), Times.Once);
    }

    [Fact]
    public async Task HandleSubmitReview_ShouldReturnNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var jobId = "nonexistent-job";
        var request = new SubmitReviewRequest
        {
            Approved = true,
            Concerns = [],
            ValidatedSummary = "Summary"
        };

        _blobStorageServiceMock
            .Setup(s => s.UpdateReviewResultAsync(jobId, It.IsAny<bool>(), It.IsAny<List<string>>(), It.IsAny<string>()))
            .ThrowsAsync(new KeyNotFoundException($"Job {jobId} not found"));

        // Act
        var result = await ReviewEndpoints.HandleSubmitReview(
            jobId, request, _blobStorageServiceMock.Object, _loggerMock.Object);

        // Assert
        var statusCodeResult = result as IStatusCodeHttpResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task HandleSubmitReview_ShouldReturnConflict_WhenJobIsNotGenerated()
    {
        // Arrange
        var jobId = "test-job-456";
        var request = new SubmitReviewRequest
        {
            Approved = false,
            Concerns = ["data mismatch"],
            ValidatedSummary = "Failed validation"
        };

        _blobStorageServiceMock
            .Setup(s => s.UpdateReviewResultAsync(jobId, It.IsAny<bool>(), It.IsAny<List<string>>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException($"Job {jobId} is not in generated status (current: generating)"));

        // Act
        var result = await ReviewEndpoints.HandleSubmitReview(
            jobId, request, _blobStorageServiceMock.Object, _loggerMock.Object);

        // Assert
        var statusCodeResult = result as IStatusCodeHttpResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task HandleSubmitReview_ShouldReturnBadRequest_WhenJobIdIsEmpty()
    {
        // Arrange
        var request = new SubmitReviewRequest
        {
            Approved = true,
            Concerns = [],
            ValidatedSummary = "Summary"
        };

        // Act
        var result = await ReviewEndpoints.HandleSubmitReview(
            " ", request, _blobStorageServiceMock.Object, _loggerMock.Object);

        // Assert
        var statusCodeResult = result as IStatusCodeHttpResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult!.StatusCode.Should().Be(400);
    }
}
