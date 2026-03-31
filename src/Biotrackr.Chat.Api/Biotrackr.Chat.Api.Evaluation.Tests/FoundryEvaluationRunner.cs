using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Biotrackr.Chat.Api.Evaluation.Tests;

/// <summary>
/// Runs dataset-based evaluations against Azure AI Foundry using safety evaluators
/// and GroundednessProEvaluator. No GPT judge model required.
/// Uses the Foundry OpenAI-compatible REST API (/openai/evals) directly since
/// the .NET SDK v2.0.0-beta.2 has evaluations listed as a Known Issue (will fail).
/// See: .copilot-tracking/research/subagents/2026-03-31/azure-ai-projects-evaluation-api-research.md
/// </summary>
public class FoundryEvaluationRunner
{
    private readonly AIProjectClient _projectClient;
    private readonly string _projectEndpoint;
    private readonly TokenCredential _credential;

    public FoundryEvaluationRunner(string foundryProjectEndpoint)
    {
        _projectEndpoint = foundryProjectEndpoint.TrimEnd('/');
        _credential = new DefaultAzureCredential();
        _projectClient = new AIProjectClient(
            new Uri(_projectEndpoint), _credential);
    }

    /// <summary>
    /// Uploads a JSONL dataset file to the Foundry project.
    /// Returns the <see cref="FileDataset"/> with its ID for use in evaluations.
    /// </summary>
    public async Task<FileDataset> UploadDatasetAsync(string datasetPath)
    {
        var datasetName = $"eval-{Path.GetFileNameWithoutExtension(datasetPath)}";
        var version = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        return await _projectClient.Datasets.UploadFileAsync(
            name: datasetName,
            version: version,
            filePath: datasetPath);
    }

    /// <summary>
    /// Creates an evaluation definition with safety + groundedness evaluators.
    /// Returns the evaluation ID.
    /// </summary>
    public async Task<string> CreateEvaluationAsync(string evaluationName)
    {
        object[] testingCriteria =
        [
            new
            {
                type = "azure_ai_evaluator",
                name = "violence_detection",
                evaluator_name = "builtin.violence",
                data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" }
            },
            new
            {
                type = "azure_ai_evaluator",
                name = "self_harm_detection",
                evaluator_name = "builtin.self_harm",
                data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" }
            },
            new
            {
                type = "azure_ai_evaluator",
                name = "sexual_content_detection",
                evaluator_name = "builtin.sexual",
                data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" }
            },
            new
            {
                type = "azure_ai_evaluator",
                name = "hate_unfairness_detection",
                evaluator_name = "builtin.hate_unfairness",
                data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" }
            },
            new
            {
                type = "azure_ai_evaluator",
                name = "groundedness_pro",
                evaluator_name = "builtin.groundedness_pro",
                data_mapping = new
                {
                    query = "{{item.query}}",
                    response = "{{item.response}}",
                    context = "{{item.context}}"
                }
            },
        ];

        object dataSourceConfig = new
        {
            type = "custom",
            item_schema = new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string" },
                    response = new { type = "string" },
                    context = new { type = "string" },
                    ground_truth = new { type = "string" }
                },
                required = new[] { "query", "response", "context" }
            }
        };

        var payload = new
        {
            name = evaluationName,
            data_source_config = dataSourceConfig,
            testing_criteria = testingCriteria
        };

        var response = await PostToFoundryAsync("openai/evals", payload);

        using var doc = JsonDocument.Parse(response);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    /// <summary>
    /// Runs an evaluation against an uploaded dataset.
    /// Returns the evaluation run ID.
    /// </summary>
    public async Task<string> CreateEvaluationRunAsync(
        string evaluationId, string datasetId, string runName)
    {
        var payload = new
        {
            name = runName,
            data_source = new
            {
                type = "jsonl",
                source = new
                {
                    type = "file_id",
                    id = datasetId
                }
            },
            metadata = new
            {
                team = "biotrackr-genaiops",
                scenario = "dataset-evaluation"
            }
        };

        var response = await PostToFoundryAsync(
            $"openai/evals/{evaluationId}/runs", payload);

        using var doc = JsonDocument.Parse(response);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    /// <summary>
    /// Polls an evaluation run until it reaches a terminal state.
    /// Returns "completed", "failed", or "timeout".
    /// </summary>
    public async Task<string> WaitForCompletionAsync(
        string evaluationId, string runId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromMinutes(10));

        while (DateTime.UtcNow < deadline)
        {
            var response = await GetFromFoundryAsync(
                $"openai/evals/{evaluationId}/runs/{runId}");

            using var doc = JsonDocument.Parse(response);
            var status = doc.RootElement.GetProperty("status").GetString()!;

            if (status is "completed" or "failed")
                return status;

            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        return "timeout";
    }

    /// <summary>
    /// Convenience method: uploads dataset, creates evaluation, runs it, and waits.
    /// Returns the final status.
    /// </summary>
    public async Task<string> RunEvaluationAsync(
        string evaluationName, string datasetPath, TimeSpan? timeout = null)
    {
        var dataset = await UploadDatasetAsync(datasetPath);
        var evaluationId = await CreateEvaluationAsync(evaluationName);
        var runId = await CreateEvaluationRunAsync(
            evaluationId, dataset.Id, $"{evaluationName}-run");
        return await WaitForCompletionAsync(evaluationId, runId, timeout);
    }

    private async Task<string> PostToFoundryAsync(string path, object payload)
    {
        using var httpClient = await CreateAuthenticatedClientAsync();
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(
            $"{_projectEndpoint}/{path}?api-version=2025-11-15-preview", content);

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Foundry API {path} returned {response.StatusCode}: {body}");
        }

        return body;
    }

    private async Task<string> GetFromFoundryAsync(string path)
    {
        using var httpClient = await CreateAuthenticatedClientAsync();

        var response = await httpClient.GetAsync(
            $"{_projectEndpoint}/{path}?api-version=2025-11-15-preview");

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Foundry API {path} returned {response.StatusCode}: {body}");
        }

        return body;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var token = await _credential.GetTokenAsync(
            new TokenRequestContext(["https://ai.azure.com/.default"]),
            CancellationToken.None);

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);

        return httpClient;
    }
}
