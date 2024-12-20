using Azure.Identity;
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
                var azureAppConfigEndpoint = Environment.GetEnvironmentVariable("azureappconfigendpoint");
                var cosmosDbEndpoint = Environment.GetEnvironmentVariable("cosmosdbendpoint");

                if (string.IsNullOrEmpty(managedIdentityClientId) || string.IsNullOrEmpty(azureAppConfigEndpoint) || string.IsNullOrEmpty(cosmosDbEndpoint))
                {
                    throw new InvalidOperationException("Required environment variables are not set.");
                }

                var defaultCredentialOptions = new DefaultAzureCredentialOptions()
                {
                    ManagedIdentityClientId = managedIdentityClientId
                };

                config.AddAzureAppConfiguration(config =>
                {
                    config.Connect(new Uri(azureAppConfigEndpoint),
                                   new DefaultAzureCredential(defaultCredentialOptions))
                          .Select(KeyFilter.Any, LabelFilter.Null);
                });
            });

            builder.ConfigureServices((context, services) =>
            {
                var cosmosClientOptions = new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };

                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = Environment.GetEnvironmentVariable("managedidentityclientid")
                });

                var cosmosClient = new CosmosClient(
                    Environment.GetEnvironmentVariable("cosmosdbendpoint"),
                    credential,
                    cosmosClientOptions);

                services.AddSingleton(cosmosClient);
                services.AddTransient<ICosmosRepository, CosmosRepository>();
            });

            builder.UseEnvironment("Development"); // Ensure the environment is set to Development for testing
        }
    }
}
