using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/sayhello", ([FromServices] DaprClient daprClient, ILogger<Program> logger) =>
{
    logger.LogInformation($"Hello Dapr Cron Job! The time is now {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}");
});

app.Run();
