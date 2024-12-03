targetScope = 'subscription'

@description('The name of the resource group for all GitHub deployment resources')
param rgName string

@description('The location where all resources will be deployed to')
param location string

@description('The tags that will be applied to all GitHub deployment resources')
param tags object

@description('The name of the user-assigned identity used for GitHub deployments')
param uaiName string

@description('The name of the GitHub Organization')
param githubOrganizationName string

@description('The name of the GitHub repository')
param githubRepositoryName string

@description('The name of the GitHub environment')
param githubEnvironmentName string

var defaultAudienceName = 'api://AzureADTokenExchange'
var githubIssuerUrl = 'https://token.actions.githubusercontent.com'
var subOwnerRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8e3af657-a8ff-443c-a75c-2fe8c4bcb635')

resource rg 'Microsoft.Resources/resourceGroups@2024-07-01' = {
  name: rgName
  location: location
  tags: tags
}

module githubUai '../modules/identity/user-assigned-identity.bicep' = {
  scope: rg
  name: 'github-uai'
  params: {
    name: uaiName
    location: rg.location
    tags: rg.tags
  }
}

module githubFederatedCredentialEnv '../modules/identity/federated-credentials.bicep' = {
  scope: rg
  name: 'github-fc-env'
  params: {
    name: '${githubOrganizationName}-${githubRepositoryName}-${githubEnvironmentName}'
    audiences: [
      defaultAudienceName
    ]
    issuer: githubIssuerUrl
    subject: 'repo:${githubOrganizationName}/${githubRepositoryName}:environment:${githubEnvironmentName}'
    uaiName: githubUai.outputs.uaiName
  }
  dependsOn: [
    githubUai
  ]
}

module githubFederatedCredentialPr '../modules/identity/federated-credentials.bicep' = {
  scope: rg
  name: 'github-fc-pr'
  params: {
    name: '${githubOrganizationName}-${githubRepositoryName}-pr'
    audiences: [
      defaultAudienceName
    ]
    issuer: githubIssuerUrl
    subject: 'repo:${githubOrganizationName}/${githubRepositoryName}:pull_request'
    uaiName: githubUai.outputs.uaiName
  }
  dependsOn: [
    githubFederatedCredentialEnv
  ]
}

resource githubUaiOwnerRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, rg.name, githubUai.name)
  properties: {
    principalId: githubUai.outputs.prinicpalId
    roleDefinitionId: subOwnerRoleId
  }
  dependsOn: [
    githubUai
  ]
}
