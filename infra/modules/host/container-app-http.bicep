metadata name = 'HTTP Container App'
metadata description = 'Deploys a Container App with public ingress enabled'

@description('The name of the Container App')
param name string

@description('The location that the Container App will be deployed to')
param location string

@description('The tags that will be applied to the Container App')
param tags object

@description('The name of the Container App Environment that this Container App will be deployed to')
param containerAppEnvironmentName string

@description('The name of the Container Registry that this Container App will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that this Container App will use')
param uaiName string

@description('The Container Image that this Container App will use')
param imageName string

@description('The target port that this Container App uses')
param targetPort int

@description('The Environment variables for this Container App')
param envVariables array = []

@description('The Health Probes configured for this Container App')
param healthProbes array = []

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerAppEnvironmentName
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: containerRegistryName
}

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource httpContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    environmentId: containerAppEnv.id
    configuration: {
      activeRevisionsMode: 'Multiple'
      ingress: {
        external: true
        transport: 'http'
        targetPort: targetPort
        allowInsecure: false
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: uai.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: name
          image: imageName
          env: envVariables
          probes: healthProbes
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 2
        rules: [
          {
            name: 'http-rule-${name}'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
        ]
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${uai.id}': {}
    }
  }
}

@description('The FQDN of the deployed Container App')
output fqdn string = httpContainerApp.properties.configuration.ingress.fqdn
