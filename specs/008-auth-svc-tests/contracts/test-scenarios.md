# Test Scenarios: Auth Service Test Coverage and Integration Tests

**Feature**: 008-auth-svc-tests  
**Date**: November 3, 2025  
**Phase**: 1 - Design & Contracts

## Overview

This document defines the test scenarios (contracts) for Auth Service testing. These scenarios ensure comprehensive coverage of unit tests, contract integration tests, and end-to-end integration tests.

---

## Unit Test Scenarios

### AuthWorker Tests

#### Scenario 1.1: Successful Token Refresh and Save
**Test**: `RefreshAndSaveTokensSuccessfullyWhenExecuteAsyncIsCalled`  
**Status**: ✅ Already Implemented

**Given**:
- RefreshTokenService.RefreshTokens returns valid RefreshTokenResponse
- RefreshTokenService.SaveTokens completes successfully
- IHostApplicationLifetime is configured to signal completion

**When**: AuthWorker.ExecuteAsync is called

**Then**:
- RefreshTokens is called exactly once
- SaveTokens is called exactly once with the returned token response
- Logs "Attempting to refresh FitBit Tokens" at Information level
- Logs "FitBit Tokens refresh successful. Saving to Secret Store" at Information level
- Logs "FitBit Tokens saved successfully" at Information level
- Returns exit code 0
- Calls IHostApplicationLifetime.StopApplication exactly once

---

#### Scenario 1.2: RefreshTokens Failure
**Test**: `ReturnsExitCode1WhenRefreshTokensThrowsException` ⚠️ **TO CREATE**

**Given**:
- RefreshTokenService.RefreshTokens throws HttpRequestException
- IHostApplicationLifetime is configured to signal completion

**When**: AuthWorker.ExecuteAsync is called

**Then**:
- RefreshTokens is called exactly once
- SaveTokens is never called
- Logs "Attempting to refresh FitBit Tokens" at Information level
- Logs "Exception thrown: [exception message]" at Error level
- Returns exit code 1
- Calls IHostApplicationLifetime.StopApplication exactly once (in finally block)

---

#### Scenario 1.3: SaveTokens Failure
**Test**: `ReturnsExitCode1WhenSaveTokensThrowsException` ⚠️ **TO CREATE**

**Given**:
- RefreshTokenService.RefreshTokens returns valid RefreshTokenResponse
- RefreshTokenService.SaveTokens throws RequestFailedException
- IHostApplicationLifetime is configured to signal completion

**When**: AuthWorker.ExecuteAsync is called

**Then**:
- RefreshTokens is called exactly once
- SaveTokens is called exactly once
- Logs "Attempting to refresh FitBit Tokens" at Information level
- Logs "FitBit Tokens refresh successful. Saving to Secret Store" at Information level
- Logs "Exception thrown: [exception message]" at Error level
- Returns exit code 1
- Calls IHostApplicationLifetime.StopApplication exactly once (in finally block)

---

#### Scenario 1.4: Cancellation via CancellationToken
**Test**: `StopsGracefullyWhenCancellationRequested` ⚠️ **TO CREATE**

**Given**:
- CancellationTokenSource is cancelled before ExecuteAsync completes
- IHostApplicationLifetime is configured to signal completion

**When**: AuthWorker.ExecuteAsync is called with cancellation token

**Then**:
- Operation is cancelled gracefully
- Calls IHostApplicationLifetime.StopApplication exactly once (in finally block)
- Returns appropriate exit code

---

### RefreshTokenService Tests

#### Scenario 2.1: Successful Token Refresh
**Test**: `RefreshTokensSuccessfullyWhenValidSecretsAndHttpResponseProvided`  
**Status**: ✅ Already Implemented

**Given**:
- SecretClient.GetSecretAsync("RefreshToken") returns "test-refresh-token"
- SecretClient.GetSecretAsync("FitbitCredentials") returns "test-credentials"
- HttpClient returns 200 OK with valid RefreshTokenResponse JSON

**When**: RefreshTokenService.RefreshTokens is called

**Then**:
- Returns RefreshTokenResponse with correct values
- All properties match the HTTP response data

---

#### Scenario 2.2: Correct HTTP Request Construction
**Test**: `MakeCorrectHttpRequestWhenRefreshTokensIsCalled`  
**Status**: ✅ Already Implemented

**Given**:
- SecretClient returns test secrets
- HttpMessageHandler captures request details

**When**: RefreshTokenService.RefreshTokens is called

**Then**:
- HTTP method is POST
- URL is https://api.fitbit.com/oauth2/token
- Query string contains "grant_type=refresh_token&refresh_token=[token]"
- Authorization header scheme is "Basic"
- Authorization header parameter is the FitbitCredentials value
- Content-Type header is "application/x-www-form-urlencoded"

---

#### Scenario 2.3: Missing RefreshToken Secret
**Test**: `ThrowsNullReferenceExceptionWhenRefreshTokenSecretNotFound` ⚠️ **TO CREATE**

**Given**:
- SecretClient.GetSecretAsync("RefreshToken") returns null
- SecretClient.GetSecretAsync("FitbitCredentials") returns valid value

**When**: RefreshTokenService.RefreshTokens is called

**Then**:
- Throws NullReferenceException
- Exception message contains "RefreshToken not found in secret store"
- Logs error with exception details

---

#### Scenario 2.4: Missing FitbitCredentials Secret
**Test**: `ThrowsNullReferenceExceptionWhenFitbitCredentialsSecretNotFound` ⚠️ **TO CREATE**

**Given**:
- SecretClient.GetSecretAsync("RefreshToken") returns valid value
- SecretClient.GetSecretAsync("FitbitCredentials") returns null

**When**: RefreshTokenService.RefreshTokens is called

**Then**:
- Throws NullReferenceException
- Exception message contains "FitbitCredentials not found in secret store"
- Logs error with exception details

---

#### Scenario 2.5: Fitbit API Returns HTTP 401 Unauthorized
**Test**: `ThrowsHttpRequestExceptionWhen401Unauthorized` ⚠️ **TO CREATE**

**Given**:
- SecretClient returns valid secrets
- HttpClient returns 401 Unauthorized

**When**: RefreshTokenService.RefreshTokens is called

**Then**:
- Throws HttpRequestException
- Logs error with exception details
- Exception propagates to caller

---

#### Scenario 2.6: Fitbit API Returns HTTP 429 Too Many Requests
**Test**: `ThrowsHttpRequestExceptionWhen429TooManyRequests` ⚠️ **TO CREATE**

**Given**:
- SecretClient returns valid secrets
- HttpClient returns 429 Too Many Requests

**When**: RefreshTokenService.RefreshTokens is called

**Then**:
- Throws HttpRequestException
- Logs error with exception details
- Exception propagates to caller

---

#### Scenario 2.7: Fitbit API Returns Malformed JSON
**Test**: `ThrowsJsonExceptionWhenResponseIsInvalidJson` ⚠️ **TO CREATE**

**Given**:
- SecretClient returns valid secrets
- HttpClient returns 200 OK with malformed JSON (e.g., `{"access_token": incomplete`)

**When**: RefreshTokenService.RefreshTokens is called

**Then**:
- Throws JsonException
- Logs error with exception details
- Exception propagates to caller

---

#### Scenario 2.8: Network Timeout
**Test**: `ThrowsTaskCanceledExceptionWhenNetworkTimeout` ⚠️ **TO CREATE**

**Given**:
- SecretClient returns valid secrets
- HttpClient throws TaskCanceledException (simulating timeout)

**When**: RefreshTokenService.RefreshTokens is called

**Then**:
- Throws TaskCanceledException
- Logs error with exception details
- Exception propagates to caller

---

#### Scenario 2.9: Successful Token Save
**Test**: `SaveBothTokensWhenSaveTokensIsCalled`  
**Status**: ✅ Already Implemented

**Given**:
- RefreshTokenResponse with valid tokens
- SecretClient.SetSecretAsync completes successfully

**When**: RefreshTokenService.SaveTokens is called

**Then**:
- SetSecretAsync is called for "RefreshToken" with response.RefreshToken
- SetSecretAsync is called for "AccessToken" with response.AccessToken
- Logs "Attempting to save tokens to secret store" at Information level
- Logs "Tokens saved to secret store" at Information level

---

#### Scenario 2.10: SetSecretAsync Fails for RefreshToken
**Test**: `ThrowsRequestFailedExceptionWhenSetRefreshTokenFails` ⚠️ **TO CREATE**

**Given**:
- RefreshTokenResponse with valid tokens
- SecretClient.SetSecretAsync("RefreshToken") throws RequestFailedException
- SecretClient.SetSecretAsync("AccessToken") is not called yet

**When**: RefreshTokenService.SaveTokens is called

**Then**:
- Throws RequestFailedException
- Logs error with exception details
- SetSecretAsync for "AccessToken" is not called (operation fails before second save)

---

#### Scenario 2.11: SetSecretAsync Fails for AccessToken
**Test**: `ThrowsRequestFailedExceptionWhenSetAccessTokenFails` ⚠️ **TO CREATE**

**Given**:
- RefreshTokenResponse with valid tokens
- SecretClient.SetSecretAsync("RefreshToken") completes successfully
- SecretClient.SetSecretAsync("AccessToken") throws RequestFailedException

**When**: RefreshTokenService.SaveTokens is called

**Then**:
- Throws RequestFailedException
- Logs error with exception details
- RefreshToken was successfully saved but AccessToken failed (partial failure)

---

### RefreshTokenResponse Tests

#### Scenario 3.1: Valid Model Properties
**Test**: `RefreshTokenResponseShould` class tests  
**Status**: ✅ Already Implemented (assuming basic property tests exist)

**Given**: RefreshTokenResponse with valid data

**When**: Properties are accessed

**Then**: All properties return expected values

---

## Contract Integration Test Scenarios

### ProgramStartup Tests

#### Scenario 4.1: Host Builds Successfully
**Test**: `HostBuildsSuccessfullyWithInMemoryConfiguration` ⚠️ **TO CREATE**

**Given**:
- ContractTestFixture with in-memory configuration
- All required configuration keys present

**When**: Host is built

**Then**:
- Host builds without exceptions
- ServiceProvider is not null
- Application can start (conceptually - actual start not tested in contract tests)

---

#### Scenario 4.2: Configuration Values Are Accessible
**Test**: `ConfigurationValuesAreAccessibleFromServiceProvider` ⚠️ **TO CREATE**

**Given**:
- ContractTestFixture with in-memory configuration
- Configuration keys: keyvaulturl, managedidentityclientid, applicationinsightsconnectionstring

**When**: Configuration is resolved from ServiceProvider

**Then**:
- IConfiguration can be resolved
- All required keys are present
- Values match in-memory configuration values

---

### ServiceRegistration Tests

#### Scenario 4.3: All Services Can Be Resolved
**Test**: `AllRequiredServicesCanBeResolvedFromDI` ⚠️ **TO CREATE**

**Given**:
- ContractTestFixture with fully configured ServiceProvider

**When**: Services are resolved

**Then**:
- SecretClient can be resolved
- IRefreshTokenService can be resolved
- IHostApplicationLifetime can be resolved
- ILogger<AuthWorker> can be resolved
- ILogger<RefreshTokenService> can be resolved

---

#### Scenario 4.4: Singleton Service Lifetime
**Test**: `SingletonServicesReturnSameInstanceAcrossResolutions` ⚠️ **TO CREATE**

**Given**:
- ContractTestFixture with configured ServiceProvider

**When**: SecretClient is resolved twice

**Then**:
- Both instances are the same object (reference equality)
- Validates Singleton lifetime

---

#### Scenario 4.5: Transient Service Lifetime (HttpClient-based)
**Test**: `HttpClientBasedServicesReturnDifferentInstances` ⚠️ **TO CREATE**

**Given**:
- ContractTestFixture with configured ServiceProvider

**When**: IRefreshTokenService is resolved twice from root provider

**Then**:
- Both instances are different objects (no reference equality)
- Validates Transient lifetime from AddHttpClient registration

---

#### Scenario 4.6: No Duplicate Service Registrations
**Test**: `RefreshTokenServiceHasOnlyOneRegistration` ⚠️ **TO CREATE**

**Given**:
- Program.cs service registration code

**When**: Code is reviewed for duplicate registrations

**Then**:
- Only AddHttpClient<IRefreshTokenService, RefreshTokenService> registration exists
- No separate AddScoped<IRefreshTokenService, RefreshTokenService> registration
- Validates fix for duplicate registration issue

---

#### Scenario 4.7: HttpClient Has Resilience Handler
**Test**: `RefreshTokenServiceHttpClientHasResilienceHandler` ⚠️ **TO CREATE**

**Given**:
- ContractTestFixture with configured ServiceProvider

**When**: IRefreshTokenService is resolved

**Then**:
- Service receives HttpClient with AddStandardResilienceHandler configured
- (This may be validated indirectly through handler pipeline inspection if possible)

---

## E2E Integration Test Scenarios

### RefreshTokenService E2E Tests

#### Scenario 5.1: Complete Token Refresh Workflow
**Test**: `RefreshesTokensEndToEndWithMockedDependencies` ⚠️ **TO CREATE**

**Given**:
- IntegrationTestFixture with MockSecretClient and MockHttpMessageHandler
- MockSecretClient returns test RefreshToken and FitbitCredentials
- MockHttpMessageHandler returns successful Fitbit API response

**When**: RefreshTokenService.RefreshTokens is called

**Then**:
- GetSecretAsync is called for "RefreshToken"
- GetSecretAsync is called for "FitbitCredentials"
- HTTP POST is sent to Fitbit API
- Returns parsed RefreshTokenResponse
- All properties match expected values

---

#### Scenario 5.2: Complete Token Save Workflow
**Test**: `SavesTokensEndToEndWithMockedSecretClient` ⚠️ **TO CREATE**

**Given**:
- IntegrationTestFixture with MockSecretClient
- RefreshTokenResponse with test tokens

**When**: RefreshTokenService.SaveTokens is called

**Then**:
- SetSecretAsync is called with "RefreshToken" and response.RefreshToken
- SetSecretAsync is called with "AccessToken" and response.AccessToken
- No exceptions thrown
- Operation completes successfully

---

#### Scenario 5.3: Error Handling for Missing Secrets
**Test**: `ThrowsExceptionWhenSecretNotFoundInE2EWorkflow` ⚠️ **TO CREATE**

**Given**:
- IntegrationTestFixture with MockSecretClient
- MockSecretClient.GetSecretAsync returns null for "RefreshToken"

**When**: RefreshTokenService.RefreshTokens is called

**Then**:
- Throws NullReferenceException
- Error is logged appropriately

---

#### Scenario 5.4: Error Handling for HTTP Failures
**Test**: `ThrowsExceptionWhenFitbitAPIReturnsErrorInE2EWorkflow` ⚠️ **TO CREATE**

**Given**:
- IntegrationTestFixture with MockHttpMessageHandler
- MockHttpMessageHandler returns 401 Unauthorized

**When**: RefreshTokenService.RefreshTokens is called

**Then**:
- Throws HttpRequestException
- Error is logged appropriately

---

### AuthWorker E2E Tests

#### Scenario 5.5: Complete Worker Execution Workflow
**Test**: `ExecutesCompleteWorkflowEndToEndWithMockedDependencies` ⚠️ **TO CREATE**

**Given**:
- IntegrationTestFixture with fully mocked dependencies
- MockSecretClient returns valid secrets
- MockHttpMessageHandler returns successful Fitbit response
- IHostApplicationLifetime configured to signal completion

**When**: AuthWorker.StartAsync is called and ExecuteAsync runs

**Then**:
- RefreshTokens is called on service
- SaveTokens is called on service with returned tokens
- All information logs are written
- Returns exit code 0
- Calls IHostApplicationLifetime.StopApplication

---

#### Scenario 5.6: Worker Error Handling
**Test**: `HandlesServiceErrorsGracefullyInE2EWorkflow` ⚠️ **TO CREATE**

**Given**:
- IntegrationTestFixture with mocked dependencies
- RefreshTokenService.RefreshTokens throws exception

**When**: AuthWorker.StartAsync is called and ExecuteAsync runs

**Then**:
- Exception is caught
- Error log is written
- Returns exit code 1
- Calls IHostApplicationLifetime.StopApplication (in finally block)

---

## Test Execution Filters

### Unit Tests
**Filter**: Default (all tests in Biotrackr.Auth.Svc.UnitTests project)  
**Command**: `dotnet test Biotrackr.Auth.Svc.UnitTests`  
**Expected Duration**: < 5 seconds

### Contract Tests
**Filter**: `FullyQualifiedName~Contract`  
**Command**: `dotnet test Biotrackr.Auth.Svc.IntegrationTests --filter "FullyQualifiedName~Contract"`  
**Expected Duration**: < 5 seconds

### E2E Tests
**Filter**: `FullyQualifiedName~E2E`  
**Command**: `dotnet test Biotrackr.Auth.Svc.IntegrationTests --filter "FullyQualifiedName~E2E"`  
**Expected Duration**: < 10 seconds

---

## Coverage Goals

### Overall Project Coverage
- **Target**: ≥ 70% line coverage for Biotrackr.Auth.Svc project
- **Exclusions**: Program.cs (marked with [ExcludeFromCodeCoverage])

### Component Coverage Targets
- **AuthWorker**: ≥ 80% (all scenarios including error handling)
- **RefreshTokenService**: ≥ 85% (comprehensive edge cases)
- **RefreshTokenResponse**: 100% (simple model with no logic)

### Test Type Distribution
- **Unit Tests**: ~75% of all tests (fast, isolated)
- **Contract Tests**: ~10% of all tests (DI validation)
- **E2E Tests**: ~15% of all tests (workflow validation)

---

## Test Scenario Summary

| Category | Total Scenarios | Implemented | To Create |
|----------|----------------|-------------|-----------|
| Unit Tests - AuthWorker | 4 | 1 | 3 |
| Unit Tests - RefreshTokenService | 11 | 3 | 8 |
| Unit Tests - RefreshTokenResponse | 1 | 1 | 0 |
| Contract Tests - ProgramStartup | 2 | 0 | 2 |
| Contract Tests - ServiceRegistration | 5 | 0 | 5 |
| E2E Tests - RefreshTokenService | 4 | 0 | 4 |
| E2E Tests - AuthWorker | 2 | 0 | 2 |
| **TOTAL** | **29** | **5** | **24** |

---

## Success Criteria Validation

- ✅ All test scenarios documented with clear Given/When/Then
- ✅ Coverage targets defined per component
- ✅ Test execution filters specified
- ✅ Missing tests clearly marked (⚠️ TO CREATE)
- ✅ Existing tests identified (✅ Already Implemented)
- ⏭️ Next Phase: Generate implementation tasks (Phase 2)

---

## References

- [Feature Specification](../spec.md) - Functional requirements source
- [Data Model](../data-model.md) - Test entity definitions
- [Research](../research.md) - Technical decisions and patterns
- [Weight Service Test Scenarios](../../003-weight-svc-integration-tests/contracts/) - Pattern reference
