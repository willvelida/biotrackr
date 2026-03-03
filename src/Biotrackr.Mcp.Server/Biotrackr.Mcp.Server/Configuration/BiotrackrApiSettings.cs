using System.Diagnostics.CodeAnalysis;

namespace Biotrackr.Mcp.Server.Configuration
{
    [ExcludeFromCodeCoverage]
    public class BiotrackrApiSettings
    {
        public string? BaseUrl { get; set; }
        public string? SubscriptionKey { get; set; }
    }
}
