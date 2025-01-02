using Azure.Identity;
using Biotrackr.Weight.Api.Configuration;
using Biotrackr.Weight.Api.Extensions;
using Biotrackr.Weight.Api.Repositories;
using Biotrackr.Weight.Api.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
var managedIdentityClientId = builder.Configuration.GetValue<string>("managedidentityclientid");
var defaultCredentailOptions = new DefaultAzureCredentialOptions()
{
    ManagedIdentityClientId = managedIdentityClientId
};
builder.Configuration.AddAzureAppConfiguration(config =>
{
    config.Connect(new Uri(builder.Configuration.GetValue<string>("azureappconfigendpoint")),
        new ManagedIdentityCredential(managedIdentityClientId))
    .Select(KeyFilter.Any, LabelFilter.Null);
});

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Biotrackr"));
var cosmosClientOptions = new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    }
};
var cosmosClient = new CosmosClient(builder.Configuration.GetValue<string>("cosmosdbendpoint"),
    new DefaultAzureCredential(defaultCredentailOptions),
    cosmosClientOptions);
builder.Services.AddSingleton(cosmosClient);
builder.Services.AddTransient<ICosmosRepository, CosmosRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "1.0.0",
        Title = "Biotrackr Weight API",
        Description = "Web API for Weight data",
        Contact = new OpenApiContact
        {
            Name = "Biotrackr",
            Url = new Uri("https://github.com/willvelida/biotrackr")
        }
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.RegisterWeightEndpoints();
app.RegisterHealthCheckEndpoints();

app.Run();

public partial class Program { }