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

@description('The database that this Activity Service will use to create the Activity Container')
param databaseName string

var activityContainerName = 'activity'
var containerSettingName = 'Biotrackr:ActivityContainer'

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerAppEnvironmentName
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: containerRegistryName
}

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

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' existing = {
  name: databaseName
  parent: cosmosDbAccount
}

resource activityService 'Microsoft.App/jobs@2024-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    environmentId: containerAppEnv.id
    configuration: {
      replicaTimeout: 600
      replicaRetryLimit: 3
      triggerType: 'Schedule'
      scheduleTriggerConfig: {
        cronExpression: '15 5 * * *'
        parallelism: 1
        replicaCompletionCount: 1
      }
      registries: [
        { 
          server: acr.properties.loginServer
          identity: uai.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: name
          image: imageName
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
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
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${uai.id}': {}
    }
  }
}

resource activityContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: activityContainerName
  parent: database
  properties: {
    resource: {
      id: activityContainerName
      partitionKey: {
        paths: [
          '/date'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

resource activityContainerSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2024-05-01' = {
  name: containerSettingName
  parent: appConfig
  properties: {
    value: activityContainerName
  }
}
