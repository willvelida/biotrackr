@description('The name of the Vitals Api application')
param name string

@description('The image that the Vitals Api will use')
param imageName string

@description('The region where the Vitals Api will be deployed')
param location string

@description('The tags that will be applied to the Vitals Api')
param tags object

@description('The name of the Container App Environment that this Vitals Api will be deployed to')
param containerAppEnvironmentName string

@description('The name of the Container Registry that this Vitals Api will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Vitals Api will use')
param uaiName string

@description('The name of the App Config Store that the Vitals Api uses')
param appConfigName string

@description('The name of the Cosmos DB account that this Vitals Api uses')
param cosmosDbAccountName string

@description('The name of the Application Insights instance')
param appInsightsName string

@description('The name of the API Management instance that this Api uses')
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

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' existing = {
  name: cosmosDbAccountName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource apim 'Microsoft.ApiManagement/service@2024-06-01-preview' existing = {
  name: apimName
}

var apiProductName = 'Vitals'
var vitalsApiEndpointConfigName = 'Biotrackr:VitalsApiUrl'

module vitalsApi '../../modules/host/container-app-http.bicep' = {
  name: 'vitals-api'
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
      {
        name: 'cosmosdbendpoint'
        value: cosmosDbAccount.properties.documentEndpoint
      }
      {
        name: 'applicationinsightsconnectionstring'
        value: appInsights.properties.ConnectionString
      }
    ]
  }
}

resource vitalsApimApi 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' = {
  name: 'vitals'
  parent: apim
  properties: {
    path: 'vitals'
    displayName: 'Vitals API'
    description: 'Endpoints for Biotrackr Vitals API'
    subscriptionRequired: true
    protocols: [
      'https'
    ]
    serviceUrl: 'https://${vitalsApi.outputs.fqdn}'
  }
}

module vitalsApiNamedValues '../../modules/apim/apim-named-values.bicep' = {
  name: 'vitals-api-named-values'
  params: {
    apimName: apim.name
    tenantId: tenantId
    jwtAudience: jwtAudience
    enableManagedIdentityAuth: enableManagedIdentityAuth
  }
}

resource vitalsApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2024-06-01-preview' = {
  name: 'policy'
  parent: vitalsApimApi
  properties: {
    value: enableManagedIdentityAuth 
      ? loadTextContent('policy-jwt-auth.xml')
      : loadTextContent('policy-subscription-key.xml')
    format: 'xml'
  }
  dependsOn: [
    vitalsApiNamedValues
  ]
}

resource vitalsApiGetAll 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'vitals-getall'
  parent: vitalsApimApi
  properties: {
    displayName: 'GetAllVitals'
    method: 'GET'
    urlTemplate: '/'
    description: 'Gets all vitals documents' 
    request: {
      queryParameters: [
        {
          name: 'pageNumber'
          description: 'The page number to retrieve (default: 1)'
          type: 'integer'
          required: false
          defaultValue: '1'
          values: [
            
          ]
        }
        {
          name: 'pageSize'
          description: 'The number of items per page (default: 20, max: 100)'
          type: 'integer'
          required: false
          defaultValue: '20'
          values: [
            
          ]
        }
      ]
    } 
  }
}

resource vitalsApiGetByDate 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'vitals-getbydate'
  parent: vitalsApimApi
  properties: {
    displayName: 'GetVitalsByDate'
    method: 'GET'
    urlTemplate: '/{date}'
    templateParameters: [
      {
        name: 'date'
        description: 'The date for the vitals summary in YYYY-MM-DD format'
        type: 'string'
        required: true
      }
    ]
  }
}

resource vitalsApiGetByDateRange 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'vitals-getbydaterange'
  parent: vitalsApimApi
  properties: {
    displayName: 'GetVitalsByDateRange'
    method: 'GET'
    urlTemplate: '/range/{startDate}/{endDate}'
    description: 'Gets paginated vitals documents within a date range'
    templateParameters: [
      {
        name: 'startDate'
        description: 'The start date for the range in YYYY-MM-DD format'
        type: 'string'
        required: true
      }
      {
        name: 'endDate'
        description: 'The end date for the range in YYYY-MM-DD format'
        type: 'string'
        required: true
      }
    ]
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

resource vitalsApiHealthCheck 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'vitals-healthcheck'
  parent: vitalsApimApi
  properties: {
    displayName: 'LivenessCheck'
    method: 'GET'
    urlTemplate: '/healthz/liveness' 
  }
}

module vitalsApiProduct '../../modules/apim/apim-products.bicep' = {
  name: 'vitals-product'
  params: {
    apiName: vitalsApimApi.name
    apimName: apim.name
    productName: apiProductName
  }
}

resource vitalsApiEndpointSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: vitalsApiEndpointConfigName
  parent: appConfig
  properties: {
    value: '${apim.properties.gatewayUrl}/vitals'
  }
}
