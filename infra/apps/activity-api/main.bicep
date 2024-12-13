@description('The name of the Activity Api application')
param name string

@description('The image that the Activity Api will use')
param imageName string

@description('The region where the Activity Api will be deployed')
param location string

@description('The tags that will be applied to the Activity Api')
param tags object

@description('The name of the Container App Environment that this Activity Api will be deployed to')
param containerAppEnvironmentName string

@description('The name of the Container Registry that this Activity Api will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Activity Api will use')
param uaiName string

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

module activityApi '../../modules/host/container-app-http.bicep' = {
  name: 'activity-api'
  params: {
    name: name
    location: location
    tags: tags
    containerAppEnvironmentName: containerAppEnvironmentName
    containerRegistryName: containerRegistryName
    imageName: imageName
    uaiName: uai.name
  }
}
