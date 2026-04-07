using System.Globalization;
using Biotrackr.Reporting.Api.Endpoints;
using Biotrackr.Reporting.Api.Models;

namespace Biotrackr.Reporting.Api.Validation
{
    /// <summary>
    /// Shared validation for report generation requests (used by both HTTP and A2A code paths).
    /// </summary>
    internal static class ReportRequestValidator
    {
        private const int MaxTaskMessageLength = 5000;

        // Prompt-injection detection patterns (ASI01)
        private static readonly string[] InjectionPatterns =
        [
            "ignore previous", "ignore all previous", "disregard previous",
            "system prompt", "you are now", "new instructions",
            "override instructions", "forget your instructions",
            "ignore above", "disregard above", "forget above",
            "act as", "pretend you are", "simulate being"
        ];

        /// <summary>
        /// Validates a report generation request, returning a result with error and optional warning messages.
        /// </summary>
        internal static ValidationResult Validate(GenerateReportRequest request)
        {
            // Validate report type
            if (!ReportType.IsValid(request.ReportType))
            {
                return new ValidationResult(false, $"Invalid report type: {request.ReportType}. Valid types: {string.Join(", ", ReportType.All)}");
            }

            // Validate dates
            if (!DateOnly.TryParseExact(request.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
            {
                return new ValidationResult(false, "Invalid startDate format. Use yyyy-MM-dd.");
            }

            if (!DateOnly.TryParseExact(request.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
            {
                return new ValidationResult(false, "Invalid endDate format. Use yyyy-MM-dd.");
            }

            if (endDate < startDate)
            {
                return new ValidationResult(false, "endDate must be after startDate.");
            }

            if ((endDate.DayNumber - startDate.DayNumber) > 365)
            {
                return new ValidationResult(false, "Date range cannot exceed 365 days.");
            }

            if (string.IsNullOrWhiteSpace(request.TaskMessage))
            {
                return new ValidationResult(false, "taskMessage is required.");
            }

            // Enforce maximum task message length (ASI01)
            if (request.TaskMessage.Length > MaxTaskMessageLength)
            {
                return new ValidationResult(false, $"taskMessage must not exceed {MaxTaskMessageLength} characters.");
            }

            // Prompt-injection detection (ASI01)
            var lowerMessage = request.TaskMessage.ToLowerInvariant();
            if (InjectionPatterns.Any(pattern => lowerMessage.Contains(pattern)))
            {
                return new ValidationResult(false, "taskMessage contains disallowed content.", "Potential prompt injection detected in taskMessage");
            }

            // Validate source data snapshot is provided (enables reviewer cross-validation)
            if (request.SourceDataSnapshot is null || SnapshotValidator.IsEmpty(request.SourceDataSnapshot))
            {
                return new ValidationResult(false, "sourceDataSnapshot is required. Health data must be fetched and provided for report generation.", "Report generation request missing sourceDataSnapshot");
            }

            return new ValidationResult(true, null);
        }
    }

    /// <summary>
    /// Result of a report request validation check.
    /// </summary>
    internal record ValidationResult(bool IsValid, string? ErrorMessage, string? WarningMessage = null);
}
