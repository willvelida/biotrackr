@description('The name of the Reporting Api application')
param name string

@description('The image that the Reporting Api will use')
param imageName string

@description('The image that the Copilot Python sidecar will use')
param sidecarImageName string

@description('The region where the Reporting Api will be deployed')
param location string

@description('The tags that will be applied to the Reporting Api')
param tags object

@description('The name of the Container App Environment that this Reporting Api will be deployed to')
param containerAppEnvironmentName string

@description('The name of the Container Registry that this Reporting Api will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Reporting Api will use')
param uaiName string

@description('The name of the App Config Store that the Reporting Api uses')
param appConfigName string

@description('The name of the Application Insights instance')
param appInsightsName string

@description('The name of the API Management instance that this Api uses')
param apimName string

@description('The name of the Key Vault that stores secrets')
param keyVaultName string

@description('Enable JWT validation for managed identity authentication')
param enableManagedIdentityAuth bool = true

@description('Azure AD tenant ID for JWT issuer validation')
param tenantId string

@description('JWT audience to validate (uses default Azure Management API audience)')
param jwtAudience string = environment().authentication.audiences[0]

@description('The application (client) ID of the Reporting.Api agent identity blueprint')
param agentBlueprintClientId string = ''

@description('The agent identity ID of the Chat.Api caller (for A2A auth validation)')
param chatApiAgentIdentityId string = ''

@description('The name of the Storage Account for report artifacts')
param storageAccountName string

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
  name: appConfigName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource apim 'Microsoft.ApiManagement/service@2024-06-01-preview' existing = {
  name: apimName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storageAccountName
}

var apiProductName = 'Reporting'
var reportingApiEndpointConfigName = 'Biotrackr:ReportingApiUrl'

module reportingApi '../../modules/host/container-app-http-sidecar.bicep' = {
  name: 'reporting-api'
  params: {
    name: name
    location: location
    tags: tags
    containerAppEnvironmentName: containerAppEnvironmentName
    containerRegistryName: containerRegistryName
    imageName: imageName
    uaiName: uai.name
    targetPort: 8080
    externalIngress: false
    minReplicas: 0
    cpu: '0.5'
    memory: '1Gi'
    volumes: [
      {
        name: 'shared-reports'
        mountPath: '/tmp/reports'
      }
    ]
    secrets: [
      {
        name: 'github-copilot-token'
        keyVaultUrl: '${keyVault.properties.vaultUri}secrets/GitHubCopilotToken'
        identity: uai.id
      }
    ]
    healthProbes: [
      {
        type: 'Liveness'
        httpGet: {
          port: 8080
          path: '/api/healthz'
        }
        initialDelaySeconds: 15
        periodSeconds: 30
        failureThreshold: 3
        timeoutSeconds: 5
      }
    ]
    envVariables: [
      {
        name: 'azureappconfigendpoint'
        value: appConfig.properties.endpoint
      }
      {
        name: 'managedidentityclientid'
        value: uai.properties.clientId
      }
      {
        name: 'applicationinsightsconnectionstring'
        value: appInsights.properties.ConnectionString
      }
      {
        name: 'BlobStorageEndpoint'
        value: storageAccount.properties.primaryEndpoints.blob
      }
    ]
    sidecarContainers: [
      {
        name: 'copilot-python'
        image: sidecarImageName
        env: [
          {
            name: 'GITHUB_TOKEN'
            secretRef: 'github-copilot-token'
          }
        ]
        resources: {
          cpu: json('0.5')
          memory: '1Gi'
        }
        probes: [
          {
            type: 'Readiness'
            tcpSocket: {
              port: 4321
            }
            initialDelaySeconds: 5
            periodSeconds: 10
            failureThreshold: 3
            timeoutSeconds: 2
          }
        ]
        volumeMounts: [
          {
            volumeName: 'shared-reports'
            mountPath: '/tmp/reports'
          }
        ]
      }
    ]
  }
}

// APIM API definition
resource reportingApimApi 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' = {
  name: 'reporting'
  parent: apim
  properties: {
    path: 'reporting'
    displayName: 'Reporting API'
    description: 'Endpoints for Biotrackr Reporting API with A2A agent and report retrieval'
    subscriptionRequired: true
    protocols: [
      'https'
    ]
    serviceUrl: 'https://${reportingApi.outputs.fqdn}'
  }
}

// APIM Named Values for JWT validation
module reportingApiNamedValues '../../modules/apim/apim-named-values.bicep' = {
  name: 'reporting-api-named-values'
  params: {
    apimName: apim.name
    tenantId: tenantId
    jwtAudience: jwtAudience
    enableManagedIdentityAuth: enableManagedIdentityAuth
  }
}

// APIM JWT auth policy
resource reportingApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2024-06-01-preview' = {
  name: 'policy'
  parent: reportingApimApi
  properties: {
    value: enableManagedIdentityAuth
      ? loadTextContent('policy-jwt-auth.xml')
      : loadTextContent('policy-subscription-key.xml')
    format: 'xml'
  }
  dependsOn: [
    reportingApiNamedValues
  ]
}

// APIM Operations

// A2A agent card discovery
resource reportingApiAgentCard 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'reporting-agent-card'
  parent: reportingApimApi
  properties: {
    displayName: 'GetAgentCard'
    method: 'GET'
    urlTemplate: '/a2a/report/v1/card'
    description: 'A2A Agent Card discovery endpoint'
  }
}

// A2A message streaming
resource reportingApiA2AMessage 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'reporting-a2a-message'
  parent: reportingApimApi
  properties: {
    displayName: 'SendA2AMessage'
    method: 'POST'
    urlTemplate: '/a2a/report/v1/message:stream'
    description: 'Send an A2A message to the report generation agent'
  }
}

// Generate report (202 async pattern)
resource reportingApiGenerate 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'reporting-generate'
  parent: reportingApimApi
  properties: {
    displayName: 'GenerateReport'
    method: 'POST'
    urlTemplate: '/api/reports/generate'
    description: 'Start asynchronous report generation. Returns 202 with job ID.'
  }
}

// List reports
resource reportingApiListReports 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'reporting-list-reports'
  parent: reportingApimApi
  properties: {
    displayName: 'ListReports'
    method: 'GET'
    urlTemplate: '/api/reports'
    description: 'List available reports with optional filters'
    request: {
      queryParameters: [
        {
          name: 'reportType'
          description: 'Filter by report type'
          type: 'string'
          required: false
          values: []
        }
        {
          name: 'startDate'
          description: 'Filter by start date (yyyy-MM-dd)'
          type: 'string'
          required: false
          values: []
        }
        {
          name: 'endDate'
          description: 'Filter by end date (yyyy-MM-dd)'
          type: 'string'
          required: false
          values: []
        }
      ]
    }
  }
}

// Get report by job ID
resource reportingApiGetReport 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'reporting-get-report'
  parent: reportingApimApi
  properties: {
    displayName: 'GetReport'
    method: 'GET'
    urlTemplate: '/api/reports/{jobId}'
    description: 'Get report metadata and SAS URLs by job ID'
    templateParameters: [
      {
        name: 'jobId'
        description: 'The job ID of the report'
        type: 'string'
        required: true
      }
    ]
  }
}

// Health check
resource reportingApiHealthCheck 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'reporting-healthcheck'
  parent: reportingApimApi
  properties: {
    displayName: 'HealthCheck'
    method: 'GET'
    urlTemplate: '/api/healthz'
    description: 'Health Check Endpoint'
  }
}

// APIM Product
module reportingApiProduct '../../modules/apim/apim-products.bicep' = {
  name: 'reporting-product'
  params: {
    apimName: apim.name
    productName: apiProductName
    apiName: reportingApimApi.name
  }
}

// App Configuration: Reporting API APIM gateway URL
resource reportingApiEndpointSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: reportingApiEndpointConfigName
  parent: appConfig
  properties: {
    value: '${apim.properties.gatewayUrl}/reporting'
  }
}

// App Configuration: Blob Storage endpoint
resource blobStorageEndpointSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:ReportingBlobStorageEndpoint'
  parent: appConfig
  properties: {
    value: storageAccount.properties.primaryEndpoints.blob
  }
}

// App Configuration: Copilot CLI URL (localhost sidecar)
resource copilotCliUrlSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:CopilotCliUrl'
  parent: appConfig
  properties: {
    value: 'http://localhost:4321'
  }
}

// App Configuration: Chat.Api UAI principal ID for A2A caller validation
resource chatApiUaiPrincipalIdSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:ChatApiUaiPrincipalId'
  parent: appConfig
  properties: {
    value: uai.properties.principalId
  }
}

// App Configuration: Chat.Api agent identity ID for bearer token validation (ASI07)
resource chatApiAgentIdentityIdSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = if (!empty(chatApiAgentIdentityId)) {
  name: 'Biotrackr:ChatApiAgentIdentityId'
  parent: appConfig
  properties: {
    value: chatApiAgentIdentityId
  }
}

// App Configuration: Azure AD settings for Reporting.Api agent identity auth
resource azureAdInstanceSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = if (enableManagedIdentityAuth) {
  name: 'AzureAd:Instance'
  parent: appConfig
  properties: {
    value: environment().authentication.loginEndpoint
  }
}

resource azureAdTenantIdSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = if (enableManagedIdentityAuth) {
  name: 'AzureAd:TenantId'
  parent: appConfig
  properties: {
    value: tenantId
  }
}

resource azureAdClientIdSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = if (enableManagedIdentityAuth && !empty(agentBlueprintClientId)) {
  name: 'AzureAd:ClientId'
  parent: appConfig
  properties: {
    value: agentBlueprintClientId
  }
}

// App Configuration: Report Generator System Prompt (Key Vault reference)
resource reportGeneratorSystemPromptSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:ReportGeneratorSystemPrompt'
  parent: appConfig
  properties: {
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
    value: '{"uri":"${keyVault.properties.vaultUri}secrets/ReportGeneratorSystemPrompt"}'
  }
}
