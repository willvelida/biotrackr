# GitHub Actions Workflow Specifications

## Unit Test Workflow Template

### Template: `template-dotnet-run-unit-tests-with-coverage.yml`

```yaml
name: Run .NET Unit Tests with Coverage

on:
  workflow_call:
    inputs:
      dotnet-version:
        required: true
        type: string
        description: '.NET version to use'
      working-directory:
        required: true
        type: string
        description: 'Directory containing the test project'
      coverage-path:
        required: true
        type: string
        description: 'Path to store coverage reports'
      coverage-threshold:
        required: false
        type: number
        default: 80
        description: 'Minimum coverage percentage required'
      timeout-minutes:
        required: false
        type: number
        default: 5
        description: 'Maximum execution time in minutes'
    outputs:
      test-success:
        description: 'Whether all tests passed'
        value: ${{ jobs.run-tests.outputs.success }}
      coverage-percentage:
        description: 'Overall code coverage percentage'
        value: ${{ jobs.run-tests.outputs.coverage }}

jobs:
  run-tests:
    runs-on: ubuntu-latest
    timeout-minutes: ${{ inputs.timeout-minutes }}
    outputs:
      success: ${{ steps.test-execution.outputs.success }}
      coverage: ${{ steps.coverage-report.outputs.coverage }}
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.dotnet-version }}
      
      - name: Restore dependencies
        run: dotnet restore
        working-directory: ${{ inputs.working-directory }}
      
      - name: Build project
        run: dotnet build --no-restore --configuration Release
        working-directory: ${{ inputs.working-directory }}
      
      - name: Run unit tests with coverage
        id: test-execution
        run: |
          dotnet test --no-build --configuration Release \
            --collect:"XPlat Code Coverage" \
            --results-directory ${{ inputs.coverage-path }} \
            --logger "trx;LogFileName=test-results.trx" \
            --verbosity normal
          
          if [ $? -eq 0 ]; then
            echo "success=true" >> $GITHUB_OUTPUT
          else
            echo "success=false" >> $GITHUB_OUTPUT
            exit 1
          fi
        working-directory: ${{ inputs.working-directory }}
      
      - name: Generate coverage report
        id: coverage-report
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator \
            -reports:"${{ inputs.coverage-path }}/**/coverage.cobertura.xml" \
            -targetdir:"${{ inputs.coverage-path }}/html" \
            -reporttypes:"Html;Cobertura" \
            -verbosity:Info
          
          coverage=$(grep -oP 'line-rate="\K[^"]*' ${{ inputs.coverage-path }}/Cobertura.xml | head -1)
          coverage_percent=$(echo "$coverage * 100" | bc -l)
          echo "coverage=${coverage_percent}" >> $GITHUB_OUTPUT
          
          echo "Coverage: ${coverage_percent}%"
          
          if (( $(echo "$coverage_percent >= ${{ inputs.coverage-threshold }}" | bc -l) )); then
            echo "Coverage meets threshold of ${{ inputs.coverage-threshold }}%"
          else
            echo "Coverage below threshold of ${{ inputs.coverage-threshold }}%"
            exit 1
          fi
      
      - name: Upload coverage reports
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: coverage-report-${{ github.run_number }}
          path: ${{ inputs.coverage-path }}
          retention-days: 30
      
      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results-${{ github.run_number }}
          path: ${{ inputs.working-directory }}/TestResults
          retention-days: 30
```

## Integration Test Workflow Template

### Template: `template-dotnet-run-integration-tests.yml`

```yaml
name: Run .NET Integration Tests

on:
  workflow_call:
    inputs:
      dotnet-version:
        required: true
        type: string
        description: '.NET version to use'
      working-directory:
        required: true
        type: string
        description: 'Directory containing the integration test project'
      api-base-url:
        required: true
        type: string
        description: 'Base URL of the deployed API'
      timeout-minutes:
        required: false
        type: number
        default: 15
        description: 'Maximum execution time in minutes'
    secrets:
      client-id:
        required: true
        description: 'Azure AD client ID'
      tenant-id:
        required: true
        description: 'Azure AD tenant ID'
      subscription-id:
        required: true
        description: 'Azure subscription ID'
      test-database-connection:
        required: true
        description: 'Test database connection string'
    outputs:
      test-success:
        description: 'Whether all integration tests passed'
        value: ${{ jobs.run-integration-tests.outputs.success }}

jobs:
  run-integration-tests:
    runs-on: ubuntu-latest
    timeout-minutes: ${{ inputs.timeout-minutes }}
    outputs:
      success: ${{ steps.test-execution.outputs.success }}
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.dotnet-version }}
      
      - name: Azure login
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.client-id }}
          tenant-id: ${{ secrets.tenant-id }}
          subscription-id: ${{ secrets.subscription-id }}
      
      - name: Restore dependencies
        run: dotnet restore
        working-directory: ${{ inputs.working-directory }}
      
      - name: Build project
        run: dotnet build --no-restore --configuration Release
        working-directory: ${{ inputs.working-directory }}
      
      - name: Run integration tests
        id: test-execution
        env:
          API_BASE_URL: ${{ inputs.api-base-url }}
          TEST_DATABASE_CONNECTION: ${{ secrets.test-database-connection }}
          AZURE_CLIENT_ID: ${{ secrets.client-id }}
          AZURE_TENANT_ID: ${{ secrets.tenant-id }}
        run: |
          dotnet test --no-build --configuration Release \
            --logger "trx;LogFileName=integration-test-results.trx" \
            --verbosity normal
          
          if [ $? -eq 0 ]; then
            echo "success=true" >> $GITHUB_OUTPUT
          else
            echo "success=false" >> $GITHUB_OUTPUT
            exit 1
          fi
        working-directory: ${{ inputs.working-directory }}
      
      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: integration-test-results-${{ github.run_number }}
          path: ${{ inputs.working-directory }}/TestResults
          retention-days: 30
      
      - name: Cleanup test data
        if: always()
        run: |
          echo "Cleaning up test data..."
          # Add cleanup commands here if needed
```

## Enhanced Weight API Deployment Workflow

### Addition to `deploy-weight-api.yml`

```yaml
# Add this job after deploy-dev and before any production deployment
run-integration-tests:
  name: Run Integration Tests
  needs: deploy-dev
  uses: willvelida/biotrackr/.github/workflows/template-dotnet-run-integration-tests.yml@main
  with:
    dotnet-version: ${{ needs.env-setup.outputs.dotnet-version }}
    working-directory: ./src/Biotrackr.Weight.Api/Biotrackr.Weight.Api.IntegrationTests
    api-base-url: ${{ needs.deploy-dev.outputs.app-url }}
  secrets:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
    test-database-connection: ${{ secrets.TEST_DATABASE_CONNECTION }}
```

## Coverage Reporting Integration

### Addition to existing unit test job

```yaml
# Enhance the existing run-unit-tests job
run-unit-tests:
  name: Run Unit Tests with Enhanced Coverage
  needs: env-setup
  uses: willvelida/biotrackr/.github/workflows/template-dotnet-run-unit-tests-with-coverage.yml@main
  with:
    dotnet-version: ${{ needs.env-setup.outputs.dotnet-version }}
    working-directory: ./src/Biotrackr.Weight.Api/Biotrackr.Weight.Api.UnitTests
    coverage-path: ${{ needs.env-setup.outputs.coverage-path }}
    coverage-threshold: 80
    timeout-minutes: 5
```

## Quality Gates Integration

### Addition to workflow for quality gates

```yaml
# Add this job to enforce quality gates
quality-gates:
  name: Enforce Quality Gates
  needs: [run-unit-tests, run-integration-tests]
  runs-on: ubuntu-latest
  if: always()
  
  steps:
    - name: Check unit test results
      if: needs.run-unit-tests.outputs.test-success != 'true'
      run: |
        echo "Unit tests failed - blocking deployment"
        exit 1
    
    - name: Check integration test results
      if: needs.run-integration-tests.outputs.test-success != 'true'
      run: |
        echo "Integration tests failed - blocking deployment"
        exit 1
    
    - name: Check coverage threshold
      if: needs.run-unit-tests.outputs.coverage-percentage < 80
      run: |
        echo "Coverage below 80% threshold - blocking deployment"
        echo "Current coverage: ${{ needs.run-unit-tests.outputs.coverage-percentage }}%"
        exit 1
    
    - name: Quality gates passed
      run: |
        echo "All quality gates passed successfully"
        echo "Unit test coverage: ${{ needs.run-unit-tests.outputs.coverage-percentage }}%"
```