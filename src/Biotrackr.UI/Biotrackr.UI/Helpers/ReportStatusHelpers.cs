using System.Text.Json;
using Biotrackr.UI.Models.Chat;

namespace Biotrackr.UI.Helpers;

public static class ReportStatusHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Determines the status display text based on elapsed seconds and current generating state.
    /// </summary>
    public static string GetStatusText(int elapsedSeconds)
    {
        return elapsedSeconds >= 60
            ? "Still working on your report..."
            : "Generating report...";
    }

    /// <summary>
    /// Determines the final status text based on the polled report status.
    /// Returns null if the status is still in progress (generating).
    /// </summary>
    public static string? GetTerminalStatusText(ReportStatusResponse? status)
    {
        if (status is null)
            return "Report not found";

        return status.Status switch
        {
            "generated" or "reviewed" => "Report ready!",
            "failed" => "Report generation failed",
            _ => null
        };
    }

    /// <summary>
    /// Determines if a report status is terminal (no longer generating).
    /// </summary>
    public static bool IsTerminalStatus(ReportStatusResponse? status)
    {
        if (status is null) return true;
        return status.Status is "generated" or "reviewed" or "failed";
    }

    /// <summary>
    /// Determines if a report status indicates successful completion.
    /// </summary>
    public static bool IsCompletedSuccessfully(ReportStatusResponse? status)
    {
        return status?.Status is "generated" or "reviewed";
    }

    /// <summary>
    /// Attempts to extract a job ID from a TOOL_CALL_RESULT content string
    /// that contains structured JSON from RequestReportTool.
    /// </summary>
    public static string? TryExtractJobId(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return null;

        try
        {
            var result = JsonSerializer.Deserialize<RequestReportResult>(content, JsonOptions);
            return result?.JobId;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Determines if the elapsed time has exceeded the maximum polling timeout.
    /// </summary>
    public static bool IsTimedOut(int elapsedSeconds, int maxSeconds = 600)
    {
        return elapsedSeconds > maxSeconds;
    }

    private sealed record RequestReportResult(string? JobId, string? Status, string? Message);
}
