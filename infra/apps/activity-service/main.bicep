@description('The name of the Activity Service application')
param name string

@description('The image that the Activity Service will use')
param imageName string

@description('The region where the Activity Service will be deployed')
param location string

@description('The tags that will be applied to the Activity Service')
param tags object

@description('The name of the Container App Environment that this Activity Service Application will use')
param containerAppEnvironmentName string

@description('The name of the Container Registry this Activity Service will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Activity Service will use')
param uaiName string

@description('The name of the Key Vault that will be used by the Activity Service')
param keyVaultName string

@description('The name of the App Insights workspace that the Activity Service sends logs to')
param appInsightsName string

@description('The name of the App Configuration Store that the Activity Service will use')
param appConfigName string

@description('The name of the Cosmos DB Account that this Activity Service will use')
param cosmosDbAccountName string

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
  name: appConfigName
}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' existing = {
  name: cosmosDbAccountName
}

module activityService '../../modules/host/container-app-jobs.bicep' = {
  name: 'activity-svc'
  params: {
    name: name
    location: location
    tags: tags
    containerAppEnvironmentName: containerAppEnvironmentName
    containerRegistryName: containerRegistryName
    cronExpression: '15 5 * * *'
    envVariables: [
            {
              name: 'keyvaulturl'
              value: keyVault.properties.vaultUri
            }
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
              name: 'cosmosdbendpoint'
              value: cosmosDbAccount.properties.documentEndpoint
            }
          ]
    imageName: imageName
    uaiName: uaiName
  }
}
