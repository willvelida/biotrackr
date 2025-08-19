@description('The name of the Sleep Api application')
param name string

@description('The image that the Sleep Api will use')
param imageName string

@description('The region where the Sleep Api will be deployed')
param location string

@description('The tags that will be applied to the Sleep Api')
param tags object

@description('The name of the Container App Environment that this Sleep Api will be deployed to')
param containerAppEnvironmentName string

@description('The name of the Container Registry that this Sleep Api will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Sleep Api will use')
param uaiName string

@description('The name of the App Config Store that the Sleep Api uses')
param appConfigName string

@description('The name of the Cosmos DB account that this Sleep Api uses')
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

var apiProductName = 'Sleep'
var sleepApiEndpointConfigName = 'Biotrackr:SleepApiUrl'

module sleepApi '../../modules/host/container-app-http.bicep' = {
  name: 'sleep-api'
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

resource sleepApimApi 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' = {
  name: 'sleep'
  parent: apim
  properties: {
    path: 'sleep'
    displayName: 'Sleep API'
    description: 'Endpoints for Biotrackr Sleep API'
    subscriptionRequired: true
    protocols: [
      'https'
    ]
    serviceUrl: 'https://${sleepApi.outputs.fqdn}'
  }
}

resource sleepApiGetAll 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'sleep-getall'
  parent: sleepApimApi
  properties: {
    displayName: 'GetAllSleeps'
    method: 'GET'
    urlTemplate: '/'
    description: 'Gets all sleep documents'
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

resource sleepApiGetByDate 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'sleep-getbydate'
  parent: sleepApimApi
  properties: {
    displayName: 'GetSleepByDate'
    method: 'get'
    urlTemplate: '/{date}'
    description: 'Gets a sleep document by date'
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

resource sleepApiGetByDateRange 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'sleep-getbydaterange'
  parent: sleepApimApi
  properties: {
    displayName: 'GetSleepsByDateRange'
    method: 'GET'
    urlTemplate: '/range/{startDate}/{endDate}'
    description: 'Gets paginated sleep documents within a date range'
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

resource sleepApiHealthCheck 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  name: 'sleep-healthcheck'
  parent: sleepApimApi
  properties: {
    displayName: 'LivenessCheck'
    method: 'GET'
    urlTemplate: '/healthz/liveness'
    description: 'Liveness Health Check Endpoint' 
  }
}

module sleepApiProduct '../../modules/apim/apim-products.bicep' = {
  name: 'sleep-product'
  params: {
    apiName: sleepApimApi.name
    apimName: apim.name
    productName: apiProductName
  }
}

resource sleepApiEndpointSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: sleepApiEndpointConfigName
  parent: appConfig
  properties: {
    value: '${apim.properties.gatewayUrl}/sleep'
  }
}
