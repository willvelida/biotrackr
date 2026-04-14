@description('The name of the Reporting Service Weekly application')
param name string

@description('The image that the Reporting Service Weekly will use')
param imageName string

@description('The region where the Reporting Service Weekly will be deployed')
param location string

@description('The tags that will be applied to the Reporting Service Weekly')
param tags object

@description('The name of the Container App Environment that this Reporting Service Weekly will use')
param containerAppEnvironmentName string

@description('The name of the Container Registry this Reporting Service Weekly will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Reporting Service Weekly will use')
param uaiName string

@description('The name of the Key Vault that will be used by the Reporting Service Weekly')
param keyVaultName string

@description('The name of the App Insights workspace that the Reporting Service Weekly sends logs to')
param appInsightsName string

@description('The name of the App Configuration Store that the Reporting Service Weekly will use')
param appConfigName string

@description('The name of the API Management instance')
param apimName string

@description('The Reporting.Svc agent identity ID for inter-service auth')
param reportingSvcAgentIdentityId string

@description('The ACS Communication Service name')
param communicationServiceName string

@description('The ACS Email Service name')
param emailServiceName string

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

resource apim 'Microsoft.ApiManagement/service@2024-06-01-preview' existing = {
  name: apimName
}

module reportingServiceWeekly '../../modules/host/container-app-jobs.bicep' = {
  name: 'reporting-svc-weekly'
  params: {
    name: name
    location: location
    tags: tags
    containerAppEnvironmentName: containerAppEnvironmentName
    containerRegistryName: containerRegistryName
    cronExpression: '0 6 * * 1'
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
        value: 'weekly'
      }
    ]
    imageName: imageName
    uaiName: uaiName
  }
}

resource reportingSvcApimSubscription 'Microsoft.ApiManagement/service/subscriptions@2024-06-01-preview' = {
  name: 'reporting-svc-internal'
  parent: apim
  properties: {
    displayName: 'Reporting Svc Internal Subscription'
    scope: '${apim.id}/apis'
    state: 'active'
  }
}

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' existing = {
  name: communicationServiceName
}

resource emailService 'Microsoft.Communication/emailServices@2023-04-01' existing = {
  name: emailServiceName
}

resource emailDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' existing = {
  name: 'AzureManagedDomain'
  parent: emailService
}

// App Configuration: Reporting.Svc APIM subscription key
resource reportingSvcApiSubscriptionKeySetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:ReportingSvcApiSubscriptionKey'
  parent: appConfig
  properties: {
    value: reportingSvcApimSubscription.listSecrets().primaryKey
  }
}

// App Configuration: Reporting.Svc agent identity ID
resource reportingSvcAgentIdentityIdSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:ReportingSvcAgentIdentityId'
  parent: appConfig
  properties: {
    value: reportingSvcAgentIdentityId
  }
}

// App Configuration: ACS endpoint for email sending
resource acsEndpointSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:AcsEndpoint'
  parent: appConfig
  properties: {
    value: 'https://${communicationService.properties.hostName}'
  }
}

// App Configuration: ACS email sender address (Azure-managed domain)
resource emailSenderAddressSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:EmailSenderAddress'
  parent: appConfig
  properties: {
    value: 'DoNotReply@${emailDomain.properties.fromSenderDomain}'
  }
}

// App Configuration: Email recipient address
resource emailRecipientAddressSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: 'Biotrackr:EmailRecipientAddress'
  parent: appConfig
  properties: {
    value: 'willvelida@hotmail.co.uk'
  }
}
