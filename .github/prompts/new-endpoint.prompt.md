---
description: "Create a new API endpoint with build and test verification"
agent: "C# Expert"
argument-hint: "Endpoint purpose, domain, and HTTP method (e.g., 'GET activity by week in Activity.Api')"
---

Create a new API endpoint in a Biotrackr domain service with deterministic build and test verification at each stage.

## Workflow

### 1. Identify Target

Determine the target service, handler file, and repository method needed for the requested endpoint. Review existing handlers in the service for patterns to follow.

### 2. Implement Handler

Generate the handler method following Biotrackr minimal API patterns:

- Root-mounted paths: `/`, `/{date}`, `/range/{startDate}/{endDate}`
- Use `PaginationResponse<T>` for list endpoints
- Date format: `yyyy-MM-dd`
- Register the endpoint in the service's `Program.cs` or handler registration

### 3. Build Check (deterministic)

Run in the service directory:

```bash
dotnet build --no-restore -v:q
```

**STOP and fix any build errors before proceeding.** Do not move to test generation with a broken build.

### 4. Generate Unit Tests

Create unit tests following project conventions:

- Class name: `{ClassUnderTest}Should`
- Method name: `{Method}_Should{Behavior}_When{Condition}`
- AAA pattern with `// Arrange`, `// Act`, `// Assert` comments
- Use AutoFixture for test data, Moq for mocking, FluentAssertions for assertions
- Cover success path, not-found, and invalid input scenarios

### 5. Test Check (deterministic)

Run in the service directory:

```bash
dotnet test --no-build --collect:"XPlat Code Coverage" --settings ../coverage.runsettings
```

Verify:

- All tests pass
- Line coverage remains >= 70%

### 6. Fix Issues (bounded retry)

If build or test checks failed, diagnose and fix the issues. **Maximum 2 retry cycles** through steps 3-5.

### 7. Escalation

**STOP** if 2 retries are exhausted without all checks passing. Present to the user:

- The exact error messages
- What was attempted
- Assessment of the root cause

## Deliverables

- Handler method registered in the service
- Repository method (if new data access is needed)
- Unit tests with passing build and test verification
- Coverage confirmation at or above 70%
