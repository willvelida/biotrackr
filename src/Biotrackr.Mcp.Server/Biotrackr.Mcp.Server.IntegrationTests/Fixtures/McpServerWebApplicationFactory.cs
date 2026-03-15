using System.Net;
using Biotrackr.Mcp.Server.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.Mcp.Server.IntegrationTests.Fixtures
{
    /// <summary>
    /// Custom WebApplicationFactory that replaces the downstream API HttpClient
    /// with a mock handler for integration testing.
    /// </summary>
    public class McpServerWebApplicationFactory : WebApplicationFactory<Program>
    {
        public MockDownstreamApiHandler MockApiHandler { get; } = new();

        public McpServerWebApplicationFactory()
        {
            // Set up default downstream responses so the server can start
            MockApiHandler.WithJsonResponse("/activity", """
            {
                "items": [],
                "totalCount": 0,
                "pageNumber": 1,
                "pageSize": 1
            }
            """);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");

            builder.ConfigureServices(services =>
            {
                // Remove the existing HttpClient singleton registration
                var httpClientDescriptor = services.FirstOrDefault(
                    d => d.ServiceType == typeof(HttpClient) && d.Lifetime == ServiceLifetime.Singleton);
                if (httpClientDescriptor != null)
                {
                    services.Remove(httpClientDescriptor);
                }

                // Remove existing IHttpClientFactory/HttpClient registrations for "BiotrackrApi"
                var httpClientFactoryDescriptors = services.Where(
                    d => d.ServiceType == typeof(IHttpClientFactory) ||
                         d.ServiceType.FullName?.Contains("HttpMessageHandler") == true)
                    .ToList();

                // Register a mock HttpClient that uses our mock handler
                services.AddSingleton<HttpClient>(_ =>
                    new HttpClient(MockApiHandler)
                    {
                        BaseAddress = new Uri("https://mock-api.test.com")
                    });
            });

            // Override config values via environment variables
            builder.UseSetting("biotrackrapiendpoint", "https://mock-api.test.com");
            builder.UseSetting("biotrackrapisubscriptionkey", "test-sub-key");
            builder.UseSetting("applicationinsightsconnectionstring", "InstrumentationKey=00000000-0000-0000-0000-000000000000");
        }
    }
}
