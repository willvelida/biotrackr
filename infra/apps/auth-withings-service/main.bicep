@description('The name of the Auth Withings Service application')
param name string

@description('The image that the Auth Withings Service will use')
param imageName string

@description('The region where the Auth Withings Service application will be deployed')
param location string

@description('The tags that will be applied to the Auth Withings Service application')
param tags object

@description('The name of the Container App Environment that this Auth Withings Service Application will use')
param containerAppEnvironmentName string

@description('The name of the Container Registry this Auth Withings Service will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Auth Withings Service will use')
param uaiName string

@description('The name of the Key Vault that will be used by the Auth Withings Service')
param keyVaultName string

@description('The name of the App Insights workspace that the Auth Withings Service sends logs to')
param appInsightsName string

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

module authWithingsService '../../modules/host/container-app-jobs.bicep' = {
  name: 'auth-withings-svc'
  params: {
    name: name
    location: location
    tags: tags
    containerAppEnvironmentName: containerAppEnvironmentName
    containerRegistryName: containerRegistryName
    cronExpression: '0 */2 * * *'
    envVariables: [
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
      {
        name: 'provider'
        value: 'withings'
      }
    ]
    imageName: imageName
    uaiName: uaiName
  }
}
