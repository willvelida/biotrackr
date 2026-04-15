@description('The name of the Reporting Service Yearly application')
param name string

@description('The image that the Reporting Service Yearly will use')
param imageName string

@description('The region where the Reporting Service Yearly will be deployed')
param location string

@description('The tags that will be applied to the Reporting Service Yearly')
param tags object

@description('The name of the Container App Environment that this Reporting Service Yearly will use')
param containerAppEnvironmentName string

@description('The name of the Container Registry this Reporting Service Yearly will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Reporting Service Yearly will use')
param uaiName string

@description('The name of the Key Vault that will be used by the Reporting Service Yearly')
param keyVaultName string

@description('The name of the App Insights workspace that the Reporting Service Yearly sends logs to')
param appInsightsName string

@description('The name of the App Configuration Store that the Reporting Service Yearly will use')
param appConfigName string

@description('Azure AD tenant ID for agent identity authentication')
param tenantId string

@description('The application (client) ID of the Reporting agent identity blueprint')
param agentBlueprintClientId string

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

module reportingServiceYearly '../../modules/host/container-app-jobs.bicep' = {
  name: 'reporting-svc-yearly'
  params: {
    name: name
    location: location
    tags: tags
    containerAppEnvironmentName: containerAppEnvironmentName
    containerRegistryName: containerRegistryName
    cronExpression: '0 6 27 12 *'
    replicaTimeout: 1800
    replicaRetryLimit: 1
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
        name: 'summarycadence'
        value: 'yearly'
      }
      {
        name: 'AzureAd__ClientId'
        value: !empty(agentBlueprintClientId) ? agentBlueprintClientId : ''
      }
      {
        name: 'AzureAd__TenantId'
        value: tenantId
      }
    ]
    imageName: imageName
    uaiName: uaiName
  }
}
