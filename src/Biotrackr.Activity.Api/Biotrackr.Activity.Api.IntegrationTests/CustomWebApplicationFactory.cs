using Azure.Identity;
using Biotrackr.Activity.Api.Configuration;
using Biotrackr.Activity.Api.Repositories;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.Activity.Api.IntegrationTests
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                      .AddEnvironmentVariables()
                      .AddJsonFile("appsettings.json");
                var builtConfig = config.Build();
                var managedIdentityClientId = Environment.GetEnvironmentVariable("managedidentityclientid");
                var tenantId = Environment.GetEnvironmentVariable("tenantid");
                var azureAppConfigEndpoint = Environment.GetEnvironmentVariable("azureappconfigendpoint");
                var cosmosDbEndpoint = Environment.GetEnvironmentVariable("cosmosdbendpoint");

                if (string.IsNullOrEmpty(managedIdentityClientId) || string.IsNullOrEmpty(azureAppConfigEndpoint) || string.IsNullOrEmpty(cosmosDbEndpoint))
                {
                    throw new InvalidOperationException("Required environment variables are not set.");
                }

                config.AddAzureAppConfiguration(config =>
                {
                    config.Connect(new Uri(azureAppConfigEndpoint),
                                   new WorkloadIdentityCredential(new WorkloadIdentityCredentialOptions
                                   {
                                       ClientId = managedIdentityClientId,
                                       TenantId = tenantId
                                   }))
                          .Select(KeyFilter.Any, LabelFilter.Null);
                });
            });

            builder.ConfigureServices((context, services) =>
            {
                services.Configure<Settings>(context.Configuration.GetSection("Biotrackr"));

                var cosmosClientOptions = new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };

                var managedIdentityClientId = Environment.GetEnvironmentVariable("managedidentityclientid");
                var tenantId = Environment.GetEnvironmentVariable("tenantid");

                var cosmosClient = new CosmosClient(
                    Environment.GetEnvironmentVariable("cosmosdbendpoint"),
                    new WorkloadIdentityCredential(new WorkloadIdentityCredentialOptions
                    {
                        ClientId = managedIdentityClientId,
                        TenantId = tenantId
                    }),
                    cosmosClientOptions);

                services.AddSingleton(cosmosClient);
                services.AddTransient<ICosmosRepository, CosmosRepository>();

                services.AddHealthChecks();
            });

            builder.UseEnvironment("Development"); // Ensure the environment is set to Development for testing
        }
    }
}
