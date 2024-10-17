data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

data "azurerm_container_app_environment" "env" {
  name                = var.env_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

data "azurerm_user_assigned_identity" "msi" {
  name                = var.uai_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

data "azurerm_container_registry" "acr" {
  name                = var.acr_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

data "azurerm_key_vault" "kv" {
  name                = var.key_vault_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

data "azurerm_servicebus_namespace" "sb" {
  name = var.sb_namespace_name
    resource_group_name = data.azurerm_resource_group.rg.name
}

data "azurerm_app_configuration" "appconfig" {
  name = var.appconfig_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

data "azurerm_application_insights" "ai" {
  name = var.app_insights_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

resource "azurerm_container_app_job" "job" {
  name = var.aca_app_name
  location = data.azurerm_resource_group.rg.location
  resource_group_name = data.azurerm_resource_group.rg.name
  tags = var.tags
  container_app_environment_id = data.azurerm_container_app_environment.env.id
  template {
    container {
      name = var.aca_app_name
      image = var.image_name
      cpu = 0.25
      memory = "0.5Gi"
      env {
        name = "keyvaulturl"
        value = data.azurerm_key_vault.kv.vault_uri
      }
        env {
            name = "managedidentityclientid"
            value = data.azurerm_user_assigned_identity.msi.client_id
        }
        env {
            name = "servicebusendpoint"
            value = data.azurerm_servicebus_namespace.sb.endpoint
        }
        env {
          name = "azureappconfigendpoint"
          value = data.azurerm_app_configuration.appconfig.endpoint
        }
        env {
          name = "appinsightsconnectionstring"
          value = data.azurerm_application_insights.ai.connection_string
        }
    }
  }
  replica_timeout_in_seconds = 600
  replica_retry_limit = 3
  schedule_trigger_config {
    cron_expression = "0 5 * * *"
    parallelism = 1
    replica_completion_count = 1
  }
}