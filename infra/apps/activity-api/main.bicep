@description('The name of the Activity Api application')
param name string

@description('The image that the Activity Api will use')
param imageName string

@description('The region where the Activity Api will be deployed')
param location string

@description('The tags that will be applied to the Activity Api')
param tags object

@description('The name of the Container App Environment that this Activity Api will be deployed to')
param containerAppEnvironmentName string

@description('The name of the Container Registry that this Activity Api will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Activity Api will use')
param uaiName string

@description('The name of the App Config Store that the Activity Api uses')
param appConfigName string

@description('The name of the Cosmos DB account that this Activity Api uses')
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

var apiProductName = 'Activity'
var activityApiEndpointConfigName = 'Biotrackr:ActivityApiUrl'

module activityApi '../../modules/host/container-app-http.bicep' = {
  name: 'activity-api'
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

resource activityApimApi 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' = {
  name: 'activity'
  parent: apim
  properties: {
    path: 'activity'
    displayName: 'Activity API'
    description: 'Endpoints for Biotrackr Activity API'
    subscriptionRequired: true
    protocols: [
      'https'
    ]
    serviceUrl: 'https://${activityApi.outputs.fqdn}'
  }
}

module activityApiNamedValues '../../modules/apim/apim-named-values.bicep' = {
  name: 'activity-api-named-values'
  params: {
    apimName: apim.name
    tenantId: tenantId
    jwtAudience: jwtAudience
    enableManagedIdentityAuth: enableManagedIdentityAuth
  }
}

resource activityApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2024-06-01-preview' = {
  name: 'policy'
  parent: activityApimApi
  properties: {
    value: enableManagedIdentityAuth 
      ? loadTextContent('policy-jwt-auth.xml')
      : loadTextContent('policy-subscription-key.xml')
    format: 'xml'
  }
  dependsOn: [
    activityApiNamedValues
  ]
}

resource activityApiGetAll 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'activity-getall'
  parent: activityApimApi
  properties: {
    displayName: 'GetAllActivities'
    method: 'GET'
    urlTemplate: '/'
    description: 'Get all Activity Summaries'
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

resource activityApiGetByDate 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'activity-getbydate'
  parent: activityApimApi
  properties: {
    displayName: 'GetActivityByDate'
    method: 'GET'
    urlTemplate: '/{date}'
    description: 'Get a specific activity summary via this endpoint by providing the date in the following format (YYYY-MM-DD)'
    templateParameters: [
      {
        name: 'date'
        description: 'The date for the activity summary in YYYY-MM-DD format'
        type: 'string'
        required: true
      }
    ]
  }
}

resource activityApiGetByDateRange 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'activity-getbydaterange'
  parent: activityApimApi
  properties: {
    displayName: 'GetActivitiesByDateRange'
    method: 'GET'
    urlTemplate: '/range/{startDate}/{endDate}'
    description: 'Gets paginated activity documents within a date range'
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

resource activityApiHealthCheck 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'activity-healthcheck'
  parent: activityApimApi
  properties: {
    displayName: 'LivenessCheck'
    method: 'GET'
    urlTemplate: '/healthz/liveness'
    description: 'Liveness Health Check Endpoint'
  }
}

module activityApiProduct '../../modules/apim/apim-products.bicep' = {
  name: 'activity-product'
  params: {
    apimName: apim.name
    productName: apiProductName
    apiName: activityApimApi.name
  }
}


resource activityApiEndpointSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: activityApiEndpointConfigName
  parent: appConfig
  properties: {
    value: '${apim.properties.gatewayUrl}/activity'
  }
}
