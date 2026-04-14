metadata name = 'Azure Communication Services Email'
metadata description = 'This module deploys Azure Communication Services with Email capabilities, an Azure-managed domain, and assigns the Contributor role to a provided user-assigned identity.'

@description('The base name for the ACS resources')
@minLength(5)
@maxLength(50)
param baseName string

@description('The deployment environment')
@allowed([
  'dev'
  'prod'
])
param environment string

@description('Tags to apply to all resources')
param tags object

@description('Name of the user-assigned managed identity for RBAC')
@minLength(3)
@maxLength(128)
param uaiName string

@description('Name of the Log Analytics workspace for diagnostic settings')
param logAnalyticsName string

var emailServiceName = 'email-${baseName}-${environment}'
var communicationServiceName = 'acs-${baseName}-${environment}'
var contributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsName
}

resource emailService 'Microsoft.Communication/emailServices@2023-04-01' = {
  name: emailServiceName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: 'Australia'
  }
}

resource domain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  name: 'AzureManagedDomain'
  parent: emailService
  location: 'global'
  tags: tags
  properties: {
    domainManagement: 'AzureManagedDomain'
  }
}

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: communicationServiceName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: 'Australia'
    linkedDomains: [
      domain.id
    ]
  }
}

resource contributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, resourceGroup().id, communicationService.id)
  scope: communicationService
  properties: {
    principalId: uai.properties.principalId
    roleDefinitionId: contributorRoleId
    principalType: 'ServicePrincipal'
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: communicationService.name
  scope: communicationService
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'EmailSendMailOperational'
        enabled: true
      }
      {
        category: 'EmailStatusUpdateOperational'
        enabled: true
      }
      {
        category: 'EmailUserEngagementOperational'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

@description('The endpoint of the Communication Service')
output acsEndpoint string = communicationService.properties.hostName

@description('The sender address from the Azure-managed email domain')
output senderAddress string = 'DoNotReply@${domain.properties.fromSenderDomain}'
