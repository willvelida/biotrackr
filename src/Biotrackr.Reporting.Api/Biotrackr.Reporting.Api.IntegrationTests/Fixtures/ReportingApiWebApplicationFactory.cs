using Biotrackr.Reporting.Api.Models;
using Biotrackr.Reporting.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Biotrackr.Reporting.Api.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for Reporting.Api integration testing.
/// Replaces real services with mocks and disables external dependencies
/// (Azure App Config, Application Insights, Copilot SDK sidecar).
/// Uses a test authentication handler to simulate authenticated requests.
/// </summary>
public class ReportingApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IBlobStorageService> MockBlobStorageService { get; } = new();
    public Mock<ICopilotService> MockCopilotService { get; } = new();
    public Mock<IReportGenerationService> MockReportGenerationService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Skip Azure App Configuration and Application Insights
        Environment.SetEnvironmentVariable("azureappconfigendpoint", string.Empty);
        Environment.SetEnvironmentVariable("managedidentityclientid", string.Empty);
        Environment.SetEnvironmentVariable("applicationinsightsconnectionstring", string.Empty);

        // Provide minimal AzureAd config to satisfy AddMicrosoftIdentityWebApi
        Environment.SetEnvironmentVariable("AzureAd:Instance", "https://login.microsoftonline.com/");
        Environment.SetEnvironmentVariable("AzureAd:TenantId", "test-tenant-id");
        Environment.SetEnvironmentVariable("AzureAd:ClientId", "test-client-id");

        // Settings
        Environment.SetEnvironmentVariable("Biotrackr:CopilotCliUrl", "http://localhost:4321");
        Environment.SetEnvironmentVariable("Biotrackr:ReportingBlobStorageEndpoint", "https://teststorage.blob.core.windows.net");
        Environment.SetEnvironmentVariable("Biotrackr:ReportGenerationEnabled", "true");

        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Replace real services with mocks
            ReplaceService<IBlobStorageService>(services, MockBlobStorageService.Object);
            ReplaceService<ICopilotService>(services, MockCopilotService.Object);
            ReplaceService<IReportGenerationService>(services, MockReportGenerationService.Object);

            // Provide a mock IChatClient required by the Agent Framework's AddAIAgent
            var mockChatClient = new Mock<IChatClient>();
            ReplaceService<IChatClient>(services, mockChatClient.Object);

            // Replace authentication with a test scheme that auto-succeeds
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });

            // Override the default authorization policy to use the test scheme
            services.AddAuthorizationBuilder()
                .SetDefaultPolicy(new AuthorizationPolicyBuilder("TestScheme")
                    .RequireAuthenticatedUser()
                    .Build())
                .AddPolicy("ChatApiAgent", policy =>
                {
                    policy.AddAuthenticationSchemes("TestScheme");
                    policy.RequireAuthenticatedUser();
                });
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
