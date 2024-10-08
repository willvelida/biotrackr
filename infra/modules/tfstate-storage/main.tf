resource "azurerm_storage_account" "account" {
  name = var.storage_account_name
  location = var.location
  resource_group_name = var.resource_group_name
  account_tier = var.account_tier
  account_replication_type = var.account_replication_type
  tags = var.tags
}

resource "azurerm_storage_container" "container" {
  for_each = toset(var.container_names)
  name = each.value
  storage_account_name = azurerm_storage_account.account.name
  container_access_type = "private"
}

