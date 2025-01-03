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

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
  name: appConfigName
}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' existing = {
  name: cosmosDbAccountName
}

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
