@description('The name of the Chat Api application')
param name string

@description('The image that the Chat Api will use')
param imageName string

@description('The region where the Chat Api will be deployed')
param location string

@description('The tags that will be applied to the Chat Api')
param tags object

@description('The name of the Container App Environment that this Chat Api will be deployed to')
param containerAppEnvironmentName string

@description('The name of the Container Registry that this Chat Api will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Chat Api will use')
param uaiName string

@description('The name of the App Config Store that the Chat Api uses')
param appConfigName string

@description('The name of the Cosmos DB account that this Chat Api uses')
param cosmosDbAccountName string

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

@description('The Claude model to use for the chat agent')
param chatAgentModel string = 'claude-sonnet-4-6'

@description('The application (client) ID of the agent identity blueprint')
param agentBlueprintClientId string

@description('The agent identity ID for Cosmos DB access')
param agentIdentityId string

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
  name: appConfigName
}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' existing = {
  name: cosmosDbAccountName
}

resource apim 'Microsoft.ApiManagement/service@2024-06-01-preview' existing = {
  name: apimName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

var apiProductName = 'Chat'
var chatApiEndpointConfigName = 'Biotrackr:ChatApiUrl'

module chatApi '../../modules/host/container-app-http.bicep' = {
  name: 'chat-api'
  params: {
    name: name
    location: location
    tags: tags
    containerAppEnvironmentName: containerAppEnvironmentName
    containerRegistryName: containerRegistryName
    imageName: imageName
    uaiName: uai.name
    targetPort: 8080
    healthProbes: [
      {
        type: 'Liveness'
        httpGet: {
          port: 8080
          path: '/healthz/liveness'
        }
        initialDelaySeconds: 15
        periodSeconds: 30
        failureThreshold: 3
        timeoutSeconds: 1
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
    ]
  }
}

// APIM API definition
resource chatApimApi 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' = {
  name: 'chat'
  parent: apim
  properties: {
    path: 'chat'
    displayName: 'Chat API'
    description: 'Endpoints for Biotrackr Chat Agent API'
    subscriptionRequired: true
    protocols: [
      'https'
    ]
    serviceUrl: 'https://${chatApi.outputs.fqdn}'
  }
}

// APIM Named Values for JWT validation
module chatApiNamedValues '../../modules/apim/apim-named-values.bicep' = {
  name: 'chat-api-named-values'
  params: {
    apimName: apim.name
    tenantId: tenantId
    jwtAudience: jwtAudience
    enableManagedIdentityAuth: enableManagedIdentityAuth
  }
}

// APIM JWT auth policy
resource chatApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2024-06-01-preview' = {
  name: 'policy'
  parent: chatApimApi
  properties: {
    value: enableManagedIdentityAuth
      ? loadTextContent('policy-jwt-auth.xml')
      : loadTextContent('policy-subscription-key.xml')
    format: 'xml'
  }
  dependsOn: [
    chatApiNamedValues
  ]
}

// APIM Operations
resource chatApiPost 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'chat-post'
  parent: chatApimApi
  properties: {
    displayName: 'SendChatMessage'
    method: 'POST'
    urlTemplate: '/'
    description: 'Send a chat message to the AI agent via AG-UI protocol (SSE streaming response)'
  }
}

resource chatApiGetConversations 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'chat-getconversations'
  parent: chatApimApi
  properties: {
    displayName: 'GetConversations'
    method: 'GET'
    urlTemplate: '/conversations'
    description: 'Get all conversations with pagination'
    request: {
      queryParameters: [
        {
          name: 'pageNumber'
          description: 'The page number to retrieve (default: 1)'
          type: 'integer'
          required: false
          defaultValue: '1'
          values: []
        }
        {
          name: 'pageSize'
          description: 'The number of items per page (default: 20, max: 100)'
          type: 'integer'
          required: false
          defaultValue: '20'
          values: []
        }
      ]
    }
  }
}

resource chatApiGetConversation 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'chat-getconversation'
  parent: chatApimApi
  properties: {
    displayName: 'GetConversation'
    method: 'GET'
    urlTemplate: '/conversations/{sessionId}'
    description: 'Get a specific conversation by session ID'
    templateParameters: [
      {
        name: 'sessionId'
        description: 'The session ID of the conversation'
        type: 'string'
        required: true
      }
    ]
  }
}

resource chatApiDeleteConversation 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'chat-deleteconversation'
  parent: chatApimApi
  properties: {
    displayName: 'DeleteConversation'
    method: 'DELETE'
    urlTemplate: '/conversations/{sessionId}'
    description: 'Delete a conversation by session ID'
    templateParameters: [
      {
        name: 'sessionId'
        description: 'The session ID of the conversation to delete'
        type: 'string'
        required: true
      }
    ]
  }
}

resource chatApiHealthCheck 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'chat-healthcheck'
  parent: chatApimApi
  properties: {
    displayName: 'LivenessCheck'
    method: 'GET'
    urlTemplate: '/healthz/liveness'
    description: 'Liveness Health Check Endpoint'
  }
}

// APIM Product
module chatApiProduct '../../modules/apim/apim-products.bicep' = {
  name: 'chat-product'
  params: {
    apimName: apim.name
    productName: apiProductName
    apiName: chatApimApi.name
  }
}

// APIM Subscription for Chat API to call other Biotrackr APIs
resource chatApiApimSubscription 'Microsoft.ApiManagement/service/subscriptions@2024-06-01-preview' = {
  name: 'chat-api-internal'
  parent: apim
  properties: {
    displayName: 'Chat API Internal Subscription'
    scope: '${apim.id}/apis'
    state: 'active'
  }
}

// App Configuration: Chat API endpoint URL
resource chatApiEndpointSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: chatApiEndpointConfigName
  parent: appConfig
  properties: {
    value: '${apim.properties.gatewayUrl}/chat'
  }
}

// App Configuration: Chat Agent Model
resource chatAgentModelSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:ChatAgentModel'
  parent: appConfig
  properties: {
    value: chatAgentModel
  }
}

// App Configuration: Anthropic API Key (Key Vault reference)
resource anthropicApiKeySetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:AnthropicApiKey'
  parent: appConfig
  properties: {
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
    value: '{"uri":"${keyVault.properties.vaultUri}secrets/AnthropicApiKey"}'
  }
}

// App Configuration: Cosmos DB endpoint
resource cosmosEndpointSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:CosmosEndpoint'
  parent: appConfig
  properties: {
    value: cosmosDbAccount.properties.documentEndpoint
  }
}

// App Configuration: Azure AD settings for agent identity
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

resource azureAdClientIdSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = if (enableManagedIdentityAuth) {
  name: 'AzureAd:ClientId'
  parent: appConfig
  properties: {
    value: agentBlueprintClientId
  }
}

// App Configuration: Client credentials — use managed identity FIC assertion
resource azureAdClientCredSourceTypeSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = if (enableManagedIdentityAuth) {
  name: 'AzureAd:ClientCredentials:0:SourceType'
  parent: appConfig
  properties: {
    value: 'SignedAssertionFromManagedIdentity'
  }
}

resource azureAdClientCredManagedIdentitySetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = if (enableManagedIdentityAuth) {
  name: 'AzureAd:ClientCredentials:0:ManagedIdentityClientId'
  parent: appConfig
  properties: {
    value: uai.properties.clientId
  }
}

// App Configuration: APIM base URL for calling other Biotrackr APIs
resource apiBaseUrlSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:ApiBaseUrl'
  parent: appConfig
  properties: {
    value: apim.properties.gatewayUrl
  }
}

// App Configuration: APIM subscription key for calling other APIs
resource apiSubscriptionKeySetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:ApiSubscriptionKey'
  parent: appConfig
  properties: {
    value: chatApiApimSubscription.listSecrets().primaryKey
  }
}

// App Configuration: Chat agent system prompt (Key Vault reference)
resource chatSystemPromptSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:ChatSystemPrompt'
  parent: appConfig
  properties: {
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
    value: '{"uri":"${keyVault.properties.vaultUri}secrets/ChatSystemPrompt"}'
  }
}

// App Configuration: Agent identity ID for Cosmos DB access
resource agentIdentityIdSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = if (enableManagedIdentityAuth) {
  name: 'Biotrackr:AgentIdentityId'
  parent: appConfig
  properties: {
    value: agentIdentityId
  }
}
