@description('The name of the Auth Service application')
param name string

@description('The image that the Auth Service will use')
param imageName string

@description('The region where the Auth Service application will be deployed')
param location string

@description('The tags that will be applied to the Auth Service application')
param tags object

@description('The name of the Container App Environment that this Auth Service Application will use')
param containerAppEnvironmentName string

@description('The name of the Container Registry this Auth Service will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Auth Service will use')
param uaiName string

@description('The name of the Key Vault that will be used by the Auth Service')
param keyVaultName string

@description('The name of the App Insights workspace that the Auth Service sends logs to')
param appInsightsName string

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

resource authService 'Microsoft.App/jobs@2024-03-01' = {
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
        cronExpression: '0 */6 * * *'
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
              name: 'managedidentityclientid'
              value: uai.properties.clientId
            }
            {
              name: 'applicationinsightsconnectionstring'
              value: appInsights.properties.ConnectionString
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
