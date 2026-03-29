using A2A.AspNetCore;
using Microsoft.Agents.AI.Hosting;

namespace Biotrackr.Reporting.Api.Endpoints
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class A2AEndpoints
    {
        public static void MapA2AEndpoints(this WebApplication app, IHostedAgentBuilder reportAgent)
        {
            app.MapA2A(reportAgent, path: "/a2a/report", agentCard: new()
            {
                Name = "Biotrackr Report Generator",
                Description = "Generates health reports using Python via GitHub Copilot SDK. Accepts structured health data with natural language instructions and produces PDF reports and chart images.",
                Version = "1.0"
            }).RequireAuthorization("ChatApiAgent");
        }
    }
}
