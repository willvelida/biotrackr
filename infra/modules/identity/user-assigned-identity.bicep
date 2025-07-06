metadata name = 'User Assigned Identity'
metadata description = 'This module deploys a User Assigned Identity.'

@description('The base name given to all resources')
@minLength(5)
@maxLength(50)
param baseName string

@description('The environment that the User-Assigned Identity will be deployed to')
param environment string

@description('The region that the user-assigned identity will be deployed to')
@allowed([
  'australiaeast'
])
param location string

@description('The tags that will be applied to the user-assigned identity')
param tags object

var uaiName = 'uai-${baseName}-${environment}'

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: uaiName
  location: location
  tags: tags
}

@description('The name of the deployed user-assigned identity')
output uaiName string = userAssignedIdentity.name

@description('The Principal Id of the deployed user-assigned identity')
output prinicpalId string = userAssignedIdentity.properties.principalId
