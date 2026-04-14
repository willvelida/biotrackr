using Microsoft.Extensions.Configuration;

namespace Biotrackr.Reporting.Svc.IntegrationTests.Fixtures;

public class IntegrationTestFixture : IAsyncLifetime
{
    protected virtual bool InitializeDatabase => true;

    public IConfiguration? Configuration { get; protected set; }

    public virtual Task InitializeAsync()
    {
        if (InitializeDatabase)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: false)
                .Build();
        }

        return Task.CompletedTask;
    }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
