using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Configuration
{
    [ExcludeFromCodeCoverage]
    public class Settings
    {
        public string? DatabaseName { get; set; }
        public string? ContainerName { get; set; }
    }
}
