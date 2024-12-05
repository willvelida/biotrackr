metadata name = 'User Assigned Identity'
metadata description = 'This module deploys a User Assigned Identity.'

@description('The name of the user-assigned identity')
@minLength(3)
@maxLength(128)
param name string

@description('The region that the user-assigned identity will be deployed to')
@allowed([
  'australiaeast'
])
param location string

@description('The tags that will be applied to the user-assigned identity')
param tags object

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
  tags: tags
}

@description('The name of the deployed user-assigned identity')
output uaiName string = userAssignedIdentity.name

@description('The Principal Id of the deployed user-assigned identity')
output prinicpalId string = userAssignedIdentity.properties.principalId
