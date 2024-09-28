resource "azurerm_virtual_network" "vnet" {
  name = var.vnet_name
  location = var.location
  tags = var.tags
    resource_group_name = var.resource_group_name
    address_space = var.address_space
}