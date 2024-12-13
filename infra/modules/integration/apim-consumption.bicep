metadata name = 'API Management'
metadata description = 'Deploys a API Management instance using the Consumption SKU'

@description('The name of the API Management instance')
param name string

@description('The region where the API Management instance will be deployed')
param location string

@description('The tags that will be applied to the API Management Instance')
param tags object

@description('The email address of the owner of this API Management instance')
param emailAddress string

@description('The name of the owner of this API Management instance')
param publisherName string

@description('The name of the user-assigned identity that this API Management instance will use')
param uaiName string

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource apiManagement 'Microsoft.ApiManagement/service@2024-05-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Consumption'
    capacity: 0
  }
  properties: {
    publisherEmail: emailAddress
    publisherName: publisherName
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${uai.id}': {}
    }
  }
}

@description('The name of the deployed API Management Instance')
output name string = apiManagement.name
