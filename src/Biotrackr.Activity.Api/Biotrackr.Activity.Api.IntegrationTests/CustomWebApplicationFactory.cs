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
                config.AddEnvironmentVariables();
                var builtConfig = config.Build();
                var managedIdentityClientId = builtConfig.GetValue<string>("managedidentityclientid");
                var defaultCredentialOptions = new DefaultAzureCredentialOptions()
                {
                    ManagedIdentityClientId = managedIdentityClientId
                };

                config.AddAzureAppConfiguration(config =>
                {
                    config.Connect(new Uri(builtConfig.GetValue<string>("azureappconfigendpoint")),
                                   new ManagedIdentityCredential(managedIdentityClientId))
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
                var cosmosClient = new CosmosClient(
                    context.Configuration.GetValue<string>("cosmosdbendpoint"),
                    new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = context.Configuration.GetValue<string>("managedidentityclientid")
                    }),
                    cosmosClientOptions);
                services.AddSingleton(cosmosClient);
                services.AddTransient<ICosmosRepository, CosmosRepository>();
            });
        }
    }
}
