resource "azurerm_servicebus_namespace" "sb" {
  name = var.sb_name
  location = var.location
  resource_group_name = var.rg_name
  sku = var.sb_sku
  tags = var.tags
  identity {
    type = "UserAssigned"
    identity_ids = [ var.identity_id ]
  }
}