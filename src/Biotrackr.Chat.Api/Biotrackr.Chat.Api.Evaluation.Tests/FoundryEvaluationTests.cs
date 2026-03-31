using System.Text.Json;
using FluentAssertions;

namespace Biotrackr.Chat.Api.Evaluation.Tests;

/// <summary>
/// Evaluation tests that run against a live Azure AI Foundry project.
/// Requires FOUNDRY_PROJECT_ENDPOINT environment variable.
/// Skipped when endpoint is not configured (e.g., local dev without Azure access).
/// </summary>
[Trait("Category", "Evaluation")]
public class FoundryEvaluationTests
{
    private readonly string? _foundryEndpoint;

    private static readonly string[] RequiredFields = ["query", "response", "context", "ground_truth"];

    public FoundryEvaluationTests()
    {
        _foundryEndpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT");
    }

    [Fact]
    [Trait("Category", "DatasetValidation")]
    public void DatasetFiles_ShouldExist()
    {
        var chatDataset = GetDatasetPath("chat-agent-eval.jsonl");
        var reviewerDataset = GetDatasetPath("report-reviewer-eval.jsonl");

        File.Exists(chatDataset).Should().BeTrue(
            because: $"chat agent dataset should be present at {chatDataset}");
        File.Exists(reviewerDataset).Should().BeTrue(
            because: $"report reviewer dataset should be present at {reviewerDataset}");
    }

    [Theory]
    [InlineData("chat-agent-eval.jsonl", 10)]
    [InlineData("report-reviewer-eval.jsonl", 10)]
    [Trait("Category", "DatasetValidation")]
    public void DatasetFiles_ShouldContainValidJsonl(string fileName, int expectedMinRecords)
    {
        var path = GetDatasetPath(fileName);
        var lines = File.ReadAllLines(path);

        lines.Should().HaveCountGreaterThanOrEqualTo(expectedMinRecords,
            because: $"{fileName} should have at least {expectedMinRecords} records");

        foreach (var line in lines)
        {
            var parseAction = () => JsonDocument.Parse(line);
            var doc = parseAction.Should().NotThrow(
                because: "each line must be valid JSON").Subject;

            foreach (var field in RequiredFields)
            {
                doc.RootElement.TryGetProperty(field, out _).Should().BeTrue(
                    because: $"each record must have a '{field}' field");
            }

            doc.Dispose();
        }
    }

    [Fact(Skip = "Requires live Foundry endpoint — run via evaluation workflow")]
    [Trait("Category", "Evaluation")]
    public async Task ChatAgentEvaluation_ShouldComplete()
    {
        _foundryEndpoint.Should().NotBeNullOrEmpty(
            because: "FOUNDRY_PROJECT_ENDPOINT must be set for live evaluation tests");

        var runner = new FoundryEvaluationRunner(_foundryEndpoint!);
        var datasetPath = GetDatasetPath("chat-agent-eval.jsonl");

        var status = await runner.RunEvaluationAsync(
            "chat-agent-safety-eval", datasetPath, TimeSpan.FromMinutes(15));

        status.Should().Be("completed");
    }

    [Fact(Skip = "Requires live Foundry endpoint — run via evaluation workflow")]
    [Trait("Category", "Evaluation")]
    public async Task ReportReviewerEvaluation_ShouldComplete()
    {
        _foundryEndpoint.Should().NotBeNullOrEmpty(
            because: "FOUNDRY_PROJECT_ENDPOINT must be set for live evaluation tests");

        var runner = new FoundryEvaluationRunner(_foundryEndpoint!);
        var datasetPath = GetDatasetPath("report-reviewer-eval.jsonl");

        var status = await runner.RunEvaluationAsync(
            "report-reviewer-safety-eval", datasetPath, TimeSpan.FromMinutes(15));

        status.Should().Be("completed");
    }

    private static string GetDatasetPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Datasets", fileName);
}
