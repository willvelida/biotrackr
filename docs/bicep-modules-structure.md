# Bicep Modules Structure

Wherever possible, this project implements modules in Bicep. The purpose of this is to reuse modules for different types of deployments. In this project, this is to ease deployment between development and production environments. To learn more about modules in Bicep, check out the following [documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/modules?WT.mc_id=MVP_400037).

## Conventions

Bicep Modules have been divided into specific domain areas. For example:

```
- infra
  - modules
    - host
        - azure-app-config.bicep
    - identity
        - user-assigned-identity.bicep
```

Modules are then implemented in respective `main.bicep` templates like so:

```bicep
module uai '../modules/identity/user-assigned-identity.bicep' = {
  name: 'user-assigned-identity'
  params: {
    name: uaiName
    location: location
    tags: tags
  }
}

module appConfig '../modules/configuration/azure-app-config.bicep' = {
  name: 'app-config'
  params: {
    name: appConfigName
    location: location
    tags: tags
    uaiName: uai.outputs.uaiName
  }
}
```

Some modules will also contain [outputs](https://learn.microsoft.com/azure/azure-resource-manager/bicep/outputs?tabs=azure-powershell&WT.mc_id=MVP_400037) to be used in other modules. The intention is to limit these to just the name of the deployed resource, so that it can be used as an [existing resource](https://learn.microsoft.com/azure/azure-resource-manager/bicep/existing-resource?WT.mc_id=MVP_400037) in other modules.