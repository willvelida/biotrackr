using System.Diagnostics.CodeAnalysis;

namespace Biotrackr.Activity.Svc.Configuration
{
    [ExcludeFromCodeCoverage]
    public class Settings
    {
        public string? DatabaseName { get; set; }
        public string? ContainerName { get; set; }
    }
}
