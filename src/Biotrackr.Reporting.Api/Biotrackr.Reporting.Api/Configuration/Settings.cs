namespace Biotrackr.Reporting.Api.Configuration
{
    public class Settings
    {
        public string ReportingBlobStorageEndpoint { get; set; } = string.Empty;
        public string CopilotCliUrl { get; set; } = "http://localhost:4321";
        public string ChatApiUaiPrincipalId { get; set; } = string.Empty;
        public string ChatApiAgentIdentityId { get; set; } = string.Empty;
        public string ReportingApiUrl { get; set; } = string.Empty;
        public string ReportGeneratorSystemPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Kill switch for report generation (ASI10). Set to false to immediately disable all report generation.
        /// </summary>
        public bool ReportGenerationEnabled { get; set; } = true;

        /// <summary>
        /// Maximum concurrent report generation jobs (ASI08).
        /// </summary>
        public int MaxConcurrentJobs { get; set; } = 3;

        /// <summary>
        /// Maximum time in minutes for a single report generation job (ASI08).
        /// </summary>
        public int ReportGenerationTimeoutMinutes { get; set; } = 10;

        /// <summary>
        /// Maximum artifact file size in bytes (ASI10). Default 50 MB.
        /// </summary>
        public long MaxArtifactSizeBytes { get; set; } = 50 * 1024 * 1024;
    }
}
