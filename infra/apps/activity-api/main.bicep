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

@description('The name of the API Management instance that this Activity uses')
param apiManagementName string

var activityProductName = 'Activity'

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
  name: appConfigName
}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' existing = {
  name: cosmosDbAccountName
}

resource apiManagement 'Microsoft.ApiManagement/service@2024-05-01' existing = {
  name: apiManagementName
}

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

resource actitivyApimProduct 'Microsoft.ApiManagement/service/products@2023-09-01-preview' = {
  name: activityProductName
  parent: apiManagement
  properties: {
    displayName: activityProductName
    subscriptionRequired: true
  }
}
