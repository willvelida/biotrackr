using Azure.Identity;
using Biotrackr.Activity.Api.Configuration;
using Biotrackr.Activity.Api.Extensions;
using Biotrackr.Activity.Api.Repositories;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
var managedIdentityClientId = builder.Configuration.GetValue<string>("managedidentityclientid");
var defaultCredentialOptions = new DefaultAzureCredentialOptions()
{
    ManagedIdentityClientId = managedIdentityClientId
};

builder.Configuration.AddAzureAppConfiguration(config =>
{
    config.Connect(new Uri(builder.Configuration.GetValue<string>("azureappconfigendpoint")),
                   new ManagedIdentityCredential(managedIdentityClientId))
        .Select(keyFilter: KeyFilter.Any, LabelFilter.Null);
});

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Biotrackr"));

var cosmosClientOptions = new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    }
};
var cosmosClient = new CosmosClient(
    builder.Configuration.GetValue<string>("cosmosdbendpoint"),
    new DefaultAzureCredential(defaultCredentialOptions),
    cosmosClientOptions);
builder.Services.AddSingleton(cosmosClient);
builder.Services.AddTransient<ICosmosRepository, CosmosRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Biotrackr Activity API",
        Description = "Web API for Activity data",
        Contact = new OpenApiContact
        {
            Name = "Biotrackr",
            Url = new Uri("https://github.com/willvelida/biotrackr")
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.RegisterActivityEndpoints();

app.Run();
