resource "azurerm_cosmosdb_account" "acc" {
  name = var.account_name
    location = var.location
    resource_group_name = var.resource_group_name
    tags = var.tags
    offer_type = "Standard"
    kind = "GlobalDocumentDB"
    automatic_failover_enabled = false
    free_tier_enabled = true
    geo_location {
        location = var.location
        failover_priority = 0
    }
    consistency_policy {
        consistency_level = "Session"
    }
}

resource "azurerm_cosmosdb_sql_database" "db" {
  name = var.database_name
  resource_group_name = var.resource_group_name
  account_name = azurerm_cosmosdb_account.acc.name
  throughput = var.throughput
}

resource "azurerm_cosmosdb_sql_container" "container" {
  name = var.container_name
  resource_group_name = var.resource_group_name
  account_name = azurerm_cosmosdb_account.acc.name
  database_name = azurerm_cosmosdb_sql_database.db.name
  partition_key_paths = [ "/documentType"]
  partition_key_version = 1
  throughput = var.throughput
  indexing_policy {
    indexing_mode = "consistent"
    included_path {
      path = "/*"
    }
  }
}