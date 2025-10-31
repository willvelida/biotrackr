# Test Execution Contracts: Sleep API Test Coverage

**Feature**: 006-sleep-api-tests  
**Date**: 2025-10-31

## Overview

This document defines the contracts (expected behaviors and interfaces) for the Sleep API test suite. Since this is a testing feature, these contracts describe what each test category validates and the expected test execution patterns.

## Unit Test Contracts

### Contract: SleepHandlers Unit Tests

**Coverage Target**: All endpoint handler methods with various input scenarios

**Test Categories**:

1. **GetSleepByDate Handler**
   - ✅ MUST return Ok with SleepDocument when document exists
   - ✅ MUST return NotFound when document doesn't exist
   - ✅ MUST call repository with correct date parameter
   - ✅ MUST propagate exceptions from repository (Exception, ArgumentException, TimeoutException, TaskCanceledException)

2. **GetAllSleeps Handler**
   - ✅ MUST return Ok with PaginationResponse containing sleep documents
   - ✅ MUST return paginated results when pagination parameters provided
   - ✅ MUST use default pagination when parameters omitted (page 1, size 20)
   - ✅ MUST use default page size when only page number provided
   - ✅ MUST use default page number when only page size provided
   - ✅ MUST call repository with correct PaginationRequest
   - ✅ MUST propagate exceptions from repository

3. **GetSleepsByDateRange Handler**
   - ✅ MUST return Ok with PaginationResponse when valid dates provided
   - ✅ MUST return BadRequest when start date is invalid
   - ✅ MUST return BadRequest when end date is invalid
   - ✅ MUST return BadRequest when both dates are invalid
   - ✅ MUST return BadRequest when start date is after end date
   - ✅ MUST return Ok when start date equals end date
   - ✅ MUST use default pagination when parameters omitted
   - ✅ MUST use provided pagination parameters correctly
   - ✅ MUST return empty result when no documents in range
   - ✅ MUST propagate exceptions from repository
   - ✅ MUST handle various valid date formats (ISO 8601)
   - ✅ MUST handle various invalid date formats

**Test Count**: 83 existing tests (to be verified for coverage completeness)

---

### Contract: CosmosRepository Unit Tests

**Coverage Target**: All repository methods with mocked Cosmos SDK

**Test Categories**:

1. **GetSleepSummaryByDate Method**
   - MUST return SleepDocument when found
   - MUST return null when not found
   - MUST query correct partition key (documentType: "Sleep")
   - MUST log appropriate information messages
   - MUST handle Cosmos exceptions (CosmosException, TaskCanceledException)

2. **GetAllSleepDocuments Method**
   - MUST return PaginationResponse with items and metadata
   - MUST apply pagination parameters correctly
   - MUST handle continuation tokens
   - MUST count total items correctly
   - MUST calculate total pages correctly
   - MUST handle empty result sets
   - MUST log information messages

3. **GetSleepDocumentsByDateRange Method**
   - MUST return PaginationResponse for date range
   - MUST construct correct date range query
   - MUST apply pagination to range queries
   - MUST handle start date = end date
   - MUST handle empty ranges
   - MUST log information messages

**Test Count**: Existing (to be analyzed and expanded)

---

### Contract: Model Validation Tests

**Coverage Target**: Model classes for proper validation and behavior

**New Test Classes Needed**:

1. **SettingsShould**
   - MUST have DatabaseName property
   - MUST have ContainerName property
   - MUST allow property initialization
   - MUST not accept null values (if using required properties)

2. **PaginationRequestShould**
   - MUST default PageNumber to 1 when null
   - MUST default PageSize to 20 when null
   - MUST accept valid PageNumber (≥1)
   - MUST accept valid PageSize (1-100)
   - MUST handle null values appropriately

3. **FitbitEntitiesShould**
   - MUST properly serialize/deserialize Sleep entity
   - MUST properly serialize/deserialize Levels entity
   - MUST properly serialize/deserialize Summary entity
   - MUST handle nested structures correctly
   - MUST preserve all properties during round-trip

**Test Count**: ~15-20 new tests estimated

---

### Contract: Extension Methods Tests

**Coverage Target**: EndpointRouteBuilderExtensions

**Test Categories**:

1. **RegisterSleepEndpoints Method**
   - MUST register GET / endpoint
   - MUST register GET /{date} endpoint
   - MUST register GET /range/{startDate}/{endDate} endpoint
   - MUST assign correct endpoint names
   - MUST configure OpenAPI metadata
   - MUST map to correct handler methods

**Test Count**: ~6-8 new tests estimated

---

## Contract Test Contracts (Integration)

### Contract: Program Startup Tests

**Purpose**: Verify application boots correctly without database

**Test Cases**:

1. **Service Registration**
   - MUST register CosmosClient as Singleton
   - MUST register ICosmosRepository as Scoped
   - MUST register Settings via IOptions<Settings>
   - MUST register HealthChecks
   - MUST NOT have duplicate service registrations

2. **Application Configuration**
   - MUST load configuration from environment
   - MUST configure middleware pipeline correctly
   - MUST enable OpenAPI/Swagger in development
   - MUST configure CORS if applicable

**Expected Duration**: <30 seconds per test

---

### Contract: Service Registration Tests

**Purpose**: Verify dependency injection configured correctly

**Test Cases**:

1. **Service Lifetime Verification**
   - CosmosClient MUST be Singleton (same instance across scopes)
   - ICosmosRepository MUST be Scoped (same instance within scope, different across scopes)
   - Settings MUST be Singleton (via IOptions)

2. **Service Resolution**
   - MUST resolve CosmosClient without exceptions
   - MUST resolve ICosmosRepository without exceptions
   - MUST resolve Settings without exceptions

**Expected Duration**: <15 seconds per test

---

### Contract: API Smoke Tests

**Purpose**: Verify basic API functionality without database

**Test Cases**:

1. **Health Endpoints**
   - GET /healthz/liveness MUST return 200 OK
   - GET /healthz/readiness MUST return 200 OK (or 503 if dependencies unavailable)

2. **Swagger Endpoints**
   - GET /swagger/v1/swagger.json MUST return 200 OK with OpenAPI spec
   - OpenAPI spec MUST include all sleep endpoints

**Expected Duration**: <20 seconds per test

---

## E2E Test Contracts (Integration)

### Contract: Health Check Integration Tests

**Purpose**: Verify health checks with real dependencies

**Test Cases**:

1. **Liveness Check**
   - GET /healthz/liveness MUST return 200 OK
   - Response MUST be immediate (no database check)

2. **Readiness Check**
   - GET /healthz/readiness MUST return 200 OK when Cosmos DB accessible
   - Response MUST verify database connectivity

**Expected Duration**: <60 seconds per test

---

### Contract: Sleep Endpoint Integration Tests

**Purpose**: Verify all sleep endpoints with real Cosmos DB

**Prerequisites**: 
- Clear container before each test
- Use test database/container names

**Test Cases**:

1. **GET /{date} Endpoint**
   - MUST return 404 when no data exists for date
   - MUST return 200 with SleepDocument when data exists
   - MUST return correct document for requested date
   - MUST handle invalid date format (400 Bad Request)

2. **GET / Endpoint**
   - MUST return 200 with empty PaginationResponse when no data exists
   - MUST return 200 with populated PaginationResponse when data exists
   - MUST respect pagination parameters (pageNumber, pageSize)
   - MUST calculate TotalPages correctly
   - MUST handle default pagination (page 1, size 20)

3. **GET /range/{startDate}/{endDate} Endpoint**
   - MUST return 200 with empty results when no data in range
   - MUST return 200 with documents within range when data exists
   - MUST exclude documents outside range
   - MUST include documents on boundary dates
   - MUST handle invalid date formats (400 Bad Request)
   - MUST handle start date > end date (400 Bad Request)
   - MUST respect pagination parameters

**Expected Duration**: <120 seconds per test

---

### Contract: Cosmos Repository Integration Tests

**Purpose**: Verify repository methods with real Cosmos DB

**Prerequisites**:
- Clear container before each test
- Seed test data as needed

**Test Cases**:

1. **GetSleepSummaryByDate**
   - MUST return null when document doesn't exist
   - MUST return SleepDocument when document exists
   - MUST query correct partition key

2. **GetAllSleepDocuments**
   - MUST return empty response when container empty
   - MUST return paginated results when data exists
   - MUST handle continuation tokens correctly
   - MUST count total items accurately

3. **GetSleepDocumentsByDateRange**
   - MUST return documents within specified range
   - MUST handle date boundaries correctly
   - MUST paginate range results

**Expected Duration**: <180 seconds per test

---

## Test Isolation Contract

**Critical Requirements**:

1. **Container Cleanup**
   - E2E tests MUST call `ClearContainerAsync()` before each test
   - Cleanup MUST remove ALL documents from container
   - Cleanup MUST handle empty containers gracefully

2. **Test Independence**
   - Tests MUST NOT depend on execution order
   - Tests MUST NOT share mutable state
   - Tests MUST produce identical results when run individually or in suite

3. **Data Seeding**
   - Each test MUST seed its own required data
   - Seeded data MUST be predictable (not randomized unless testing randomness)
   - Seeded data MUST be cleaned up by container cleanup

---

## Performance Contracts

**Execution Time Limits**:

| Test Category | Maximum Duration | Target Duration |
|--------------|-----------------|-----------------|
| Unit Tests (all) | 5 minutes | 2 minutes |
| Contract Tests (all) | 10 minutes | 3 minutes |
| E2E Tests (all) | 15 minutes | 8 minutes |
| Single Unit Test | 10 seconds | <1 second |
| Single Contract Test | 60 seconds | 15 seconds |
| Single E2E Test | 120 seconds | 30 seconds |

**Coverage Requirements**:

| Component | Minimum Coverage | Target Coverage |
|-----------|-----------------|-----------------|
| Overall | 80% | 85% |
| Handlers | 80% | 90% |
| Repository | 80% | 85% |
| Models | 70% | 80% |
| Extensions | 80% | 90% |
| Configuration | 60% | 70% |

---

## Failure Handling Contracts

**Test Failure Reporting**:

1. **Failed Assertions**
   - MUST use FluentAssertions for clear error messages
   - MUST include actual vs expected values
   - MUST include context (e.g., "because exactly one document should be saved")

2. **Exception Handling**
   - Expected exceptions MUST be caught with `Assert.ThrowsAsync<T>`
   - Unexpected exceptions MUST cause test failure with full stack trace
   - Database connection failures MUST fail with clear error message

3. **CI/CD Integration**
   - Failed tests MUST report to GitHub Checks via dorny/test-reporter
   - Failed tests MUST prevent merge (if required checks enabled)
   - Coverage reports MUST be uploaded as artifacts

---

## GitHub Actions Workflow Contract

**Required Permissions**:
```yaml
permissions:
  contents: read
  id-token: write
  pull-requests: write
  checks: write  # For test-reporter action
```

**Job Dependencies**:
```
env-setup
  ↓
run-unit-tests ──┐
run-contract-tests ┘
  ↓
run-e2e-tests (requires Cosmos DB Emulator)
```

**Test Filters**:
- Unit Tests: No filter (entire UnitTests project)
- Contract Tests: `--filter "FullyQualifiedName~Contract"`
- E2E Tests: `--filter "FullyQualifiedName~E2E"`

**Working Directory Pattern**:
- Unit Tests: `./src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.UnitTests`
- Contract Tests: `./src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests`
- E2E Tests: `./src/Biotrackr.Sleep.Api/Biotrackr.Sleep.Api.IntegrationTests`

---

## References

- Test Pyramid: https://martinfowler.com/articles/practical-test-pyramid.html
- xUnit Best Practices: https://xunit.net/docs/getting-started/netcore/cmdline
- FluentAssertions: https://fluentassertions.com/
- Cosmos DB Testing: https://learn.microsoft.com/en-us/azure/cosmos-db/emulator
