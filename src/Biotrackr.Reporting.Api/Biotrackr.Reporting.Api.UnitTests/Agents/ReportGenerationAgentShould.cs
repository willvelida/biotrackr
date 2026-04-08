using System.Text.Json;
using Biotrackr.Reporting.Api.Agents;
using Biotrackr.Reporting.Api.Endpoints;
using Biotrackr.Reporting.Api.Models;
using Biotrackr.Reporting.Api.Services;
using FluentAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

#pragma warning disable MEAI001

namespace Biotrackr.Reporting.Api.UnitTests.Agents
{
    public class ReportGenerationAgentShould
    {
        private readonly Mock<IReportGenerationService> _reportService;
        private readonly Mock<IBlobStorageService> _blobStorageService;
        private readonly Mock<ILoggerFactory> _loggerFactory;

        public ReportGenerationAgentShould()
        {
            _reportService = new Mock<IReportGenerationService>();
            _blobStorageService = new Mock<IBlobStorageService>();
            _loggerFactory = new Mock<ILoggerFactory>();
            _loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                .Returns(new Mock<ILogger>().Object);
        }

        [Fact]
        public async Task ReturnResponseWithContinuationTokenForValidNewRequest()
        {
            var request = CreateValidRequest();
            var messages = CreateUserMessages(JsonSerializer.Serialize(request));

            _reportService.Setup(s => s.StartReportGenerationAsync(
                    request.ReportType, request.StartDate, request.EndDate,
                    request.TaskMessage, It.IsAny<object>()))
                .ReturnsAsync(new ReportJobResult
                {
                    JobId = "test-job-id",
                    Status = ReportStatus.Generating,
                    Message = "Report generation started"
                });

            var agent = CreateAgent();

            var response = await agent.RunAsync(messages);

            response.ContinuationToken.Should().NotBeNull();
            var continuation = JsonSerializer.Deserialize<ReportJobContinuation>(
                response.ContinuationToken!.ToBytes().Span);
            continuation!.JobId.Should().Be("test-job-id");
        }

        [Fact]
        public async Task ThrowForInvalidJsonInNewRequest()
        {
            var messages = CreateUserMessages("not valid json {{{");
            var agent = CreateAgent();

            var act = () => agent.RunAsync(messages);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Invalid JSON*");
        }

        [Fact]
        public async Task ThrowForValidationFailureWithBadDate()
        {
            var request = CreateValidRequest();
            request.StartDate = "not-a-date";
            var messages = CreateUserMessages(JsonSerializer.Serialize(request));
            var agent = CreateAgent();

            var act = () => agent.RunAsync(messages);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Invalid startDate*");
        }

        [Fact]
        public async Task ThrowWhenServiceReturnsFailedStatus()
        {
            var request = CreateValidRequest();
            var messages = CreateUserMessages(JsonSerializer.Serialize(request));

            _reportService.Setup(s => s.StartReportGenerationAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(new ReportJobResult
                {
                    JobId = "failed-job",
                    Status = ReportStatus.Failed,
                    Message = "Sidecar is unhealthy"
                });

            var agent = CreateAgent();

            var act = () => agent.RunAsync(messages);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Sidecar is unhealthy*");
        }

        [Fact]
        public async Task ReturnResponseWithContinuationTokenWhenGenerating()
        {
            var jobId = "polling-job-id";
            var options = CreatePollOptions(jobId);

            _blobStorageService.Setup(s => s.GetMetadataAsync(jobId))
                .ReturnsAsync(new ReportMetadata
                {
                    JobId = jobId,
                    Status = ReportStatus.Generating
                });

            var agent = CreateAgent();
            var messages = CreateUserMessages("poll");

            var response = await agent.RunAsync(messages, options: options);

            response.ContinuationToken.Should().NotBeNull();
            var continuation = JsonSerializer.Deserialize<ReportJobContinuation>(
                response.ContinuationToken!.ToBytes().Span);
            continuation!.JobId.Should().Be(jobId);
        }

        [Fact]
        public async Task ReturnResponseWithoutContinuationTokenWhenGenerated()
        {
            var jobId = "completed-job-id";
            var options = CreatePollOptions(jobId);

            _blobStorageService.Setup(s => s.GetMetadataAsync(jobId))
                .ReturnsAsync(new ReportMetadata
                {
                    JobId = jobId,
                    Status = ReportStatus.Generated,
                    ReportType = "weekly_summary",
                    DateRange = new ReportDateRange { Start = "2026-03-01", End = "2026-03-07" },
                    Summary = "Test summary"
                });

            var agent = CreateAgent();
            var messages = CreateUserMessages("poll");

            var response = await agent.RunAsync(messages, options: options);

            response.ContinuationToken.Should().BeNull();
        }

        [Fact]
        public async Task ThrowWhenPollReturnsFailed()
        {
            var jobId = "failed-poll-job";
            var options = CreatePollOptions(jobId);

            _blobStorageService.Setup(s => s.GetMetadataAsync(jobId))
                .ReturnsAsync(new ReportMetadata
                {
                    JobId = jobId,
                    Status = ReportStatus.Failed,
                    Error = "Python execution failed"
                });

            var agent = CreateAgent();
            var messages = CreateUserMessages("poll");

            var act = () => agent.RunAsync(messages, options: options);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Report generation failed*");
        }

        [Fact]
        public async Task ThrowForInvalidContinuationToken()
        {
            var invalidBytes = System.Text.Encoding.UTF8.GetBytes("not valid json");
            var token = ResponseContinuationToken.FromBytes(invalidBytes);
            var options = new AgentRunOptions { ContinuationToken = token };
            var agent = CreateAgent();
            var messages = CreateUserMessages("poll");

            var act = () => agent.RunAsync(messages, options: options);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Invalid continuation token*");
        }

        private ReportGenerationAgent CreateAgent()
        {
            return new ReportGenerationAgent(
                "test-agent",
                _reportService.Object,
                _blobStorageService.Object,
                _loggerFactory.Object);
        }

        private static IEnumerable<ChatMessage> CreateUserMessages(string content)
        {
            return [new ChatMessage(ChatRole.User, content)];
        }

        private static AgentRunOptions CreatePollOptions(string jobId)
        {
            var continuation = new ReportJobContinuation { JobId = jobId };
            var tokenBytes = JsonSerializer.SerializeToUtf8Bytes(continuation);
            return new AgentRunOptions
            {
                ContinuationToken = ResponseContinuationToken.FromBytes(tokenBytes)
            };
        }

        private static GenerateReportRequest CreateValidRequest()
        {
            return new GenerateReportRequest
            {
                ReportType = "weekly_summary",
                StartDate = "2026-03-01",
                EndDate = "2026-03-07",
                TaskMessage = "Generate a weekly summary report",
                SourceDataSnapshot = JsonSerializer.Deserialize<JsonElement>(
                    """{"steps":[{"date":"2026-03-01","count":8500}]}""")
            };
        }
    }
}
