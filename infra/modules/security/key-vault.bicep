metadata name = 'Azure Key Vault'
metadata description = 'This module deploys a Key Vault, and assigns the "Key Vault Secrets Officer" role to a provided user-assigned identity.'

@description('The name of the Key Vault')
@minLength(3)
@maxLength(24)
param name string

@description('The region that the Key Vault will be deployed to')
@allowed([
  'australiaeast'
])
param location string

@description('The tags that will be applied to the Key Vault resource')
param tags object

@description('The name of the user-assigned identity that will be granted RBAC roles over the Key Vault')
@minLength(3)
@maxLength(50)
param uaiName string

var keyVaultSecretsOfficerRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions','b86a8fe4-44ce-4948-aee5-eccb2c155cd7')

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'standard'
      family: 'A'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

resource keyVaultSecretsOfficerRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id ,resourceGroup().id, keyVault.id)
  properties: {
    principalId: uai.properties.principalId
    roleDefinitionId: keyVaultSecretsOfficerRoleDefinitionId
    principalType: 'ServicePrincipal'
  }
}

@description('The name of the deployed Key Vault')
output keyVaultName string = keyVault.name
