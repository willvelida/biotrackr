using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Biotrackr.Reporting.Api.Endpoints;
using Biotrackr.Reporting.Api.Models;
using Biotrackr.Reporting.Api.Services;
using Biotrackr.Reporting.Api.Validation;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // ResponseContinuationToken is in preview

namespace Biotrackr.Reporting.Api.Agents
{
    [ExcludeFromCodeCoverage]
    public class ReportGenerationAgent : AIAgent
    {
        private readonly IReportGenerationService _reportService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<ReportGenerationAgent> _logger;

        public ReportGenerationAgent(
            string name,
            IReportGenerationService reportService,
            IBlobStorageService blobStorageService,
            ILoggerFactory loggerFactory)
        {
            _reportService = reportService;
            _blobStorageService = blobStorageService;
            _logger = loggerFactory.CreateLogger<ReportGenerationAgent>();
            Name = name;
        }

        protected override string? IdCore => "report-generation-agent";
        public override string? Name { get; }
        public override string? Description =>
            "Generates health reports using Python via GitHub Copilot SDK. Accepts structured health data with natural language instructions and produces PDF reports and chart images.";

        protected override ValueTask<AgentSession> CreateSessionCoreAsync(
            CancellationToken cancellationToken = default)
            => new(new ReportAgentSession());

        protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session,
            AgentRunOptions? options,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Streaming is not supported by ReportGenerationAgent.");

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            JsonSerializerOptions? jsonSerializerOptions,
            CancellationToken cancellationToken = default)
            => new(JsonSerializer.SerializeToElement(session, jsonSerializerOptions));

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement sessionData,
            JsonSerializerOptions? jsonSerializerOptions,
            CancellationToken cancellationToken = default)
            => new(JsonSerializer.Deserialize<ReportAgentSession>(sessionData, jsonSerializerOptions) ?? new ReportAgentSession());

        protected override async Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session,
            AgentRunOptions? options,
            CancellationToken cancellationToken = default)
        {
            // POLL FLOW: if a ContinuationToken is present, this is a status check
            if (options?.ContinuationToken is { } continuationToken)
            {
                return await HandleStatusPollAsync(continuationToken, cancellationToken);
            }

            // NEW REQUEST FLOW: parse the incoming message as a report generation request
            return await HandleNewRequestAsync(messages, cancellationToken);
        }

        private async Task<AgentResponse> HandleNewRequestAsync(
            IEnumerable<ChatMessage> messages,
            CancellationToken cancellationToken)
        {
            var userMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
            if (userMessage is null)
            {
                throw new InvalidOperationException("No user message found in the request.");
            }

            var messageText = userMessage.Text;
            if (string.IsNullOrWhiteSpace(messageText))
            {
                throw new InvalidOperationException("User message text is empty.");
            }

            GenerateReportRequest request;
            try
            {
                request = JsonSerializer.Deserialize<GenerateReportRequest>(messageText)
                    ?? throw new InvalidOperationException("Failed to deserialize report request.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid JSON in report request: {ex.Message}", ex);
            }

            // Validate request using shared validator
            var validation = ReportRequestValidator.Validate(request);
            if (!validation.IsValid)
            {
                if (validation.WarningMessage is not null)
                {
                    _logger.LogWarning("A2A report request validation warning: {Warning}", validation.WarningMessage);
                }
                throw new InvalidOperationException(validation.ErrorMessage);
            }

            // Delegate to ReportGenerationService (fires background Task.Run internally)
            var result = await _reportService.StartReportGenerationAsync(
                request.ReportType,
                request.StartDate,
                request.EndDate,
                request.TaskMessage,
                request.SourceDataSnapshot ?? new object());

            if (result.Status == ReportStatus.Failed)
            {
                throw new InvalidOperationException(result.Message);
            }

            _logger.LogInformation("A2A report generation started. Job {JobId}", result.JobId);

            // Return response with ContinuationToken containing the jobId for polling
            var continuation = new ReportJobContinuation { JobId = result.JobId };
            var tokenBytes = JsonSerializer.SerializeToUtf8Bytes(continuation);

            var response = new AgentResponse(new ChatMessage(ChatRole.Assistant,
                $"Report generation started. Job ID: {result.JobId}"))
            {
                ContinuationToken = ResponseContinuationToken.FromBytes(tokenBytes)
            };

            return response;
        }

        private async Task<AgentResponse> HandleStatusPollAsync(
            ResponseContinuationToken continuationToken,
            CancellationToken cancellationToken)
        {
            var tokenBytes = continuationToken.ToBytes();
            ReportJobContinuation? continuation;
            try
            {
                continuation = JsonSerializer.Deserialize<ReportJobContinuation>(tokenBytes.Span);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid continuation token: {ex.Message}", ex);
            }

            if (continuation is null || string.IsNullOrEmpty(continuation.JobId))
            {
                throw new InvalidOperationException("Continuation token missing JobId.");
            }

            var metadata = await _blobStorageService.GetMetadataAsync(continuation.JobId);
            if (metadata is null)
            {
                throw new InvalidOperationException($"Report job {continuation.JobId} not found.");
            }

            return metadata.Status switch
            {
                ReportStatus.Generating => CreatePollingResponse(continuation, metadata),
                ReportStatus.Generated or ReportStatus.Reviewed => await CreateCompletedResponseAsync(metadata),
                ReportStatus.Failed => throw new InvalidOperationException(
                    $"Report generation failed: {metadata.Error ?? "Unknown error"}"),
                _ => throw new InvalidOperationException(
                    $"Unexpected report status: {metadata.Status}")
            };
        }

        private static AgentResponse CreatePollingResponse(
            ReportJobContinuation continuation, ReportMetadata metadata)
        {
            var tokenBytes = JsonSerializer.SerializeToUtf8Bytes(continuation);

            return new AgentResponse(new ChatMessage(ChatRole.Assistant,
                $"Report generation in progress. Job ID: {continuation.JobId}, Status: {metadata.Status}"))
            {
                ContinuationToken = ResponseContinuationToken.FromBytes(tokenBytes)
            };
        }

        private async Task<AgentResponse> CreateCompletedResponseAsync(ReportMetadata metadata)
        {
            var resultBuilder = new StringBuilder();
            resultBuilder.AppendLine($"Report generation completed. Job ID: {metadata.JobId}");
            resultBuilder.AppendLine($"Report Type: {metadata.ReportType}");
            resultBuilder.AppendLine($"Date Range: {metadata.DateRange.Start} to {metadata.DateRange.End}");

            if (!string.IsNullOrEmpty(metadata.Summary))
            {
                resultBuilder.AppendLine($"Summary: {metadata.Summary}");
            }

            if (metadata.Artifacts.Count > 0 && !string.IsNullOrEmpty(metadata.BlobPath))
            {
                resultBuilder.AppendLine("Artifacts:");
                foreach (var artifact in metadata.Artifacts)
                {
                    var artifactPath = $"{metadata.BlobPath}/{artifact}";
                    var sasUrl = await _blobStorageService.GetReportSasUrlAsync(artifactPath);
                    resultBuilder.AppendLine($"  - {artifact}: {sasUrl}");
                }
            }

            // No ContinuationToken → signals task completion
            return new AgentResponse(new ChatMessage(ChatRole.Assistant, resultBuilder.ToString()));
        }
    }
}
