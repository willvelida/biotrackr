namespace Biotrackr.Chat.Api.Services
{
    /// <summary>
    /// Provides agent identity bearer tokens for inter-service authentication (ASI07).
    /// </summary>
    public interface IAgentTokenProvider
    {
        Task<string?> AcquireTokenForReportingApiAsync(CancellationToken cancellationToken);
    }
}
