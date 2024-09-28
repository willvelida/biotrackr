resource "azurerm_application_insights" "ai" {
  name = var.app_insights_name
  location = var.location
  tags = var.tags
  resource_group_name = var.resource_group_name
  workspace_id = var.workspace_id
  application_type = "web"
}