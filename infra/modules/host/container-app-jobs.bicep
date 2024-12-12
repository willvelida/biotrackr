metadata name = 'Container App Job'
metadata description = 'This module deploys a Container App Job, within a provided Container App Environment'

@description('The name of the Container App Job')
param name string

@description('The location that the Container App Job will be deployed to')
param location string

@description('The tags that will be applied to the Container App Job')
param tags object

@description('The name of the Container App Environment that this Container App Job will use')
param containerAppEnvironmentName string

@description('The name of the Container Registry this Container App Job will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the Container App Job will use')
param uaiName string

@description('The CRON expression used to trigger this job')
param cronExpression string

@description('The Container Image that this Container App Job will use')
param imageName string

@description('The Environment Variables for this Container App Job')
param envVariables array

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerAppEnvironmentName
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: containerRegistryName
}

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource containerAppJob 'Microsoft.App/jobs@2024-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    environmentId: containerAppEnv.id
    configuration: {
      replicaTimeout: 600
      triggerType: 'Schedule'
      scheduleTriggerConfig: {
        cronExpression: cronExpression
        parallelism: 1
        replicaCompletionCount: 1
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
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: envVariables
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${uai.id}': {}
    }
  }
}
