using System.Diagnostics.CodeAnalysis;

namespace Biotrackr.UI.Configuration
{
    [ExcludeFromCodeCoverage]
    public class BiotrackrApiSettings
    {
        public string? BaseUrl { get; set; }
        public string? SubscriptionKey { get; set; }
    }
}
