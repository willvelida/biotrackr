resource "azurerm_container_app_environment" "env" {
  name = var.env_name
    location = var.location
    tags = var.tags
    resource_group_name = var.resource_group_name
    log_analytics_workspace_id = var.log_analytics_workspace_id
    dapr_application_insights_connection_string = var.dapr_application_insights_connection_string
    infrastructure_subnet_id = var.infrastructure_subnet_id
    zone_redundancy_enabled = true
}