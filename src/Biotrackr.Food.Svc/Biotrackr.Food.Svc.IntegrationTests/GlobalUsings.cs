global using AutoFixture;
global using FluentAssertions;
global using Moq;
global using Xunit;

// Microsoft Extensions
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

// Azure SDK
global using Azure.Security.KeyVault.Secrets;
global using Microsoft.Azure.Cosmos;

// Biotrackr namespaces
global using Biotrackr.Food.Svc;
global using Biotrackr.Food.Svc.Models;
global using Biotrackr.Food.Svc.Models.FitbitEntities;
global using Biotrackr.Food.Svc.Repositories;
global using Biotrackr.Food.Svc.Repositories.Interfaces;
global using Biotrackr.Food.Svc.Services;
global using Biotrackr.Food.Svc.Services.Interfaces;
global using Biotrackr.Food.Svc.Workers;
global using Biotrackr.Food.Svc.IntegrationTests.Fixtures;
global using Biotrackr.Food.Svc.IntegrationTests.Helpers;
