metadata name = 'Azure Storage Account'
metadata description = 'Deploys an Azure Storage Account with a blob container and RBAC for managed identity'

@description('The name of the Storage Account')
@minLength(3)
@maxLength(24)
param storageAccountName string

@description('The location that the Storage Account will be deployed to')
param location string

@description('The tags that will be applied to the Storage Account')
param tags object

@description('The principal ID of the user-assigned identity to grant Storage Blob Data Contributor')
param uaiPrincipalId string

resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  name: 'default'
  parent: storageAccount
}

resource reportsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = {
  name: 'reports'
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

// Storage Blob Data Contributor role assignment for UAI
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource storageBlobDataContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, uaiPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    principalId: uaiPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalType: 'ServicePrincipal'
  }
}

@description('The blob primary endpoint URL of the deployed Storage Account')
output endpoint string = storageAccount.properties.primaryEndpoints.blob
