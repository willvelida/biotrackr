using System.Net;
using System.Text.Json;
using AutoFixture;
using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Models;
using Biotrackr.Reporting.Svc.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Biotrackr.Reporting.Svc.UnitTests.Services;

public class ReportingApiServiceShould
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ReportingApiService>> _loggerMock;
    private readonly Fixture _fixture;

    public ReportingApiServiceShould()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ReportingApiService>>();
        _fixture = new Fixture();
    }

    private ReportingApiService CreateService(int pollIntervalSeconds = 1, int timeoutMinutes = 1)
    {
        var settings = new Settings
        {
            ReportPollIntervalSeconds = pollIntervalSeconds,
            ReportTimeoutMinutes = timeoutMinutes,
            ReportingApiUrl = "https://reporting.example.com"
        };
        var options = Options.Create(settings);
        return new ReportingApiService(_httpClientFactoryMock.Object, options, _loggerMock.Object);
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(params HttpResponseMessage[] responses)
    {
        var mock = new Mock<HttpMessageHandler>();
        var sequence = mock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        foreach (var response in responses)
        {
            sequence.ReturnsAsync(response);
        }

        return mock;
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldReturnResult_WhenReportGeneratedImmediately()
    {
        // Arrange
        var jobResult = new ReportJobResult { JobId = "job-123", Status = "accepted" };
        var statusResponse = new ReportStatusResponse
        {
            Metadata = new ReportMetadata { JobId = "job-123", Status = "generated", Summary = "Test summary" },
            ArtifactUrls = new Dictionary<string, string> { ["report.pdf"] = "https://storage.example.com/report.pdf" }
        };

        var submitResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(jobResult), System.Text.Encoding.UTF8, "application/json")
        };
        var pollResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(statusResponse), System.Text.Encoding.UTF8, "application/json")
        };

        var handler = CreateMockHandler(submitResponse, pollResponse);
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://reporting.example.com") };

        _httpClientFactoryMock.Setup(x => x.CreateClient("ReportingApi")).Returns(httpClient);

        var sut = CreateService();
        var snapshot = _fixture.Create<HealthDataSnapshot>();

        // Act
        var result = await sut.GenerateReportAsync("weekly_summary", "2025-01-01", "2025-01-07", "Generate weekly", snapshot, CancellationToken.None);

        // Assert
        result.JobId.Should().Be("job-123");
        result.Status.Should().Be("generated");
        result.Summary.Should().Be("Test summary");
        result.PdfUrl.Should().Be("https://storage.example.com/report.pdf");
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldPollUntilComplete_WhenReportIsGenerating()
    {
        // Arrange
        var jobResult = new ReportJobResult { JobId = "job-456", Status = "accepted" };
        var pendingResponse = new ReportStatusResponse
        {
            Metadata = new ReportMetadata { JobId = "job-456", Status = "generating" },
            ArtifactUrls = []
        };
        var completedResponse = new ReportStatusResponse
        {
            Metadata = new ReportMetadata { JobId = "job-456", Status = "reviewed", Summary = "Final summary" },
            ArtifactUrls = new Dictionary<string, string> { ["report.pdf"] = "https://storage.example.com/final.pdf" }
        };

        var handler = CreateMockHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(jobResult), System.Text.Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(pendingResponse), System.Text.Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(completedResponse), System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://reporting.example.com") };
        _httpClientFactoryMock.Setup(x => x.CreateClient("ReportingApi")).Returns(httpClient);

        var sut = CreateService();
        var snapshot = _fixture.Create<HealthDataSnapshot>();

        // Act
        var result = await sut.GenerateReportAsync("weekly_summary", "2025-01-01", "2025-01-07", "Generate weekly", snapshot, CancellationToken.None);

        // Assert
        result.JobId.Should().Be("job-456");
        result.Status.Should().Be("reviewed");
        result.Summary.Should().Be("Final summary");
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldContinuePolling_WhenStatusReturns404()
    {
        // Arrange
        var jobResult = new ReportJobResult { JobId = "job-404", Status = "accepted" };
        var completedResponse = new ReportStatusResponse
        {
            Metadata = new ReportMetadata { JobId = "job-404", Status = "generated", Summary = "After 404" },
            ArtifactUrls = new Dictionary<string, string> { ["report.pdf"] = "https://storage.example.com/report.pdf" }
        };

        var handler = CreateMockHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(jobResult), System.Text.Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(completedResponse), System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://reporting.example.com") };
        _httpClientFactoryMock.Setup(x => x.CreateClient("ReportingApi")).Returns(httpClient);

        var sut = CreateService();
        var snapshot = _fixture.Create<HealthDataSnapshot>();

        // Act
        var result = await sut.GenerateReportAsync("weekly_summary", "2025-01-01", "2025-01-07", "Generate weekly", snapshot, CancellationToken.None);

        // Assert
        result.JobId.Should().Be("job-404");
        result.Status.Should().Be("generated");
        result.Summary.Should().Be("After 404");
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldThrow_WhenReportGenerationFails()
    {
        // Arrange
        var jobResult = new ReportJobResult { JobId = "job-789", Status = "accepted" };
        var failedResponse = new ReportStatusResponse
        {
            Metadata = new ReportMetadata { JobId = "job-789", Status = "failed", Error = "Generation error" },
            ArtifactUrls = []
        };

        var handler = CreateMockHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(jobResult), System.Text.Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(failedResponse), System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://reporting.example.com") };
        _httpClientFactoryMock.Setup(x => x.CreateClient("ReportingApi")).Returns(httpClient);

        var sut = CreateService();
        var snapshot = _fixture.Create<HealthDataSnapshot>();

        // Act
        var act = () => sut.GenerateReportAsync("weekly_summary", "2025-01-01", "2025-01-07", "Generate weekly", snapshot, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*failed*job-789*");
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldThrow_WhenSubmissionReturnsError()
    {
        // Arrange
        var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://reporting.example.com") };
        _httpClientFactoryMock.Setup(x => x.CreateClient("ReportingApi")).Returns(httpClient);

        var sut = CreateService();
        var snapshot = _fixture.Create<HealthDataSnapshot>();

        // Act
        var act = () => sut.GenerateReportAsync("weekly_summary", "2025-01-01", "2025-01-07", "Generate weekly", snapshot, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldReturnNullPdfUrl_WhenNoArtifacts()
    {
        // Arrange
        var jobResult = new ReportJobResult { JobId = "job-nopdf", Status = "accepted" };
        var statusResponse = new ReportStatusResponse
        {
            Metadata = new ReportMetadata { JobId = "job-nopdf", Status = "generated", Summary = "No PDF" },
            ArtifactUrls = []
        };

        var handler = CreateMockHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(jobResult), System.Text.Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(statusResponse), System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://reporting.example.com") };
        _httpClientFactoryMock.Setup(x => x.CreateClient("ReportingApi")).Returns(httpClient);

        var sut = CreateService();
        var snapshot = _fixture.Create<HealthDataSnapshot>();

        // Act
        var result = await sut.GenerateReportAsync("weekly_summary", "2025-01-01", "2025-01-07", "Generate weekly", snapshot, CancellationToken.None);

        // Assert
        result.PdfUrl.Should().BeNull();
    }

    [Fact]
    public async Task DownloadArtifactAsync_ShouldReturnBytes_WhenSasUrlIsValid()
    {
        // Arrange
        var expectedBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedBytes)
        });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("ArtifactDownload")).Returns(httpClient);

        var sut = CreateService();

        // Act
        var result = await sut.DownloadArtifactAsync("https://storage.example.com/report.pdf?sas=token", CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedBytes);
    }

    [Fact]
    public async Task DownloadArtifactAsync_ShouldThrow_WhenDownloadFails()
    {
        // Arrange
        var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("ArtifactDownload")).Returns(httpClient);

        var sut = CreateService();

        // Act
        var act = () => sut.DownloadArtifactAsync("https://storage.example.com/missing.pdf", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
