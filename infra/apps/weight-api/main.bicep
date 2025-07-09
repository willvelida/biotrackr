@description('The name of the Weight Api application')
param name string

@description('The image that the Weight Api will use')
param imageName string

@description('The region where the Weight Api will be deployed')
param location string

@description('The tags that will be applied to the Weight Api')
param tags object

@description('The name of the Container App Environment that this Weight Api will be deployed to')
param containerAppEnvironmentName string

@description('The name of the Container Registry that this Weight Api will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Weight Api will use')
param uaiName string

@description('The name of the App Config Store that the Weight Api uses')
param appConfigName string

@description('The name of the Cosmos DB account that this Weight Api uses')
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

var apiProductName = 'Weight'

module weightApi '../../modules/host/container-app-http.bicep' = {
  name: 'weight-api'
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

resource weightApimApi 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' = {
  name: 'weight'
  parent: apim
  properties: {
    path: 'weight'
    displayName: 'Weight API'
    description: 'Endpoints for Biotrackr Weight API'
    subscriptionRequired: true
    protocols: [
      'https'
    ]
    serviceUrl: 'https://${weightApi.outputs.fqdn}'
  }
}

resource weightApiGetAll 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'weight-getall'
  parent: weightApimApi
  properties: {
    displayName: 'GetAllWeights'
    method: 'GET'
    urlTemplate: '/'
    description: 'Gets all weight documents' 
  }
}

resource weightApiGetByDate 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'weight-getbydate'
  parent: weightApimApi
  properties: {
    displayName: 'GetWeightByDate'
    method: 'GET'
    urlTemplate: '/{date}'
    templateParameters: [
      {
        name: 'date'
        description: 'The date for the weight summary in YYYY-MM-DD format'
        type: 'string'
        required: true
      }
    ]
  }
}

resource weightApiHealthCheck 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'weight-healthcheck'
  parent: weightApimApi
  properties: {
    displayName: 'LivenessCheck'
    method: 'GET'
    urlTemplate: '/healthz/liveness' 
  }
}

module weightApiProduct '../../modules/apim/apim-products.bicep' = {
  name: 'weight-product'
  params: {
    apiName: weightApimApi.name
    apimName: apim.name
    productName: apiProductName
  }
}
