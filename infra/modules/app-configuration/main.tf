resource "azurerm_app_configuration" "appconfig" {
  name = var.app_configuration_name
  location = var.location
  resource_group_name = var.resource_group_name
  tags = var.tags
  sku = "free"
  identity {
    type = "UserAssigned"
    identity_ids = [ var.identity_id ]
  }
}