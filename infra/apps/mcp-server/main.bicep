@description('The name of the MCP Server application')
param name string

@description('The image that the MCP Server will use')
param imageName string

@description('The region where the MCP Server will be deployed')
param location string

@description('The tags that will be applied to the MCP Server')
param tags object

@description('The name of the Container App Environment that this MCP Server will be deployed to')
param containerAppEnvironmentName string

@description('The name of the Container Registry that this MCP Server will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the MCP Server will use')
param uaiName string

@description('The name of the App Config Store that the MCP Server uses')
param appConfigName string

@description('The name of the API Management instance that this MCP Server uses')
param apimName string

@description('Enable JWT validation for managed identity authentication')
param enableManagedIdentityAuth bool = true

@description('Azure AD tenant ID for JWT issuer validation')
param tenantId string

@description('JWT audience to validate (uses default Azure Management API audience)')
param jwtAudience string = environment().authentication.audiences[0]

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
  name: appConfigName
}

resource apim 'Microsoft.ApiManagement/service@2024-06-01-preview' existing = {
  name: apimName
}

var apiProductName = 'MCP-Server'
var mcpServerEndpointConfigName = 'Biotrackr:McpServerUrl'

module mcpServer '../../modules/host/container-app-http.bicep' = {
  name: 'mcp-server'
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
    ]
  }
}

resource mcpApimApi 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' = {
  name: 'mcp-server'
  parent: apim
  properties: {
    path: 'mcp'
    displayName: 'MCP Server'
    description: 'Model Context Protocol Server for Biotrackr'
    subscriptionRequired: true
    protocols: [
      'https'
    ]
    serviceUrl: 'https://${mcpServer.outputs.fqdn}'
  }
}

module mcpApiNamedValues '../../modules/apim/apim-named-values.bicep' = {
  name: 'mcp-server-named-values'
  params: {
    apimName: apim.name
    tenantId: tenantId
    jwtAudience: jwtAudience
    enableManagedIdentityAuth: enableManagedIdentityAuth
  }
}

resource mcpApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2024-06-01-preview' = {
  name: 'policy'
  parent: mcpApimApi
  properties: {
    value: enableManagedIdentityAuth
      ? loadTextContent('policy-jwt-auth.xml')
      : loadTextContent('policy-subscription-key.xml')
    format: 'xml'
  }
  dependsOn: [
    mcpApiNamedValues
  ]
}

resource mcpApiPostMessage 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'mcp-post-message'
  parent: mcpApimApi
  properties: {
    displayName: 'PostMcpMessage'
    method: 'POST'
    urlTemplate: '/'
    description: 'MCP Streamable HTTP transport endpoint. Sends JSON-RPC messages and receives SSE responses.'
  }
}

resource mcpApiGetSse 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'mcp-get-sse'
  parent: mcpApimApi
  properties: {
    displayName: 'GetMcpSse'
    method: 'GET'
    urlTemplate: '/'
    description: 'MCP Streamable HTTP SSE endpoint for server-initiated notifications.'
  }
}

resource mcpApiDeleteSession 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'mcp-delete-session'
  parent: mcpApimApi
  properties: {
    displayName: 'DeleteMcpSession'
    method: 'DELETE'
    urlTemplate: '/'
    description: 'MCP Streamable HTTP session termination endpoint.'
  }
}

resource mcpApiHealthCheck 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'mcp-healthcheck'
  parent: mcpApimApi
  properties: {
    displayName: 'HealthCheck'
    method: 'GET'
    urlTemplate: '/api/healthz'
    description: 'Health Check Endpoint'
  }
}

module mcpApiProduct '../../modules/apim/apim-products.bicep' = {
  name: 'mcp-server-product'
  params: {
    apimName: apim.name
    productName: apiProductName
    apiName: mcpApimApi.name
  }
}

resource mcpServerEndpointSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: mcpServerEndpointConfigName
  parent: appConfig
  properties: {
    value: '${apim.properties.gatewayUrl}/mcp'
  }
}
