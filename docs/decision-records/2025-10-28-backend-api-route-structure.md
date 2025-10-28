# Decision Record: Backend API Route Structure for APIM Integration

- **Status**: Accepted
- **Deciders**: willvelida, GitHub Copilot
- **Date**: 28 October 2025
- **Related Docs**: [PR #79](https://github.com/willvelida/biotrackr/pull/79), [main.bicep](../../infra/apps/weight-api/main.bicep)

## Context

During E2E test debugging, all tests failed with 404 errors. Tests were calling `/api/weight/*` endpoints, but the API wasn't responding. Initial investigation suggested adding `/api/weight` prefix to the backend routes.

However, reviewing the Bicep infrastructure configuration revealed:
- **APIM API path**: `weight` (configured in Bicep)
- **APIM operation urlTemplate**: `/` (root)
- **Backend serviceUrl**: `https://${weightApi.outputs.fqdn}`

This meant:
- Production flow: `Client → APIM (/weight) → Backend (/)`
- Test flow: `Tests → Backend (/)` (direct, no APIM)

The tests were incorrectly assuming an `/api/weight` prefix that didn't exist in the backend.

## Decision

**Backend APIs serve routes at root path (`/`), with APIM adding path prefixes for external clients.**

Route structure:
```
Backend API:
  GET /                           # GetAllWeights
  GET /{date}                     # GetWeightByDate
  GET /range/{startDate}/{endDate} # GetWeightsByDateRange
  GET /healthz/liveness           # Health check

APIM (production):
  GET /weight                     # → Backend /
  GET /weight/{date}              # → Backend /{date}
  GET /weight/range/...           # → Backend /range/...
  GET /weight/healthz/liveness    # → Backend /healthz/liveness
```

Tests call backend directly without APIM, so must use root paths.

## Consequences

### Positive
- ✅ Clean separation between API Gateway (APIM) and backend concerns
- ✅ Backend remains agnostic to external routing structure
- ✅ APIM can route multiple backends to same path space
- ✅ Backend can be tested independently without APIM
- ✅ Easier to change external API structure without backend changes
- ✅ Follows microservices best practices

### Negative
- ⚠️ Tests must use different paths than production clients
- ⚠️ Potential confusion for developers about "real" vs "test" URLs
- ⚠️ APIM configuration must be kept in sync with backend routes

### Trade-offs
- **Accepted**: Test/production URL difference for architectural flexibility
- **Mitigated**: Clear documentation in Bicep files and test comments

## Alternatives Considered

### Alternative 1: Add /api/weight Prefix to Backend Routes
**Why rejected**:
- Would create `/weight/api/weight` path in production through APIM
- Couples backend to specific APIM routing structure
- Makes backend routes longer and more complex
- Reduces flexibility to change APIM routing

### Alternative 2: Configure Different Routes for Test vs Production
**Why rejected**:
- Tests wouldn't validate actual production routes
- Increased complexity maintaining two route configurations
- Risk of test/production behavior divergence

### Alternative 3: Run APIM in Test Environment
**Why rejected**:
- Significantly increases test complexity and execution time
- APIM emulator not available/practical for CI/CD
- Contract/E2E tests should focus on backend behavior
- APIM configuration tested separately through infrastructure

### Alternative 4: Use Test-Specific BaseAddress in Client
**Why this is the solution**:
- Tests create HttpClient with `BaseAddress = backend-url`
- Production clients use `BaseAddress = apim-url/weight`
- Backend code remains unchanged
- Clear separation of concerns

## Follow-up Actions

- [x] Update E2E tests from `/api/weight/*` to `/*`
- [x] Update health check test from `/api/weight/healthz/liveness` to `/healthz/liveness`
- [x] Verify all E2E tests pass with correct paths
- [x] Document route structure in Bicep comments
- [ ] Create diagram showing APIM → Backend routing flow
- [ ] Add test documentation explaining path differences
- [ ] Consider adding APIM integration tests (separate from backend tests)

## Notes

### Route Registration Code
```csharp
// EndpointRouteBuilderExtensions.cs
var weightEndpoints = endpointRouteBuilder.MapGroup("/");
```

The `MapGroup("/")` registers routes at root, which is correct for APIM integration.

### APIM Configuration
```bicep
// main.bicep
path: 'weight'           # APIM adds this prefix
serviceUrl: 'https://${weightApi.outputs.fqdn}'  # Backend base URL
```

### Testing Strategy
- **Unit tests**: Test individual components in isolation
- **Contract tests**: Verify service registration and startup (no HTTP calls)
- **E2E tests**: Test endpoints via direct backend HTTP calls
- **APIM tests**: (Future) Test API Gateway routing and policies

### Path Confusion Resolution
When developers see different paths in tests vs Bicep:
1. **Backend serves**: Root paths (`/`)
2. **APIM exposes**: Prefixed paths (`/weight`)
3. **Tests validate**: Backend behavior (root paths)
4. **Clients use**: APIM URLs (prefixed paths)

This is correct and intentional - backend should not know about APIM routing.

### Similar Patterns in Project
All other APIs (Activity, Sleep, Food, Auth) follow the same pattern:
- Backend: Root paths
- APIM: Service-specific prefixes (`/activity`, `/sleep`, etc.)
