@description('The resource name of the federated credential for the user-assigned identity')
param name string

@description('The name of the user-assigned identity that the federated credential will be applied to')
param uaiName string

@description('The list of audiences that can appear in the issued token')
param audiences array

@description('The URL of the issuer to be trusted')
param issuer string

@description('The subject identifier of the external identity')
param subject string

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource federatedCredential 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  name: name
  parent: uai
  properties: {
    audiences: audiences
    issuer: issuer
    subject: subject
  }
}
