using Biotrackr.Reporting.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Biotrackr.Reporting.Api.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory variant for testing authorization policy configuration.
/// Unlike <see cref="ReportingApiWebApplicationFactory"/>, this preserves the real
/// ChatApiAgent authorization policy from Program.cs so azp claim requirements
/// can be verified against the actual policy wiring.
/// </summary>
public class AuthorizationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    public string? ChatApiAgentIdentityId { get; set; }
    public string? ReportingSvcAgentIdentityId { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Skip Azure App Configuration and Application Insights
        Environment.SetEnvironmentVariable("azureappconfigendpoint", string.Empty);
        Environment.SetEnvironmentVariable("managedidentityclientid", string.Empty);
        Environment.SetEnvironmentVariable("applicationinsightsconnectionstring", string.Empty);

        // Provide minimal AzureAd config
        Environment.SetEnvironmentVariable("AzureAd:Instance", "https://login.microsoftonline.com/");
        Environment.SetEnvironmentVariable("AzureAd:TenantId", "test-tenant-id");
        Environment.SetEnvironmentVariable("AzureAd:ClientId", "test-client-id");

        // Settings
        Environment.SetEnvironmentVariable("Biotrackr:CopilotCliUrl", "http://localhost:4321");
        Environment.SetEnvironmentVariable("Biotrackr:ReportingBlobStorageEndpoint", "https://teststorage.blob.core.windows.net");
        Environment.SetEnvironmentVariable("Biotrackr:ReportGenerationEnabled", "true");

        // Configure authorized caller identity IDs
        Environment.SetEnvironmentVariable("Biotrackr:ChatApiAgentIdentityId", ChatApiAgentIdentityId ?? string.Empty);
        Environment.SetEnvironmentVariable("Biotrackr:ReportingSvcAgentIdentityId", ReportingSvcAgentIdentityId ?? string.Empty);

        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Replace real services with mocks
            var mockBlobStorage = new Mock<IBlobStorageService>();
            var mockCopilot = new Mock<ICopilotService>();
            var mockReportGeneration = new Mock<IReportGenerationService>();
            var mockChatClient = new Mock<IChatClient>();

            ReplaceService<IBlobStorageService>(services, mockBlobStorage.Object);
            ReplaceService<ICopilotService>(services, mockCopilot.Object);
            ReplaceService<IReportGenerationService>(services, mockReportGeneration.Object);
            ReplaceService<IChatClient>(services, mockChatClient.Object);

            // Replace authentication with a configurable test scheme but preserve the real authorization policy
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, ConfigurableAuthHandler>("TestScheme", _ => { });
        });
    }

    private static void ReplaceService<T>(IServiceCollection services, T instance) where T : class
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
        services.AddSingleton(instance);
    }
}
