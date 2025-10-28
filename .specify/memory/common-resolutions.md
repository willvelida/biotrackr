# Common Resolutions

This document captures working solutions to recurring issues in the Biotrackr project.

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

## Notes

- Keep this document updated as new patterns emerge
- Link to relevant decision records for detailed context
- Include commit hashes for traceability
- Update "Prevention" sections as preventive measures are implemented
