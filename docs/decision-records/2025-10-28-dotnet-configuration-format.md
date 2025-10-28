# Decision Record: .NET Configuration Format for Environment Variables

- **Status**: Accepted
- **Deciders**: willvelida, GitHub Copilot
- **Date**: 28 October 2025
- **Related Docs**: [PR #79](https://github.com/willvelida/biotrackr/pull/79), [.NET Configuration Docs](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider)

## Context

E2E tests failed in GitHub Actions CI with consistent 404 errors after fixing the URL paths. The Cosmos DB environment variables were being set, but the application couldn't read them.

Environment variables in the E2E workflow were set using double underscore format:
```yaml
env:
  Biotrackr__CosmosDb__AccountKey: ${{ env.COSMOS_KEY }}
  Biotrackr__DatabaseName: biotrackr-test
  Biotrackr__ContainerName: weight-test
```

However, `Program.cs` reads configuration using colon-separated paths:
```csharp
var cosmosDbAccountKey = builder.Configuration.GetValue<string>("Biotrackr:CosmosDb:AccountKey");
var databaseName = builder.Configuration.GetValue<string>("Biotrackr:DatabaseName");
```

The mismatch between double underscore environment variables and colon-separated configuration paths caused the values to not be found.

## Decision

**Use colon-separated format (`:`) for environment variables in all workflows and configuration contexts.**

```yaml
env:
  Biotrackr:CosmosDb:AccountKey: ${{ env.COSMOS_KEY }}
  Biotrackr:DatabaseName: biotrackr-test
  Biotrackr:ContainerName: weight-test
```

This format:
- Works universally across all platforms (Windows, Linux, macOS)
- Matches .NET Configuration API expectations
- Is the recommended format in .NET documentation
- Works in both GitHub Actions and Azure environments

## Consequences

### Positive
- ✅ Consistent configuration format across all environments
- ✅ Works on all platforms without special handling
- ✅ Matches .NET Configuration documentation and examples
- ✅ No translation needed between environment and configuration
- ✅ Easier to debug configuration issues
- ✅ Clear hierarchical structure visible in variable names

### Negative
- ⚠️ Colon is less common in environment variable names
- ⚠️ Some shell scripts might require quoting
- ⚠️ Existing documentation might show double underscore format

### Trade-offs
- **Accepted**: Non-traditional environment variable syntax for platform consistency
- **Mitigated**: .NET officially supports and recommends colon format

## Alternatives Considered

### Alternative 1: Keep Double Underscore Format
```yaml
Biotrackr__CosmosDb__AccountKey: value
```
**Why rejected**:
- Doesn't work universally on all platforms
- Requires translation by .NET Configuration provider
- Can fail in certain environments (as we experienced)
- Not the recommended format in .NET docs

### Alternative 2: Flat Environment Variables
```yaml
BIOTRACKR_COSMOSDB_ACCOUNTKEY: value
BIOTRACKR_DATABASENAME: value
```
**Why rejected**:
- Loses hierarchical configuration structure
- Would require significant code changes to read flat keys
- Makes configuration less maintainable
- Doesn't align with appsettings.json structure

### Alternative 3: JSON String in Single Variable
```yaml
BIOTRACKR_CONFIG: '{"CosmosDb":{"AccountKey":"..."},"DatabaseName":"..."}'
```
**Why rejected**:
- Difficult to read and maintain
- Hard to override individual settings
- Complex escaping requirements
- Doesn't work with standard .NET Configuration binding

### Alternative 4: Use appsettings.{Environment}.json Only
**Why rejected**:
- Can't use secrets in configuration files (security risk)
- Doesn't work for CI/CD environments with dynamic values
- Environment variables needed for Azure deployment
- Less flexible for different environments

## Follow-up Actions

- [x] Update E2E workflow environment variables to colon format
- [x] Verify E2E tests pass with new format
- [x] Update `WeightApiWebApplicationFactory.cs` to use colon format
- [x] Document configuration format in project README
- [ ] Update other workflow templates to use colon format consistently
- [ ] Create configuration guidelines document
- [ ] Audit all environment variable usage across project
- [ ] Add linting rule to enforce colon format

## Notes

### Platform Behavior

**Windows**: Both formats work
```powershell
$env:Biotrackr:CosmosDb:AccountKey = "value"  # Works
$env:Biotrackr__CosmosDb__AccountKey = "value" # Works (translated to colon)
```

**Linux/macOS**: Colon format recommended
```bash
export "Biotrackr:CosmosDb:AccountKey=value"  # Works (requires quotes)
export Biotrackr__CosmosDb__AccountKey=value  # Works but translated
```

**GitHub Actions**: Both formats work
```yaml
env:
  Biotrackr:CosmosDb:AccountKey: value  # ✅ Works universally
  Biotrackr__CosmosDb__AccountKey: value # ❌ Failed in our testing
```

### .NET Configuration Provider Hierarchy
.NET Configuration system reads sources in this order:
1. appsettings.json
2. appsettings.{Environment}.json
3. User secrets (Development only)
4. Environment variables
5. Command-line arguments

Environment variables using `:` or `__` both map to the same configuration path, but `:` is more reliable.

### Configuration Binding Example
```csharp
// appsettings.json
{
  "Biotrackr": {
    "CosmosDb": {
      "AccountKey": "default-key"
    },
    "DatabaseName": "biotrackr"
  }
}

// Environment variable (overrides appsettings)
Biotrackr:CosmosDb:AccountKey=override-key

// Code reads as:
builder.Configuration.GetValue<string>("Biotrackr:CosmosDb:AccountKey")
// Returns: "override-key"
```

### Testing Strategy
To verify configuration format works:
1. Set environment variables in test workflow
2. Application reads via Configuration API
3. Tests validate correct values are used
4. No translation or special handling needed

### Azure App Service Configuration
Azure App Service also uses colon format for nested configuration:
```
Application Settings:
  Biotrackr:CosmosDb:AccountKey = "@Microsoft.KeyVault(...)"
  Biotrackr:DatabaseName = "biotrackr-prod"
```

This consistency across GitHub Actions and Azure simplifies configuration management.

### Migration Guide
To update existing workflows:
```yaml
# Old format ❌
env:
  Biotrackr__CosmosDb__AccountKey: ${{ secrets.COSMOS_KEY }}
  
# New format ✅
env:
  Biotrackr:CosmosDb:AccountKey: ${{ secrets.COSMOS_KEY }}
```

No code changes needed - only update workflow YAML files.
