@description('The name of the UI application')
param name string

@description('The image that the UI will use')
param imageName string

@description('The region where the UI will be deployed')
param location string

@description('The tags that will be applied to the UI')
param tags object

@description('The name of the Container App Environment that this UI will be deployed to')
param containerAppEnvironmentName string

@description('The name of the Container Registry that this UI will pull images from')
param containerRegistryName string

@description('The name of the user-assigned identity that the UI will use')
param uaiName string

@description('The name of the App Config Store that the UI uses')
param appConfigName string

@description('The name of the API Management instance that this UI uses')
param apimName string

@description('The Entra ID application (client) ID for Easy Auth')
param easyAuthClientId string

@description('The Entra ID tenant ID for Easy Auth')
param easyAuthTenantId string

@description('The custom domain name for the UI (apex domain, e.g. biotrackr.dev). Leave empty to skip custom domain configuration.')
param customDomainName string = ''

@description('The www subdomain for the UI (e.g. www.biotrackr.dev). Leave empty to skip.')
param customDomainWww string = ''

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
  name: appConfigName
}

resource apim 'Microsoft.ApiManagement/service@2024-06-01-preview' existing = {
  name: apimName
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerAppEnvironmentName
}

var uiApiEndpointConfigName = 'biotrackruiapiendpoint'
var uiApiSubscriptionKeyConfigName = 'biotrackruiapisubscriptionkey'

var uiHealthProbes = [
  {
    type: 'Liveness'
    httpGet: {
      port: 8080
      path: '/healthz'
    }
    initialDelaySeconds: 15
    periodSeconds: 30
    failureThreshold: 3
    timeoutSeconds: 5
  }
]

var uiEnvVariables = [
  {
    name: 'azureappconfigendpoint'
    value: appConfig.properties.endpoint
  }
  {
    name: 'managedidentityclientid'
    value: uai.properties.clientId
  }
]

// Phase 1: Deploy Container App with custom domains registered (Disabled binding, no certificate)
module ui '../../modules/host/container-app-http.bicep' = {
  name: 'ui'
  params: {
    name: name
    location: location
    tags: tags
    containerAppEnvironmentName: containerAppEnvironmentName
    containerRegistryName: containerRegistryName
    imageName: imageName
    uaiName: uai.name
    targetPort: 8080
    healthProbes: uiHealthProbes
    envVariables: uiEnvVariables
    customDomains: concat(
      !empty(customDomainName) ? [
        {
          name: customDomainName
          bindingType: 'Disabled'
        }
      ] : [],
      !empty(customDomainWww) ? [
        {
          name: customDomainWww
          bindingType: 'Disabled'
        }
      ] : []
    )
  }
}

// Phase 2: Create managed certificates (requires hostnames to be registered on the Container App)
resource managedCertApex 'Microsoft.App/managedEnvironments/managedCertificates@2024-03-01' = if (!empty(customDomainName)) {
  name: 'cert-${replace(customDomainName, '.', '-')}'
  parent: containerAppEnv
  location: location
  tags: tags
  properties: {
    subjectName: customDomainName
    domainControlValidation: 'HTTP'
  }
  dependsOn: [
    ui
  ]
}

resource managedCertWww 'Microsoft.App/managedEnvironments/managedCertificates@2024-03-01' = if (!empty(customDomainWww)) {
  name: 'cert-${replace(customDomainWww, '.', '-')}'
  parent: containerAppEnv
  location: location
  tags: tags
  properties: {
    subjectName: customDomainWww
    domainControlValidation: 'CNAME'
  }
  dependsOn: [
    ui
  ]
}

// Phase 3: Re-deploy Container App with certificate bindings (SniEnabled)
module uiCertBinding '../../modules/host/container-app-http.bicep' = if (!empty(customDomainName) || !empty(customDomainWww)) {
  name: 'ui-cert-binding'
  params: {
    name: name
    location: location
    tags: tags
    containerAppEnvironmentName: containerAppEnvironmentName
    containerRegistryName: containerRegistryName
    imageName: imageName
    uaiName: uai.name
    targetPort: 8080
    healthProbes: uiHealthProbes
    envVariables: uiEnvVariables
    customDomains: concat(
      !empty(customDomainName) ? [
        {
          name: customDomainName
          certificateId: managedCertApex.id
          bindingType: 'SniEnabled'
        }
      ] : [],
      !empty(customDomainWww) ? [
        {
          name: customDomainWww
          certificateId: managedCertWww.id
          bindingType: 'SniEnabled'
        }
      ] : []
    )
  }
  dependsOn: [
    ui
  ]
}

resource uiApimSubscription 'Microsoft.ApiManagement/service/subscriptions@2024-06-01-preview' = {
  name: 'ui-internal'
  parent: apim
  properties: {
    displayName: 'UI Internal Subscription'
    scope: '${apim.id}/apis'
    state: 'active'
  }
}

resource uiContainerApp 'Microsoft.App/containerApps@2024-03-01' existing = {
  name: name
  dependsOn: [
    ui
    uiCertBinding
  ]
}

resource easyAuth 'Microsoft.App/containerApps/authConfigs@2024-03-01' = {
  name: 'current'
  parent: uiContainerApp
  properties: {
    platform: {
      enabled: true
    }
    globalValidation: {
      unauthenticatedClientAction: 'AllowAnonymous'
      redirectToProvider: 'azureactivedirectory'
    }
    identityProviders: {
      azureActiveDirectory: {
        registration: {
          clientId: easyAuthClientId
          openIdIssuer: 'https://login.microsoftonline.com/${easyAuthTenantId}/v2.0'
        }
        validation: {
          allowedAudiences: [
            'api://${easyAuthClientId}'
            easyAuthClientId
          ]
        }
      }
    }
    login: {
      preserveUrlFragmentsForLogins: true
    }
  }
}

resource uiApiEndpointSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: uiApiEndpointConfigName
  parent: appConfig
  properties: {
    value: apim.properties.gatewayUrl
  }
}

resource uiApiSubscriptionKeySetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2025-02-01-preview' = {
  name: uiApiSubscriptionKeyConfigName
  parent: appConfig
  properties: {
    value: uiApimSubscription.listSecrets().primaryKey
  }
}
