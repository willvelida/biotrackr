@description('The name given to the App Configuration')
param name string

@description('The region that the App Configuration instance will be deployed to')
param location string

@description('The tags that will be applied to the App Configuration instance')
param tags object

@description('The name of the User-Assigned Identity that will be granted RBAC roles over the App Configuration')
param uaiName string

var appConfigDataReaderRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions','516239f1-63e1-4d78-a4de-a74fb236a071')

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'free'
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${uai.id}': {}
    }
  }
}


resource appConfigDataReaderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, resourceGroup().id, appConfig.id)
  properties: {
    principalId: uai.properties.principalId
    roleDefinitionId: appConfigDataReaderRoleId
    principalType: 'ServicePrincipal'
  }
}
