# Common Resolutions

This document captures working solutions to recurring issues in the Biotrackr project.

## E2E Test Issues

### Issue: RuntimeBinderException When Using FluentAssertions with Dynamic Types

**Symptoms**:
- E2E tests pass locally but fail in CI/CD
- Error: `RuntimeBinderException: 'Newtonsoft.Json.Linq.JObject' does not contain a definition for 'Should'`
- Error occurs when calling FluentAssertions methods on `dynamic` types from Cosmos DB queries
- Tests using `_fixture.Container.GetItemQueryIterator<dynamic>(query)` fail

**Root Cause**:
When using `dynamic` types in E2E tests, the C# runtime binder cannot resolve FluentAssertions extension methods in certain environments (especially CI/CD). The runtime tries to find the `Should()` method on the dynamic type itself rather than recognizing it as an extension method.

**Solution**:
Use strongly-typed models instead of `dynamic` when reading from Cosmos DB:

**❌ Wrong** (fails in CI/CD):
```csharp
var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);
var documents = new List<dynamic>();
while (iterator.HasMoreResults)
{
    var response = await iterator.ReadNextAsync();
    documents.AddRange(response);
}
var savedDoc = documents.First();
savedDoc.id.Should().Be(expected); // RuntimeBinderException!
```

**✅ Correct** (works everywhere):
```csharp
var iterator = _fixture.Container.GetItemQueryIterator<SleepDocument>(query);
var documents = new List<SleepDocument>();
while (iterator.HasMoreResults)
{
    var response = await iterator.ReadNextAsync();
    documents.AddRange(response);
}
var savedDoc = documents.First();
savedDoc.Id.Should().Be(expected); // Works!
```

**For ReadItemAsync**:
```csharp
// Wrong
var readResponse = await _fixture.Container.ReadItemAsync<dynamic>(id, partitionKey);
string readId = readResponse.Resource.id.ToString(); // Dynamic binding issues

// Correct
var readResponse = await _fixture.Container.ReadItemAsync<SleepDocument>(id, partitionKey);
readResponse.Resource.Id.Should().Be(expected);
```

**Resolution History**:
- Fixed in `Biotrackr.Sleep.Svc.IntegrationTests` E2E tests (commit dbc0085, 2025-11-03)
- Changed `CosmosRepositoryTests` from `dynamic` to `SleepDocument`
- Changed `SleepServiceTests` from `dynamic` to `SleepDocument`

**Prevention**:
- Always use strongly-typed models when querying Cosmos DB in E2E tests
- Avoid `dynamic` types when using FluentAssertions
- Only use `dynamic` for cleanup operations (ClearContainerAsync) where you need flexible document deletion

---

## GitHub Actions Workflow Issues

### Issue: Test Reporter Action Failing with Permissions Error

**Symptoms**:
- Tests run successfully and produce results
- Artifact upload completes successfully
- "Publish Test Results" step fails
- Uses `dorny/test-reporter@v1` action

**Root Cause**:
The `dorny/test-reporter@v1` action requires `checks: write` permission to create GitHub check runs with test results.

**Solution**:
Add `checks: write` to the workflow permissions:

```yaml
permissions:
    contents: read
    id-token: write
    pull-requests: write
    checks: write  # Required for dorny/test-reporter@v1
```

**Resolution History**:
- Fixed in `deploy-weight-service.yml` (commit c10a763, 2025-10-28)
- Already present in `deploy-weight-api.yml`

**Prevention**:
- All workflows using `dorny/test-reporter@v1` must include `checks: write` permission
- Consider adding this to workflow templates for consistency

---

### Issue: Target Framework Mismatch Between Tests and Workflow

**Symptoms**:
- Tests fail to run in CI/CD
- Error about .NET version not found
- Local tests work fine

**Root Cause**:
Test project targets a different .NET version than what's configured in the GitHub Actions workflow.

**Solution**:
1. Check workflow's `DOTNET_VERSION` environment variable (e.g., `9.0.x`)
2. Verify test project's `<TargetFramework>` in `.csproj` matches (e.g., `net9.0`)
3. Update `.csproj` if mismatch exists

**Resolution History**:
- Fixed in `Biotrackr.Weight.Svc.IntegrationTests.csproj` (changed `net10.0` → `net9.0`, commit 1a8f80f, 2025-10-28)

**Prevention**:
- Document required .NET version in project README
- Consider adding validation step in workflow to check version compatibility

---

### Issue: Incorrect Working Directory for Reusable Workflow Templates

**Symptoms**:
- Workflow templates can't find test projects
- Build or test commands fail with "project not found"
- Works locally but fails in CI/CD

**Root Cause**:
Workflow passes solution directory path instead of specific test project directory to reusable templates.

**Solution**:
Use the specific test project directory, not the solution directory:

**❌ Wrong**:
```yaml
working-directory: ./src/Biotrackr.Weight.Svc
```

**✅ Correct**:
```yaml
working-directory: ./src/Biotrackr.Weight.Svc/Biotrackr.Weight.Svc.UnitTests
```

**Resolution History**:
- Fixed in `deploy-weight-service.yml` for unit, contract, and E2E test jobs (commit d48cd40, 2025-10-28)

**Prevention**:
- Reusable templates should expect test project paths, not solution paths
- Document this pattern in workflow template documentation

---

## Service Lifetime & Dependency Injection

### Issue: Duplicate Service Registrations with AddHttpClient

**Symptoms**:
- Service registered twice in `Program.cs`
- Test expectations don't match runtime behavior
- Confusion about service lifetime

**Root Cause**:
HttpClient-based services registered both manually (e.g., `AddScoped`) and via `AddHttpClient`, causing the second registration to override the first.

**Solution**:
Remove duplicate registration. Only use `AddHttpClient<TInterface, TImplementation>()`:

**❌ Wrong**:
```csharp
services.AddScoped<IFitbitService, FitbitService>();  // This gets overridden
services.AddHttpClient<IFitbitService, FitbitService>()
    .AddStandardResilienceHandler();
```

**✅ Correct**:
```csharp
services.AddHttpClient<IFitbitService, FitbitService>()
    .AddStandardResilienceHandler();
```

**Service Lifetime Guidelines**:
| Service Type | Lifetime | Reason |
|-------------|----------|--------|
| Azure SDK clients (CosmosClient, SecretClient) | Singleton | Expensive to create, thread-safe, manages connection pooling |
| Application services (Repositories, Services) | Scoped | One instance per request/execution scope |
| HttpClient-based services | Transient | Managed by HttpClientFactory, lightweight |

**Resolution History**:
- Fixed in `Biotrackr.Weight.Svc/Program.cs` (removed duplicate FitbitService registration, commit e5d89ab, 2025-10-28)
- Documented in `docs/decision-records/2025-10-28-service-lifetime-registration.md`

**Prevention**:
- Code review checklist: Verify no duplicate registrations with `AddHttpClient`
- Follow guidelines in `.github/copilot-instructions.md` and decision record

---

## Test Organization

### Issue: Integration Tests Need Both Contract and E2E Types

**Symptoms**:
- Need different test execution patterns (some need Cosmos DB, some don't)
- Want to run fast tests in parallel with unit tests
- Need Cosmos DB setup only for E2E tests

**Solution**:
Organize integration tests into separate namespaces and use xUnit test filters:

```
IntegrationTests/
├── Contract/              # Fast tests, no external dependencies
│   ├── ProgramStartupTests.cs
│   └── ServiceRegistrationTests.cs
└── E2E/                   # Tests requiring Cosmos DB
    ├── CosmosRepositoryTests.cs
    └── WeightServiceTests.cs
```

**Workflow Configuration**:
```yaml
# Contract tests - parallel with unit tests, no Cosmos DB
run-contract-tests:
    needs: env-setup
    test-filter: 'FullyQualifiedName~Contract'

# E2E tests - requires Cosmos DB Emulator
run-e2e-tests:
    needs: [env-setup, run-contract-tests]
    test-filter: 'FullyQualifiedName~E2E'
```

**Resolution History**:
- Implemented in `Biotrackr.Weight.Svc.IntegrationTests` structure (commit e5d89ab, 2025-10-28)
- Documented in `docs/decision-records/2025-10-28-integration-test-project-structure.md`

**Prevention**:
- Follow this pattern for all service integration tests
- Use test filters consistently across workflows

---

## E2E Test Issues

### Issue: E2E Tests Fail with "SSL negotiation failed" on Direct Connection Mode

**Symptoms**:
- E2E tests fail with `Microsoft.Azure.Documents.GoneException`
- Error: `TransportException: SSL negotiation failed`
- Error mentions `rntbd://127.0.0.1:10251/` (Direct mode TCP+SSL protocol)
- Tests trying to connect to Cosmos DB Emulator port 10251

**Root Cause**:
CosmosClient defaults to **Direct mode** (TCP+SSL via rntbd:// protocol), which requires proper SSL/TLS certificate negotiation. Even with `ServerCertificateCustomValidationCallback`, the Direct mode connection fails with the Cosmos DB Emulator's self-signed certificate.

**Solution**:
Force **Gateway mode** (HTTPS only) in `CosmosClientOptions`:

```csharp
services.AddSingleton<CosmosClient>(sp =>
{
    return new CosmosClient(cosmosDbEndpoint, cosmosDbAccountKey, new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway, // Force Gateway mode (HTTPS only)
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        },
        HttpClientFactory = () => new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        })
    });
});
```

**Why This Works**:
- **Gateway mode**: Uses HTTPS (port 8081) which respects `ServerCertificateCustomValidationCallback`
- **Direct mode**: Uses TCP+SSL (port 10251) which requires system-level certificate trust
- Local emulator uses self-signed certificates that work better with HTTPS than TCP+SSL

**Resolution History**:
- Fixed in `ActivityApiWebApplicationFactory.cs` (added ConnectionMode.Gateway, commit TBD, 2025-10-29)
- 10 of 12 E2E tests now pass with local Docker Cosmos DB Emulator

**Prevention**:
- Always use `ConnectionMode.Gateway` for local Cosmos DB Emulator tests
- Apply this pattern to all `*WebApplicationFactory.cs` files in integration test projects
- For production: Direct mode is preferred for performance, but Gateway mode works everywhere

**Performance Note**:
Gateway mode has slightly higher latency (~2-3ms per request) but is more reliable for local development and testing.

---

### Issue: E2E Tests Fail with "Value cannot be null (Parameter 'implementationInstance')"

**Symptoms**:
- E2E tests fail during fixture initialization
- Error: `System.ArgumentNullException : Value cannot be null. (Parameter 'implementationInstance')`
- Stack trace points to `AddSingleton` call in test fixture
- Tests report "Failed to initialize Cosmos DB Emulator connection"

**Root Cause**:
Test fixture attempts to register a null service instance with `AddSingleton`:
```csharp
var fakeSecretClient = (SecretClient?)null!;
services.AddSingleton(fakeSecretClient);  // Throws ArgumentNullException
```

`AddSingleton` requires a non-null instance when registering with an instance parameter.

**Solution**:
Remove the null service registration. If the service isn't needed for tests, don't register it:

**❌ Wrong**:
```csharp
var fakeSecretClient = (SecretClient?)null!;
services.AddSingleton(fakeSecretClient);  // Will throw
```

**✅ Correct**:
```csharp
// Don't register services that aren't needed for tests
// Or use a mock/fake implementation if required by dependencies
```

**Alternative** (if service is required by constructor injection):
```csharp
// Use NSubstitute or Moq to create a fake
var fakeSecretClient = Substitute.For<SecretClient>();
services.AddSingleton(fakeSecretClient);
```

**Resolution History**:
- Fixed in `IntegrationTestFixture.cs` (removed null SecretClient registration, commit TBD, 2025-10-28)

**Prevention**:
- Never call `AddSingleton(null)` - it will always throw `ArgumentNullException`
- Use mocking libraries for fake dependencies
- Only register services that are actually needed by the code under test
- Consider using `AddSingleton<TService>(implementationFactory)` if conditional registration is needed

---

### Issue: E2E Tests Find More Documents Than Expected (Test Isolation Failure)

**Symptoms**:
- Test expects to find 1 document but finds multiple (e.g., 3)
- All documents have the same date
- Error: `Expected documents to contain 1 item(s) because exactly one document should be saved, but found 3`
- Tests pass individually but fail when run together

**Root Cause**:
xUnit Collection Fixtures share the same database instance across all tests in the collection. When tests don't clean up after themselves, subsequent tests find leftover data from previous tests.

Tests query by date (`WHERE c.date = @date`), and since all tests run on the same date (e.g., 2025-10-28), they find each other's documents.

**Solution**:
Add a cleanup method to clear the container before each test:

```csharp
/// <summary>
/// Clears all documents from the test container to ensure test isolation.
/// </summary>
private async Task ClearContainerAsync()
{
    var query = new QueryDefinition("SELECT c.id, c.documentType FROM c");
    var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);

    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();
        foreach (var item in response)
        {
            await _fixture.Container.DeleteItemAsync<dynamic>(
                item.id.ToString(),
                new PartitionKey(item.documentType.ToString()));
        }
    }
}

[Fact]
public async Task MyTest()
{
    // Arrange - Clear container for test isolation
    await ClearContainerAsync();
    
    // ... rest of test
}
```

**Alternative Solutions**:
1. **Use unique test data** - Generate unique dates/identifiers per test
2. **Delete specific documents** - Track created IDs and delete only those
3. **Separate collections** - Don't use Collection Fixtures (slower, more isolation)

**Resolution History**:
- Fixed in `WeightServiceTests.cs` (added ClearContainerAsync method, commit TBD, 2025-10-28)

**Prevention**:
- Always clean up test data before or after each test
- Use `[Collection]` fixtures carefully - understand data is shared
- Consider using IAsyncLifetime on test classes for per-test setup/teardown
- For CosmosDB E2E tests, always clear container in test setup

---

## Code Coverage Exclusions

### Issue: Program.cs Not Excluded from Code Coverage in CI/CD

**Symptoms**:
- Local tests show 100% coverage with `<ExcludeByFile>**/Program.cs</ExcludeByFile>` in .csproj
- CI/CD coverage reports show lower percentage (e.g., 52% instead of 100%)
- Program.cs appears in coverage reports despite exclusion configuration
- `coverlet.msbuild` package exclusions work locally but not in CI

**Root Cause**:
The `<ExcludeByFile>` property in .csproj files works with `coverlet.msbuild` for local coverage runs, but GitHub Actions workflows use `dotnet test --collect:"XPlat Code Coverage"` which uses `coverlet.collector`. The collector doesn't respect .csproj `<ExcludeByFile>` properties without a runsettings file.

**Solution**:
Use the `[ExcludeFromCodeCoverage]` attribute directly in the Program.cs file, following the Weight Service pattern:

**❌ Wrong** (doesn't work in CI):
```xml
<!-- In .csproj -->
<PropertyGroup>
  <ExcludeByFile>**/Program.cs</ExcludeByFile>
</PropertyGroup>
```

**✅ Correct** (works everywhere):
```csharp
// In Program.cs
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        // ... application setup code
        host.Run();
    }
}
```

**Why This Works**:
- `[ExcludeFromCodeCoverage]` attribute is recognized by all coverage tools (coverlet.collector, coverlet.msbuild, dotCover, etc.)
- Works consistently in both local development and CI/CD environments
- No need for runsettings files or template modifications
- Follows established pattern from Weight Service

**Resolution History**:
- Fixed in `Biotrackr.Activity.Svc/Program.cs` (added [ExcludeFromCodeCoverage] attribute, commit 1741655, 2025-10-31)
- Removed unnecessary `<ExcludeByFile>` from .csproj (commit 60b2826, 2025-10-31)
- Removed `coverlet.msbuild` package dependency (commit 60b2826, 2025-10-31)

**Prevention**:
- Always use `[ExcludeFromCodeCoverage]` attribute for Program.cs in .NET worker services
- Don't rely on .csproj `<ExcludeByFile>` properties for CI/CD coverage
- Only include `coverlet.collector` package (not `coverlet.msbuild`) in test projects
- Follow Weight Service pattern for consistency across all services

**Pattern to Follow**:
```csharp
using System.Diagnostics.CodeAnalysis;
// ... other usings

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        // Host builder and configuration
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(...)
            .Build();
        
        host.Run();
    }
}
```

---

## Notes

- Keep this document updated as new patterns emerge
- Link to relevant decision records for detailed context
- Include commit hashes for traceability
- Update "Prevention" sections as preventive measures are implemented
