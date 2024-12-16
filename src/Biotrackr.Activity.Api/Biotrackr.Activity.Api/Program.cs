using Azure.Identity;
using Biotrackr.Activity.Api.Configuration;
using Biotrackr.Activity.Api.Extensions;
using Biotrackr.Activity.Api.Repositories;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.RegisterActivityEndpoints();

app.Run();
