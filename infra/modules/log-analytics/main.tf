resource "azurerm_log_analytics_workspace" "law" {
  name = var.law_name
  location = var.location
  tags = var.tags
  resource_group_name = var.resource_group_name
  sku = "PerGB2018"
  retention_in_days = 30
}