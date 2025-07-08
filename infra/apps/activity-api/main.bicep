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

@description('The name of the API Management instance that this Api uses')
param apimName string

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

var apiProductName = 'Activity'

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
    ]
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

resource activityApimApi 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' = {
  name: 'activity'
  parent: apim
  properties: {
    path: 'activity'
    displayName: 'Activity API'
    description: 'Endpoints for Biotrack Activity API'
    subscriptionRequired: false
    protocols: [
      'https'
    ]
    serviceUrl: 'https://${activityApi.outputs.fqdn}'
  }
}

resource activityApiGetAll 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'activity-getall'
  parent: activityApimApi
  properties: {
    displayName: 'GetAllActivities'
    method: 'GET'
    urlTemplate: '/activity'
    description: 'Get all Activity Summaries' 
  }
}

resource activityApiGetByDate 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'activity-getbydate'
  parent: activityApimApi
  properties: {
    displayName: 'GetActivityByDate'
    method: 'GET'
    urlTemplate: '/activity/{date}'
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
