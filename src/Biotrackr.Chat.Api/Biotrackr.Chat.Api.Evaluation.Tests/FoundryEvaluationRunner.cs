using Azure.AI.Projects;
using Azure.Identity;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;

namespace Biotrackr.Chat.Api.Evaluation.Tests;

/// <summary>
/// Runs dataset-based evaluations against Azure AI Foundry using safety evaluators
/// and GroundednessProEvaluator. No GPT judge model required.
/// Uses the Schedules API with a one-time trigger for ad-hoc evaluation runs.
/// </summary>
public class FoundryEvaluationRunner
{
    private readonly AIProjectClient _projectClient;

    public FoundryEvaluationRunner(string foundryEndpoint)
    {
        _projectClient = new AIProjectClient(
            new Uri(foundryEndpoint), new DefaultAzureCredential());
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
    /// Creates an evaluation schedule with safety + groundedness evaluators
    /// using a one-time trigger. Returns the schedule ID for polling.
    /// </summary>
    public async Task<string> CreateEvaluationScheduleAsync(
        string evaluationName, string datasetId)
    {
        var evalConfig = new
        {
            evaluators = new Dictionary<string, object>
            {
                ["violence"] = new
                {
                    id = "azureai://built-in-evaluators/violence",
                    init_params = new { },
                    data_mapping = new { query = "${data.query}", response = "${data.response}" }
                },
                ["self_harm"] = new
                {
                    id = "azureai://built-in-evaluators/self-harm",
                    init_params = new { },
                    data_mapping = new { query = "${data.query}", response = "${data.response}" }
                },
                ["sexual"] = new
                {
                    id = "azureai://built-in-evaluators/sexual",
                    init_params = new { },
                    data_mapping = new { query = "${data.query}", response = "${data.response}" }
                },
                ["hate_unfairness"] = new
                {
                    id = "azureai://built-in-evaluators/hate-unfairness",
                    init_params = new { },
                    data_mapping = new { query = "${data.query}", response = "${data.response}" }
                },
                ["groundedness_pro"] = new
                {
                    id = "azureai://built-in-evaluators/groundedness-pro",
                    init_params = new { },
                    data_mapping = new
                    {
                        query = "${data.query}",
                        response = "${data.response}",
                        context = "${data.context}"
                    }
                }
            },
            data = new
            {
                type = "dataset",
                id = datasetId
            }
        };

        var schedulePayload = new
        {
            display_name = evaluationName,
            trigger = new
            {
                type = "one_time",
                trigger_at = DateTime.UtcNow.AddSeconds(5).ToString("O")
            },
            task = new
            {
                type = "evaluation",
                eval_id = evaluationName,
                eval_run = evalConfig
            }
        };

        var scheduleId = $"eval-{evaluationName}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        BinaryData scheduleData = BinaryData.FromObjectAsJson(schedulePayload);
        using BinaryContent content = BinaryContent.Create(scheduleData);

        ClientResult result = await _projectClient.Schedules.CreateOrUpdateAsync(
            scheduleId, content, new RequestOptions());

        using var doc = JsonDocument.Parse(result.GetRawResponse().Content.ToString());
        return doc.RootElement.TryGetProperty("id", out var idProp)
            ? idProp.GetString() ?? scheduleId
            : scheduleId;
    }

    /// <summary>
    /// Polls a schedule until it reaches a terminal state.
    /// Returns "completed", "failed", or "timeout".
    /// </summary>
    public async Task<string> WaitForCompletionAsync(
        string scheduleId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromMinutes(10));

        while (DateTime.UtcNow < deadline)
        {
            ClientResult scheduleResult = await _projectClient.Schedules.GetAsync(
                scheduleId, new RequestOptions());

            using var doc = JsonDocument.Parse(
                scheduleResult.GetRawResponse().Content.ToString());

            if (doc.RootElement.TryGetProperty("provisioning_status", out var statusProp))
            {
                var status = statusProp.GetString();
                if (status is "completed" or "failed")
                    return status;
            }

            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        return "timeout";
    }

    /// <summary>
    /// Convenience method: uploads dataset, creates evaluation schedule, and waits.
    /// Returns the final status.
    /// </summary>
    public async Task<string> RunEvaluationAsync(
        string evaluationName, string datasetPath, TimeSpan? timeout = null)
    {
        var dataset = await UploadDatasetAsync(datasetPath);
        var scheduleId = await CreateEvaluationScheduleAsync(evaluationName, dataset.Id);
        return await WaitForCompletionAsync(scheduleId, timeout);
    }
}
