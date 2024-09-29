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

resource "azurerm_container_app_job" "job" {
  name                         = var.aca_app_name
  location                     = data.azurerm_resource_group.rg.location
  resource_group_name          = data.azurerm_resource_group.rg.name
  tags                         = var.tags
  container_app_environment_id = data.azurerm_container_app_environment.env.id
  template {
    container {
      name   = var.aca_app_name
      image  = var.image_name
      cpu    = 0.25
      memory = "0.5Gi"
    }
  }
  replica_timeout_in_seconds = 60
  replica_retry_limit        = 3
  schedule_trigger_config {
    cron_expression          = "*/15 * * * *"
    parallelism              = 1
    replica_completion_count = 1
  }

  registry {
    server   = data.azurerm_container_registry.acr.login_server
    identity = data.azurerm_user_assigned_identity.msi.id
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [data.azurerm_user_assigned_identity.msi.id]
  }
}