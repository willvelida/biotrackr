# Contract Test Scenarios

**Date**: October 28, 2025  
**Feature**: Weight Service Integration Tests - Contract Tests

## Overview

Contract tests verify service registration, dependency injection configuration, and basic service initialization without external dependencies. These tests execute quickly (<2 seconds total) and provide fast feedback on configuration issues.

---

## Test Class: ProgramStartupTests

**Purpose**: Verify application startup and service configuration  
**Fixture**: ContractTestFixture  
**Collection**: "Contract Tests"

### Test: Application_Builds_Service_Provider_Successfully

**Scenario**: Application can build the service provider with all required registrations

**Given**:
- ContractTestFixture is initialized
- Minimal configuration is provided

**When**:
- ServiceProvider is accessed

**Then**:
- ServiceProvider is not null
- ServiceProvider can be used to resolve services

**Implementation**:
```csharp
[Fact]
public void Application_Builds_Service_Provider_Successfully()
{
    // Arrange & Act
    var serviceProvider = _fixture.ServiceProvider;
    
    // Assert
    serviceProvider.Should().NotBeNull();
}
```

**Expected Result**: Test passes, ServiceProvider created successfully

---

### Test: All_Required_Services_Are_Registered

**Scenario**: All required services can be resolved from the DI container

**Given**:
- ServiceProvider is built with production configuration

**When**:
- Each required service is resolved

**Then**:
- ICosmosRepository resolves to CosmosRepository
- IWeightService resolves to WeightService
- IFitbitService resolves to FitbitService
- ILogger instances can be resolved

**Implementation**:
```csharp
[Fact]
public void All_Required_Services_Are_Registered()
{
    // Arrange
    var serviceProvider = _fixture.ServiceProvider;
    
    // Act & Assert
    var cosmosRepository = serviceProvider.GetService<ICosmosRepository>();
    cosmosRepository.Should().NotBeNull();
    cosmosRepository.Should().BeOfType<CosmosRepository>();
    
    var weightService = serviceProvider.GetService<IWeightService>();
    weightService.Should().NotBeNull();
    weightService.Should().BeOfType<WeightService>();
    
    var fitbitService = serviceProvider.GetService<IFitbitService>();
    fitbitService.Should().NotBeNull();
    fitbitService.Should().BeOfType<FitbitService>();
}
```

**Expected Result**: All services resolve to correct implementations

---

### Test: Settings_Are_Bound_From_Configuration

**Scenario**: Settings class is properly configured from IConfiguration

**Given**:
- Configuration contains Biotrackr section
- Settings are registered with Options pattern

**When**:
- Settings are resolved from DI container

**Then**:
- Settings object is not null
- DatabaseName matches configuration value
- ContainerName matches configuration value

**Implementation**:
```csharp
[Fact]
public void Settings_Are_Bound_From_Configuration()
{
    // Arrange
    var serviceProvider = _fixture.ServiceProvider;
    
    // Act
    var settings = serviceProvider.GetService<IOptions<Settings>>();
    
    // Assert
    settings.Should().NotBeNull();
    settings.Value.Should().NotBeNull();
    settings.Value.DatabaseName.Should().Be("test");
    settings.Value.ContainerName.Should().Be("test");
}
```

**Expected Result**: Settings bound correctly from configuration

---

## Test Class: ServiceRegistrationTests

**Purpose**: Verify service lifetime and registration patterns  
**Fixture**: ContractTestFixture  
**Collection**: "Contract Tests"

### Test: CosmosRepository_Is_Registered_As_Scoped

**Scenario**: CosmosRepository uses scoped lifetime

**Given**:
- ServiceProvider is configured

**When**:
- CosmosRepository is resolved twice from same scope
- CosmosRepository is resolved from different scopes

**Then**:
- Same instance returned within scope
- Different instances returned across scopes

**Implementation**:
```csharp
[Fact]
public void CosmosRepository_Is_Registered_As_Scoped()
{
    // Arrange
    var serviceProvider = _fixture.ServiceProvider;
    
    // Act - Same scope
    using (var scope1 = serviceProvider.CreateScope())
    {
        var repo1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
        var repo2 = scope1.ServiceProvider.GetService<ICosmosRepository>();
        
        // Assert - Same instance in same scope
        repo1.Should().BeSameAs(repo2);
    }
    
    // Act - Different scopes
    ICosmosRepository repoScope1;
    ICosmosRepository repoScope2;
    
    using (var scope1 = serviceProvider.CreateScope())
    {
        repoScope1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
    }
    
    using (var scope2 = serviceProvider.CreateScope())
    {
        repoScope2 = scope2.ServiceProvider.GetService<ICosmosRepository>();
    }
    
    // Assert - Different instances across scopes
    repoScope1.Should().NotBeSameAs(repoScope2);
}
```

**Expected Result**: Scoped lifetime behavior verified

---

### Test: WeightService_Is_Registered_As_Scoped

**Scenario**: WeightService uses scoped lifetime

**Given**:
- ServiceProvider is configured

**When**:
- WeightService is resolved from different scopes

**Then**:
- Service follows scoped lifetime pattern

**Implementation**:
```csharp
[Fact]
public void WeightService_Is_Registered_As_Scoped()
{
    // Arrange
    var serviceProvider = _fixture.ServiceProvider;
    
    // Act & Assert
    IWeightService serviceScope1;
    IWeightService serviceScope2;
    
    using (var scope1 = serviceProvider.CreateScope())
    {
        serviceScope1 = scope1.ServiceProvider.GetService<IWeightService>();
        serviceScope1.Should().NotBeNull();
    }
    
    using (var scope2 = serviceProvider.CreateScope())
    {
        serviceScope2 = scope2.ServiceProvider.GetService<IWeightService>();
        serviceScope2.Should().NotBeNull();
    }
    
    serviceScope1.Should().NotBeSameAs(serviceScope2);
}
```

**Expected Result**: Scoped lifetime behavior verified

---

### Test: FitbitService_Is_Registered_As_Scoped

**Scenario**: FitbitService uses scoped lifetime

**Given**:
- ServiceProvider is configured

**When**:
- FitbitService is resolved from different scopes

**Then**:
- Service follows scoped lifetime pattern

**Implementation**:
```csharp
[Fact]
public void FitbitService_Is_Registered_As_Scoped()
{
    // Arrange
    var serviceProvider = _fixture.ServiceProvider;
    
    // Act & Assert
    IFitbitService serviceScope1;
    IFitbitService serviceScope2;
    
    using (var scope1 = serviceProvider.CreateScope())
    {
        serviceScope1 = scope1.ServiceProvider.GetService<IFitbitService>();
        serviceScope1.Should().NotBeNull();
    }
    
    using (var scope2 = serviceProvider.CreateScope())
    {
        serviceScope2 = scope2.ServiceProvider.GetService<IFitbitService>();
        serviceScope2.Should().NotBeNull();
    }
    
    serviceScope1.Should().NotBeSameAs(serviceScope2);
}
```

**Expected Result**: Scoped lifetime behavior verified

---

### Test: Singleton_Dependencies_Are_Registered_Correctly

**Scenario**: Singleton dependencies (CosmosClient, SecretClient) are registered correctly

**Given**:
- ServiceProvider is configured with singleton mocks

**When**:
- Singleton services are resolved multiple times

**Then**:
- Same instance returned each time

**Implementation**:
```csharp
[Fact]
public void Singleton_Dependencies_Are_Registered_Correctly()
{
    // Arrange
    var serviceProvider = _fixture.ServiceProvider;
    
    // Act
    var cosmosClient1 = serviceProvider.GetService<CosmosClient>();
    var cosmosClient2 = serviceProvider.GetService<CosmosClient>();
    
    var secretClient1 = serviceProvider.GetService<SecretClient>();
    var secretClient2 = serviceProvider.GetService<SecretClient>();
    
    // Assert
    cosmosClient1.Should().BeSameAs(cosmosClient2);
    secretClient1.Should().BeSameAs(secretClient2);
}
```

**Expected Result**: Singleton lifetime behavior verified

---

### Test: HttpClient_Factory_Is_Configured

**Scenario**: HttpClient factory is configured for FitbitService

**Given**:
- ServiceProvider includes HttpClient configuration

**When**:
- HttpClient is requested through IHttpClientFactory

**Then**:
- HttpClient is created successfully
- Resilience policies are applied

**Implementation**:
```csharp
[Fact]
public void HttpClient_Factory_Is_Configured()
{
    // Arrange
    var serviceProvider = _fixture.ServiceProvider;
    
    // Act
    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
    var httpClient = httpClientFactory?.CreateClient();
    
    // Assert
    httpClientFactory.Should().NotBeNull();
    httpClient.Should().NotBeNull();
}
```

**Expected Result**: HttpClient factory configured correctly

---

## Test Class: ConfigurationTests

**Purpose**: Verify configuration loading and binding  
**Fixture**: ContractTestFixture  
**Collection**: "Contract Tests"

### Test: Configuration_Values_Are_Loaded

**Scenario**: Configuration values are accessible

**Given**:
- Configuration is built with test values

**When**:
- Configuration is resolved from DI

**Then**:
- Configuration contains expected keys
- Values match test configuration

**Implementation**:
```csharp
[Fact]
public void Configuration_Values_Are_Loaded()
{
    // Arrange
    var serviceProvider = _fixture.ServiceProvider;
    
    // Act
    var configuration = serviceProvider.GetService<IConfiguration>();
    
    // Assert
    configuration.Should().NotBeNull();
    configuration["cosmosdbendpoint"].Should().Be("https://localhost:8081");
    configuration["Biotrackr:DatabaseName"].Should().Be("test");
    configuration["Biotrackr:ContainerName"].Should().Be("test");
}
```

**Expected Result**: Configuration values loaded correctly

---

### Test: Options_Pattern_Works_For_Settings

**Scenario**: Options pattern correctly binds Settings

**Given**:
- Settings are registered with IOptions<Settings>

**When**:
- IOptions<Settings> is resolved

**Then**:
- Settings value is populated
- Settings reflect configuration values

**Implementation**:
```csharp
[Fact]
public void Options_Pattern_Works_For_Settings()
{
    // Arrange
    var serviceProvider = _fixture.ServiceProvider;
    
    // Act
    var options = serviceProvider.GetService<IOptions<Settings>>();
    
    // Assert
    options.Should().NotBeNull();
    options.Value.Should().NotBeNull();
    options.Value.DatabaseName.Should().NotBeNullOrEmpty();
    options.Value.ContainerName.Should().NotBeNullOrEmpty();
}
```

**Expected Result**: Options pattern binding works correctly

---

## Test Execution

### Run All Contract Tests
```bash
dotnet test --filter "FullyQualifiedName~Contract"
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~ProgramStartupTests"
```

### Expected Performance
- Total execution time: <2 seconds
- All tests should pass
- No external dependencies required

---

## Success Criteria

Contract tests are successful when:
1. All tests pass consistently
2. Total execution time <2 seconds
3. No external dependencies (Cosmos DB, Key Vault, Fitbit API) required
4. Tests can run in parallel without conflicts
5. Service registration changes detected immediately
6. Configuration issues identified before E2E tests run

---

## Coverage

Contract tests provide coverage for:
- Program.cs service registration (excluded from coverage by attribute)
- Settings configuration binding
- DI container setup
- Service lifetime verification
- HttpClient factory configuration

These tests do NOT cover:
- Actual service logic (covered by unit tests)
- Database operations (covered by E2E tests)
- External API calls (covered by E2E tests)
- End-to-end workflows (covered by E2E tests)
